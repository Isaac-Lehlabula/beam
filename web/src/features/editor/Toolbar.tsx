"use client";

import { useRef, useState } from "react";
import { assetsApi } from "@/lib/elementsApi";
import { useEditorStore } from "@/features/editor/store";
import { ELEMENT_DEFAULTS } from "@/features/editor/elementDefaults";
import type { ElementType } from "@/lib/types";

const MORE_ELEMENT_TYPES: ElementType[] = [
  "StatCounter", "QrCode", "Countdown", "ProgressBar", "Ticker",
  "LowerThird", "Poll", "Scoreboard", "NowPlaying", "BrowserEmbed", "Alert", "Camera",
];

export function Toolbar() {
  const { addElement, undo, redo, canUndo, canRedo } = useEditorStore();
  const [showMore, setShowMore] = useState(false);
  const imageInputRef = useRef<HTMLInputElement>(null);
  const videoInputRef = useRef<HTMLInputElement>(null);
  const carouselInputRef = useRef<HTMLInputElement>(null);

  async function handleImageSelected(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    const asset = await assetsApi.upload(file);
    await addElement("Image", file.name, 100, 100, 300, 300, { assetId: asset.id, fit: "contain" });
  }

  async function handleVideoSelected(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    const asset = await assetsApi.upload(file);
    await addElement("Video", file.name, 100, 100, 480, 270, { assetId: asset.id, loop: true, muted: true, fit: "contain" });
  }

  async function handleCarouselSelected(e: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files ?? []);
    e.target.value = "";
    if (files.length === 0) return;
    const assets = await Promise.all(files.map((f) => assetsApi.upload(f)));
    await addElement("ImageCarousel", "Image Carousel", 100, 100, 400, 300, {
      assetIds: assets.map((a) => a.id),
      intervalSeconds: 5,
      fit: "contain",
    });
  }

  function handleAddDefault(type: ElementType) {
    const def = ELEMENT_DEFAULTS[type];
    if (!def) return;
    addElement(type, def.label, 100, 100, def.width, def.height, def.config);
    setShowMore(false);
  }

  return (
    <div className="relative flex items-center gap-2 border-b border-neutral-200 px-4 py-2">
      <button onClick={() => handleAddDefault("Text")} className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50">
        + Text
      </button>

      <button onClick={() => imageInputRef.current?.click()} className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50">
        + Image
      </button>
      <input ref={imageInputRef} type="file" accept="image/png,image/jpeg,image/webp" className="hidden" onChange={handleImageSelected} />

      <button onClick={() => videoInputRef.current?.click()} className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50">
        + Video
      </button>
      <input ref={videoInputRef} type="file" accept="video/mp4,video/webm" className="hidden" onChange={handleVideoSelected} />

      <button onClick={() => carouselInputRef.current?.click()} className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50">
        + Carousel
      </button>
      <input ref={carouselInputRef} type="file" accept="image/png,image/jpeg,image/webp" multiple className="hidden" onChange={handleCarouselSelected} />

      <div className="relative">
        <button onClick={() => setShowMore((v) => !v)} className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50">
          + More ▾
        </button>
        {showMore && (
          <>
            <div className="fixed inset-0 z-10" onClick={() => setShowMore(false)} />
            <div className="absolute top-full left-0 z-20 mt-1 w-48 rounded-md border border-neutral-200 bg-white py-1 shadow-lg">
              {MORE_ELEMENT_TYPES.map((type) => (
                <button
                  key={type}
                  onClick={() => handleAddDefault(type)}
                  className="block w-full px-3 py-1.5 text-left text-sm hover:bg-neutral-50"
                >
                  {ELEMENT_DEFAULTS[type]?.label ?? type}
                </button>
              ))}
            </div>
          </>
        )}
      </div>

      <div className="mx-2 h-5 w-px bg-neutral-200" />

      <button
        onClick={() => undo()}
        disabled={!canUndo()}
        className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50 disabled:opacity-40"
      >
        Undo
      </button>
      <button
        onClick={() => redo()}
        disabled={!canRedo()}
        className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50 disabled:opacity-40"
      >
        Redo
      </button>
    </div>
  );
}
