"use client";

import { useEffect, useState } from "react";
import QRCode from "qrcode";
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
  TickerElementConfig,
  VideoElementConfig,
} from "@/lib/types";

export function VideoRenderer({ config, assetBaseUrl }: { config: VideoElementConfig; assetBaseUrl: string }) {
  return (
    // eslint-disable-next-line jsx-a11y/media-has-caption -- decorative broadcast graphic, not user-facing media content
    <video
      src={`${assetBaseUrl}/api/assets/${config.assetId}/file`}
      loop={config.loop}
      muted={config.muted}
      autoPlay
      playsInline
      style={{ width: "100%", height: "100%", objectFit: config.fit }}
    />
  );
}

/** Editor-only: the live overlay renders nothing here -- see CameraElementConfig's doc comment. */
export function CameraPlaceholderRenderer({ config, isEditorPreview }: { config: CameraElementConfig; isEditorPreview: boolean }) {
  if (!isEditorPreview) return null;
  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        border: `2px dashed ${config.borderColor}`,
        color: config.borderColor,
        fontFamily: "monospace",
        fontSize: 14,
        background: "rgba(0,0,0,0.2)",
      }}
    >
      🎥 {config.label || "Camera"}
    </div>
  );
}

export function StatCounterRenderer({ config, scale }: { config: StatCounterElementConfig; scale: number }) {
  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        fontFamily: config.fontFamily,
        color: config.color,
      }}
    >
      <div style={{ fontSize: config.fontSize * scale, fontWeight: 700, lineHeight: 1 }}>
        {config.manualValue.toLocaleString()}
      </div>
      <div style={{ fontSize: config.fontSize * scale * 0.4 }}>{config.label}</div>
    </div>
  );
}

export function QrCodeRenderer({ config }: { config: QrCodeElementConfig }) {
  const [dataUrl, setDataUrl] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    QRCode.toDataURL(config.value || " ", { color: { dark: config.foregroundColor, light: config.backgroundColor } })
      .then((url) => !cancelled && setDataUrl(url))
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [config.value, config.foregroundColor, config.backgroundColor]);

  if (!dataUrl) return null;
  // eslint-disable-next-line @next/next/no-img-element -- must render inside OBS's embedded browser
  return <img src={dataUrl} alt="QR code" style={{ width: "100%", height: "100%", objectFit: "contain" }} />;
}

export function CountdownRenderer({ config, scale }: { config: CountdownElementConfig; scale: number }) {
  const [remainingMs, setRemainingMs] = useState(() => new Date(config.endsAtUtc).getTime() - Date.now());

  useEffect(() => {
    const interval = setInterval(() => setRemainingMs(new Date(config.endsAtUtc).getTime() - Date.now()), 1000);
    return () => clearInterval(interval);
  }, [config.endsAtUtc]);

  const totalSeconds = Math.max(0, Math.floor(remainingMs / 1000));
  const hh = Math.floor(totalSeconds / 3600);
  const mm = Math.floor((totalSeconds % 3600) / 60);
  const ss = totalSeconds % 60;
  const display = hh > 0 ? `${hh}:${String(mm).padStart(2, "0")}:${String(ss).padStart(2, "0")}` : `${mm}:${String(ss).padStart(2, "0")}`;

  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        fontFamily: config.fontFamily,
        color: config.color,
      }}
    >
      {config.label && <div style={{ fontSize: config.fontSize * scale * 0.35 }}>{config.label}</div>}
      <div style={{ fontSize: config.fontSize * scale, fontWeight: 700, fontVariantNumeric: "tabular-nums" }}>
        {remainingMs > 0 ? display : config.completedText}
      </div>
    </div>
  );
}

export function ProgressBarRenderer({ config }: { config: ProgressBarElementConfig }) {
  const percent = Math.min(100, Math.max(0, (config.currentValue / config.targetValue) * 100));
  return (
    <div style={{ width: "100%", height: "100%", display: "flex", flexDirection: "column", justifyContent: "center", gap: 4 }}>
      <div style={{ display: "flex", justifyContent: "space-between", fontSize: 14, color: config.textColor }}>
        <span>{config.label}</span>
        <span>
          {config.currentValue.toLocaleString()} / {config.targetValue.toLocaleString()}
        </span>
      </div>
      <div style={{ width: "100%", height: "40%", background: config.trackColor, borderRadius: 999, overflow: "hidden" }}>
        <div style={{ width: `${percent}%`, height: "100%", background: config.barColor, transition: "width 0.5s ease" }} />
      </div>
    </div>
  );
}

export function TickerRenderer({ config, elementId }: { config: TickerElementConfig; elementId: string }) {
  const animationName = `ticker-scroll-${elementId}`;
  const text = config.items.join("      •      ");
  return (
    <div style={{ width: "100%", height: "100%", overflow: "hidden", background: config.backgroundColor, display: "flex", alignItems: "center" }}>
      <style>{`
        @keyframes ${animationName} {
          from { transform: translateX(100%); }
          to { transform: translateX(-100%); }
        }
      `}</style>
      <div
        style={{
          whiteSpace: "nowrap",
          color: config.textColor,
          fontSize: config.fontSize,
          animation: `${animationName} ${Math.max(5, text.length * (100 / config.speedPxPerSecond))}s linear infinite`,
        }}
      >
        {text}
      </div>
    </div>
  );
}

export function LowerThirdRenderer({ config }: { config: LowerThirdElementConfig }) {
  return (
    <div style={{ width: "100%", height: "100%", display: "flex", background: config.backgroundColor, overflow: "hidden" }}>
      <div style={{ width: 8, background: config.accentColor }} />
      <div style={{ display: "flex", flexDirection: "column", justifyContent: "center", padding: "0 16px", color: config.textColor }}>
        <div style={{ fontSize: "1.4em", fontWeight: 700 }}>{config.primaryText}</div>
        {config.secondaryText && <div style={{ fontSize: "1em", opacity: 0.85 }}>{config.secondaryText}</div>}
      </div>
    </div>
  );
}

export function ImageCarouselRenderer({ config, assetBaseUrl }: { config: ImageCarouselElementConfig; assetBaseUrl: string }) {
  const [index, setIndex] = useState(0);

  useEffect(() => {
    if (config.assetIds.length <= 1) return;
    const interval = setInterval(() => setIndex((i) => (i + 1) % config.assetIds.length), config.intervalSeconds * 1000);
    return () => clearInterval(interval);
  }, [config.assetIds.length, config.intervalSeconds]);

  const assetId = config.assetIds[index];
  if (!assetId) return null;
  return (
    // eslint-disable-next-line @next/next/no-img-element -- must render inside OBS's embedded browser
    <img
      src={`${assetBaseUrl}/api/assets/${assetId}/file`}
      alt=""
      style={{ width: "100%", height: "100%", objectFit: config.fit }}
    />
  );
}

export function PollRenderer({ config }: { config: PollElementConfig }) {
  const totalVotes = config.options.reduce((sum, o) => sum + o.votes, 0) || 1;
  return (
    <div style={{ width: "100%", height: "100%", display: "flex", flexDirection: "column", gap: 6, color: config.textColor, justifyContent: "center" }}>
      <div style={{ fontWeight: 700, fontSize: "1.1em" }}>{config.question}</div>
      {config.options.map((option) => (
        <div key={option.label}>
          <div style={{ display: "flex", justifyContent: "space-between", fontSize: "0.85em" }}>
            <span>{option.label}</span>
            <span>{Math.round((option.votes / totalVotes) * 100)}%</span>
          </div>
          <div style={{ width: "100%", height: 8, background: "rgba(255,255,255,0.2)", borderRadius: 999 }}>
            <div
              style={{
                width: `${(option.votes / totalVotes) * 100}%`,
                height: "100%",
                background: config.accentColor,
                borderRadius: 999,
              }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

export function ScoreboardRenderer({ config }: { config: ScoreboardElementConfig }) {
  const teamBox = (team: ScoreboardElementConfig["teamA"]) => (
    <div style={{ flex: 1, display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", background: team.color }}>
      <div style={{ fontSize: "0.9em", color: config.textColor }}>{team.name}</div>
      <div style={{ fontSize: "1.8em", fontWeight: 700, color: config.textColor }}>{team.score}</div>
    </div>
  );
  return (
    <div style={{ width: "100%", height: "100%", display: "flex" }}>
      {teamBox(config.teamA)}
      {teamBox(config.teamB)}
    </div>
  );
}

export function NowPlayingRenderer({ config, assetBaseUrl }: { config: NowPlayingElementConfig; assetBaseUrl: string }) {
  return (
    <div style={{ width: "100%", height: "100%", display: "flex", alignItems: "center", gap: 10, color: config.textColor }}>
      {config.artworkAssetId && (
        // eslint-disable-next-line @next/next/no-img-element -- must render inside OBS's embedded browser
        <img
          src={`${assetBaseUrl}/api/assets/${config.artworkAssetId}/file`}
          alt=""
          style={{ width: "100%", maxWidth: "20%", aspectRatio: "1/1", objectFit: "cover", borderRadius: 4 }}
        />
      )}
      <div style={{ overflow: "hidden" }}>
        <div style={{ fontWeight: 700, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{config.track}</div>
        <div style={{ opacity: 0.8, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{config.artist}</div>
      </div>
    </div>
  );
}

/**
 * Sandbox deliberately omits allow-same-origin and allow-top-navigation: the embedded page can
 * run scripts (needed for most real widgets) but can't read/write cookies or storage as if it
 * were same-origin, can't navigate the parent frame, and can't submit forms. This is the actual
 * XSS mitigation for "custom text/HTML" the brief calls out, not just the https-only validator.
 */
export function BrowserEmbedRenderer({ url }: { url: string }) {
  return (
    <iframe
      src={url}
      sandbox="allow-scripts allow-popups"
      style={{ width: "100%", height: "100%", border: "none" }}
      title="Embedded content"
    />
  );
}

export function AlertRenderer({ config }: { config: AlertElementConfig }) {
  const [entered, setEntered] = useState(false);
  useEffect(() => {
    const id = requestAnimationFrame(() => setEntered(true));
    return () => cancelAnimationFrame(id);
  }, []);

  const base = config.animationStyle === "slide" ? { transform: entered ? "translateY(0)" : "translateY(30px)" } : {};

  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: config.accentColor,
        color: "#fff",
        fontWeight: 700,
        fontSize: "1.3em",
        borderRadius: 8,
        opacity: entered ? 1 : 0,
        transition: "opacity 0.4s ease, transform 0.4s ease",
        ...base,
      }}
    >
      {config.message}
    </div>
  );
}
