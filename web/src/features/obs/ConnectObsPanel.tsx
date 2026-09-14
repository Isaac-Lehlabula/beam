"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { ObsIntegration, type BroadcastScene, type BroadcastStatus } from "@/lib/obs/ObsIntegration";

const CONNECTION_PREFS_KEY = "beam.obs.connectionPrefs";

interface ConnectionPrefs {
  host: string;
  port: number;
}

/**
 * Runs entirely in this browser tab, talking directly to OBS on localhost -- see
 * ObsWebSocketClient's doc comment for why. Only usable when this tab is open on the same
 * machine as OBS; a remote producer would need a different mechanism (not built yet, see
 * ARCHITECTURE.md §4's "cross-machine control" limitation).
 */
export function ConnectObsPanel({ projectId, resolutionWidth, resolutionHeight }: { projectId: string; resolutionWidth: number; resolutionHeight: number }) {
  const [obs] = useState(() => new ObsIntegration());
  const [connected, setConnected] = useState(false);
  const [connecting, setConnecting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [host, setHost] = useState("localhost");
  const [port, setPort] = useState(4455);
  const [password, setPassword] = useState("");
  const [scenes, setScenes] = useState<BroadcastScene[]>([]);
  const [status, setStatus] = useState<BroadcastStatus | null>(null);
  const [sourceMessage, setSourceMessage] = useState<string | null>(null);

  useEffect(() => {
    const saved = localStorage.getItem(CONNECTION_PREFS_KEY);
    if (saved) {
      try {
        const prefs: ConnectionPrefs = JSON.parse(saved);
        setHost(prefs.host);
        setPort(prefs.port);
      } catch {
        // ignore malformed local storage
      }
    }
  }, []);

  async function refreshScenesAndStatus() {
    setScenes(await obs.listScenes());
    setStatus(await obs.getStatus());
  }

  async function handleConnect() {
    setConnecting(true);
    setError(null);
    try {
      await obs.connect(host, port, password);
      localStorage.setItem(CONNECTION_PREFS_KEY, JSON.stringify({ host, port } satisfies ConnectionPrefs));
      setConnected(true);
      await refreshScenesAndStatus();
      obs.onSceneChanged(() => refreshScenesAndStatus());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to connect to OBS.");
    } finally {
      setConnecting(false);
    }
  }

  function handleDisconnect() {
    obs.disconnect();
    setConnected(false);
    setScenes([]);
    setStatus(null);
  }

  async function handleCreateBrowserSource(sceneName: string) {
    setSourceMessage(null);
    const { overlayPath } = await api.get<{ overlayPath: string }>(`/projects/${projectId}/overlay-token`);
    const { apiUrl } = await fetch("/config").then((r) => r.json());
    await obs.createOrUpdateBrowserSource(sceneName, "Beam Overlay", `${apiUrl}${overlayPath}`, resolutionWidth, resolutionHeight);
    setSourceMessage(`Browser source added to "${sceneName}".`);
  }

  if (!connected) {
    return (
      <div className="rounded-md border border-neutral-200 p-3">
        <h2 className="mb-2 text-sm font-medium">Connect OBS</h2>
        <p className="mb-2 text-xs text-neutral-500">
          Requires OBS Studio 28+ running on this computer, with Tools &rarr; WebSocket Server Settings enabled.
        </p>
        <div className="flex flex-wrap gap-2">
          <input value={host} onChange={(e) => setHost(e.target.value)} placeholder="Host" className="w-28 rounded border border-neutral-300 px-2 py-1 text-sm" />
          <input type="number" value={port} onChange={(e) => setPort(Number(e.target.value))} placeholder="Port" className="w-20 rounded border border-neutral-300 px-2 py-1 text-sm" />
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password (if set)" className="flex-1 rounded border border-neutral-300 px-2 py-1 text-sm" />
          <button onClick={handleConnect} disabled={connecting} className="rounded-md bg-black px-3 py-1.5 text-sm text-white hover:bg-neutral-800 disabled:opacity-50">
            {connecting ? "Connecting..." : "Connect"}
          </button>
        </div>
        {error && <p className="mt-2 text-xs text-red-600">{error}</p>}
      </div>
    );
  }

  return (
    <div className="rounded-md border border-neutral-200 p-3">
      <div className="mb-2 flex items-center justify-between">
        <h2 className="text-sm font-medium">OBS connected</h2>
        <div className="flex items-center gap-3 text-xs">
          {status && (
            <span className="text-neutral-500">
              {status.streaming ? "🔴 Streaming" : "Not streaming"} · {status.recording ? "⏺ Recording" : "Not recording"}
            </span>
          )}
          <button onClick={handleDisconnect} className="text-neutral-500 underline hover:text-black">
            Disconnect
          </button>
        </div>
      </div>

      <ul className="space-y-1">
        {scenes.map((scene) => (
          <li key={scene.name} className="flex items-center justify-between rounded border border-neutral-200 px-2 py-1.5 text-sm">
            <span className={scene.isCurrent ? "font-medium" : ""}>
              {scene.isCurrent && "▶ "}
              {scene.name}
            </span>
            <div className="flex gap-2 text-xs">
              <button onClick={() => obs.switchScene(scene.name)} className="text-blue-600 hover:underline">
                Switch
              </button>
              <button onClick={() => handleCreateBrowserSource(scene.name)} className="text-blue-600 hover:underline">
                Add Beam overlay
              </button>
            </div>
          </li>
        ))}
      </ul>
      {sourceMessage && <p className="mt-2 text-xs text-green-700">{sourceMessage}</p>}
    </div>
  );
}
