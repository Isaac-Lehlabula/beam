"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ThemeToggle } from "@/components/theme/ThemeToggle";
import { ScrollReveal } from "@/components/ScrollReveal";
import { Logo } from "@/components/Logo";

interface HealthResponse {
  status: string;
  app: string;
  utc: string;
}

const FEATURES = [
  {
    emoji: "🎨",
    title: "Design it your way",
    body: "Drag, drop, resize, rotate -- a full visual editor for your overlays. No code, no fuss, just your vibe.",
    accent: "bg-primary",
  },
  {
    emoji: "📡",
    title: "Beams straight to your broadcast software",
    body: "One Browser Source, live forever, in OBS, Streamlabs, etc. Edit a color or swap a scene and it updates instantly -- no re-adding sources.",
    accent: "bg-secondary",
  },
  {
    emoji: "✨",
    title: "AI starts you off",
    body: "Describe your show in a sentence and get a full overlay set back, ready to tweak and make your own.",
    accent: "bg-primary",
  },
  {
    emoji: "🧩",
    title: "Templates for every vibe",
    body: "Gaming, podcast, just chatting, corporate -- start from a template your whole crew can reuse.",
    accent: "bg-secondary",
  },
] as const;

// Streamer-y confetti drifting around the hero -- purely decorative, each with its own
// position/rotation/timing so the cluster doesn't read as a single repeating tile.
const PARTICLES = [
  { emoji: "🎮", top: "8%", left: "10%", duration: "5s", delay: "0s", size: "text-3xl", rotA: "-10deg", rotB: "12deg" },
  { emoji: "🎤", top: "18%", left: "85%", duration: "4.2s", delay: "-1s", size: "text-2xl", rotA: "8deg", rotB: "-10deg" },
  { emoji: "🔥", top: "62%", left: "4%", duration: "4.8s", delay: "-2.3s", size: "text-2xl", rotA: "-6deg", rotB: "10deg" },
  { emoji: "💬", top: "5%", left: "45%", duration: "5.5s", delay: "-0.6s", size: "text-2xl", rotA: "10deg", rotB: "-8deg" },
  { emoji: "⭐", top: "70%", left: "80%", duration: "4s", delay: "-3s", size: "text-xl", rotA: "-12deg", rotB: "6deg" },
  { emoji: "🚀", top: "40%", left: "92%", duration: "5.2s", delay: "-1.6s", size: "text-2xl", rotA: "6deg", rotB: "-12deg" },
] as const;

export default function LandingPage() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [healthError, setHealthError] = useState(false);

  useEffect(() => {
    fetch("/api/health")
      .then((res) => (res.ok ? res.json() : Promise.reject(res)))
      .then(setHealth)
      .catch(() => setHealthError(true));
  }, []);

  return (
    <main className="flex flex-1 flex-col overflow-x-hidden">
      <ThemeToggle className="fixed bottom-6 right-6 z-50" />
      {/* Header + hero share one decorative background so the banner's gradient blobs bleed up behind the nav too. */}
      <div className="relative overflow-hidden">
        <div aria-hidden className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
          <div className="animate-blob bg-primary/25 dark:bg-primary/20 absolute -top-24 left-1/4 h-72 w-72 rounded-full blur-3xl" />
          <div className="animate-blob-delay bg-secondary/25 dark:bg-secondary/20 absolute top-10 right-1/4 h-72 w-72 rounded-full blur-3xl" />
          <div className="animate-blob-delay-2 bg-primary/15 dark:bg-primary/10 absolute top-48 left-1/3 h-72 w-72 rounded-full blur-3xl" />

          {PARTICLES.map((p, i) => (
            <span
              key={i}
              className={`animate-float absolute select-none opacity-70 dark:opacity-50 ${p.size}`}
              style={{
                top: p.top,
                left: p.left,
                animationDuration: p.duration,
                animationDelay: p.delay,
                ["--float-rot-a" as string]: p.rotA,
                ["--float-rot-b" as string]: p.rotB,
              }}
            >
              {p.emoji}
            </span>
          ))}
        </div>

        <nav className="flex items-center justify-between px-6 py-5 sm:px-10">
          <Logo textClassName="text-primary-ink dark:text-primary text-2xl" iconClassName="h-14 w-auto" />
          <div className="flex items-center gap-3">
            <Link
              href="/login"
              className="hidden text-sm font-medium text-neutral-600 hover:text-black sm:inline-block dark:text-neutral-300 dark:hover:text-white"
            >
              Log in
            </Link>
            <Link
              href="/register"
              className="btn-shine bg-primary hidden rounded-full px-4 py-2 text-sm font-semibold text-white shadow-md shadow-primary/25 transition-transform duration-300 [transition-timing-function:cubic-bezier(0.34,1.56,0.64,1)] hover:scale-110 active:scale-95 sm:inline-block"
            >
              Get started free
            </Link>
          </div>
        </nav>

        {/* Hero */}
        <section className="flex flex-col items-center px-6 pb-24 pt-10 text-center sm:pt-16">
          <span className="animate-bounce-badge border-primary/20 bg-primary/10 text-primary-ink dark:border-primary/30 dark:text-primary mb-5 inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-medium">
            ⚡ Canva, but for going live
          </span>

        <h1 className="animate-fade-in-up font-display max-w-3xl text-5xl font-bold tracking-tight sm:text-6xl md:text-7xl" style={{ animationDelay: "0.05s" }}>
          <span className="text-primary-ink dark:text-primary">Beam</span> your stream.
        </h1>

        <p
          className="animate-fade-in-up mt-5 max-w-xl text-lg text-neutral-600 dark:text-neutral-300"
          style={{ animationDelay: "0.12s" }}
        >
          Design gorgeous overlays in your browser, sync them live to OBS, Streamlabs, etc., and
          never touch a Browser Source again. Built for streamers who&apos;d rather be streaming.
        </p>

        <div className="animate-fade-in-up mt-8 flex flex-wrap items-center justify-center gap-3" style={{ animationDelay: "0.18s" }}>
          <Link
            href="/register"
            className="btn-shine bg-primary rounded-full px-7 py-3 text-base font-semibold text-white shadow-lg shadow-primary/30 transition-transform duration-300 [transition-timing-function:cubic-bezier(0.34,1.56,0.64,1)] hover:scale-110 active:scale-95"
          >
            Start designing -- it&apos;s free
          </Link>
          <Link
            href="/login"
            className="rounded-full border border-neutral-300 px-7 py-3 text-base font-semibold transition-transform duration-300 [transition-timing-function:cubic-bezier(0.34,1.56,0.64,1)] hover:scale-105 hover:bg-neutral-50 active:scale-95 dark:border-neutral-700 dark:hover:bg-neutral-900"
          >
            I have an account
          </Link>
        </div>

        <p className="animate-fade-in-up mt-4 text-xs text-neutral-400 dark:text-neutral-500" style={{ animationDelay: "0.24s" }}>
          No credit card. No download. Just a browser tab and your broadcast software.
        </p>
        </section>
      </div>

      {/* Features */}
      <section className="mx-auto grid w-full max-w-5xl grid-cols-1 gap-5 px-6 pb-24 sm:grid-cols-2 sm:px-10">
        {FEATURES.map((feature, i) => (
          <ScrollReveal key={feature.title} delayMs={i * 90} className="h-full">
            <div className="group hover:shadow-primary/10 relative flex h-full flex-col overflow-hidden rounded-2xl border border-neutral-200 bg-white p-6 transition-all duration-300 hover:-translate-y-1.5 hover:rotate-1 hover:shadow-xl dark:border-neutral-800 dark:bg-neutral-900">
              <div
                className={`absolute -right-6 -top-6 h-24 w-24 rounded-full ${feature.accent} opacity-10 transition-opacity group-hover:opacity-20`}
              />
              <span className="animate-icon-bob hover:animate-wiggle inline-block text-3xl">{feature.emoji}</span>
              <h2 className="font-display mt-3 text-lg font-semibold">{feature.title}</h2>
              <p className="mt-1.5 text-sm text-neutral-600 dark:text-neutral-400">{feature.body}</p>
            </div>
          </ScrollReveal>
        ))}
      </section>

      {/* Bottom CTA */}
      <section className="px-6 pb-20 text-center sm:px-10">
        <ScrollReveal className="mx-auto max-w-3xl">
          <div className="bg-primary shadow-primary/20 rounded-3xl px-8 py-14 text-white shadow-2xl">
            <h2 className="font-display text-3xl font-bold sm:text-4xl">Your stream deserves better graphics.</h2>
            <p className="mx-auto mt-3 max-w-md text-white/90">
              Spin up your first overlay in minutes -- free, and no OBS/Streamlabs/etc. plugins required.
            </p>
            <Link
              href="/register"
              className="btn-shine text-primary-ink mt-6 inline-block rounded-full bg-white px-7 py-3 text-base font-semibold shadow-lg transition-transform duration-300 [transition-timing-function:cubic-bezier(0.34,1.56,0.64,1)] hover:scale-110 active:scale-95"
            >
              Get started free
            </Link>
          </div>
        </ScrollReveal>
      </section>

      <footer className="mt-auto flex flex-col items-center gap-2 px-6 pb-8 text-center text-xs text-neutral-400 dark:text-neutral-500">
        <Logo textClassName="text-primary-ink dark:text-primary text-lg" iconClassName="h-9 w-auto" />
        <span>Beam your stream.</span>
        <span>
          {healthError && "API unreachable"}
          {health && `API ${health.status} · ${health.app}`}
          {!health && !healthError && "Checking API..."}
        </span>
      </footer>
    </main>
  );
}
