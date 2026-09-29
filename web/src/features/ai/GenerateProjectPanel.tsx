"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import type { ProjectDto } from "@/lib/types";

export function GenerateProjectPanel() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [prompt, setPrompt] = useState("");
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleGenerate() {
    setGenerating(true);
    setError(null);
    try {
      const project = await api.post<ProjectDto>("/ai/generate-project", { prompt, resolutionWidth: 1920, resolutionHeight: 1080 });
      router.push(`/projects/${project.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Generation failed.");
    } finally {
      setGenerating(false);
    }
  }

  return (
    <div className="mb-10">
      {!open ? (
        <button
          onClick={() => setOpen(true)}
          className="group border-primary/40 bg-primary/5 text-primary-ink hover:border-primary hover:shadow-primary/10 dark:border-primary/50 dark:text-primary w-full rounded-lg border border-dashed py-4 text-sm font-medium transition-all hover:-translate-y-0.5 hover:shadow-md"
        >
          <span className="group-hover:animate-wiggle inline-block">✨</span> Describe your show and let AI design a starting point
        </button>
      ) : (
        <div className="animate-pop-in border-primary/30 rounded-lg border p-4">
          <label className="block text-sm font-medium">Describe your show</label>
          <textarea
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            placeholder="I'm starting a football podcast called The Final Whistle. Our colors are black and gold and we normally have three presenters."
            rows={3}
            className="mt-1 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
          />
          {error && <p className="mt-2 text-xs text-red-600 dark:text-red-400">{error}</p>}
          <div className="mt-3 flex justify-end gap-2">
            <button onClick={() => setOpen(false)} className="rounded-md px-3 py-1.5 text-sm hover:bg-neutral-100 dark:hover:bg-neutral-800">
              Cancel
            </button>
            <button
              onClick={handleGenerate}
              disabled={generating || !prompt.trim()}
              className="bg-primary shadow-primary/20 rounded-md px-4 py-1.5 text-sm font-medium text-white shadow-sm transition hover:brightness-110 active:scale-95 disabled:opacity-50"
            >
              {generating ? "Designing..." : "Generate"}
            </button>
          </div>
          <p className="mt-2 text-xs text-neutral-400 dark:text-neutral-500">
            The result opens in the normal editor -- edit anything afterward, same as any other project.
          </p>
        </div>
      )}
    </div>
  );
}
