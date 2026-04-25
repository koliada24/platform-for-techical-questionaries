# Copilot Custom Instructions

## Project Overview

A full-stack web application for creating and administering technical tests, integrated with **Google Classroom**. Teachers create tests in the app, generate per-student links, grade submissions, and have grades pushed back to Google Classroom automatically.

- **Backend API**: .NET 10.0 ASP.NET Core (controller-based)
- **Frontend Client**: TypeScript + React 19 + Vite

### User roles
- **Teacher** — signs in via Google through the main login screen. Manages courses/tests, grades submissions, syncs grades back to Classroom.
- **Student** — does **not** sign in from the main login screen. Students reach the app through a per-test link and authenticate via Google at that point.

## Project Structure

```
repo-root/
├── .github/
│   └── copilot-instructions.md
├── .gitignore                    # Ignores VS artifacts, node_modules, Client/dist
├── Api/                          # .NET 10.0 backend API
│   ├── Api.csproj
│   ├── Api.sln
│   ├── Program.cs                # Composition root: DI, auth, CORS, pipeline
│   ├── app.db                    # SQLite database (auto-created on startup)
│   ├── appsettings.json          # Connection strings, Google client, Client base URL
│   ├── appsettings.Development.json
│   ├── Properties/launchSettings.json
│   ├── Contracts/                # DTOs / request-response records
│   │   └── AuthContracts.cs
│   ├── Controllers/              # API endpoints, grouped by feature
│   │   └── AuthController.cs
│   ├── Data/                     # EF Core DbContext
│   │   └── AppDbContext.cs       # IdentityDbContext<ApplicationUser>
│   └── Models/                   # Domain entities
│       └── ApplicationUser.cs    # IdentityUser + Role + Google token fields
│
└── Client/                       # React + TypeScript frontend
    ├── package.json
    ├── vite.config.ts
    ├── tsconfig.json / tsconfig.app.json / tsconfig.node.json
    ├── eslint.config.js
    ├── index.html
    └── src/
        ├── main.tsx              # React entry
        ├── App.tsx               # Auth-gated shell
        ├── api/
        │   └── client.ts         # Axios instance (withCredentials, baseURL = API)
        ├── auth/
        │   └── AuthContext.tsx   # AuthProvider, useAuth (user, loading, logout, loginWithGoogle)
        ├── pages/
        │   ├── LoginPage.tsx     # Teacher-only Google sign-in screen
        │   └── HomePage.tsx      # Authenticated landing page
        └── types/
            └── auth.ts           # User, UserRole types
```

## Key Technologies & Versions

### Backend
- **.NET**: 10.0
- **ASP.NET Core**: controller-based Web API (no Minimal APIs)
- **EF Core 10** + **SQLite** (`app.db`)
- **ASP.NET Core Identity** (`IdentityCore<ApplicationUser>` + `IdentityRole`) for user/role storage. Local password sign-in is **disabled** — only the user store is used.
- **Cookie auth** (`qapp.auth`) is the app's session scheme.
- **Google OAuth** via `Microsoft.AspNetCore.Authentication.Google` with two named schemes:
  - `Google-Teacher` → Google's `CallbackPath` = `/api/auth/google-callback-teacher`, then redirects to `/api/auth/google-complete-teacher` (the controller action that finalizes app sign-in).
  - `Google-Student` → `CallbackPath` = `/api/auth/google-callback-student`, completion at `/api/auth/google-complete-student`.
  - Each Google handler stores its temporary external identity in a dedicated cookie scheme (`External-Teacher` / `External-Student`) — this separation is required, do not collapse them.
  - `SaveTokens = true` and `AccessType = "offline"` so refresh tokens are persisted on `ApplicationUser` for later Classroom API calls.
- Enums are serialized as strings (`JsonStringEnumConverter`) — the client expects `"Teacher"` / `"Student"`.

### Frontend
- **TypeScript** ~6.0
- **React** 19 / **react-dom** 19
- **Vite** 8 (dev server on `http://localhost:5173`)
- **react-hook-form** (forms)
- **react-bootstrap** + **bootstrap** (UI / styling)
- **axios** (HTTP, always with `withCredentials: true` against the API)
- ESLint 10 + typescript-eslint

## Configuration

### Ports & URLs
- API: `http://localhost:5107` (HTTP only — required because the registered Google redirect URIs are HTTP). Configured in `Api/Properties/launchSettings.json`.
- Client dev server: `http://localhost:5173`. The API allows this origin via CORS with credentials.
- Google Cloud OAuth client has these registered Authorized redirect URIs:
  - `http://localhost:5107/api/auth/google-callback-teacher`
  - `http://localhost:5107/api/auth/google-callback-student`

### Required configuration values (`Api/appsettings.json` or user-secrets)
- `ConnectionStrings:Default` — SQLite connection string (default: `Data Source=app.db`)
- `Google:ClientId`, `Google:ClientSecret` — store via user-secrets in dev:
  ```bash
  cd Api
  dotnet user-secrets init
  dotnet user-secrets set "Google:ClientId" "<id>"
  dotnet user-secrets set "Google:ClientSecret" "<secret>"
  ```
- `Client:BaseUrl` — used by the API to redirect back to the SPA after OAuth (default `http://localhost:5173`).

## Build & Development

### Prerequisites
- **.NET 10.0 SDK**
- **Node.js 18+**

### Backend
```bash
cd Api
dotnet build        # validate C#
dotnet run          # starts API on http://localhost:5107
dotnet clean
```
The DB schema is created on startup via `EnsureCreated()`; roles `Teacher` and `Student` are seeded automatically.

### Frontend
```bash
cd Client
npm install         # after clone or dependency change
npm run dev         # http://localhost:5173, HMR
npm run build       # tsc -b && vite build → dist/
npm run lint        # ESLint
npm run preview
```

## Backend Architecture & Conventions

- **Controllers only** — do not introduce Minimal API endpoints. Group by feature/resource (e.g. `AuthController`, future `TestsController`, `SubmissionsController`, `ClassroomController`).
- **DTOs** live under `Api/Contracts/`. Use `record` types. **Never expose `ApplicationUser.Id`** or other internal identifiers in DTOs returned to the client.
- **EF Core**: extend `AppDbContext` for new entities. Seed reference data in `Program.cs` startup block.
- **Auth**:
  - Default scheme is the cookie scheme; protect endpoints with `[Authorize]`.
  - For role-restricted endpoints use `[Authorize(Roles = "Teacher")]` / `"Student"` — roles are populated as claims at sign-in.
  - 401/403 are returned as plain status codes (no redirect to a login page) so the SPA can react.
- **Google tokens**: read/refresh `GoogleAccessToken` / `GoogleRefreshToken` from `ApplicationUser` when calling Google Classroom APIs on behalf of the user.
- **Nullable reference types** are enabled — handle `null` explicitly.

## Frontend Architecture & Conventions

- **Auth state** flows through `AuthProvider` / `useAuth()` in `src/auth/AuthContext.tsx`. Components must consume the user via `useAuth()`, not by calling `/api/auth/me` directly.
- **HTTP**: always use the shared `api` axios instance from `src/api/client.ts` — it sets `baseURL` and `withCredentials: true`. Do not use `fetch`.
- **Forms**: use **react-hook-form** (`useForm`, `useFieldArray`). Wire validation through `register(...)` options and surface errors via `Form.Control.Feedback`.
- **UI**: prefer **react-bootstrap** components (`Button`, `Form`, `Card`, `Container`, `Navbar`, `Modal`, …). Fall back to plain Bootstrap classes only when no component exists.
- **Routing**: there is currently no router. The shell switches between `LoginPage` and `HomePage` based on `user`. When student test-link pages are added, introduce `react-router-dom` rather than ad-hoc `window.location` switches.
- **Login UX**: the main `LoginPage` is **teacher-only** — do not re-add a "sign in as Student" button there. Student authentication will be triggered from the test-link page via `loginWithGoogle('Student')`.

## Auth Endpoints (current)

- `GET  /api/auth/me` — returns the current `UserDto` (`email`, `fullName`, `role`, `hasGoogleLink`). 401 if not signed in.
- `POST /api/auth/logout` — clears the app cookie.
- `GET  /api/auth/google-login-teacher` — starts Google OAuth (teacher scopes).
- `GET  /api/auth/google-callback-teacher` — Google redirect URI; handled by the auth middleware.
- `GET  /api/auth/google-complete-teacher` — controller action that finalizes app sign-in and redirects to the SPA.
- Same triplet exists for `student` (kept available for the future test-link flow even though the login UI hides it).

There are **no** local register/login endpoints. Do not reintroduce password-based auth.

## Development Workflow

1. **API changes**: edit/add controllers under `Api/Controllers/`, DTOs under `Api/Contracts/`, entities under `Api/Models/`, then `dotnet build` and `dotnet run`. Stop any prior `Api.exe` process before rebuilding (file lock).
2. **Client changes**: edit files under `Client/src/`, run `npm run dev` for HMR. Validate with `npm run lint` and `npm run build`.
3. **Both running**: API on `:5107`, client on `:5173`. CORS is preconfigured for credentialed requests between them.

## Important Notes

- Run `npm install` after pulling changes that touch `package.json`.
- The API uses **HTTP only in dev** (not HTTPS) because the Google OAuth redirect URIs are registered as `http://localhost:5107/...`. Don't switch the dev profile to HTTPS without re-registering the URIs in Google Cloud Console.
- `Client/dist/`, `Api/bin/`, `Api/obj/`, and `app.db` are git-ignored.
- Don't expose internal IDs (e.g., `ApplicationUser.Id`) in API responses to the client.
- Don't use Minimal APIs; do not use `fetch` on the client.

## Trust These Instructions

Follow the build and development instructions exactly as documented above. Only perform additional exploration if:
- A command produces an error not mentioned here
- The instructions appear to be incomplete or outdated
- New dependencies, endpoints, or tools have been explicitly added to the project
