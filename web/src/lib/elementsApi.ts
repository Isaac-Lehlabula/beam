import { api } from "@/lib/api";
import type { AssetDto, ElementConfig, ElementDto, ElementTransform, ElementType } from "@/lib/types";

export const elementsApi = {
  list: (sceneId: string) => api.get<ElementDto[]>(`/scenes/${sceneId}/elements`),

  create: (
    sceneId: string,
    body: { type: ElementType; name: string; x: number; y: number; width: number; height: number; config: ElementConfig },
  ) => api.post<ElementDto>(`/scenes/${sceneId}/elements`, body),

  updateTransform: (sceneId: string, elementId: string, transform: ElementTransform) =>
    api.put<ElementDto>(`/scenes/${sceneId}/elements/${elementId}/transform`, transform),

  duplicate: (sceneId: string, elementId: string) => api.post<ElementDto>(`/scenes/${sceneId}/elements/${elementId}/duplicate`),

  updateConfig: (sceneId: string, elementId: string, config: ElementConfig) =>
    api.put<ElementDto>(`/scenes/${sceneId}/elements/${elementId}/config`, { config }),

  remove: (sceneId: string, elementId: string) => api.delete(`/scenes/${sceneId}/elements/${elementId}`),
};

export const assetsApi = {
  list: () => api.get<AssetDto[]>("/assets"),
  upload: async (file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    const response = await fetch("/api/assets", { method: "POST", body: formData });
    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      throw new Error(problem?.detail ?? "Upload failed.");
    }
    return (await response.json()) as AssetDto;
  },
};
