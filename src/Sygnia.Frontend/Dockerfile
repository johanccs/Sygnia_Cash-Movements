FROM node:20 AS build
WORKDIR /app
COPY src/Sygnia.Frontend/package*.json ./
RUN npm ci
COPY src/Sygnia.Frontend/ .
RUN npx ng build --configuration production

FROM nginx:alpine
COPY --from=build /app/dist/Sygnia.Frontend/browser /usr/share/nginx/html
COPY src/Sygnia.Frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
