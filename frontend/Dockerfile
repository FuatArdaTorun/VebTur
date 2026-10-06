# Build stage: install dependencies and build the Angular app for production.
FROM node:24.19.0-alpine AS build
WORKDIR /app

# Install dependencies first, so this layer stays cached until package-lock.json changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx ng build

# Runtime stage: nginx serves the built files and forwards /api requests to the backend.
FROM nginx:1.30.5-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
EXPOSE 80
