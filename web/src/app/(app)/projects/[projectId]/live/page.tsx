"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { ElementDto, LiveSessionDto, ProjectDto, SceneDto } from "@/lib/types";
import { OverlayShare } from "@/features/editor/OverlayShare";
import { ConnectObsPanel } from "@/features/obs/ConnectObsPanel";

export default function LiveControlPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<ProjectDto | null>(null);
  const [scenes, setScenes] = useState<SceneDto[]>([]);
  const [session, setSession] = useState<LiveSessionDto | null>(null);
  const [elements, setElements] = useState<ElementDto[]>([]);
  const [busy, setBusy] = useState(false);

  const refreshSession = useCallback(async () => {
    const current = await api.get<LiveSessionDto>(`/projects/${projectId}/live-session`);
    setSession(current);
    if (current.currentSceneId) {
      setElements(await api.get<ElementDto[]>(`/scenes/${current.currentSceneId}/elements`));
    } else {
      setElements([]);
    }
  }, [projectId]);

  useEffect(() => {
    api.get<ProjectDto>(`/projects/${projectId}`).then(setProject);
    api.get<SceneDto[]>(`/projects/${projectId}/scenes`).then(setScenes);
    refreshSession();
  }, [projectId, refreshSession]);

  async function run(action: () => Promise<unknown>) {
    setBusy(true);
    try {
      await action();
      await refreshSession();
    } finally {
      setBusy(false);
    }
  }

  async function setVisibility(elementId: string, visible: boolean) {
    await api.post(`/projects/${projectId}/live-session/elements/${elementId}/visibility`, { visible });
  }

  /** Alerts are a trigger, not a toggle: show, hold, then auto-hide -- no dedicated backend
   *  endpoint needed, this is just two ordinary visibility overrides a few seconds apart. */
  async function handleTriggerAlert(elementId: string) {
    await run(() => setVisibility(elementId, true));
    setTimeout(() => run(() => setVisibility(elementId, false)), 5000);
  }

  if (!project || !session) {
    return <div className="p-10 text-sm text-neutral-400 dark:text-neutral-500">Loading...</div>;
  }

  return (
    <main className="mx-auto max-w-2xl px-4 py-8 sm:px-6">
      <Link href={`/projects/${projectId}`} className="text-sm text-neutral-500 hover:underline dark:text-neutral-400">
        &larr; {project.name}
      </Link>

      <div className="mt-2 mb-6 flex items-center justify-between">
        <h1 className="font-display text-2xl font-bold">Live Control</h1>
        <div className="flex items-center gap-2">
          <span className={`h-2.5 w-2.5 rounded-full ${session.isActive ? "bg-red-500" : "bg-neutral-300 dark:bg-neutral-700"}`} />
          <span className="text-sm text-neutral-600 dark:text-neutral-300">{session.isActive ? "Live" : "Not live"}</span>
        </div>
      </div>

      <div className="mb-6 flex flex-wrap items-center gap-3">
        {session.isActive ? (
          <button
            disabled={busy}
            onClick={() => run(() => api.delete(`/projects/${projectId}/live-session`))}
            className="rounded-md bg-red-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
          >
            End session
          </button>
        ) : (
          <button
            disabled={busy}
            onClick={() => run(() => api.post(`/projects/${projectId}/live-session`))}
            className="bg-primary shadow-primary/20 rounded-md px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:brightness-110 active:scale-95 disabled:opacity-50"
          >
            Start session
          </button>
        )}
        <OverlayShare sceneId={undefined} projectId={projectId} resolutionWidth={project.resolutionWidth} resolutionHeight={project.resolutionHeight} />
      </div>

      <div className="mb-6">
        <ConnectObsPanel projectId={projectId} resolutionWidth={project.resolutionWidth} resolutionHeight={project.resolutionHeight} />
      </div>

      <h2 className="mb-2 text-sm font-medium text-neutral-500 dark:text-neutral-400">Scenes</h2>
      <div className="mb-6 grid grid-cols-2 gap-2 sm:grid-cols-3">
        {scenes.map((scene) => (
          <button
            key={scene.id}
            disabled={busy}
            onClick={() =>
              run(() => api.post(`/projects/${projectId}/live-session/switch-scene`, { sceneId: scene.id }))
            }
            className={`rounded-lg border px-4 py-3 text-sm font-medium transition-all ${
              session.currentSceneId === scene.id
                ? "bg-primary shadow-primary/20 border-transparent text-white shadow-sm"
                : "hover:border-primary border-neutral-200 dark:border-neutral-800"
            }`}
          >
            {scene.name}
          </button>
        ))}
      </div>

      {session.currentSceneId && (
        <>
          <h2 className="mb-2 text-sm font-medium text-neutral-500 dark:text-neutral-400">Elements on air</h2>
          <ul className="space-y-2">
            {elements.map((element) => {
              const overridden = session.elementVisibilityOverrides[element.id];
              const visible = overridden ?? element.visible;
              return (
                <li
                  key={element.id}
                  className="flex items-center justify-between rounded-md border border-neutral-200 px-4 py-3 dark:border-neutral-800"
                >
                  <span className="text-sm">{element.name}</span>
                  <div className="flex gap-2">
                    {element.type === "Alert" && (
                      <button
                        disabled={busy}
                        onClick={() => handleTriggerAlert(element.id)}
                        className="bg-secondary shadow-secondary/30 animate-pulse rounded-md px-3 py-1.5 text-xs font-medium text-white shadow-sm transition hover:brightness-110 active:scale-95 disabled:animate-none disabled:opacity-50"
                      >
                        ⚡ Trigger
                      </button>
                    )}
                    <button
                      disabled={busy}
                      onClick={() => run(() => setVisibility(element.id, !visible))}
                      className={`rounded-md px-3 py-1.5 text-xs font-medium transition-colors ${
                        visible
                          ? "bg-primary shadow-primary/20 text-white shadow-sm"
                          : "bg-neutral-100 text-neutral-500 dark:bg-neutral-800 dark:text-neutral-400"
                      }`}
                    >
                      {visible ? "Showing" : "Hidden"}
                    </button>
                  </div>
                </li>
              );
            })}
            {elements.length === 0 && <li className="text-xs text-neutral-400 dark:text-neutral-500">No elements on this scene.</li>}
          </ul>
        </>
      )}
    </main>
  );
}
