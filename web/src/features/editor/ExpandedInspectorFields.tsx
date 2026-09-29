"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type {
  AlertElementConfig,
  CameraElementConfig,
  CountdownElementConfig,
  ImageCarouselElementConfig,
  LowerThirdElementConfig,
  NowPlayingElementConfig,
  PollElementConfig,
  ProgressBarElementConfig,
  QrCodeElementConfig,
  ScoreboardElementConfig,
  StatCounterElementConfig,
  StreamingConnectionDto,
  TickerElementConfig,
  VideoElementConfig,
} from "@/lib/types";

const disabledClass = "disabled:bg-neutral-100 disabled:text-neutral-400 dark:disabled:bg-neutral-900 dark:disabled:text-neutral-600";
const fieldClass = "dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100";

function isEditable(editableProperties: string[] | null, key: string) {
  return editableProperties === null || editableProperties.includes(key);
}

function TextRow({ label, value, disabled, onChange }: { label: string; value: string; disabled: boolean; onChange: (v: string) => void }) {
  return (
    <label className="block text-xs">
      <span className="text-neutral-500 dark:text-neutral-400">{label}</span>
      <input value={value} disabled={disabled} onChange={(e) => onChange(e.target.value)} className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 ${fieldClass} ${disabledClass}`} />
    </label>
  );
}

function NumberRow({ label, value, disabled, onChange }: { label: string; value: number; disabled: boolean; onChange: (v: number) => void }) {
  return (
    <label className="block text-xs">
      <span className="text-neutral-500 dark:text-neutral-400">{label}</span>
      <input type="number" value={value} disabled={disabled} onChange={(e) => onChange(Number(e.target.value))} className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 ${fieldClass} ${disabledClass}`} />
    </label>
  );
}

function ColorRow({ label, value, disabled, onChange }: { label: string; value: string; disabled: boolean; onChange: (v: string) => void }) {
  return (
    <label className="block text-xs">
      <span className="text-neutral-500 dark:text-neutral-400">{label}</span>
      <input type="color" value={value.startsWith("#") ? value : "#000000"} disabled={disabled} onChange={(e) => onChange(e.target.value)} className={`mt-0.5 h-8 w-full rounded border border-neutral-300 ${fieldClass} ${disabledClass}`} />
    </label>
  );
}

interface Fields<T> {
  elementId: string;
  config: T;
  applyConfig: (id: string, prev: T, next: T) => void;
  editableProperties: string[] | null;
}

export function StatCounterFields({ elementId, config, applyConfig, editableProperties }: Fields<StatCounterElementConfig>) {
  const set = (patch: Partial<StatCounterElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  const [connections, setConnections] = useState<StreamingConnectionDto[]>([]);
  const [syncing, setSyncing] = useState(false);
  const [syncError, setSyncError] = useState<string | null>(null);

  useEffect(() => {
    api.get<StreamingConnectionDto[]>("/integrations/streaming").then(setConnections).catch(() => {});
  }, []);

  async function handleSync(connection: StreamingConnectionDto) {
    setSyncing(true);
    setSyncError(null);
    try {
      const metrics = await api.get<{ viewerCount: number; followerCount: number | null; subscriberCount: number | null }>(
        `/integrations/streaming/${connection.id}/metrics`,
      );
      const value = config.kind === "subscribers" ? metrics.subscriberCount : config.kind === "followers" ? metrics.followerCount : metrics.viewerCount;
      set({ manualValue: value ?? 0 });
    } catch (err) {
      setSyncError(err instanceof Error ? err.message : "Sync failed.");
    } finally {
      setSyncing(false);
    }
  }

  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Stat Counter</h2>
      <label className="block text-xs">
        <span className="text-neutral-500 dark:text-neutral-400">Kind</span>
        <select value={config.kind} disabled={!editable("kind")} onChange={(e) => set({ kind: e.target.value as StatCounterElementConfig["kind"] })} className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 ${fieldClass} ${disabledClass}`}>
          <option value="viewers">Viewers</option>
          <option value="subscribers">Subscribers</option>
          <option value="followers">Followers</option>
        </select>
      </label>
      <TextRow label="Label" value={config.label} disabled={!editable("label")} onChange={(v) => set({ label: v })} />
      <NumberRow label="Manual value" value={config.manualValue} disabled={!editable("manualValue")} onChange={(v) => set({ manualValue: v })} />
      <ColorRow label="Color" value={config.color} disabled={!editable("color")} onChange={(v) => set({ color: v })} />
      {connections.length > 0 ? (
        <div className="space-y-1">
          {connections.map((c) => (
            <button
              key={c.id}
              onClick={() => handleSync(c)}
              disabled={syncing || !editable("manualValue")}
              className="w-full rounded border border-neutral-300 px-2 py-1 text-xs hover:bg-neutral-50 disabled:opacity-50 dark:border-neutral-700 dark:hover:bg-neutral-900"
            >
              {syncing ? "Syncing..." : `Sync from ${c.provider} (${c.externalAccountName})`}
            </button>
          ))}
          {syncError && <p className="text-xs text-red-600 dark:text-red-400">{syncError}</p>}
        </div>
      ) : (
        <p className="text-xs text-neutral-400 dark:text-neutral-500">
          Connect a channel under Streaming destinations to sync this value from live data.
        </p>
      )}
    </div>
  );
}

export function QrCodeFields({ elementId, config, applyConfig, editableProperties }: Fields<QrCodeElementConfig>) {
  const set = (patch: Partial<QrCodeElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">QR Code</h2>
      <TextRow label="Value / URL" value={config.value} disabled={!editable("value")} onChange={(v) => set({ value: v })} />
      <ColorRow label="Foreground" value={config.foregroundColor} disabled={!editable("foregroundColor")} onChange={(v) => set({ foregroundColor: v })} />
      <ColorRow label="Background" value={config.backgroundColor} disabled={!editable("backgroundColor")} onChange={(v) => set({ backgroundColor: v })} />
    </div>
  );
}

export function CountdownFields({ elementId, config, applyConfig, editableProperties }: Fields<CountdownElementConfig>) {
  const set = (patch: Partial<CountdownElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Countdown</h2>
      <label className="block text-xs">
        <span className="text-neutral-500 dark:text-neutral-400">Ends at</span>
        <input
          type="datetime-local"
          value={new Date(config.endsAtUtc).toISOString().slice(0, 16)}
          disabled={!editable("endsAtUtc")}
          onChange={(e) => set({ endsAtUtc: new Date(e.target.value).toISOString() })}
          className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 ${fieldClass} ${disabledClass}`}
        />
      </label>
      <TextRow label="Label" value={config.label} disabled={!editable("label")} onChange={(v) => set({ label: v })} />
      <TextRow label="Completed text" value={config.completedText} disabled={!editable("completedText")} onChange={(v) => set({ completedText: v })} />
      <ColorRow label="Color" value={config.color} disabled={!editable("color")} onChange={(v) => set({ color: v })} />
    </div>
  );
}

export function ProgressBarFields({ elementId, config, applyConfig, editableProperties }: Fields<ProgressBarElementConfig>) {
  const set = (patch: Partial<ProgressBarElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Progress Bar</h2>
      <TextRow label="Label" value={config.label} disabled={!editable("label")} onChange={(v) => set({ label: v })} />
      <NumberRow label="Current" value={config.currentValue} disabled={!editable("currentValue")} onChange={(v) => set({ currentValue: v })} />
      <NumberRow label="Target" value={config.targetValue} disabled={!editable("targetValue")} onChange={(v) => set({ targetValue: v })} />
      <ColorRow label="Bar color" value={config.barColor} disabled={!editable("barColor")} onChange={(v) => set({ barColor: v })} />
      <ColorRow label="Track color" value={config.trackColor} disabled={!editable("trackColor")} onChange={(v) => set({ trackColor: v })} />
    </div>
  );
}

export function TickerFields({ elementId, config, applyConfig, editableProperties }: Fields<TickerElementConfig>) {
  const set = (patch: Partial<TickerElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Ticker</h2>
      <label className="block text-xs">
        <span className="text-neutral-500 dark:text-neutral-400">Items (one per line)</span>
        <textarea
          value={config.items.join("\n")}
          disabled={!editable("items")}
          onChange={(e) => set({ items: e.target.value.split("\n").filter(Boolean) })}
          rows={4}
          className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 text-sm ${fieldClass} ${disabledClass}`}
        />
      </label>
      <NumberRow label="Speed (px/s)" value={config.speedPxPerSecond} disabled={!editable("speedPxPerSecond")} onChange={(v) => set({ speedPxPerSecond: v })} />
      <ColorRow label="Background" value={config.backgroundColor} disabled={!editable("backgroundColor")} onChange={(v) => set({ backgroundColor: v })} />
      <ColorRow label="Text color" value={config.textColor} disabled={!editable("textColor")} onChange={(v) => set({ textColor: v })} />
    </div>
  );
}

export function LowerThirdFields({ elementId, config, applyConfig, editableProperties }: Fields<LowerThirdElementConfig>) {
  const set = (patch: Partial<LowerThirdElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Lower Third</h2>
      <TextRow label="Primary text (name)" value={config.primaryText} disabled={!editable("primaryText")} onChange={(v) => set({ primaryText: v })} />
      <TextRow label="Secondary text (title)" value={config.secondaryText} disabled={!editable("secondaryText")} onChange={(v) => set({ secondaryText: v })} />
      <ColorRow label="Accent" value={config.accentColor} disabled={!editable("accentColor")} onChange={(v) => set({ accentColor: v })} />
      <ColorRow label="Text color" value={config.textColor} disabled={!editable("textColor")} onChange={(v) => set({ textColor: v })} />
    </div>
  );
}

export function ImageCarouselFields({ config }: Fields<ImageCarouselElementConfig>) {
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Image Carousel</h2>
      <p className="text-xs text-neutral-500 dark:text-neutral-400">{config.assetIds.length} image(s), rotating every {config.intervalSeconds}s. Use &quot;+ Carousel&quot; to replace the set.</p>
    </div>
  );
}

export function PollFields({ elementId, config, applyConfig, editableProperties }: Fields<PollElementConfig>) {
  const set = (patch: Partial<PollElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Poll</h2>
      <TextRow label="Question" value={config.question} disabled={!editable("question")} onChange={(v) => set({ question: v })} />
      {config.options.map((opt, i) => (
        <div key={i} className="flex gap-1">
          <input
            value={opt.label}
            disabled={!editable("options")}
            onChange={(e) => set({ options: config.options.map((o, j) => (j === i ? { ...o, label: e.target.value } : o)) })}
            className={`w-2/3 rounded border border-neutral-300 px-2 py-1 text-xs ${fieldClass} ${disabledClass}`}
          />
          <input
            type="number"
            value={opt.votes}
            disabled={!editable("options")}
            onChange={(e) => set({ options: config.options.map((o, j) => (j === i ? { ...o, votes: Number(e.target.value) } : o)) })}
            className={`w-1/3 rounded border border-neutral-300 px-2 py-1 text-xs ${fieldClass} ${disabledClass}`}
          />
        </div>
      ))}
      {editable("options") && (
        <button
          onClick={() => set({ options: [...config.options, { label: "New option", votes: 0 }] })}
          className="text-primary-ink dark:text-primary text-xs hover:underline"
        >
          + Add option
        </button>
      )}
      <ColorRow label="Accent" value={config.accentColor} disabled={!editable("accentColor")} onChange={(v) => set({ accentColor: v })} />
    </div>
  );
}

export function ScoreboardFields({ elementId, config, applyConfig, editableProperties }: Fields<ScoreboardElementConfig>) {
  const set = (patch: Partial<ScoreboardElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Scoreboard</h2>
      <TextRow label="Team A name" value={config.teamA.name} disabled={!editable("teamA")} onChange={(v) => set({ teamA: { ...config.teamA, name: v } })} />
      <NumberRow label="Team A score" value={config.teamA.score} disabled={!editable("teamA")} onChange={(v) => set({ teamA: { ...config.teamA, score: v } })} />
      <ColorRow label="Team A color" value={config.teamA.color} disabled={!editable("teamA")} onChange={(v) => set({ teamA: { ...config.teamA, color: v } })} />
      <TextRow label="Team B name" value={config.teamB.name} disabled={!editable("teamB")} onChange={(v) => set({ teamB: { ...config.teamB, name: v } })} />
      <NumberRow label="Team B score" value={config.teamB.score} disabled={!editable("teamB")} onChange={(v) => set({ teamB: { ...config.teamB, score: v } })} />
      <ColorRow label="Team B color" value={config.teamB.color} disabled={!editable("teamB")} onChange={(v) => set({ teamB: { ...config.teamB, color: v } })} />
    </div>
  );
}

export function NowPlayingFields({ elementId, config, applyConfig, editableProperties }: Fields<NowPlayingElementConfig>) {
  const set = (patch: Partial<NowPlayingElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Now Playing</h2>
      <TextRow label="Track" value={config.track} disabled={!editable("track")} onChange={(v) => set({ track: v })} />
      <TextRow label="Artist" value={config.artist} disabled={!editable("artist")} onChange={(v) => set({ artist: v })} />
      <ColorRow label="Text color" value={config.textColor} disabled={!editable("textColor")} onChange={(v) => set({ textColor: v })} />
    </div>
  );
}

export function BrowserEmbedFields({ elementId, config, applyConfig, editableProperties }: Fields<{ url: string }>) {
  const editable = isEditable(editableProperties, "url");
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Browser Embed</h2>
      <TextRow label="URL (https only)" value={config.url} disabled={!editable} onChange={(v) => applyConfig(elementId, config, { ...config, url: v })} />
    </div>
  );
}

export function AlertFields({ elementId, config, applyConfig, editableProperties }: Fields<AlertElementConfig>) {
  const set = (patch: Partial<AlertElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Alert</h2>
      <TextRow label="Message" value={config.message} disabled={!editable("message")} onChange={(v) => set({ message: v })} />
      <ColorRow label="Accent" value={config.accentColor} disabled={!editable("accentColor")} onChange={(v) => set({ accentColor: v })} />
      <label className="block text-xs">
        <span className="text-neutral-500 dark:text-neutral-400">Animation</span>
        <select value={config.animationStyle} disabled={!editable("animationStyle")} onChange={(e) => set({ animationStyle: e.target.value as AlertElementConfig["animationStyle"] })} className={`mt-0.5 w-full rounded border border-neutral-300 px-2 py-1 ${fieldClass} ${disabledClass}`}>
          <option value="fade">Fade</option>
          <option value="slide">Slide</option>
        </select>
      </label>
      <p className="text-xs text-neutral-400 dark:text-neutral-500">Show/hide this from Live Control to trigger it during a broadcast.</p>
    </div>
  );
}

export function CameraFields({ elementId, config, applyConfig, editableProperties }: Fields<CameraElementConfig>) {
  const set = (patch: Partial<CameraElementConfig>) => applyConfig(elementId, config, { ...config, ...patch });
  const editable = (k: string) => isEditable(editableProperties, k);
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Camera Placeholder</h2>
      <TextRow label="Label" value={config.label} disabled={!editable("label")} onChange={(v) => set({ label: v })} />
      <ColorRow label="Border color" value={config.borderColor} disabled={!editable("borderColor")} onChange={(v) => set({ borderColor: v })} />
      <p className="text-xs text-neutral-400 dark:text-neutral-500">Design-time only -- invisible in the live overlay. Point a camera source (OBS, Streamlabs, etc.) at this area.</p>
    </div>
  );
}

export function VideoFields({ config }: Fields<VideoElementConfig>) {
  return (
    <div className="space-y-2">
      <h2 className="text-xs font-semibold uppercase text-neutral-500 dark:text-neutral-400">Video</h2>
      <p className="text-xs text-neutral-500 dark:text-neutral-400">Loop: {config.loop ? "on" : "off"} · Muted: {config.muted ? "on" : "off"} · Fit: {config.fit}</p>
    </div>
  );
}
