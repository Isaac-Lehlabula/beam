import { ObsWebSocketClient } from "@/lib/obs/ObsWebSocketClient";

/**
 * The browser-side equivalent of ARCHITECTURE.md's `IBroadcastSoftwareIntegration` --
 * deliberately a TypeScript interface, not a C# one on the backend, because this connection is
 * browser-to-localhost and never touches our server (see ObsWebSocketClient's doc comment).
 * OBS is the first (and, for now, only) implementation; Streamlabs/XSplit would each get their
 * own class behind this same shape once their control APIs are researched -- see
 * ARCHITECTURE.md §4/§11 for why capability flags exist instead of assuming feature parity.
 */
export interface BroadcastSoftwareCapabilities {
  supportsCreateSource: boolean;
  supportsSceneSwitching: boolean;
  supportsSourceVisibility: boolean;
  supportsStatusMonitoring: boolean;
  supportsStreamingControl: boolean;
  supportsRecordingControl: boolean;
}

export interface BroadcastScene {
  name: string;
  isCurrent: boolean;
}

export interface BroadcastStatus {
  streaming: boolean;
  recording: boolean;
}

export interface IBroadcastSoftwareIntegration {
  readonly providerId: string;
  readonly capabilities: BroadcastSoftwareCapabilities;

  connect(host: string, port: number, password: string): Promise<void>;
  disconnect(): void;
  isConnected(): boolean;

  listScenes(): Promise<BroadcastScene[]>;
  switchScene(sceneName: string): Promise<void>;
  createOrUpdateBrowserSource(sceneName: string, sourceName: string, url: string, width: number, height: number): Promise<void>;
  setSourceVisibility(sceneName: string, sourceName: string, visible: boolean): Promise<void>;
  getStatus(): Promise<BroadcastStatus>;
  startStreaming(): Promise<void>;
  stopStreaming(): Promise<void>;
  startRecording(): Promise<void>;
  stopRecording(): Promise<void>;

  onSceneChanged(listener: (sceneName: string) => void): () => void;
}

export class ObsIntegration implements IBroadcastSoftwareIntegration {
  readonly providerId = "obs";
  readonly capabilities: BroadcastSoftwareCapabilities = {
    supportsCreateSource: true,
    supportsSceneSwitching: true,
    supportsSourceVisibility: true,
    supportsStatusMonitoring: true,
    supportsStreamingControl: true,
    supportsRecordingControl: true,
  };

  private client = new ObsWebSocketClient();

  async connect(host: string, port: number, password: string) {
    await this.client.connect(host, port, password);
  }

  disconnect() {
    this.client.disconnect();
  }

  isConnected() {
    return this.client.isConnected;
  }

  async listScenes(): Promise<BroadcastScene[]> {
    const { scenes, currentProgramSceneName } = await this.client.call<{
      scenes: { sceneName: string }[];
      currentProgramSceneName: string;
    }>("GetSceneList");
    return scenes.map((s) => ({ name: s.sceneName, isCurrent: s.sceneName === currentProgramSceneName })).reverse();
  }

  async switchScene(sceneName: string) {
    await this.client.call("SetCurrentProgramScene", { sceneName });
  }

  /**
   * Creates the browser source if it doesn't exist on the scene yet, or updates its URL if it
   * does -- this is what makes "Connect OBS" a one-click convenience layer on top of the manual
   * copy-paste-into-OBS workflow from Phase 3, which keeps working identically either way.
   */
  async createOrUpdateBrowserSource(sceneName: string, sourceName: string, url: string, width: number, height: number) {
    const { sceneItems } = await this.client.call<{ sceneItems: { sourceName: string; sceneItemId: number }[] }>(
      "GetSceneItemList",
      { sceneName },
    );
    const existing = sceneItems.find((item) => item.sourceName === sourceName);

    if (existing) {
      await this.client.call("SetInputSettings", {
        inputName: sourceName,
        inputSettings: { url, width, height },
      });
      return;
    }

    await this.client.call("CreateInput", {
      sceneName,
      inputName: sourceName,
      inputKind: "browser_source",
      inputSettings: { url, width, height },
      sceneItemEnabled: true,
    });
  }

  async setSourceVisibility(sceneName: string, sourceName: string, visible: boolean) {
    const { sceneItems } = await this.client.call<{ sceneItems: { sourceName: string; sceneItemId: number }[] }>(
      "GetSceneItemList",
      { sceneName },
    );
    const item = sceneItems.find((i) => i.sourceName === sourceName);
    if (!item) throw new Error(`Source '${sourceName}' not found in scene '${sceneName}'.`);

    await this.client.call("SetSceneItemEnabled", {
      sceneName,
      sceneItemId: item.sceneItemId,
      sceneItemEnabled: visible,
    });
  }

  async getStatus(): Promise<BroadcastStatus> {
    const [streamStatus, recordStatus] = await Promise.all([
      this.client.call<{ outputActive: boolean }>("GetStreamStatus"),
      this.client.call<{ outputActive: boolean }>("GetRecordStatus"),
    ]);
    return { streaming: streamStatus.outputActive, recording: recordStatus.outputActive };
  }

  async startStreaming() {
    await this.client.call("StartStream");
  }

  async stopStreaming() {
    await this.client.call("StopStream");
  }

  async startRecording() {
    await this.client.call("StartRecord");
  }

  async stopRecording() {
    await this.client.call("StopRecord");
  }

  onSceneChanged(listener: (sceneName: string) => void) {
    return this.client.onEvent((eventType, data) => {
      if (eventType === "CurrentProgramSceneChanged") {
        listener(data.sceneName as string);
      }
    });
  }
}
