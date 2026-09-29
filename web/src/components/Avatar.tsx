function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();
  return (parts[0]![0] + parts[parts.length - 1]![0]).toUpperCase();
}

/** A simple initials avatar -- no photo upload exists yet, so this is the profile "icon" everywhere a user needs one. */
export function Avatar({ name, className = "h-8 w-8 text-sm" }: { name: string; className?: string }) {
  return (
    <span
      className={`bg-primary inline-flex shrink-0 items-center justify-center rounded-full font-semibold text-white ${className}`}
      title={name}
    >
      {initials(name)}
    </span>
  );
}
