"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { ProjectDto } from "@/lib/types";
import { useEditorStore } from "@/features/editor/store";
import { EditableCanvas } from "@/features/editor/EditableCanvas";
import { LayerPanel } from "@/features/editor/LayerPanel";
import { Inspector } from "@/features/editor/Inspector";
import { Toolbar } from "@/features/editor/Toolbar";
import { OverlayShare } from "@/features/editor/OverlayShare";

const MAX_CANVAS_WIDTH = 960;

export default function SceneEditorPage() {
  const { projectId, sceneId } = useParams<{ projectId: string; sceneId: string }>();
  const [project, setProject] = useState<ProjectDto | null>(null);
  const { load, loading, selectedId, undo, redo, removeElement } = useEditorStore();

  useEffect(() => {
    api.get<ProjectDto>(`/projects/${projectId}`).then(setProject);
    load(sceneId);
  }, [projectId, sceneId, load]);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      const target = e.target as HTMLElement;
      if (["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName)) return;

      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "z") {
        e.preventDefault();
        if (e.shiftKey) redo();
        else undo();
      } else if ((e.key === "Delete" || e.key === "Backspace") && selectedId) {
        e.preventDefault();
        removeElement(selectedId);
      }
    }

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [selectedId, undo, redo, removeElement]);

  if (!project || loading) {
    return <div className="p-10 text-sm text-neutral-400">Loading editor...</div>;
  }

  const scale = Math.min(1, MAX_CANVAS_WIDTH / project.resolutionWidth);

  return (
    <div className="flex h-[calc(100vh-57px)] flex-col">
      <div className="flex items-center justify-between border-b border-neutral-200 px-4 py-2">
        <Link href={`/projects/${projectId}`} className="text-sm text-neutral-500 hover:underline">
          &larr; {project.name}
        </Link>
        <div className="flex items-center gap-3">
          <span className="text-xs text-neutral-400">
            {project.resolutionWidth}x{project.resolutionHeight}
          </span>
          <OverlayShare sceneId={sceneId} resolutionWidth={project.resolutionWidth} resolutionHeight={project.resolutionHeight} />
        </div>
      </div>

      <Toolbar />

      <div className="flex flex-1 overflow-hidden">
        {/* Plain overflow:auto + margin:auto centers the canvas without the classic
            flex `justify-center`/`align-items-center` bug where content past the viewport
            on the "leading" side becomes unreachable by scrolling. */}
        <div className="flex-1 overflow-auto bg-neutral-100 p-6">
          <div className="w-fit" style={{ margin: "0 auto" }}>
            <EditableCanvas resolutionWidth={project.resolutionWidth} resolutionHeight={project.resolutionHeight} scale={scale} />
          </div>
        </div>
        <LayerPanel />
        <Inspector />
      </div>
    </div>
  );
}
