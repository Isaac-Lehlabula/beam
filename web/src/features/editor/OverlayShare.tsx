"use client";

import { useState } from "react";
import { api } from "@/lib/api";

interface OverlayTokenDto {
  token: string;
  overlayPath: string;
}

interface OverlayShareProps {
  /** Exactly one of sceneId/projectId should be set. */
  sceneId?: string;
  projectId?: string;
  resolutionWidth: number;
  resolutionHeight: number;
}

export function OverlayShare({ sceneId, projectId, resolutionWidth, resolutionHeight }: OverlayShareProps) {
  const [open, setOpen] = useState(false);
  const [tokenInfo, setTokenInfo] = useState<OverlayTokenDto | null>(null);
  const [apiUrl, setApiUrl] = useState<string>("");
  const [copied, setCopied] = useState(false);

  const basePath = sceneId ? `/scenes/${sceneId}/overlay-token` : `/projects/${projectId}/overlay-token`;
  const title = sceneId ? "Overlay URL for this scene" : "Overlay URL for this project";
  const description = sceneId
    ? "Add this URL as a Browser Source. It always shows this one scene -- add each scene you want as its own source and switch between them in OBS, Streamlabs, etc. directly."
    : "Add this URL as a single, persistent Browser Source. It follows whatever scene Live Control has active -- switch scenes from your phone without ever touching OBS, Streamlabs, etc.";

  async function handleOpen() {
    setOpen(true);
    const [token, config] = await Promise.all([
      api.get<OverlayTokenDto>(basePath),
      fetch("/config").then((r) => r.json()),
    ]);
    setTokenInfo(token);
    setApiUrl(config.apiUrl);
  }

  async function handleRotate() {
    setTokenInfo(await api.post<OverlayTokenDto>(`${basePath}/rotate`));
    setCopied(false);
  }

  const fullUrl = tokenInfo ? `${apiUrl}${tokenInfo.overlayPath}` : "";

  return (
    <>
      <button
        onClick={handleOpen}
        className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50 dark:border-neutral-700 dark:hover:bg-neutral-900"
      >
        Get overlay URL
      </button>

      {open && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/30 px-6" onClick={() => setOpen(false)}>
          <div
            className="animate-pop-in w-full max-w-lg space-y-3 rounded-xl bg-white p-6 shadow-xl dark:bg-neutral-900 dark:shadow-black/50"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="text-lg font-semibold">{title}</h2>
            <p className="text-sm text-neutral-600 dark:text-neutral-300">
              {description} Set the source&apos;s width/height to{" "}
              <strong>
                {resolutionWidth}x{resolutionHeight}
              </strong>{" "}
              and disable &quot;refresh browser when scene becomes active&quot; -- edits update the source live,
              without recreating it.
            </p>

            {fullUrl ? (
              <div className="flex gap-2">
                <input
                  readOnly
                  value={fullUrl}
                  className="flex-1 rounded border border-neutral-300 px-2 py-1.5 text-xs dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100"
                />
                <button
                  onClick={() => {
                    navigator.clipboard.writeText(fullUrl);
                    setCopied(true);
                  }}
                  className="bg-primary shadow-primary/20 rounded-md px-3 py-1.5 text-sm text-white shadow-sm transition hover:brightness-110 active:scale-95"
                >
                  {copied ? "Copied!" : "Copy"}
                </button>
              </div>
            ) : (
              <p className="text-sm text-neutral-400 dark:text-neutral-500">Loading...</p>
            )}

            <div className="flex items-center justify-between pt-2">
              <button onClick={handleRotate} className="text-xs text-neutral-500 underline hover:text-black dark:text-neutral-400 dark:hover:text-white">
                Rotate URL (revokes the old one)
              </button>
              <button onClick={() => setOpen(false)} className="rounded-md px-3 py-1.5 text-sm hover:bg-neutral-100 dark:hover:bg-neutral-800">
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
