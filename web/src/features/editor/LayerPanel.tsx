"use client";

import { useEditorStore, toTransform } from "@/features/editor/store";

export function LayerPanel() {
  const { elements, selectedId, select, applyTransform, duplicateElement, removeElement } = useEditorStore();
  const sorted = [...elements].sort((a, b) => b.zIndex - a.zIndex);

  return (
    <div className="w-64 shrink-0 border-l border-neutral-200 p-3">
      <h2 className="mb-2 text-xs font-semibold uppercase text-neutral-500">Layers</h2>
      <ul className="space-y-1">
        {sorted.map((element) => (
          <li
            key={element.id}
            onClick={() => select(element.id)}
            className={`flex items-center justify-between rounded px-2 py-1.5 text-sm cursor-pointer ${
              element.id === selectedId ? "bg-blue-50 text-blue-700" : "hover:bg-neutral-50"
            }`}
          >
            <span className="flex items-center gap-1 truncate">
              {element.editableProperties !== null && <span title="Locked by template">🔒</span>}
              {element.name}
            </span>
            <div className="flex items-center gap-1 text-xs text-neutral-400">
              <button
                title={element.visible ? "Hide" : "Show"}
                onClick={(e) => {
                  e.stopPropagation();
                  const prev = toTransform(element);
                  applyTransform(element.id, prev, { ...prev, visible: !prev.visible });
                }}
                className="hover:text-black"
              >
                {element.visible ? "👁" : "🚫"}
              </button>
              {element.editableProperties === null && (
                <button
                  title={element.locked ? "Unlock" : "Lock"}
                  onClick={(e) => {
                    e.stopPropagation();
                    const prev = toTransform(element);
                    applyTransform(element.id, prev, { ...prev, locked: !prev.locked });
                  }}
                  className="hover:text-black"
                >
                  {element.locked ? "🔒" : "🔓"}
                </button>
              )}
              <button
                title="Duplicate"
                onClick={(e) => {
                  e.stopPropagation();
                  duplicateElement(element.id);
                }}
                className="hover:text-black"
              >
                ⧉
              </button>
              {element.editableProperties === null && (
                <button
                  title="Delete"
                  onClick={(e) => {
                    e.stopPropagation();
                    removeElement(element.id);
                  }}
                  className="hover:text-red-600"
                >
                  ✕
                </button>
              )}
            </div>
          </li>
        ))}
        {sorted.length === 0 && <li className="text-xs text-neutral-400">No elements yet.</li>}
      </ul>
    </div>
  );
}
