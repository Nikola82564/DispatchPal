FROM node:24-alpine AS build

WORKDIR /app

COPY src/DispatchPal.Web/package.json .
COPY src/DispatchPal.Web/package-lock.json .

RUN npm ci

COPY src/DispatchPal.Web/ .

RUN npm run build


FROM nginx:alpine AS runtime

ENV API_UPSTREAM=http://api:8080

COPY src/DispatchPal.Web/nginx.conf \
     /etc/nginx/templates/default.conf.template

COPY --from=build \
     /app/dist/dispatchpal-web/browser \
     /usr/share/nginx/html

EXPOSE 80
