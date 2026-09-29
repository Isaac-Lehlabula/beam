"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { useCurrentUser } from "@/lib/useCurrentUser";
import { api } from "@/lib/api";
import { ThemeToggle } from "@/components/theme/ThemeToggle";
import { Logo } from "@/components/Logo";
import { Avatar } from "@/components/Avatar";

const NAV_ITEMS = [
  { href: "/dashboard", label: "Dashboard", emoji: "🏠" },
  { href: "/organizations", label: "Organizations", emoji: "🏢" },
  { href: "/settings/streaming", label: "Streaming", emoji: "📡" },
] as const;

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const { user, loading } = useCurrentUser();
  const router = useRouter();
  const pathname = usePathname();

  // Redirecting during render (rather than here, in an effect) triggers React's
  // "Cannot update a component while rendering a different component" error -- router.replace
  // is itself a state update on the Router, so it has to happen after render commits.
  useEffect(() => {
    if (!loading && !user) {
      router.replace("/login");
    }
  }, [loading, user, router]);

  if (loading || !user) {
    return <div className="flex flex-1 items-center justify-center text-sm text-neutral-400 dark:text-neutral-500">Loading...</div>;
  }

  async function handleLogout() {
    await api.post("/auth/logout");
    router.push("/login");
  }

  return (
    <div className="flex min-h-full flex-1 flex-col">
      <header className="flex items-center justify-between border-b border-neutral-200 px-6 py-3 dark:border-neutral-800">
        <Link href="/dashboard">
          <Logo textClassName="text-primary-ink dark:text-primary text-lg" iconClassName="h-10 w-auto" />
        </Link>
        <div className="flex items-center gap-1 text-sm text-neutral-600 dark:text-neutral-300">
          <nav className="flex items-center gap-1">
            {NAV_ITEMS.map((item) => {
              const active = pathname === item.href || pathname?.startsWith(`${item.href}/`);
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`flex items-center gap-1.5 rounded-md px-3 py-1.5 font-medium transition-colors ${
                    active
                      ? "bg-primary/10 text-primary-ink dark:text-primary"
                      : "hover:bg-neutral-100 hover:text-black dark:hover:bg-neutral-800 dark:hover:text-white"
                  }`}
                >
                  <span aria-hidden="true">{item.emoji}</span>
                  {item.label}
                </Link>
              );
            })}
          </nav>
          <span className="mx-2 h-5 w-px bg-neutral-200 dark:bg-neutral-800" />
          <button
            onClick={handleLogout}
            className="rounded-md px-3 py-1.5 font-medium transition-colors hover:bg-neutral-100 hover:text-black dark:hover:bg-neutral-800 dark:hover:text-white"
          >
            Log out
          </button>
          <Link href="/profile" className="ml-1 hover:opacity-80" title={user.displayName}>
            <Avatar name={user.displayName} />
          </Link>
        </div>
      </header>
      <div className="flex-1">{children}</div>
      <ThemeToggle className="fixed bottom-6 right-6 z-50" />
    </div>
  );
}
