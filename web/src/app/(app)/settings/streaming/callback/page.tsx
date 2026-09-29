"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { StreamingConnectionDto, StreamingProvider } from "@/lib/types";

const OAUTH_STATE_KEY = "beam.streamingOAuth.state";
const OAUTH_PROVIDER_KEY = "beam.streamingOAuth.provider";

/**
 * CSRF protection for the OAuth flow happens entirely here, client-side: the state value
 * generated before redirecting to the provider is compared against what the provider hands
 * back. The backend never sees or validates `state` -- see OAuthCallbackRequest's doc comment.
 */
export default function StreamingOAuthCallbackPage() {
  const searchParams = useSearchParams();
  const [status, setStatus] = useState<"working" | "success" | "error">("working");
  const [message, setMessage] = useState("Connecting...");

  useEffect(() => {
    async function run() {
      const code = searchParams.get("code");
      const returnedState = searchParams.get("state");
      const providerError = searchParams.get("error");

      if (providerError) {
        setStatus("error");
        setMessage(`The provider declined the request: ${providerError}`);
        return;
      }

      const expectedState = sessionStorage.getItem(OAUTH_STATE_KEY);
      const provider = sessionStorage.getItem(OAUTH_PROVIDER_KEY) as StreamingProvider | null;
      sessionStorage.removeItem(OAUTH_STATE_KEY);
      sessionStorage.removeItem(OAUTH_PROVIDER_KEY);

      if (!code || !provider || !returnedState || returnedState !== expectedState) {
        setStatus("error");
        setMessage("This connection request could not be verified. Please try connecting again.");
        return;
      }

      try {
        const redirectUri = `${window.location.origin}/settings/streaming/callback`;
        const connection = await api.post<StreamingConnectionDto>(`/integrations/streaming/${provider}/callback`, {
          code,
          redirectUri,
        });
        setStatus("success");
        setMessage(`Connected as ${connection.externalAccountName}.`);
      } catch (err) {
        setStatus("error");
        setMessage(err instanceof Error ? err.message : "Failed to complete the connection.");
      }
    }

    run();
  }, [searchParams]);

  return (
    <main className="mx-auto max-w-md px-6 py-16 text-center">
      <p className={`mb-4 text-sm ${status === "error" ? "text-red-600 dark:text-red-400" : "text-neutral-600 dark:text-neutral-300"}`}>
        {message}
      </p>
      {status !== "working" && (
        <Link href="/settings/streaming" className="text-primary-ink dark:text-primary text-sm hover:underline">
          Back to streaming destinations
        </Link>
      )}
    </main>
  );
}
