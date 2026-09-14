"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { ElementDto, OrganizationDto, SceneDto } from "@/lib/types";

const CATEGORIES = [
  "Gaming", "Podcast", "JustChatting", "Church", "Sports", "News",
  "Corporate", "Conference", "DjMusic", "Interview", "Education", "ProductLaunch",
] as const;

interface LockChoice {
  locked: boolean;
  editableProperties: string;
}

export function SaveAsOrgTemplate({ projectId }: { projectId: string }) {
  const [open, setOpen] = useState(false);
  const [orgs, setOrgs] = useState<OrganizationDto[]>([]);
  const [scenes, setScenes] = useState<SceneDto[]>([]);
  const [sceneId, setSceneId] = useState("");
  const [elements, setElements] = useState<ElementDto[]>([]);
  const [choices, setChoices] = useState<Record<string, LockChoice>>({});
  const [organizationId, setOrganizationId] = useState("");
  const [name, setName] = useState("");
  const [category, setCategory] = useState<(typeof CATEGORIES)[number]>("Corporate");
  const [saving, setSaving] = useState(false);
  const [done, setDone] = useState(false);

  async function handleOpen() {
    setOpen(true);
    setDone(false);
    const [orgList, sceneList] = await Promise.all([
      api.get<OrganizationDto[]>("/organizations"),
      api.get<SceneDto[]>(`/projects/${projectId}/scenes`),
    ]);
    setOrgs(orgList.filter((o) => ["Owner", "OrgAdmin", "Designer"].includes(o.myRole)));
    setScenes(sceneList);
    if (sceneList[0]) setSceneId(sceneList[0].id);
  }

  useEffect(() => {
    if (!sceneId) return;
    api.get<ElementDto[]>(`/scenes/${sceneId}/elements`).then((els) => {
      setElements(els);
      setChoices(Object.fromEntries(els.map((e) => [e.id, { locked: false, editableProperties: "" }])));
    });
  }, [sceneId]);

  async function handleSave() {
    setSaving(true);
    try {
      const lockedElements = Object.entries(choices)
        .filter(([, c]) => c.locked)
        .map(([elementId, c]) => ({
          elementId,
          editableProperties: c.editableProperties
            .split(",")
            .map((s) => s.trim())
            .filter(Boolean),
        }));

      await api.post("/templates/from-project", { projectId, organizationId, name, category, lockedElements });
      setDone(true);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <button onClick={handleOpen} className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50">
        Save as org template
      </button>

      {open && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/30 px-6" onClick={() => setOpen(false)}>
          <div className="max-h-[85vh] w-full max-w-lg space-y-3 overflow-y-auto rounded-lg bg-white p-6 shadow-lg" onClick={(e) => e.stopPropagation()}>
            <h2 className="text-lg font-semibold">Save as organization template</h2>

            {orgs.length === 0 ? (
              <p className="text-sm text-neutral-500">
                You need Owner, Admin, or Designer in an organization to author templates.
              </p>
            ) : done ? (
              <p className="text-sm text-green-700">Template saved. Members of the organization can now use it.</p>
            ) : (
              <>
                <p className="text-xs text-neutral-500">
                  Snapshots every scene in this project. Pick elements from one scene below to lock in the
                  template -- locked elements can never be moved, resized, or deleted once instantiated, and only
                  the config properties you list stay editable (e.g. <code>content</code>).
                </p>

                <div className="grid grid-cols-2 gap-2">
                  <label className="text-xs">
                    <span className="block text-neutral-500">Organization</span>
                    <select value={organizationId} onChange={(e) => setOrganizationId(e.target.value)} className="w-full rounded border border-neutral-300 px-2 py-1.5 text-sm">
                      <option value="">Choose...</option>
                      {orgs.map((o) => (
                        <option key={o.id} value={o.id}>
                          {o.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="text-xs">
                    <span className="block text-neutral-500">Category</span>
                    <select value={category} onChange={(e) => setCategory(e.target.value as (typeof CATEGORIES)[number])} className="w-full rounded border border-neutral-300 px-2 py-1.5 text-sm">
                      {CATEGORIES.map((c) => (
                        <option key={c} value={c}>
                          {c}
                        </option>
                      ))}
                    </select>
                  </label>
                </div>

                <label className="block text-xs">
                  <span className="text-neutral-500">Template name</span>
                  <input value={name} onChange={(e) => setName(e.target.value)} className="w-full rounded border border-neutral-300 px-2 py-1.5 text-sm" />
                </label>

                <label className="block text-xs">
                  <span className="text-neutral-500">Preview/lock elements from scene</span>
                  <select value={sceneId} onChange={(e) => setSceneId(e.target.value)} className="w-full rounded border border-neutral-300 px-2 py-1.5 text-sm">
                    {scenes.map((s) => (
                      <option key={s.id} value={s.id}>
                        {s.name}
                      </option>
                    ))}
                  </select>
                </label>

                <ul className="space-y-1">
                  {elements.map((el) => (
                    <li key={el.id} className="rounded border border-neutral-200 p-2">
                      <label className="flex items-center gap-2 text-sm">
                        <input
                          type="checkbox"
                          checked={choices[el.id]?.locked ?? false}
                          onChange={(e) =>
                            setChoices((c) => ({ ...c, [el.id]: { ...c[el.id], locked: e.target.checked } }))
                          }
                        />
                        {el.name} ({el.type})
                      </label>
                      {choices[el.id]?.locked && (
                        <input
                          placeholder="editable config keys, e.g. content"
                          value={choices[el.id]?.editableProperties ?? ""}
                          onChange={(e) =>
                            setChoices((c) => ({ ...c, [el.id]: { ...c[el.id], editableProperties: e.target.value } }))
                          }
                          className="mt-1 w-full rounded border border-neutral-300 px-2 py-1 text-xs"
                        />
                      )}
                    </li>
                  ))}
                </ul>

                <div className="flex justify-end gap-2 pt-2">
                  <button onClick={() => setOpen(false)} className="rounded-md px-3 py-1.5 text-sm hover:bg-neutral-100">
                    Cancel
                  </button>
                  <button
                    onClick={handleSave}
                    disabled={saving || !organizationId || !name.trim()}
                    className="rounded-md bg-black px-3 py-1.5 text-sm text-white hover:bg-neutral-800 disabled:opacity-50"
                  >
                    {saving ? "Saving..." : "Save template"}
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </>
  );
}
