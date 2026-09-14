"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCurrentUser } from "@/lib/useCurrentUser";
import { api } from "@/lib/api";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const { user, loading } = useCurrentUser();
  const router = useRouter();

  if (loading) {
    return <div className="flex flex-1 items-center justify-center text-sm text-neutral-400">Loading...</div>;
  }

  if (!user) {
    router.replace("/login");
    return null;
  }

  async function handleLogout() {
    await api.post("/auth/logout");
    router.push("/login");
  }

  return (
    <div className="flex min-h-full flex-1 flex-col">
      <header className="flex items-center justify-between border-b border-neutral-200 px-6 py-3">
        <Link href="/dashboard" className="text-lg font-semibold">
          Beam
        </Link>
        <div className="flex items-center gap-4 text-sm text-neutral-600">
          <Link href="/organizations" className="hover:text-black">
            Organizations
          </Link>
          <Link href="/settings/streaming" className="hover:text-black">
            Streaming
          </Link>
          <span>{user.displayName}</span>
          <button onClick={handleLogout} className="underline hover:text-black">
            Log out
          </button>
        </div>
      </header>
      <div className="flex-1">{children}</div>
    </div>
  );
}
