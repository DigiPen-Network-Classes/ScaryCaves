FROM node:18 AS build
EXPOSE 3000
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build

FROM node:18 AS runtime
WORKDIR /app
COPY --from=BUILD /app .
CMD ["npm", "run", "start"]
