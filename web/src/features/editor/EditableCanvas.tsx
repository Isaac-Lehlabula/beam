"use client";

import { useRef } from "react";
import Moveable, { type OnDrag, type OnDragEnd, type OnResize, type OnResizeEnd, type OnRotate, type OnRotateEnd } from "react-moveable";
import { SceneStage } from "@/components/scene/SceneStage";
import { toTransform, useEditorStore } from "@/features/editor/store";
import type { ElementDto } from "@/lib/types";

/** Position/size/rotation are always forbidden on a template-locked element -- see Element.EditableProperties. */
function isPositionLocked(element: ElementDto) {
  return element.locked || element.editableProperties !== null;
}

interface EditableCanvasProps {
  resolutionWidth: number;
  resolutionHeight: number;
  scale: number;
}

export function EditableCanvas({ resolutionWidth, resolutionHeight, scale }: EditableCanvasProps) {
  const { elements, selectedId, select, applyTransform } = useEditorStore();
  const elementRefs = useRef<Record<string, HTMLDivElement>>({});

  const selectedElement = elements.find((e) => e.id === selectedId) ?? null;
  const selectedTarget = selectedId ? elementRefs.current[selectedId] : null;

  const handleDrag = ({ target, beforeTranslate }: OnDrag) => {
    if (!selectedElement) return;
    target.style.transform = `translate(${beforeTranslate[0]}px, ${beforeTranslate[1]}px) rotate(${selectedElement.rotation}deg)`;
  };

  const handleDragEnd = ({ lastEvent }: OnDragEnd) => {
    if (!lastEvent || !selectedElement) return;
    const [dx, dy] = lastEvent.beforeTranslate;
    const prev = toTransform(selectedElement);
    applyTransform(selectedElement.id, prev, { ...prev, x: prev.x + dx / scale, y: prev.y + dy / scale });
  };

  const handleResize = ({ target, width, height, drag }: OnResize) => {
    if (!selectedElement) return;
    target.style.width = `${width}px`;
    target.style.height = `${height}px`;
    target.style.transform = `translate(${drag.beforeTranslate[0]}px, ${drag.beforeTranslate[1]}px) rotate(${selectedElement.rotation}deg)`;
  };

  const handleResizeEnd = ({ lastEvent }: OnResizeEnd) => {
    if (!lastEvent || !selectedElement) return;
    const { width, height, drag } = lastEvent;
    const prev = toTransform(selectedElement);
    applyTransform(selectedElement.id, prev, {
      ...prev,
      width: width / scale,
      height: height / scale,
      x: prev.x + drag.beforeTranslate[0] / scale,
      y: prev.y + drag.beforeTranslate[1] / scale,
    });
  };

  const handleRotate = ({ target, beforeRotate }: OnRotate) => {
    target.style.transform = `rotate(${beforeRotate}deg)`;
  };

  const handleRotateEnd = ({ lastEvent }: OnRotateEnd) => {
    if (!lastEvent || !selectedElement) return;
    const prev = toTransform(selectedElement);
    applyTransform(selectedElement.id, prev, { ...prev, rotation: lastEvent.beforeRotate });
  };

  return (
    <div
      className="relative bg-neutral-900"
      style={{ width: resolutionWidth * scale, height: resolutionHeight * scale }}
      onClick={() => select(null)}
    >
      <SceneStage
        elements={elements}
        resolutionWidth={resolutionWidth}
        resolutionHeight={resolutionHeight}
        scale={scale}
        isEditorPreview
        renderElement={(element, style, content) => (
          <div
            ref={(el) => {
              if (el) elementRefs.current[element.id] = el;
            }}
            onClick={(e) => {
              e.stopPropagation();
              select(element.id);
            }}
            style={{
              ...style,
              outline: element.id === selectedId ? "1px solid #3b82f6" : element.editableProperties !== null ? "1px dashed #f59e0b" : "none",
              cursor: isPositionLocked(element) ? "default" : "move",
            }}
          >
            {content}
          </div>
        )}
      />

      {selectedTarget && selectedElement && !isPositionLocked(selectedElement) && (
        <Moveable
          target={selectedTarget}
          draggable
          resizable
          rotatable
          throttleDrag={0}
          throttleResize={0}
          throttleRotate={0}
          keepRatio={false}
          onDrag={handleDrag}
          onDragEnd={handleDragEnd}
          onResize={handleResize}
          onResizeEnd={handleResizeEnd}
          onRotate={handleRotate}
          onRotateEnd={handleRotateEnd}
        />
      )}
    </div>
  );
}
