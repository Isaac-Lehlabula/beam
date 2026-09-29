import { BeamIcon } from "./BeamIcon";

/**
 * The wordmark: the icon rendered large next to the full "Beam" word (set in the display font).
 * The icon is decorative (BeamIcon has an empty alt) so screen readers just read "Beam" once,
 * from the text -- no aria tricks needed.
 */
export function Logo({ className = "", iconClassName = "h-12 w-auto", textClassName = "text-2xl" }: { className?: string; iconClassName?: string; textClassName?: string }) {
  return (
    <span className={`inline-flex items-center gap-2 ${className}`}>
      <BeamIcon className={iconClassName} />
      <span className={`font-display font-bold ${textClassName}`}>Beam</span>
    </span>
  );
}
