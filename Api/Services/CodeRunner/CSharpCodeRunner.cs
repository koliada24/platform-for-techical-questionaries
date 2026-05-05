using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Api.Services.CodeRunner;

/// <summary>
/// Compiles and runs C# code as a console program via Roslyn. Supports both:
///   - top-level statements (e.g. <c>Console.WriteLine("hi");</c>)
///   - classic <c>static void Main()</c> / <c>static int Main()</c> entry points
///     (sync or async).
/// Console.Write/WriteLine output is captured.
///
/// SECURITY: this executes untrusted code in-process. There is NO sandboxing. A wall-clock
/// timeout is the only guard. Do not expose this without isolation in production.
/// </summary>
public class CSharpCodeRunner : ICodeRunner
{
    private static readonly MetadataReference[] s_references = BuildReferences();

    private static readonly CSharpCompilationOptions s_compileOptions =
        new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithPlatform(Platform.AnyCpu)
            .WithConcurrentBuild(true);

    private static readonly CSharpParseOptions s_parseOptions =
        new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None, SourceCodeKind.Regular);

    public async Task<CodeRunResult> RunCSharpAsync(string code, TimeSpan timeout, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var syntaxTree = CSharpSyntaxTree.ParseText(code, s_parseOptions);
        var assemblyName = "UserCode_" + Guid.NewGuid().ToString("N");
        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            s_references,
            s_compileOptions);

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream, cancellationToken: ct);
        if (!emitResult.Success)
        {
            sw.Stop();
            var error = string.Join(
                Environment.NewLine,
                emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));
            return new CodeRunResult(
                Success: false,
                Stdout: string.Empty,
                Error: error,
                TimedOut: false,
                DurationMs: sw.ElapsedMilliseconds);
        }

        peStream.Seek(0, SeekOrigin.Begin);

        // Collectible load context so the user assembly can be unloaded after each run.
        var loadContext = new AssemblyLoadContext("UserCode_" + assemblyName, isCollectible: true);
        var stdout = new StringBuilder();
        var writer = new StringWriter(stdout);
        var originalOut = Console.Out;

        try
        {
            var assembly = loadContext.LoadFromStream(peStream);
            var entryPoint = assembly.EntryPoint;
            if (entryPoint is null)
            {
                sw.Stop();
                return new CodeRunResult(
                    Success: false,
                    Stdout: string.Empty,
                    Error: "No entry point found. Provide top-level statements or a 'static Main' method.",
                    TimedOut: false,
                    DurationMs: sw.ElapsedMilliseconds);
            }

            Console.SetOut(writer);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            var args = entryPoint.GetParameters().Length == 1
                ? new object[] { Array.Empty<string>() }
                : Array.Empty<object>();

            var runTask = Task.Run(() =>
            {
                var result = entryPoint.Invoke(null, args);
                if (result is Task t)
                {
                    t.GetAwaiter().GetResult();
                }
            });

            var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);
            Task completed;
            try
            {
                completed = await Task.WhenAny(runTask, timeoutTask);
            }
            catch (OperationCanceledException)
            {
                completed = timeoutTask;
            }

            if (completed != runTask)
            {
                sw.Stop();
                await writer.FlushAsync(ct);
                return new CodeRunResult(
                    Success: false,
                    Stdout: stdout.ToString(),
                    Error: $"Execution timed out after {timeout.TotalSeconds:F1}s.",
                    TimedOut: true,
                    DurationMs: sw.ElapsedMilliseconds);
            }

            try
            {
                await runTask;
            }
            catch (TargetInvocationException tie)
            {
                sw.Stop();
                await writer.FlushAsync(ct);
                return new CodeRunResult(
                    Success: false,
                    Stdout: stdout.ToString(),
                    Error: (tie.InnerException ?? tie).ToString(),
                    TimedOut: false,
                    DurationMs: sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                await writer.FlushAsync(ct);
                return new CodeRunResult(
                    Success: false,
                    Stdout: stdout.ToString(),
                    Error: ex.ToString(),
                    TimedOut: false,
                    DurationMs: sw.ElapsedMilliseconds);
            }

            sw.Stop();
            await writer.FlushAsync(ct);
            return new CodeRunResult(
                Success: true,
                Stdout: stdout.ToString(),
                Error: null,
                TimedOut: false,
                DurationMs: sw.ElapsedMilliseconds);
        }
        finally
        {
            Console.SetOut(originalOut);
            loadContext.Unload();
        }
    }

    private static MetadataReference[] BuildReferences()
    {
        // Reference the trusted platform assemblies (the BCL the host runs on).
        // Gives user code access to the standard surface: System, System.Console,
        // System.Linq, System.Collections, System.Text.RegularExpressions, etc.
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        var refs = new List<MetadataReference>();
        foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                refs.Add(MetadataReference.CreateFromFile(path));
            }
            catch
            {
                // ignore unreadable entries
            }
        }
        return refs.ToArray();
    }
}
