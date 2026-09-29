"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import Link from "next/link";
import { api, ApiError } from "@/lib/api";
import type { UserResponse } from "@/lib/types";
import { ThemeToggle } from "@/components/theme/ThemeToggle";
import { Logo } from "@/components/Logo";

export default function RegisterPage() {
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await api.post<UserResponse>("/auth/register", { email, password, displayName });
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Registration failed.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="relative flex flex-1 flex-col items-center justify-center overflow-hidden px-6">
      <div aria-hidden className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
        <div className="animate-blob bg-secondary/20 dark:bg-secondary/10 absolute -top-16 right-1/4 h-64 w-64 rounded-full blur-3xl" />
        <div className="animate-blob-delay bg-primary/20 dark:bg-primary/15 absolute bottom-0 left-1/4 h-64 w-64 rounded-full blur-3xl" />
      </div>
      <ThemeToggle className="fixed bottom-6 right-6 z-50" />
      <Link href="/" className="animate-fade-in-up mb-8">
        <Logo textClassName="text-primary-ink dark:text-primary text-2xl" iconClassName="h-16 w-auto" />
      </Link>
      <form onSubmit={handleSubmit} className="animate-fade-in-up w-full max-w-sm space-y-4" style={{ animationDelay: "0.05s" }}>
        <h1 className="font-display text-2xl font-bold">Create your Beam account</h1>

        <div className="space-y-1">
          <label className="text-sm font-medium">Name</label>
          <input
            required
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
          />
        </div>

        <div className="space-y-1">
          <label className="text-sm font-medium">Email</label>
          <input
            required
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
          />
        </div>

        <div className="space-y-1">
          <label className="text-sm font-medium">Password</label>
          <input
            required
            type="password"
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
          />
        </div>

        {error && <p className="text-sm text-red-600 dark:text-red-400">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="bg-primary shadow-primary/20 w-full rounded-md px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:brightness-110 active:scale-[0.98] disabled:opacity-50"
        >
          {submitting ? "Creating account..." : "Create account"}
        </button>

        <p className="text-center text-sm text-neutral-500 dark:text-neutral-400">
          Already have an account?{" "}
          <Link href="/login" className="underline">
            Log in
          </Link>
        </p>
      </form>
    </main>
  );
}
