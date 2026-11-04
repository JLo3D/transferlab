# syntax=docker/dockerfile:1

FROM node:24-alpine AS build
WORKDIR /app

RUN npm install --global pnpm@11.25.0

COPY src/TransferLab.Web/package.json src/TransferLab.Web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY src/TransferLab.Web/ ./
RUN pnpm build

FROM nginx:1.29-alpine AS final

COPY src/TransferLab.Web/nginx.conf /etc/nginx/templates/default.conf.template
COPY src/TransferLab.Web/15-transferlab-resolver.envsh /docker-entrypoint.d/15-transferlab-resolver.envsh
RUN chmod +x /docker-entrypoint.d/15-transferlab-resolver.envsh
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
    CMD wget --quiet --output-document=/dev/null http://127.0.0.1:8080/health/live || exit 1
