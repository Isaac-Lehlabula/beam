import type { NextConfig } from "next";

// The browser only ever talks to this Next.js origin. Next.js transparently proxies /api/*
// to the ASP.NET Core backend, so the auth cookie the API sets is same-origin from the
// browser's point of view in every environment -- no CORS, no cross-site cookie config needed.
const backendUrl = process.env.BEAM_API_INTERNAL_URL ?? "http://localhost:5080";

const nextConfig: NextConfig = {
  output: "standalone",
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${backendUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
