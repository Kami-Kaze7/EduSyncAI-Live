import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: 'http://172.20.10.5:5152/api/:path*',
      },
    ];
  },
};

export default nextConfig;
