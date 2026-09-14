"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

interface HealthResponse {
  status: string;
  app: string;
  utc: string;
}

export default function LandingPage() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [healthError, setHealthError] = useState(false);

  useEffect(() => {
    fetch("/api/health")
      .then((res) => (res.ok ? res.json() : Promise.reject(res)))
      .then(setHealth)
      .catch(() => setHealthError(true));
  }, []);

  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-6 px-6 text-center">
      <h1 className="text-5xl font-bold tracking-tight">Beam</h1>
      <p className="text-lg text-neutral-500">Beam your stream.</p>

      <div className="flex gap-3">
        <Link
          href="/register"
          className="rounded-md bg-black px-5 py-2.5 text-sm font-medium text-white hover:bg-neutral-800"
        >
          Get started
        </Link>
        <Link
          href="/login"
          className="rounded-md border border-neutral-300 px-5 py-2.5 text-sm font-medium hover:bg-neutral-50"
        >
          Log in
        </Link>
      </div>

      <p className="mt-8 text-xs text-neutral-400">
        {healthError && "API unreachable"}
        {health && `API ${health.status} · ${health.app}`}
        {!health && !healthError && "Checking API..."}
      </p>
    </main>
  );
}
