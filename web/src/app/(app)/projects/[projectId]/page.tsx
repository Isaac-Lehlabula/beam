"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { api } from "@/lib/api";
import type { ProjectDto, SceneDto } from "@/lib/types";
import { SaveAsOrgTemplate } from "@/features/templates/SaveAsOrgTemplate";

export default function ProjectPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [project, setProject] = useState<ProjectDto | null>(null);
  const [scenes, setScenes] = useState<SceneDto[]>([]);
  const [newSceneName, setNewSceneName] = useState("");
  const dragIndex = useRef<number | null>(null);

  async function loadAll() {
    const [proj, sceneList] = await Promise.all([
      api.get<ProjectDto>(`/projects/${projectId}`),
      api.get<SceneDto[]>(`/projects/${projectId}/scenes`),
    ]);
    setProject(proj);
    setScenes(sceneList);
  }

  useEffect(() => {
    loadAll();
  }, [projectId]);

  async function handleCreateScene() {
    if (!newSceneName.trim()) return;
    await api.post<SceneDto>(`/projects/${projectId}/scenes`, { name: newSceneName });
    setNewSceneName("");
    await loadAll();
  }

  async function handleRename(sceneId: string, currentName: string) {
    const name = window.prompt("Rename scene", currentName);
    if (!name || name === currentName) return;
    await api.patch<SceneDto>(`/projects/${projectId}/scenes/${sceneId}`, { name });
    await loadAll();
  }

  async function handleDuplicate(sceneId: string) {
    await api.post<SceneDto>(`/projects/${projectId}/scenes/${sceneId}/duplicate`);
    await loadAll();
  }

  async function handleDelete(sceneId: string) {
    if (!window.confirm("Delete this scene?")) return;
    await api.delete(`/projects/${projectId}/scenes/${sceneId}`);
    await loadAll();
  }

  async function persistOrder(ordered: SceneDto[]) {
    setScenes(ordered);
    await api.post(`/projects/${projectId}/scenes/reorder`, {
      orderedSceneIds: ordered.map((s) => s.id),
    });
  }

  function handleDrop(targetIndex: number) {
    if (dragIndex.current === null || dragIndex.current === targetIndex) return;
    const next = [...scenes];
    const [moved] = next.splice(dragIndex.current, 1);
    next.splice(targetIndex, 0, moved);
    dragIndex.current = null;
    persistOrder(next);
  }

  if (!project) {
    return <div className="p-10 text-sm text-neutral-400 dark:text-neutral-500">Loading...</div>;
  }

  return (
    <main className="mx-auto max-w-3xl px-6 py-10">
      <Link href="/dashboard" className="text-sm text-neutral-500 hover:underline dark:text-neutral-400">
        &larr; All projects
      </Link>

      <div className="mt-2 flex items-center justify-between">
        <div>
          <h1 className="font-display text-2xl font-bold">{project.name}</h1>
          <p className="text-sm text-neutral-500 dark:text-neutral-400">
            {project.resolutionWidth}x{project.resolutionHeight}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <SaveAsOrgTemplate projectId={projectId} />
          <Link
            href={`/projects/${projectId}/live`}
            className="rounded-md border border-neutral-300 px-4 py-2 text-sm font-medium hover:bg-neutral-50 dark:border-neutral-700 dark:hover:bg-neutral-900"
          >
            Live Control
          </Link>
        </div>
      </div>

      <h2 className="mt-8 mb-3 text-sm font-medium text-neutral-500 dark:text-neutral-400">Scenes</h2>
      <ul className="space-y-2">
        {scenes.map((scene, index) => (
          <li
            key={scene.id}
            draggable
            onDragStart={() => (dragIndex.current = index)}
            onDragOver={(e) => e.preventDefault()}
            onDrop={() => handleDrop(index)}
            className="flex items-center justify-between rounded-md border border-neutral-200 px-4 py-3 cursor-move dark:border-neutral-800"
          >
            <div className="flex items-center gap-3">
              <span className="text-neutral-400 dark:text-neutral-600">⠿</span>
              <Link href={`/projects/${projectId}/scenes/${scene.id}`} className="font-medium hover:underline">
                {scene.name}
              </Link>
            </div>
            <div className="flex gap-3 text-xs text-neutral-500 dark:text-neutral-400">
              <button onClick={() => handleRename(scene.id, scene.name)} className="hover:text-black dark:hover:text-white">
                Rename
              </button>
              <button onClick={() => handleDuplicate(scene.id)} className="hover:text-black dark:hover:text-white">
                Duplicate
              </button>
              <button onClick={() => handleDelete(scene.id)} className="hover:text-red-600 dark:hover:text-red-400">
                Delete
              </button>
            </div>
          </li>
        ))}
      </ul>

      <div className="mt-4 flex gap-2">
        <input
          value={newSceneName}
          onChange={(e) => setNewSceneName(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleCreateScene()}
          placeholder="New scene name"
          className="flex-1 rounded-md border border-neutral-300 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
        />
        <button
          onClick={handleCreateScene}
          className="bg-primary shadow-primary/20 rounded-md px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:brightness-110 active:scale-95"
        >
          Add scene
        </button>
      </div>
    </main>
  );
}
