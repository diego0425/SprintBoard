# ============================================================
# BUILD
# ============================================================

FROM node:22-alpine AS build

WORKDIR /app

# Copy package files first to improve layer caching.
COPY sprintboard-web/package*.json ./

# Install dependencies using the lock file.
RUN npm ci

# Copy frontend source code.
COPY sprintboard-web/ ./

# API address used by Vite during the production build.
ARG VITE_API_BASE_URL=/api/v1
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Build the React application.
RUN npm run build

# ============================================================
# RUNTIME
# ============================================================

FROM nginx:alpine AS final

# Replace the default Nginx configuration.
COPY sprintboard-web/nginx.conf /etc/nginx/conf.d/default.conf

# Copy the Vite production build.
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80