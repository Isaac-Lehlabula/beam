import type { ElementConfig, ElementType } from "@/lib/types";

interface ElementDefault {
  label: string;
  width: number;
  height: number;
  config: ElementConfig;
}

/** Everything needed to add one of each element type from the toolbar, with sane starting values. */
export const ELEMENT_DEFAULTS: Partial<Record<ElementType, ElementDefault>> = {
  Text: {
    label: "Text",
    width: 400,
    height: 80,
    config: { content: "New text", fontFamily: "Inter, sans-serif", fontSize: 32, fontWeight: "700", textAlign: "left", color: "#ffffff", backgroundColor: null },
  },
  StatCounter: {
    label: "Stat Counter",
    width: 200,
    height: 100,
    config: { kind: "viewers", label: "Viewers", manualValue: 0, fontFamily: "Inter, sans-serif", fontSize: 32, color: "#ffffff" },
  },
  QrCode: {
    label: "QR Code",
    width: 160,
    height: 160,
    config: { value: "https://example.com", foregroundColor: "#000000", backgroundColor: "#ffffff" },
  },
  Countdown: {
    label: "Countdown",
    width: 300,
    height: 100,
    config: {
      endsAtUtc: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
      label: "Starting in",
      completedText: "We're live!",
      fontFamily: "Inter, sans-serif",
      fontSize: 48,
      color: "#ffffff",
    },
  },
  ProgressBar: {
    label: "Progress Bar",
    width: 500,
    height: 60,
    config: { label: "Sub goal", currentValue: 0, targetValue: 100, barColor: "#22c55e", trackColor: "#374151", textColor: "#ffffff" },
  },
  Ticker: {
    label: "News Ticker",
    width: 1920,
    height: 60,
    config: { items: ["Breaking news goes here"], speedPxPerSecond: 120, backgroundColor: "#000000", textColor: "#ffffff", fontSize: 24 },
  },
  LowerThird: {
    label: "Lower Third",
    width: 600,
    height: 100,
    config: { primaryText: "Jane Doe", secondaryText: "Host", accentColor: "#3b82f6", textColor: "#ffffff", backgroundColor: "rgba(0,0,0,0.75)" },
  },
  Poll: {
    label: "Poll",
    width: 400,
    height: 220,
    config: { question: "Who wins tonight?", options: [{ label: "Option A", votes: 0 }, { label: "Option B", votes: 0 }], accentColor: "#3b82f6", textColor: "#ffffff" },
  },
  Scoreboard: {
    label: "Scoreboard",
    width: 500,
    height: 120,
    config: { teamA: { name: "Team A", score: 0, color: "#1d4ed8" }, teamB: { name: "Team B", score: 0, color: "#b91c1c" }, textColor: "#ffffff" },
  },
  NowPlaying: {
    label: "Now Playing",
    width: 400,
    height: 90,
    config: { track: "Track title", artist: "Artist name", artworkAssetId: null, textColor: "#ffffff" },
  },
  BrowserEmbed: {
    label: "Browser Embed",
    width: 500,
    height: 400,
    config: { url: "https://example.com" },
  },
  Alert: {
    label: "Alert",
    width: 500,
    height: 120,
    config: { message: "New follower!", accentColor: "#a855f7", animationStyle: "fade" },
  },
  Camera: {
    label: "Camera",
    width: 480,
    height: 270,
    config: { label: "Webcam", borderColor: "#3b82f6" },
  },
};
