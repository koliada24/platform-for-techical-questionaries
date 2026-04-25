# Copilot Custom Instructions

## Project Overview

This is a full-stack web application called "Platform for Technical Questionaries" - a system for creating, managing, and answering technical questionnaires. The project consists of two main components:

- **Backend API**: .NET 10.0 ASP.NET Core web API
- **Frontend Client**: TypeScript/React application using Vite

## Project Structure

```
repo-root/
├── Api/                          # .NET 10.0 backend API
│   ├── Api.csproj               # Project file
│   ├── Api.sln                  # Solution file
│   ├── Program.cs               # Application entry point
│   ├── appsettings.json         # Production configuration
│   ├── appsettings.Development.json # Development configuration
│   ├── Properties/launchSettings.json # Launch settings
│   ├── bin/                     # Build output
│   └── obj/                     # Build intermediate files
│
└── Client/                       # TypeScript/React frontend
    ├── package.json             # npm dependencies
    ├── tsconfig.json            # TypeScript configuration
    ├── eslint.config.js         # ESLint configuration
    ├── vite.config.ts           # Vite bundler configuration
    ├── index.html               # HTML entry point
    ├── src/
    │   ├── main.tsx             # React entry point
    │   └── App.tsx              # Main React component
    └── node_modules/            # npm packages
```

## Build & Development Instructions

### Prerequisites

Before working on either component, ensure you have installed:
- **.NET 10.0 SDK** - Required for API development
- **Node.js 18+** - Required for client development (npm comes with Node.js)

### Backend (API) Setup & Build

**Navigate to API directory:**
```bash
cd Api
```

**Build the API:**
```bash
dotnet build
```

**Run the API in development mode:**
```bash
dotnet run
```
The API will start and listen on `https://localhost:5001` (or http://localhost:5000) as configured in `Properties/launchSettings.json`.

**Clean build artifacts:**
```bash
dotnet clean
```

### Frontend (Client) Setup & Build

**Navigate to client directory:**
```bash
cd Client
```

**Install dependencies (always run this first after cloning or when package.json changes):**
```bash
npm install
```

**Start development server:**
```bash
npm run dev
```
The dev server will start on `http://localhost:5173` by default.

**Build for production:**
```bash
npm run build
```
This runs TypeScript type checking (`tsc -b`) followed by Vite build, producing optimized output in the `dist/` directory.

**Lint code:**
```bash
npm run lint
```
Ensures TypeScript and React code follows ESLint rules defined in `eslint.config.js`.

**Preview production build:**
```bash
npm run preview
```

## Key Technologies & Versions

- **.NET**: 10.0
- **Node.js**: 18+ (recommended)
- **npm**: Latest (included with Node.js)
- **TypeScript**: 6.0.2
- **React**: 19.2.5
- **React-DOM**: 19.2.5
- **Vite**: 8.0.10
- **ESLint**: 10.2.1

## Development Workflow

1. **For API changes**: Navigate to `Api/` directory, edit C# files in `Program.cs` or create new files as needed, then run `dotnet build` and `dotnet run`.

2. **For Client changes**: Navigate to `Client/` directory, run `npm install` if dependencies changed, edit TypeScript/React files in `src/`, and run `npm run dev` to see hot-reload changes.

3. **Type checking**: 
   - Client: Run `npm run lint` to validate TypeScript and ESLint rules
   - API: Build with `dotnet build` to validate C#

4. **Production builds**:
   - Client: Run `npm run build` from Client directory
   - API: Run `dotnet publish` to prepare for deployment

## Configuration Files

- **Api/appsettings.json** - Production API configuration
- **Api/appsettings.Development.json** - Development API configuration
- **Api/Properties/launchSettings.json** - API server launch profiles (ports, URLs)
- **Client/vite.config.ts** - Vite bundler and dev server configuration
- **Client/eslint.config.js** - Linting rules for Client code
- **Client/tsconfig.json** - TypeScript compiler options for Client

## Important Notes

- Always run `npm install` in the Client directory before building or developing - this is required if dependencies are modified
- The API runs on HTTPS by default in development; adjust `launchSettings.json` if needed
- Client uses Vite's fast refresh for hot module replacement during development
- Both TypeScript checking and ESLint validation run as part of the client build process
- The project is set up with strict null checking (`Nullable=enable` in Api.csproj) and TypeScript strict mode

## Trust These Instructions

Follow the build and development instructions exactly as documented above. Only perform additional exploration if:
- A command produces an error not mentioned here
- The instructions appear to be incomplete or outdated
- New dependencies or tools have been explicitly added to the project
