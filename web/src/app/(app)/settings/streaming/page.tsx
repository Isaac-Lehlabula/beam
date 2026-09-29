"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { StreamingConnectionDto, StreamingProvider } from "@/lib/types";

const OAUTH_STATE_KEY = "beam.streamingOAuth.state";
const OAUTH_PROVIDER_KEY = "beam.streamingOAuth.provider";

export default function StreamingSettingsPage() {
  const [connections, setConnections] = useState<StreamingConnectionDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<StreamingConnectionDto[]>("/integrations/streaming").then(setConnections);
  }, []);

  async function handleConnect(provider: StreamingProvider) {
    setError(null);
    try {
      const state = crypto.randomUUID();
      sessionStorage.setItem(OAUTH_STATE_KEY, state);
      sessionStorage.setItem(OAUTH_PROVIDER_KEY, provider);

      const redirectUri = `${window.location.origin}/settings/streaming/callback`;
      const { url } = await api.get<{ url: string }>(
        `/integrations/streaming/${provider}/authorize-url?redirectUri=${encodeURIComponent(redirectUri)}&state=${state}`,
      );
      window.location.href = url;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to start the connection.");
    }
  }

  async function handleDisconnect(id: string) {
    await api.delete(`/integrations/streaming/${id}`);
    setConnections(await api.get<StreamingConnectionDto[]>("/integrations/streaming"));
  }

  const connectedProviders = new Set(connections?.map((c) => c.provider));

  return (
    <main className="mx-auto max-w-2xl px-6 py-10">
      <h1 className="font-display mb-2 text-2xl font-bold">Streaming destinations</h1>
      <p className="mb-6 text-sm text-neutral-500 dark:text-neutral-400">
        Connect a channel so live viewer/subscriber counts and chat can be synced into Stat Counter elements.
      </p>

      {error && (
        <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950/50 dark:text-red-400">{error}</p>
      )}

      <div className="space-y-3">
        {(["Twitch", "YouTube"] as const).map((provider) => {
          const connection = connections?.find((c) => c.provider === provider);
          return (
            <div
              key={provider}
              className="flex items-center justify-between rounded-md border border-neutral-200 px-4 py-3 dark:border-neutral-800"
            >
              <div>
                <div className="font-medium">{provider}</div>
                {connection && (
                  <div className="text-xs text-neutral-500 dark:text-neutral-400">Connected as {connection.externalAccountName}</div>
                )}
              </div>
              {connection ? (
                <button
                  onClick={() => handleDisconnect(connection.id)}
                  className="text-sm text-red-600 hover:underline dark:text-red-400"
                >
                  Disconnect
                </button>
              ) : (
                <button
                  onClick={() => handleConnect(provider)}
                  disabled={connectedProviders.has(provider)}
                  className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm hover:bg-neutral-50 dark:border-neutral-700 dark:hover:bg-neutral-900"
                >
                  Connect
                </button>
              )}
            </div>
          );
        })}
      </div>

      <p className="mt-6 text-xs text-neutral-400 dark:text-neutral-500">
        Requires this Beam deployment to have its own registered Twitch/Google OAuth app credentials configured server-side.
      </p>
    </main>
  );
}
