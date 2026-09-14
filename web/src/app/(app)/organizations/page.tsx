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
        <h1 className="text-2xl font-semibold">Organizations</h1>
        <button
          onClick={() => setShowCreate(true)}
          className="rounded-md bg-black px-4 py-2 text-sm font-medium text-white hover:bg-neutral-800"
        >
          New organization
        </button>
      </div>

      {orgs?.length === 0 && (
        <p className="rounded-md border border-dashed border-neutral-300 p-8 text-center text-sm text-neutral-500">
          You&apos;re not part of any organization yet.
        </p>
      )}

      <ul className="space-y-2">
        {orgs?.map((org) => (
          <li key={org.id}>
            <Link
              href={`/organizations/${org.id}`}
              className="flex items-center justify-between rounded-md border border-neutral-200 px-4 py-3 hover:border-neutral-400"
            >
              <span className="font-medium">{org.name}</span>
              <span className="text-xs text-neutral-500">{org.myRole}</span>
            </Link>
          </li>
        ))}
      </ul>

      {showCreate && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/30 px-6" onClick={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="w-full max-w-sm space-y-4 rounded-lg bg-white p-6 shadow-lg" onClick={(e) => e.stopPropagation()}>
            <h2 className="text-lg font-semibold">New organization</h2>
            <div className="space-y-1">
              <label className="text-sm font-medium">Name</label>
              <input
                required
                autoFocus
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm"
              />
            </div>
            <div className="flex justify-end gap-2">
              <button type="button" onClick={() => setShowCreate(false)} className="rounded-md px-4 py-2 text-sm hover:bg-neutral-100">
                Cancel
              </button>
              <button
                type="submit"
                disabled={creating}
                className="rounded-md bg-black px-4 py-2 text-sm font-medium text-white hover:bg-neutral-800 disabled:opacity-50"
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
