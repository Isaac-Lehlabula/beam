"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { RESOLUTION_PRESETS, type ProjectDto, type TemplateDto } from "@/lib/types";

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
      <h2 className="mb-3 text-sm font-medium text-neutral-500">Start from a template</h2>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        {templates.map((template) => (
          <button
            key={template.id}
            onClick={() => openFor(template)}
            className="rounded-lg border border-neutral-200 p-4 text-left hover:border-neutral-400"
          >
            <div className="font-medium">{template.name}</div>
            <div className="text-xs text-neutral-500">{template.category}</div>
          </button>
        ))}
      </div>

      {active && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/30 px-6" onClick={() => setActive(null)}>
          <div className="w-full max-w-sm space-y-4 rounded-lg bg-white p-6 shadow-lg" onClick={(e) => e.stopPropagation()}>
            <h2 className="text-lg font-semibold">New project from &quot;{active.name}&quot;</h2>

            <div className="space-y-1">
              <label className="text-sm font-medium">Project name</label>
              <input
                autoFocus
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm"
              />
            </div>

            <div className="space-y-1">
              <label className="text-sm font-medium">Resolution</label>
              <select
                value={resolution.label}
                onChange={(e) => setResolution(RESOLUTION_PRESETS.find((p) => p.label === e.target.value)!)}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm"
              >
                {RESOLUTION_PRESETS.map((preset) => (
                  <option key={preset.label} value={preset.label}>
                    {preset.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex justify-end gap-2">
              <button onClick={() => setActive(null)} className="rounded-md px-4 py-2 text-sm hover:bg-neutral-100">
                Cancel
              </button>
              <button
                onClick={handleInstantiate}
                disabled={creating || !name.trim()}
                className="rounded-md bg-black px-4 py-2 text-sm font-medium text-white hover:bg-neutral-800 disabled:opacity-50"
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
