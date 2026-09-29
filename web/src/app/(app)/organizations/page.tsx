"use client";

import Link from "next/link";
import { useEffect, useState, type FormEvent } from "react";
import { api } from "@/lib/api";
import type { OrganizationDto } from "@/lib/types";

export default function OrganizationsPage() {
  const [orgs, setOrgs] = useState<OrganizationDto[] | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [name, setName] = useState("");
  const [creating, setCreating] = useState(false);

  async function load() {
    setOrgs(await api.get<OrganizationDto[]>("/organizations"));
  }

  useEffect(() => {
    load();
  }, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreating(true);
    try {
      await api.post("/organizations", { name });
      await api.post("/auth/refresh-claims"); // pick up the new "org" claim immediately
      setShowCreate(false);
      setName("");
      await load();
    } finally {
      setCreating(false);
    }
  }

  return (
    <main className="mx-auto max-w-2xl px-6 py-10">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="font-display text-2xl font-bold">Organizations</h1>
        <button
          onClick={() => setShowCreate(true)}
          className="bg-primary shadow-primary/20 rounded-md px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:brightness-110 active:scale-95"
        >
          New organization
        </button>
      </div>

      {orgs?.length === 0 && (
        <p className="rounded-md border border-dashed border-neutral-300 p-8 text-center text-sm text-neutral-500 dark:border-neutral-700 dark:text-neutral-400">
          <span className="mb-1 block text-2xl">👥</span>
          You&apos;re not part of any organization yet.
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

      {showCreate && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/30 px-6" onClick={() => setShowCreate(false)}>
          <form
            onSubmit={handleCreate}
            className="animate-pop-in w-full max-w-sm space-y-4 rounded-xl bg-white p-6 shadow-xl dark:bg-neutral-900 dark:shadow-black/50"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="text-lg font-semibold">New organization</h2>
            <div className="space-y-1">
              <label className="text-sm font-medium">Name</label>
              <input
                required
                autoFocus
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
              />
            </div>
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setShowCreate(false)}
                className="rounded-md px-4 py-2 text-sm hover:bg-neutral-100 dark:hover:bg-neutral-800"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={creating}
                className="bg-primary shadow-primary/20 rounded-md px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:brightness-110 active:scale-95 disabled:opacity-50"
              >
                {creating ? "Creating..." : "Create"}
              </button>
            </div>
          </form>
        </div>
      )}
    </main>
  );
}
