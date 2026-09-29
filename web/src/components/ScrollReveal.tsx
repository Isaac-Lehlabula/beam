"use client";

import { useInView } from "@/lib/useInView";

/** Wraps children in a div that pops in (bouncy overshoot) the first time it scrolls into view. */
export function ScrollReveal({
  children,
  className = "",
  delayMs = 0,
}: {
  children: React.ReactNode;
  className?: string;
  delayMs?: number;
}) {
  const { ref, inView } = useInView<HTMLDivElement>();

  return (
    <div
      ref={ref}
      className={`scroll-reveal ${inView ? "is-visible" : ""} ${className}`}
      style={inView ? { animationDelay: `${delayMs}ms` } : undefined}
    >
      {children}
    </div>
  );
}
