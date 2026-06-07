using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/lsp")]
public class LspController : ControllerBase
{
    private readonly ILogger<LspController> _logger;

    public LspController(ILogger<LspController> logger)
    {
        _logger = logger;
    }

    [HttpGet("csharp")]
    [Authorize(Roles = "Student")]
    public async Task CSharp()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var ct = HttpContext.RequestAborted;
        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

        var sessionId = Guid.NewGuid().ToString("N");
        var workDir = Path.Combine(Path.GetTempPath(), "qapp-lsp", sessionId);
        Directory.CreateDirectory(workDir);

        var documentPath = Path.Combine(workDir, "Program.cs");
        var workspaceUri = new Uri(workDir + Path.DirectorySeparatorChar).AbsoluteUri;
        var documentUri = new Uri(documentPath).AbsoluteUri;

        Process? process = null;
        try
        {
            await System.IO.File.WriteAllTextAsync(
                Path.Combine(workDir, "Program.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                </Project>
                """,
                ct);
            await System.IO.File.WriteAllTextAsync(documentPath, "// student code\n", ct);

            var hello = JsonSerializer.Serialize(new
            {
                workspaceUri,
                documentUri,
            });
            await SendTextAsync(ws, hello, ct);

            var psi = new ProcessStartInfo
            {
                FileName = "csharp-ls",
                WorkingDirectory = workDir,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start csharp-ls. Is the tool installed and on PATH?");
                await CloseWsAsync(ws, WebSocketCloseStatus.InternalServerError, "csharp-ls unavailable", ct);
                return;
            }

            if (process is null)
            {
                await CloseWsAsync(ws, WebSocketCloseStatus.InternalServerError, "csharp-ls unavailable", ct);
                return;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _ = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync(cts.Token)) != null)
                    {
                        _logger.LogDebug("csharp-ls[{Sid}] stderr: {Line}", sessionId, line);
                    }
                }
                catch { /* ignored */ }
            }, cts.Token);

            var wsToStdin = PumpWsToStdinAsync(ws, process.StandardInput.BaseStream, cts.Token);
            var stdoutToWs = PumpStdoutToWsAsync(process.StandardOutput.BaseStream, ws, cts.Token);

            await Task.WhenAny(wsToStdin, stdoutToWs);
            cts.Cancel();
            try { process.Kill(entireProcessTree: true); } catch { /* ignored */ }
            try { await Task.WhenAll(wsToStdin, stdoutToWs); } catch { /* ignored */ }
        }
        catch (OperationCanceledException) { /* expected on disconnect */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LSP session {Sid} aborted with error.", sessionId);
        }
        finally
        {
            try { process?.Dispose(); } catch { /* ignored */ }
            try { Directory.Delete(workDir, recursive: true); } catch { /* ignored */ }

            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "session ended", CancellationToken.None);
                }
                catch { /* ignored */ }
            }
        }
    }

    private static async Task SendTextAsync(WebSocket ws, string text, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
    }

    private static async Task CloseWsAsync(WebSocket ws, WebSocketCloseStatus status, string description, CancellationToken ct)
    {
        if (ws.State == WebSocketState.Open)
        {
            try { await ws.CloseAsync(status, description, ct); } catch { /* ignored */ }
        }
    }

    private static async Task PumpWsToStdinAsync(WebSocket ws, Stream stdin, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        while (!ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close) return;
                if (result.Count > 0) ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (ms.Length == 0) continue;

            var payload = ms.ToArray();
            var header = Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n");
            await stdin.WriteAsync(header, ct);
            await stdin.WriteAsync(payload, ct);
            await stdin.FlushAsync(ct);
        }
    }

    private static async Task PumpStdoutToWsAsync(Stream stdout, WebSocket ws, CancellationToken ct)
    {
        var reader = PipeReader.Create(stdout);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(ct);
                var buf = result.Buffer;

                while (TryReadMessage(ref buf, out var payload))
                {
                    if (ws.State != WebSocketState.Open) return;
                    await ws.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, ct);
                }

                reader.AdvanceTo(buf.Start, buf.End);
                if (result.IsCompleted) break;
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }

    private static readonly byte[] HeaderSeparator = { 0x0D, 0x0A, 0x0D, 0x0A }; // \r\n\r\n

    private static bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out byte[] payload)
    {
        payload = Array.Empty<byte>();
        var reader = new SequenceReader<byte>(buffer);

        if (!reader.TryReadTo(out ReadOnlySequence<byte> headerSeq, HeaderSeparator, advancePastDelimiter: true))
            return false;

        int contentLength = -1;
        var headerStr = Encoding.ASCII.GetString(headerSeq.ToArray());
        foreach (var line in headerStr.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = line.IndexOf(':');
            if (colon < 0) continue;
            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value, out var n))
            {
                contentLength = n;
            }
        }

        if (contentLength < 0) return false;
        if (reader.Remaining < contentLength) return false;

        var body = reader.UnreadSequence.Slice(0, contentLength);
        payload = body.ToArray();
        reader.Advance(contentLength);
        buffer = reader.UnreadSequence;
        return true;
    }
}
