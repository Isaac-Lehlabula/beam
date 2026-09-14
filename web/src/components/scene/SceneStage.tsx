import { cloneElement, isValidElement, type CSSProperties, type ReactElement } from "react";
import type {
  AlertElementConfig,
  BrowserEmbedElementConfig,
  CameraElementConfig,
  CountdownElementConfig,
  ElementDto,
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
  AlertRenderer,
  BrowserEmbedRenderer,
  CameraPlaceholderRenderer,
  CountdownRenderer,
  ImageCarouselRenderer,
  LowerThirdRenderer,
  NowPlayingRenderer,
  PollRenderer,
  ProgressBarRenderer,
  QrCodeRenderer,
  ScoreboardRenderer,
  StatCounterRenderer,
  TickerRenderer,
  VideoRenderer,
} from "@/components/scene/ExpandedElementRenderers";

interface SceneStageProps {
  elements: ElementDto[];
  resolutionWidth: number;
  resolutionHeight: number;
  /** Editor renders at a fitted scale; the overlay (Phase 3) always renders at 1 (native resolution). */
  scale?: number;
  /**
   * Lets the editor attach ref/click/selection behavior directly on the positioned element
   * (not a nested wrapper -- the returned node's root IS the absolutely-positioned element,
   * so `style` must be applied to it). Defaults to a plain positioned div.
   */
  renderElement?: (element: ElementDto, style: CSSProperties, content: React.ReactNode) => ReactElement;
  /** The editor is same-origin via next.config.ts's rewrite, so "" (relative) is correct there.
   *  The overlay talks to the API directly (see app/config/route.ts) and must pass its origin. */
  assetBaseUrl?: string;
  /** True only for <EditableCanvas>: lets design-time-only elements (Camera) render a placeholder. */
  isEditorPreview?: boolean;
}

/**
 * The one place that turns Scene/Element data into pixels. Both the editor's <EditableCanvas>
 * and the public overlay page render through this, so what the designer sees is guaranteed to
 * be what ends up in the broadcast -- see ARCHITECTURE.md "Frontend Architecture".
 */
export function SceneStage({
  elements,
  resolutionWidth,
  resolutionHeight,
  scale = 1,
  renderElement,
  assetBaseUrl = "",
  isEditorPreview = false,
}: SceneStageProps) {
  const sorted = [...elements].sort((a, b) => a.zIndex - b.zIndex);

  return (
    <div
      style={{
        position: "relative",
        width: resolutionWidth * scale,
        height: resolutionHeight * scale,
        background: "transparent",
        overflow: "hidden",
      }}
    >
      {sorted.map((element) => {
        if (!element.visible) return null;

        const style: CSSProperties = {
          position: "absolute",
          left: element.x * scale,
          top: element.y * scale,
          width: element.width * scale,
          height: element.height * scale,
          transform: `rotate(${element.rotation}deg)`,
          opacity: element.opacity,
        };
        const content = (
          <ElementContent element={element} scale={scale} assetBaseUrl={assetBaseUrl} isEditorPreview={isEditorPreview} />
        );

        const node = renderElement ? renderElement(element, style, content) : <div style={style}>{content}</div>;
        return isValidElement(node) ? cloneElement(node, { key: element.id }) : null;
      })}
    </div>
  );
}

function ElementContent({
  element,
  scale,
  assetBaseUrl,
  isEditorPreview,
}: {
  element: ElementDto;
  scale: number;
  assetBaseUrl: string;
  isEditorPreview: boolean;
}) {
  switch (element.type) {
    case "Text": {
      const config = element.config as TextElementConfig;
      return (
        <div
          style={{
            width: "100%",
            height: "100%",
            display: "flex",
            alignItems: "center",
            justifyContent:
              config.textAlign === "center" ? "center" : config.textAlign === "right" ? "flex-end" : "flex-start",
            fontFamily: config.fontFamily,
            fontSize: config.fontSize * scale,
            fontWeight: config.fontWeight,
            color: config.color,
            background: config.backgroundColor ?? "transparent",
            textAlign: config.textAlign,
            whiteSpace: "pre-wrap",
            overflow: "hidden",
          }}
        >
          {config.content}
        </div>
      );
    }
    case "Image": {
      const config = element.config as ImageElementConfig;
      return (
        // eslint-disable-next-line @next/next/no-img-element -- overlay must render inside OBS's
        // embedded browser without Next's image optimizer/server in the loop.
        <img
          src={`${assetBaseUrl}/api/assets/${config.assetId}/file`}
          alt={element.name}
          style={{ width: "100%", height: "100%", objectFit: config.fit }}
          draggable={false}
        />
      );
    }
    case "Video":
      return <VideoRenderer config={element.config as VideoElementConfig} assetBaseUrl={assetBaseUrl} />;
    case "Camera":
      return <CameraPlaceholderRenderer config={element.config as CameraElementConfig} isEditorPreview={isEditorPreview} />;
    case "StatCounter":
      return <StatCounterRenderer config={element.config as StatCounterElementConfig} scale={scale} />;
    case "QrCode":
      return <QrCodeRenderer config={element.config as QrCodeElementConfig} />;
    case "Countdown":
      return <CountdownRenderer config={element.config as CountdownElementConfig} scale={scale} />;
    case "ProgressBar":
      return <ProgressBarRenderer config={element.config as ProgressBarElementConfig} />;
    case "Ticker":
      return <TickerRenderer config={element.config as TickerElementConfig} elementId={element.id} />;
    case "LowerThird":
      return <LowerThirdRenderer config={element.config as LowerThirdElementConfig} />;
    case "ImageCarousel":
      return <ImageCarouselRenderer config={element.config as ImageCarouselElementConfig} assetBaseUrl={assetBaseUrl} />;
    case "Poll":
      return <PollRenderer config={element.config as PollElementConfig} />;
    case "Scoreboard":
      return <ScoreboardRenderer config={element.config as ScoreboardElementConfig} />;
    case "NowPlaying":
      return <NowPlayingRenderer config={element.config as NowPlayingElementConfig} assetBaseUrl={assetBaseUrl} />;
    case "BrowserEmbed":
      return <BrowserEmbedRenderer url={(element.config as BrowserEmbedElementConfig).url} />;
    case "Alert":
      return <AlertRenderer config={element.config as AlertElementConfig} />;
    default:
      return null;
  }
}
