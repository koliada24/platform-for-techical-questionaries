# syntax=docker/dockerfile:1.7

# ---- Build stage ----
# Use a glibc-based image (not alpine/musl) to avoid the npm optional-dependencies
# bug with rolldown's native binding (vite 8 uses rolldown).
FROM node:20-slim AS build
WORKDIR /app

COPY package.json package-lock.json* ./
# Force a clean install so the correct platform-specific optional deps
# (e.g. @rolldown/binding-linux-x64-gnu) are resolved inside the container.
# We delete any host-generated lockfile because npm's optional-dependency
# resolution is platform-locked at lockfile-generation time
# (https://github.com/npm/cli/issues/4828).
RUN rm -rf node_modules package-lock.json \
 && npm install --no-audit --no-fund --include=optional

COPY . ./
RUN npm run build

# ---- Runtime stage (nginx serving static SPA) ----
FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
