FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# next.config.ts evaluates rewrites() at BUILD time and freezes the result, so the API's internal
# address has to be supplied here -- setting BEAM_API_INTERNAL_URL only at container runtime has
# no effect on the /api/* proxy. (Runtime-read values like BEAM_API_PUBLIC_URL are unaffected.)
ARG BEAM_API_INTERNAL_URL=http://api:8080
ENV BEAM_API_INTERNAL_URL=$BEAM_API_INTERNAL_URL
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
EXPOSE 3000
CMD ["node", "server.js"]
