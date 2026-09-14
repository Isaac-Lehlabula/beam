export interface UserResponse {
  id: string;
  email: string;
  displayName: string;
}

export interface ProjectDto {
  id: string;
  name: string;
  resolutionWidth: number;
  resolutionHeight: number;
  organizationId: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SceneDto {
  id: string;
  projectId: string;
  name: string;
  orderIndex: number;
}

export const RESOLUTION_PRESETS = [
  { label: "1080p (1920x1080)", width: 1920, height: 1080 },
  { label: "720p (1280x720)", width: 1280, height: 720 },
] as const;

export type ElementType =
  | "Text"
  | "Image"
  | "Video"
  | "Camera"
  | "StatCounter"
  | "QrCode"
  | "Countdown"
  | "ProgressBar"
  | "Ticker"
  | "LowerThird"
  | "ImageCarousel"
  | "Poll"
  | "Scoreboard"
  | "NowPlaying"
  | "BrowserEmbed"
  | "Alert";

export interface TextElementConfig {
  content: string;
  fontFamily: string;
  fontSize: number;
  fontWeight: string;
  textAlign: "left" | "center" | "right";
  color: string;
  backgroundColor: string | null;
}

export interface ImageElementConfig {
  assetId: string;
  fit: "cover" | "contain" | "fill";
}

export interface VideoElementConfig {
  assetId: string;
  loop: boolean;
  muted: boolean;
  fit: "cover" | "contain" | "fill";
}

/** Renders a labeled placeholder in the editor only -- see SceneStage's Camera branch. */
export interface CameraElementConfig {
  label: string;
  borderColor: string;
}

export interface StatCounterElementConfig {
  kind: "viewers" | "subscribers" | "followers";
  label: string;
  manualValue: number;
  fontFamily: string;
  fontSize: number;
  color: string;
}

export interface QrCodeElementConfig {
  value: string;
  foregroundColor: string;
  backgroundColor: string;
}

export interface CountdownElementConfig {
  endsAtUtc: string;
  label: string;
  completedText: string;
  fontFamily: string;
  fontSize: number;
  color: string;
}

export interface ProgressBarElementConfig {
  label: string;
  currentValue: number;
  targetValue: number;
  barColor: string;
  trackColor: string;
  textColor: string;
}

export interface TickerElementConfig {
  items: string[];
  speedPxPerSecond: number;
  backgroundColor: string;
  textColor: string;
  fontSize: number;
}

export interface LowerThirdElementConfig {
  primaryText: string;
  secondaryText: string;
  accentColor: string;
  textColor: string;
  backgroundColor: string;
}

export interface ImageCarouselElementConfig {
  assetIds: string[];
  intervalSeconds: number;
  fit: "cover" | "contain" | "fill";
}

export interface PollOption {
  label: string;
  votes: number;
}

export interface PollElementConfig {
  question: string;
  options: PollOption[];
  accentColor: string;
  textColor: string;
}

export interface ScoreboardTeam {
  name: string;
  score: number;
  color: string;
}

export interface ScoreboardElementConfig {
  teamA: ScoreboardTeam;
  teamB: ScoreboardTeam;
  textColor: string;
}

export interface NowPlayingElementConfig {
  track: string;
  artist: string;
  artworkAssetId: string | null;
  textColor: string;
}

export interface BrowserEmbedElementConfig {
  url: string;
}

export interface AlertElementConfig {
  message: string;
  accentColor: string;
  animationStyle: "fade" | "slide";
}

export type ElementConfig =
  | TextElementConfig
  | ImageElementConfig
  | VideoElementConfig
  | CameraElementConfig
  | StatCounterElementConfig
  | QrCodeElementConfig
  | CountdownElementConfig
  | ProgressBarElementConfig
  | TickerElementConfig
  | LowerThirdElementConfig
  | ImageCarouselElementConfig
  | PollElementConfig
  | ScoreboardElementConfig
  | NowPlayingElementConfig
  | BrowserEmbedElementConfig
  | AlertElementConfig;

export interface ElementDto {
  id: string;
  sceneId: string;
  type: ElementType;
  name: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  zIndex: number;
  opacity: number;
  locked: boolean;
  visible: boolean;
  config: ElementConfig;
  /** Null = not template-locked. Non-null = the whitelisted config keys that may still change. */
  editableProperties: string[] | null;
}

export interface ElementTransform {
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  zIndex: number;
  opacity: number;
  locked: boolean;
  visible: boolean;
}

export interface AssetDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  url: string;
  createdAtUtc: string;
}

export interface TemplateDto {
  id: string;
  name: string;
  category: string;
  scope: "System" | "Organization";
}

export interface LiveSessionDto {
  projectId: string;
  isActive: boolean;
  currentSceneId: string | null;
  elementVisibilityOverrides: Record<string, boolean>;
}

export type OrganizationRole = "Owner" | "OrgAdmin" | "Streamer" | "Producer" | "Designer" | "Moderator";

export interface OrganizationDto {
  id: string;
  name: string;
  myRole: OrganizationRole;
}

export interface MembershipDto {
  userId: string;
  displayName: string | null;
  role: OrganizationRole;
}

export type StreamingProvider = "Twitch" | "YouTube";

export interface StreamingConnectionDto {
  id: string;
  provider: StreamingProvider;
  externalAccountName: string;
  createdAtUtc: string;
}

export interface ViewerMetrics {
  isLive: boolean;
  viewerCount: number;
  followerCount: number | null;
  subscriberCount: number | null;
}
