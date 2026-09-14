import { NextResponse } from "next/server";

/**
 * The overlay/control pages connect to the API directly (see next.config.ts's comment on why
 * REST calls from the authenticated app go through a same-origin rewrite instead). Reading
 * this env var here, in a server route handler, means it's resolved at request time -- unlike
 * a NEXT_PUBLIC_* variable, which Next.js would bake into the client bundle at build time and
 * can't be changed by docker-compose's runtime environment without a rebuild.
 */
export function GET() {
  return NextResponse.json({ apiUrl: process.env.BEAM_API_PUBLIC_URL ?? "http://localhost:5080" });
}
