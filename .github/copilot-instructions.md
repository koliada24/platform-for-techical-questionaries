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

### Backend
- **.NET**: 10.0
- **ASP.NET Core**: Web API with Controllers

### Frontend
- **Node.js**: 18+ (recommended)
- **npm**: Latest (included with Node.js)
- **TypeScript**: 6.0.2
- **React**: 19.2.5
- **React-DOM**: 19.2.5
- **Vite**: 8.0.10
- **ESLint**: 10.2.1
- **react-hook-form**: Latest (form state management)
- **bootstrap**: Latest (CSS framework)
- **react-bootstrap**: Latest (Bootstrap React components)
- **axios**: Latest (HTTP client for API requests)

## Backend Architecture

### ASP.NET Core Controllers
- Use ASP.NET Core controllers for all API endpoints
- Organize controllers logically by feature domain
- Each controller should handle a specific resource or feature area
- All HTTP endpoints must be implemented as controller actions
- Use appropriate HTTP verbs (GET, POST, PUT, DELETE, PATCH)
- Follow REST conventions for endpoint design

## Frontend Architecture

### React Components & Forms
- Use **react-hook-form** for all form state management and validation
- Use **react-bootstrap** components for UI elements (buttons, forms, modals, cards, etc.)
- Use **bootstrap** classes for styling and layout when react-bootstrap components are not available
- Use **axios** for all HTTP requests to the backend API
- Organize components logically by feature in the `src/` directory

## Development Workflow

1. **For API changes**: Navigate to `Api/` directory, create or modify controller files in the appropriate controller class, then run `dotnet build` and `dotnet run`. Always organize endpoints logically within controllers by feature/resource.

2. **For Client changes**: Navigate to `Client/` directory, run `npm install` if dependencies changed, edit TypeScript/React files in `src/`, and run `npm run dev` to see hot-reload changes. Use react-hook-form for forms, react-bootstrap for UI, and axios for API calls.

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

### Frontend
- Always run `npm install` in the Client directory before building or developing - this is required if dependencies are modified
- Client uses Vite's fast refresh for hot module replacement during development
- Both TypeScript checking and ESLint validation run as part of the client build process
- Use react-hook-form hooks (useForm, useFieldArray, etc.) for form management
- Always use axios for API requests - do not use fetch
- Use react-bootstrap components for consistent styling with Bootstrap

### Backend
- The API runs on HTTPS by default in development; adjust `launchSettings.json` if needed
- The project is set up with strict null checking (`Nullable=enable` in Api.csproj) and TypeScript strict mode
- All new endpoints must be implemented in appropriate controller classes
- Controllers should be organized by feature/resource domain
- Do not use minimal APIs - use controller-based endpoints exclusively

## Trust These Instructions

Follow the build and development instructions exactly as documented above. Only perform additional exploration if:
- A command produces an error not mentioned here
- The instructions appear to be incomplete or outdated
- New dependencies or tools have been explicitly added to the project
