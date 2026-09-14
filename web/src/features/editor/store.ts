import { create } from "zustand";
import { elementsApi } from "@/lib/elementsApi";
import type { ElementConfig, ElementDto, ElementTransform, ElementType } from "@/lib/types";

/**
 * Every command is invertible and, on both do() and undo(), talks to the API directly -- the
 * server (not client state) is the source of truth, so undo/redo is "real" persisted history,
 * not a client-side illusion. See ARCHITECTURE.md "Editor state & undo/redo".
 */
interface Command {
  label: string;
  do(): Promise<void>;
  undo(): Promise<void>;
}

export function toTransform(e: ElementDto): ElementTransform {
  return {
    x: e.x,
    y: e.y,
    width: e.width,
    height: e.height,
    rotation: e.rotation,
    zIndex: e.zIndex,
    opacity: e.opacity,
    locked: e.locked,
    visible: e.visible,
  };
}

interface EditorState {
  sceneId: string | null;
  elements: ElementDto[];
  selectedId: string | null;
  loading: boolean;
  undoStack: Command[];
  redoStack: Command[];

  load: (sceneId: string) => Promise<void>;
  refresh: () => Promise<void>;
  select: (id: string | null) => void;

  addElement: (type: ElementType, name: string, x: number, y: number, width: number, height: number, config: ElementConfig) => Promise<void>;
  applyTransform: (elementId: string, prev: ElementTransform, next: ElementTransform) => Promise<void>;
  applyConfig: (elementId: string, prev: ElementConfig, next: ElementConfig) => Promise<void>;
  duplicateElement: (elementId: string) => Promise<void>;
  removeElement: (elementId: string) => Promise<void>;

  runCommand: (command: Command) => Promise<void>;
  undo: () => Promise<void>;
  redo: () => Promise<void>;
  canUndo: () => boolean;
  canRedo: () => boolean;
}

export const useEditorStore = create<EditorState>((set, get) => ({
  sceneId: null,
  elements: [],
  selectedId: null,
  loading: false,
  undoStack: [],
  redoStack: [],

  load: async (sceneId) => {
    set({ sceneId, loading: true, undoStack: [], redoStack: [], selectedId: null });
    const elements = await elementsApi.list(sceneId);
    set({ elements, loading: false });
  },

  refresh: async () => {
    const { sceneId } = get();
    if (!sceneId) return;
    set({ elements: await elementsApi.list(sceneId) });
  },

  select: (id) => set({ selectedId: id }),

  runCommand: async (command) => {
    await command.do();
    await get().refresh();
    set((s) => ({ undoStack: [...s.undoStack, command], redoStack: [] }));
  },

  addElement: async (type, name, x, y, width, height, config) => {
    const { sceneId, runCommand } = get();
    if (!sceneId) return;
    let createdId: string | null = null;

    await runCommand({
      label: `Add ${name}`,
      do: async () => {
        const created = await elementsApi.create(sceneId, { type, name, x, y, width, height, config });
        createdId = created.id;
      },
      undo: async () => {
        if (createdId) await elementsApi.remove(sceneId, createdId);
      },
    });

    if (createdId) set({ selectedId: createdId });
  },

  applyTransform: async (elementId, prev, next) => {
    const { sceneId, runCommand } = get();
    if (!sceneId) return;
    await runCommand({
      label: "Move/resize",
      do: async () => {
        await elementsApi.updateTransform(sceneId, elementId, next);
      },
      undo: async () => {
        await elementsApi.updateTransform(sceneId, elementId, prev);
      },
    });
  },

  applyConfig: async (elementId, prev, next) => {
    const { sceneId, runCommand } = get();
    if (!sceneId) return;
    await runCommand({
      label: "Edit properties",
      do: async () => {
        await elementsApi.updateConfig(sceneId, elementId, next);
      },
      undo: async () => {
        await elementsApi.updateConfig(sceneId, elementId, prev);
      },
    });
  },

  duplicateElement: async (elementId) => {
    const { sceneId, runCommand } = get();
    if (!sceneId) return;
    let createdId: string | null = null;

    await runCommand({
      label: "Duplicate",
      do: async () => {
        const created = await elementsApi.duplicate(sceneId, elementId);
        createdId = created.id;
      },
      undo: async () => {
        if (createdId) await elementsApi.remove(sceneId, createdId);
      },
    });

    if (createdId) set({ selectedId: createdId });
  },

  removeElement: async (elementId) => {
    const { sceneId, elements, runCommand } = get();
    if (!sceneId) return;
    const snapshot = elements.find((e) => e.id === elementId);
    if (!snapshot) return;

    await runCommand({
      label: "Delete",
      do: async () => {
        await elementsApi.remove(sceneId, elementId);
      },
      undo: async () => {
        // Recreating loses the original id (acceptable simplification -- see ARCHITECTURE.md
        // "Editor state & undo/redo"); it reselects the new id below so undo still feels correct.
        const recreated = await elementsApi.create(sceneId, {
          type: snapshot.type,
          name: snapshot.name,
          x: snapshot.x,
          y: snapshot.y,
          width: snapshot.width,
          height: snapshot.height,
          config: snapshot.config,
        });
        await elementsApi.updateTransform(sceneId, recreated.id, toTransform(snapshot));
        set({ selectedId: recreated.id });
      },
    });

    set((s) => ({ selectedId: s.selectedId === elementId ? null : s.selectedId }));
  },

  undo: async () => {
    const { undoStack, redoStack } = get();
    const command = undoStack.at(-1);
    if (!command) return;
    await command.undo();
    await get().refresh();
    set({ undoStack: undoStack.slice(0, -1), redoStack: [...redoStack, command] });
  },

  redo: async () => {
    const { undoStack, redoStack } = get();
    const command = redoStack.at(-1);
    if (!command) return;
    await command.do();
    await get().refresh();
    set({ redoStack: redoStack.slice(0, -1), undoStack: [...undoStack, command] });
  },

  canUndo: () => get().undoStack.length > 0,
  canRedo: () => get().redoStack.length > 0,
}));
