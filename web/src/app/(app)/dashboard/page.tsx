"use client";

import Link from "next/link";
import { useEffect, useState, type FormEvent } from "react";
import { api } from "@/lib/api";
import { RESOLUTION_PRESETS, type OrganizationDto, type ProjectDto } from "@/lib/types";
import { TemplateGallery } from "@/features/templates/TemplateGallery";
import { GenerateProjectPanel } from "@/features/ai/GenerateProjectPanel";

export default function DashboardPage() {
  const [projects, setProjects] = useState<ProjectDto[] | null>(null);
  const [orgs, setOrgs] = useState<OrganizationDto[]>([]);
  const [showCreate, setShowCreate] = useState(false);
  const [name, setName] = useState("");
  const [resolution, setResolution] = useState<(typeof RESOLUTION_PRESETS)[number]>(RESOLUTION_PRESETS[0]);
  const [organizationId, setOrganizationId] = useState<string>("");
  const [creating, setCreating] = useState(false);

  async function loadProjects() {
    setProjects(await api.get<ProjectDto[]>("/projects"));
  }

  useEffect(() => {
    loadProjects();
    api.get<OrganizationDto[]>("/organizations").then(setOrgs);
  }, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreating(true);
    try {
      await api.post<ProjectDto>("/projects", {
        name,
        resolutionWidth: resolution.width,
        resolutionHeight: resolution.height,
        organizationId: organizationId || null,
      });
      setShowCreate(false);
      setName("");
      await loadProjects();
    } finally {
      setCreating(false);
    }
  }

  return (
    <main className="mx-auto max-w-3xl px-6 py-10">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="font-display text-2xl font-bold">Dashboard</h1>
        <button
          onClick={() => setShowCreate(true)}
          className="bg-primary shadow-primary/20 rounded-md px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:brightness-110 active:scale-95"
        >
          Blank project
        </button>
      </div>

      <GenerateProjectPanel />

      <TemplateGallery />

      <h2 className="mb-3 text-sm font-medium text-neutral-500 dark:text-neutral-400">Your projects</h2>

      {projects?.length === 0 && (
        <p className="rounded-md border border-dashed border-neutral-300 p-8 text-center text-sm text-neutral-500 dark:border-neutral-700 dark:text-neutral-400">
          <span className="mb-1 block text-2xl">🎬</span>
          No projects yet. Create your first one to start designing a stream layout.
        </p>
      )}

      <ul className="space-y-2">
        {projects?.map((project) => (
          <li key={project.id}>
            <Link
              href={`/projects/${project.id}`}
              className="hover:border-primary hover:shadow-primary/10 block rounded-md border border-neutral-200 px-4 py-3 transition-all hover:-translate-y-0.5 hover:shadow-md dark:border-neutral-800"
            >
              <div className="font-medium">{project.name}</div>
              <div className="text-xs text-neutral-500 dark:text-neutral-400">
                {project.resolutionWidth}x{project.resolutionHeight}
              </div>
            </Link>
          </li>
        ))}
      </ul>

      {showCreate && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/30 px-6">
          <form
            onSubmit={handleCreate}
            className="animate-pop-in w-full max-w-sm space-y-4 rounded-xl bg-white p-6 shadow-xl dark:bg-neutral-900 dark:shadow-black/50"
          >
            <h2 className="text-lg font-semibold">New project</h2>

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

            <div className="space-y-1">
              <label className="text-sm font-medium">Resolution</label>
              <select
                value={resolution.label}
                onChange={(e) =>
                  setResolution(RESOLUTION_PRESETS.find((p) => p.label === e.target.value)!)
                }
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
              >
                {RESOLUTION_PRESETS.map((preset) => (
                  <option key={preset.label} value={preset.label}>
                    {preset.label}
                  </option>
                ))}
              </select>
            </div>

            {orgs.length > 0 && (
              <div className="space-y-1">
                <label className="text-sm font-medium">Owner</label>
                <select
                  value={organizationId}
                  onChange={(e) => setOrganizationId(e.target.value)}
                  className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
                >
                  <option value="">Just me (personal)</option>
                  {orgs.map((org) => (
                    <option key={org.id} value={org.id}>
                      {org.name}
                    </option>
                  ))}
                </select>
              </div>
            )}

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
