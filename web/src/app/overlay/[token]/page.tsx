"use client";

import { useParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { SceneStage } from "@/components/scene/SceneStage";
import type { ElementDto } from "@/lib/types";

interface OverlaySnapshot {
  sceneId: string;
  resolutionWidth: number;
  resolutionHeight: number;
  elements: ElementDto[];
}

/**
 * Public, unauthenticated, meant to be pasted into OBS (or any Browser Source host) as-is.
 * Always does a full REST re-fetch on load AND on every reconnect -- never assumes the
 * SignalR event stream picks up where it left off. See ARCHITECTURE.md "Overlay Rendering
 * Architecture" for why that's the actual reload-safety requirement, not an implementation detail.
 */
export default function OverlayPage() {
  const { token } = useParams<{ token: string }>();
  const [apiUrl, setApiUrl] = useState<string | null>(null);
  const [snapshot, setSnapshot] = useState<OverlaySnapshot | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    // The root layout's body has an opaque background (fine for the authenticated app); a
    // Browser Source needs a transparent one to composite over other sources. Inline styles
    // win over the stylesheet regardless of specificity, so this is reliable.
    document.documentElement.style.background = "transparent";
    document.body.style.background = "transparent";
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function init() {
      const { apiUrl: resolvedApiUrl } = await fetch("/config").then((r) => r.json());
      if (cancelled) return;
      setApiUrl(resolvedApiUrl);

      const fetchSnapshot = async () => {
        const res = await fetch(`${resolvedApiUrl}/api/overlay/${token}/state`);
        if (res.ok && !cancelled) setSnapshot(await res.json());
      };

      await fetchSnapshot();

      const connection = new signalR.HubConnectionBuilder()
        // withCredentials: false -- this connection is anonymous/token-based by design (no
        // cookies), which is also what makes the backend's wildcard CORS policy valid; browsers
        // reject Access-Control-Allow-Origin: * combined with a credentialed request.
        .withUrl(`${resolvedApiUrl}/hubs/live`, { withCredentials: false })
        .withAutomaticReconnect()
        .build();

      connection.on("sceneChanged", fetchSnapshot);
      // Full resync on every (re)connect, not just the first one -- a Browser Source can drop
      // and re-establish its connection at any time (network blip, OBS scene deactivate/reactivate).
      connection.onreconnected(() => {
        connection.invoke("JoinOverlay", token).catch(() => {});
        fetchSnapshot();
      });

      await connection.start();
      await connection.invoke("JoinOverlay", token);
      connectionRef.current = connection;
    }

    init();

    return () => {
      cancelled = true;
      connectionRef.current?.stop();
    };
  }, [token]);

  if (!snapshot || !apiUrl) {
    // Transparent, not a loading spinner: this sits over live video in a compositor.
    return null;
  }

  return (
    <SceneStage
      elements={snapshot.elements}
      resolutionWidth={snapshot.resolutionWidth}
      resolutionHeight={snapshot.resolutionHeight}
      scale={1}
      assetBaseUrl={apiUrl}
    />
  );
}
