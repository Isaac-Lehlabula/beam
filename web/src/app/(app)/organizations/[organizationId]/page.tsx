"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { MembershipDto, OrganizationDto, OrganizationRole } from "@/lib/types";

const ROLES: OrganizationRole[] = ["Owner", "OrgAdmin", "Streamer", "Producer", "Designer", "Moderator"];
const ADMIN_ROLES: OrganizationRole[] = ["Owner", "OrgAdmin"];

export default function OrganizationDetailPage() {
  const { organizationId } = useParams<{ organizationId: string }>();
  const [org, setOrg] = useState<OrganizationDto | null>(null);
  const [members, setMembers] = useState<MembershipDto[]>([]);
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<OrganizationRole>("Streamer");
  const [error, setError] = useState<string | null>(null);

  async function load() {
    const orgs = await api.get<OrganizationDto[]>("/organizations");
    setOrg(orgs.find((o) => o.id === organizationId) ?? null);
    setMembers(await api.get<MembershipDto[]>(`/organizations/${organizationId}/members`));
  }

  useEffect(() => {
    load();
  }, [organizationId]);

  const isAdmin = org && ADMIN_ROLES.includes(org.myRole);

  async function handleAddMember() {
    setError(null);
    try {
      await api.post(`/organizations/${organizationId}/members`, { email, role });
      setEmail("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add member.");
    }
  }

  if (!org) {
    return <div className="p-10 text-sm text-neutral-400 dark:text-neutral-500">Loading...</div>;
  }

  return (
    <main className="mx-auto max-w-2xl px-6 py-10">
      <Link href="/organizations" className="text-sm text-neutral-500 hover:underline dark:text-neutral-400">
        &larr; Organizations
      </Link>
      <h1 className="font-display mt-2 mb-6 text-2xl font-bold">{org.name}</h1>

      <h2 className="mb-2 text-sm font-medium text-neutral-500 dark:text-neutral-400">Members</h2>
      <ul className="mb-4 space-y-2">
        {members.map((member) => (
          <li
            key={member.userId}
            className="flex items-center justify-between rounded-md border border-neutral-200 px-4 py-2.5 dark:border-neutral-800"
          >
            <span className="text-sm">{member.displayName ?? member.userId}</span>
            {isAdmin ? (
              <select
                value={member.role}
                onChange={async (e) => {
                  await api.patch(`/organizations/${organizationId}/members/${member.userId}`, { role: e.target.value });
                  await load();
                }}
                className="rounded border border-neutral-300 px-2 py-1 text-xs dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
              >
                {ROLES.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            ) : (
              <span className="text-xs text-neutral-500 dark:text-neutral-400">{member.role}</span>
            )}
          </li>
        ))}
      </ul>

      {isAdmin && (
        <div className="mb-8 flex flex-wrap items-end gap-2 rounded-md border border-neutral-200 p-3 dark:border-neutral-800">
          <label className="text-xs">
            <span className="block text-neutral-500 dark:text-neutral-400">Email</span>
            <input
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="teammate@example.com"
              className="rounded border border-neutral-300 px-2 py-1.5 text-sm dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
            />
          </label>
          <label className="text-xs">
            <span className="block text-neutral-500 dark:text-neutral-400">Role</span>
            <select
              value={role}
              onChange={(e) => setRole(e.target.value as OrganizationRole)}
              className="rounded border border-neutral-300 px-2 py-1.5 text-sm dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
            >
              {ROLES.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </select>
          </label>
          <button
            onClick={handleAddMember}
            className="bg-primary shadow-primary/20 rounded-md px-3 py-1.5 text-sm text-white shadow-sm transition hover:brightness-110 active:scale-95"
          >
            Add member
          </button>
          {error && <p className="w-full text-xs text-red-600 dark:text-red-400">{error}</p>}
          <p className="w-full text-xs text-neutral-400 dark:text-neutral-500">
            They need an existing Beam account first -- there&apos;s no email invite flow yet.
          </p>
        </div>
      )}
    </main>
  );
}
