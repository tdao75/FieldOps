FROM node:24-alpine AS build
WORKDIR /app

COPY src/fieldops-web/package*.json ./
RUN npm ci

COPY src/fieldops-web/ ./

ARG VITE_API_BASE_URL=http://localhost:5080
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

FROM nginx:alpine AS final

COPY src/fieldops-web/nginx.conf \
    /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist \
    /usr/share/nginx/html

EXPOSE 80