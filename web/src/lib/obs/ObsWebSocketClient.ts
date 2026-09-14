/**
 * A minimal client for the obs-websocket v5 protocol (built into OBS Studio 28+, no plugin
 * needed). This connects directly from the browser to `ws://<host>:<port>` on the SAME machine
 * as OBS -- there is no backend involvement and the password never leaves this browser tab.
 * See ARCHITECTURE.md §4/§11 for why that's the deliberate design, not an oversight.
 *
 * Protocol shape (https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md):
 *   1. Server sends `Hello` (op 0) with rpcVersion and, if a password is set, an auth challenge.
 *   2. Client sends `Identify` (op 1) with rpcVersion and (if required) a computed auth digest.
 *   3. Server sends `Identified` (op 2) once accepted.
 *   4. Client sends `Request` (op 6) with a requestId; server replies `RequestResponse` (op 7).
 *   5. Server pushes `Event` (op 5) messages for scene changes, stream state, etc.
 */

const OpCode = {
  Hello: 0,
  Identify: 1,
  Identified: 2,
  Event: 5,
  Request: 6,
  RequestResponse: 7,
} as const;

export class ObsRequestError extends Error {
  constructor(
    public requestType: string,
    public code: number,
    message: string,
  ) {
    super(`OBS request '${requestType}' failed (${code}): ${message}`);
  }
}

interface PendingRequest {
  resolve: (data: Record<string, unknown>) => void;
  reject: (err: Error) => void;
}

export class ObsWebSocketClient {
  private socket: WebSocket | null = null;
  private pending = new Map<string, PendingRequest>();
  private requestCounter = 0;
  private eventListeners = new Set<(eventType: string, data: Record<string, unknown>) => void>();

  async connect(host: string, port: number, password: string): Promise<void> {
    await new Promise<void>((resolve, reject) => {
      const socket = new WebSocket(`ws://${host}:${port}`);
      this.socket = socket;

      socket.onerror = () => reject(new Error("Could not reach OBS. Is OBS running with WebSocket server enabled?"));
      socket.onclose = () => {
        this.pending.forEach((p) => p.reject(new Error("OBS connection closed")));
        this.pending.clear();
      };

      socket.onmessage = async (event) => {
        const message = JSON.parse(event.data);

        if (message.op === OpCode.Hello) {
          const identify: Record<string, unknown> = { rpcVersion: 1 };
          const authInfo = message.d?.authentication;
          if (authInfo) {
            identify.authentication = await computeAuthDigest(password, authInfo.challenge, authInfo.salt);
          }
          socket.send(JSON.stringify({ op: OpCode.Identify, d: identify }));
          return;
        }

        if (message.op === OpCode.Identified) {
          resolve();
          return;
        }

        if (message.op === OpCode.RequestResponse) {
          const { requestId, requestStatus, responseData } = message.d;
          const pending = this.pending.get(requestId);
          this.pending.delete(requestId);
          if (!pending) return;
          if (requestStatus.result) {
            pending.resolve(responseData ?? {});
          } else {
            pending.reject(new ObsRequestError(message.d.requestType, requestStatus.code, requestStatus.comment ?? "Unknown error"));
          }
          return;
        }

        if (message.op === OpCode.Event) {
          this.eventListeners.forEach((listener) => listener(message.d.eventType, message.d.eventData ?? {}));
        }
      };
    });
  }

  disconnect() {
    this.socket?.close();
    this.socket = null;
  }

  get isConnected() {
    return this.socket?.readyState === WebSocket.OPEN;
  }

  onEvent(listener: (eventType: string, data: Record<string, unknown>) => void) {
    this.eventListeners.add(listener);
    return () => this.eventListeners.delete(listener);
  }

  async call<T = Record<string, unknown>>(requestType: string, requestData?: Record<string, unknown>): Promise<T> {
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
      throw new Error("Not connected to OBS.");
    }

    const requestId = `req-${++this.requestCounter}`;
    const responsePromise = new Promise<Record<string, unknown>>((resolve, reject) => {
      this.pending.set(requestId, { resolve, reject });
    });

    this.socket.send(JSON.stringify({ op: OpCode.Request, d: { requestType, requestId, requestData } }));
    return (await responsePromise) as T;
  }
}

/** auth_response = base64(sha256(base64(sha256(password + salt)) + challenge)) -- obs-websocket's documented algorithm. */
async function computeAuthDigest(password: string, challenge: string, salt: string): Promise<string> {
  const secretBytes = await sha256(password + salt);
  const secretBase64 = arrayBufferToBase64(secretBytes);
  const responseBytes = await sha256(secretBase64 + challenge);
  return arrayBufferToBase64(responseBytes);
}

async function sha256(text: string): Promise<ArrayBuffer> {
  return crypto.subtle.digest("SHA-256", new TextEncoder().encode(text));
}

function arrayBufferToBase64(buffer: ArrayBuffer): string {
  return btoa(String.fromCharCode(...new Uint8Array(buffer)));
}
