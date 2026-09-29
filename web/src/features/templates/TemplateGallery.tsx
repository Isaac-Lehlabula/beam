"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { RESOLUTION_PRESETS, type ProjectDto, type TemplateDto } from "@/lib/types";

const CATEGORY_COLORS: Record<string, string> = {
  Gaming: "bg-violet-500",
  Podcast: "bg-fuchsia-500",
  JustChatting: "bg-amber-500",
  Church: "bg-cyan-500",
  Sports: "bg-emerald-500",
  News: "bg-rose-500",
  Corporate: "bg-blue-500",
  Conference: "bg-indigo-500",
  DjMusic: "bg-pink-500",
  Interview: "bg-teal-500",
  Education: "bg-orange-500",
  ProductLaunch: "bg-lime-500",
};

export function TemplateGallery() {
  const router = useRouter();
  const [templates, setTemplates] = useState<TemplateDto[] | null>(null);
  const [active, setActive] = useState<TemplateDto | null>(null);
  const [name, setName] = useState("");
  const [resolution, setResolution] = useState<(typeof RESOLUTION_PRESETS)[number]>(RESOLUTION_PRESETS[0]);
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    api.get<TemplateDto[]>("/templates").then(setTemplates);
  }, []);

  function openFor(template: TemplateDto) {
    setActive(template);
    setName(template.name);
  }

  async function handleInstantiate() {
    if (!active) return;
    setCreating(true);
    try {
      const project = await api.post<ProjectDto>(`/templates/${active.id}/instantiate`, {
        projectName: name,
        resolutionWidth: resolution.width,
        resolutionHeight: resolution.height,
      });
      router.push(`/projects/${project.id}`);
    } finally {
      setCreating(false);
    }
  }

  if (!templates || templates.length === 0) return null;

  return (
    <div className="mb-10">
      <h2 className="mb-3 text-sm font-medium text-neutral-500 dark:text-neutral-400">Start from a template</h2>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        {templates.map((template) => (
          <button
            key={template.id}
            onClick={() => openFor(template)}
            className="group hover:border-primary hover:shadow-primary/10 rounded-lg border border-neutral-200 p-4 text-left transition-all hover:-translate-y-0.5 hover:shadow-md dark:border-neutral-800"
          >
            <div className="font-medium">{template.name}</div>
            <div className="mt-1 flex items-center gap-1.5 text-xs text-neutral-500 dark:text-neutral-400">
              <span className={`h-1.5 w-1.5 rounded-full ${CATEGORY_COLORS[template.category] ?? "bg-neutral-400"}`} />
              {template.category}
            </div>
          </button>
        ))}
      </div>

      {active && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/30 px-6" onClick={() => setActive(null)}>
          <div
            className="animate-pop-in w-full max-w-sm space-y-4 rounded-xl bg-white p-6 shadow-xl dark:bg-neutral-900 dark:shadow-black/50"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="text-lg font-semibold">New project from &quot;{active.name}&quot;</h2>

            <div className="space-y-1">
              <label className="text-sm font-medium">Project name</label>
              <input
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
                onChange={(e) => setResolution(RESOLUTION_PRESETS.find((p) => p.label === e.target.value)!)}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
              >
                {RESOLUTION_PRESETS.map((preset) => (
                  <option key={preset.label} value={preset.label}>
                    {preset.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex justify-end gap-2">
              <button
                onClick={() => setActive(null)}
                className="rounded-md px-4 py-2 text-sm hover:bg-neutral-100 dark:hover:bg-neutral-800"
              >
                Cancel
              </button>
              <button
                onClick={handleInstantiate}
                disabled={creating || !name.trim()}
                className="bg-primary shadow-primary/20 rounded-md px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:brightness-110 active:scale-95 disabled:opacity-50"
              >
                {creating ? "Creating..." : "Create project"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
