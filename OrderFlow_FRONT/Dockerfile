# Etapa 1: compilar Angular.
FROM node:22-alpine AS build
WORKDIR /app

# Primero solo package*.json: la capa de dependencias queda en caché mientras no cambien.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npx ng build --configuration production

# Etapa 2: solo los archivos estáticos sobre nginx (sin Node ni node_modules).
FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/OrderFlow_FRONT/browser /usr/share/nginx/html
EXPOSE 80
