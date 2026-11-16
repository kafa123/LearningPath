import type { ReactNode } from 'react'

export type NavGlyphName =
  | 'dashboard'
  | 'learning'
  | 'challenges'
  | 'practice'
  | 'examinations'
  | 'progress'
  | 'authoring'
  | 'settings'

const glyphs: Record<NavGlyphName, ReactNode> = {
  dashboard: (
    <>
      <rect x="4" y="4" width="6" height="6" rx="1" />
      <rect x="14" y="4" width="6" height="10" rx="1" />
      <rect x="4" y="14" width="6" height="6" rx="1" />
      <rect x="14" y="18" width="6" height="2" rx="1" />
    </>
  ),
  learning: (
    <>
      <path d="M4 6.5A2.5 2.5 0 0 1 6.5 4H20v14H6.5A2.5 2.5 0 0 0 4 20.5z" />
      <path d="M4 20.5A2.5 2.5 0 0 1 6.5 18H20" />
    </>
  ),
  challenges: (
    <>
      <path d="m8 9-3 3 3 3" />
      <path d="m16 9 3 3-3 3" />
      <path d="m14 5-4 14" />
    </>
  ),
  practice: (
    <>
      <circle cx="12" cy="12" r="8" />
      <circle cx="12" cy="12" r="4" />
      <circle cx="12" cy="12" r="1" />
    </>
  ),
  examinations: (
    <>
      <rect x="4" y="6" width="16" height="14" rx="2" />
      <path d="M9 6V4h6v2" />
      <path d="m8 14 2.5 2.5L16 11" />
    </>
  ),
  progress: (
    <>
      <path d="M4 20V10" />
      <path d="M10 20V4" />
      <path d="M16 20v-7" />
      <path d="M2 20h20" />
    </>
  ),
  authoring: (
    <>
      <path d="M4 20h4L20 8l-4-4L4 16z" />
      <path d="m13 7 4 4" />
    </>
  ),
  settings: (
    <>
      <path d="M4 6h16" />
      <path d="M4 12h16" />
      <path d="M4 18h16" />
      <circle cx="9" cy="6" r="2" />
      <circle cx="15" cy="12" r="2" />
      <circle cx="8" cy="18" r="2" />
    </>
  ),
}

export function NavGlyph({ name }: { name: NavGlyphName }) {
  return (
    <svg
      aria-hidden="true"
      className="size-4 shrink-0"
      fill="none"
      stroke="currentColor"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth="1.6"
      viewBox="0 0 24 24"
    >
      {glyphs[name]}
    </svg>
  )
}
