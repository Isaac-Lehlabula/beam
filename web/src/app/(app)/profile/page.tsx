"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { useCurrentUser } from "@/lib/useCurrentUser";
import type { OrganizationDto } from "@/lib/types";
import { Avatar } from "@/components/Avatar";

export default function ProfilePage() {
  const { user, loading } = useCurrentUser();
  const router = useRouter();
  const [orgs, setOrgs] = useState<OrganizationDto[] | null>(null);

  useEffect(() => {
    api.get<OrganizationDto[]>("/organizations").then(setOrgs);
  }, []);

  async function handleLogout() {
    await api.post("/auth/logout");
    router.push("/login");
  }

  if (loading || !user) {
    return <div className="p-10 text-sm text-neutral-400 dark:text-neutral-500">Loading...</div>;
  }

  return (
    <main className="mx-auto max-w-2xl px-6 py-10">
      <h1 className="font-display mb-6 text-2xl font-bold">Your profile</h1>

      <div className="flex items-center gap-4 rounded-lg border border-neutral-200 p-5 dark:border-neutral-800">
        <Avatar name={user.displayName} className="h-16 w-16 text-xl" />
        <div>
          <div className="text-lg font-semibold">{user.displayName}</div>
          <div className="text-sm text-neutral-500 dark:text-neutral-400">{user.email}</div>
        </div>
      </div>

      <h2 className="mt-8 mb-3 text-sm font-medium text-neutral-500 dark:text-neutral-400">Organizations</h2>
      {orgs?.length === 0 && (
        <p className="rounded-md border border-dashed border-neutral-300 p-6 text-center text-sm text-neutral-500 dark:border-neutral-700 dark:text-neutral-400">
          You&apos;re not part of any organization yet.{" "}
          <Link href="/organizations" className="text-primary-ink dark:text-primary hover:underline">
            Create or join one
          </Link>
          .
        </p>
      )}
      <ul className="space-y-2">
        {orgs?.map((org) => (
          <li key={org.id}>
            <Link
              href={`/organizations/${org.id}`}
              className="hover:border-primary hover:shadow-primary/10 flex items-center justify-between rounded-md border border-neutral-200 px-4 py-3 transition-all hover:-translate-y-0.5 hover:shadow-md dark:border-neutral-800"
            >
              <span className="font-medium">{org.name}</span>
              <span className="text-xs text-neutral-500 dark:text-neutral-400">{org.myRole}</span>
            </Link>
          </li>
        ))}
      </ul>

      <button
        onClick={handleLogout}
        className="mt-8 rounded-md border border-neutral-300 px-4 py-2 text-sm font-medium hover:bg-neutral-50 dark:border-neutral-700 dark:hover:bg-neutral-900"
      >
        Log out
      </button>
    </main>
  );
}
