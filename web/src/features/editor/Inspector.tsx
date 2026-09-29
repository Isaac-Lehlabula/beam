"use client";

import { useEditorStore, toTransform } from "@/features/editor/store";
import type {
  AlertElementConfig,
  CameraElementConfig,
  CountdownElementConfig,
  ImageCarouselElementConfig,
  ImageElementConfig,
  LowerThirdElementConfig,
  NowPlayingElementConfig,
  PollElementConfig,
  ProgressBarElementConfig,
  QrCodeElementConfig,
  ScoreboardElementConfig,
  StatCounterElementConfig,
  TextElementConfig,
  TickerElementConfig,
  VideoElementConfig,
} from "@/lib/types";
import {
  AlertFields,
  BrowserEmbedFields,
  CameraFields,
  CountdownFields,
  ImageCarouselFields,
  LowerThirdFields,
  NowPlayingFields,
  PollFields,
  ProgressBarFields,
  QrCodeFields,
  ScoreboardFields,
  StatCounterFields,
  TickerFields,
  VideoFields,
} from "@/features/editor/ExpandedInspectorFields";

export function Inspector() {
  const { elements, selectedId, applyTransform, applyConfig } = useEditorStore();
  const element = elements.find((e) => e.id === selectedId);

  if (!element) {
    return (
      <div className="w-64 shrink-0 border-l border-neutral-200 p-3 text-xs text-neutral-400 dark:border-neutral-800 dark:text-neutral-500">
        Select an element to edit its properties.
      </div>
    );
  }

  const transform = toTransform(element);
  const editableProperties = element.editableProperties;
  const transformLocked = editableProperties !== null;

  function numberField(label: string, key: "x" | "y" | "width" | "height" | "rotation" | "opacity", step = 1) {
    return (
      <label className="block text-xs">
        <span className="text-neutral-500 dark:text-neutral-400">{label}</span>
        <input
          type="number"
          step={step}
          disabled={transformLocked}
          value={Math.round(transform[key] * 100) / 100}
          onChange={(e) => {
            const value = Number(e.target.value);
            if (Number.isNaN(value)) return;
            applyTransform(element!.id, transform, { ...transform, [key]: value });
          }}
          className="mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 disabled:bg-neutral-100 disabled:text-neutral-400 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100 dark:disabled:bg-neutral-900 dark:disabled:text-neutral-600"
        />
      </label>
    );
  }

  return (
    <div className="w-64 shrink-0 space-y-4 overflow-y-auto border-l border-neutral-200 p-3 dark:border-neutral-800">
      {editableProperties !== null && (
        <div className="rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800 dark:bg-amber-950/50 dark:text-amber-400">
          🔒 Locked by template. Position, size and deletion can&apos;t be changed
          {editableProperties.length > 0 ? `; only ${editableProperties.join(", ")} can be edited.` : "."}
        </div>
      )}

      <div>
        <h2 className="mb-2 text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Transform</h2>
        <div className="grid grid-cols-2 gap-2">
          {numberField("X", "x")}
          {numberField("Y", "y")}
          {numberField("Width", "width")}
          {numberField("Height", "height")}
          {numberField("Rotation", "rotation")}
          {numberField("Opacity", "opacity", 0.05)}
        </div>
      </div>

      {element.type === "Text" && (
        <TextConfigFields
          elementId={element.id}
          config={element.config as TextElementConfig}
          applyConfig={applyConfig}
          editableProperties={editableProperties}
        />
      )}
      {element.type === "Image" && (
        <ImageConfigFields
          elementId={element.id}
          config={element.config as ImageElementConfig}
          applyConfig={applyConfig}
          editableProperties={editableProperties}
        />
      )}
      {element.type === "Video" && <VideoFields elementId={element.id} config={element.config as VideoElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "Camera" && <CameraFields elementId={element.id} config={element.config as CameraElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "StatCounter" && <StatCounterFields elementId={element.id} config={element.config as StatCounterElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "QrCode" && <QrCodeFields elementId={element.id} config={element.config as QrCodeElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "Countdown" && <CountdownFields elementId={element.id} config={element.config as CountdownElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "ProgressBar" && <ProgressBarFields elementId={element.id} config={element.config as ProgressBarElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "Ticker" && <TickerFields elementId={element.id} config={element.config as TickerElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "LowerThird" && <LowerThirdFields elementId={element.id} config={element.config as LowerThirdElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "ImageCarousel" && <ImageCarouselFields elementId={element.id} config={element.config as ImageCarouselElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "Poll" && <PollFields elementId={element.id} config={element.config as PollElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "Scoreboard" && <ScoreboardFields elementId={element.id} config={element.config as ScoreboardElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "NowPlaying" && <NowPlayingFields elementId={element.id} config={element.config as NowPlayingElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "BrowserEmbed" && <BrowserEmbedFields elementId={element.id} config={element.config as { url: string }} applyConfig={applyConfig} editableProperties={editableProperties} />}
      {element.type === "Alert" && <AlertFields elementId={element.id} config={element.config as AlertElementConfig} applyConfig={applyConfig} editableProperties={editableProperties} />}
    </div>
  );
}

/** Null (not locked) allows everything; a whitelist allows only the named keys. */
function isFieldEditable(editableProperties: string[] | null, key: string) {
  return editableProperties === null || editableProperties.includes(key);
}

function TextConfigFields({
  elementId,
  config,
  applyConfig,
  editableProperties,
}: {
  elementId: string;
  config: TextElementConfig;
  applyConfig: (id: string, prev: TextElementConfig, next: TextElementConfig) => void;
  editableProperties: string[] | null;
}) {
  const update = (patch: Partial<TextElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (key: string) => isFieldEditable(editableProperties, key);
  const disabledClass = "disabled:bg-neutral-100 disabled:text-neutral-400 dark:disabled:bg-neutral-900 dark:disabled:text-neutral-600";
  const fieldClass = "dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100";

  return (
    <div>
      <h2 className="mb-2 text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Text</h2>
      <div className="space-y-2">
        <label className="block text-xs">
          <span className="text-neutral-500 dark:text-neutral-400">Content</span>
          <textarea
            value={config.content}
            disabled={!editable("content")}
            onChange={(e) => update({ content: e.target.value })}
            rows={3}
            className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 text-sm ${fieldClass} ${disabledClass}`}
          />
        </label>
        <label className="block text-xs">
          <span className="text-neutral-500 dark:text-neutral-400">Font size</span>
          <input
            type="number"
            value={config.fontSize}
            disabled={!editable("fontSize")}
            onChange={(e) => update({ fontSize: Number(e.target.value) })}
            className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 ${fieldClass} ${disabledClass}`}
          />
        </label>
        <label className="block text-xs">
          <span className="text-neutral-500 dark:text-neutral-400">Weight</span>
          <select
            value={config.fontWeight}
            disabled={!editable("fontWeight")}
            onChange={(e) => update({ fontWeight: e.target.value })}
            className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 ${fieldClass} ${disabledClass}`}
          >
            <option value="400">Normal</option>
            <option value="600">Semibold</option>
            <option value="700">Bold</option>
            <option value="900">Black</option>
          </select>
        </label>
        <label className="block text-xs">
          <span className="text-neutral-500 dark:text-neutral-400">Align</span>
          <select
            value={config.textAlign}
            disabled={!editable("textAlign")}
            onChange={(e) => update({ textAlign: e.target.value as TextElementConfig["textAlign"] })}
            className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 ${fieldClass} ${disabledClass}`}
          >
            <option value="left">Left</option>
            <option value="center">Center</option>
            <option value="right">Right</option>
          </select>
        </label>
        <label className="block text-xs">
          <span className="text-neutral-500 dark:text-neutral-400">Color</span>
          <input
            type="color"
            value={config.color}
            disabled={!editable("color")}
            onChange={(e) => update({ color: e.target.value })}
            className={`mt-0.5 h-8 w-full rounded border border-neutral-300 dark:border-neutral-700 ${disabledClass}`}
          />
        </label>
      </div>
    </div>
  );
}

function ImageConfigFields({
  elementId,
  config,
  applyConfig,
  editableProperties,
}: {
  elementId: string;
  config: ImageElementConfig;
  applyConfig: (id: string, prev: ImageElementConfig, next: ImageElementConfig) => void;
  editableProperties: string[] | null;
}) {
  return (
    <div>
      <h2 className="mb-2 text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Image</h2>
      <label className="block text-xs">
        <span className="text-neutral-500 dark:text-neutral-400">Fit</span>
        <select
          value={config.fit}
          disabled={!isFieldEditable(editableProperties, "fit")}
          onChange={(e) => applyConfig(elementId, config, { ...config, fit: e.target.value as ImageElementConfig["fit"] })}
          className="mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 disabled:bg-neutral-100 disabled:text-neutral-400 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100 dark:disabled:bg-neutral-900 dark:disabled:text-neutral-600"
        >
          <option value="cover">Cover</option>
          <option value="contain">Contain</option>
          <option value="fill">Fill</option>
        </select>
      </label>
    </div>
  );
}
