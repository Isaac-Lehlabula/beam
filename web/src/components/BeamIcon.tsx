/**
 * The Beam mark: the approved logo asset (public/beam-icon.png, transparent background) --
 * a vertical stem fused with a rounded "play" triangle, with a pause glyph cut into it.
 * Fixed flat teal by design (baked into the asset, not currentColor-tintable), which is fine:
 * it reads cleanly on both light and dark app backgrounds.
 */
export function BeamIcon({ className = "" }: { className?: string }) {
  // eslint-disable-next-line @next/next/no-img-element -- fixed brand asset, not a responsive/optimized content image
  return <img src="/beam-icon.png" alt="" className={className} />;
}
