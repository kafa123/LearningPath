import type { ReactNode } from 'react'

export type DashboardGlyphName =
  | 'mastered'
  | 'assessment'
  | 'activity'
  | 'streak'
  | 'clock'
  | 'arrow'

const glyphs: Record<DashboardGlyphName, ReactNode> = {
  mastered: (
    <>
      <path d="M5 12.5 9.5 17 19 7.5" />
    </>
  ),
  assessment: (
    <>
      <path d="M6 4h9l3 3v13H6z" />
      <path d="M9 11h6" />
      <path d="M9 15h4" />
    </>
  ),
  activity: (
    <>
      <path d="M4 17h3l2.5-8 3 11 2.5-7H20" />
    </>
  ),
  streak: (
    <>
      <path d="M12 4c1 3-2 4-2 7a2 2 0 0 0 4 0c0-1-.5-2-.5-2 2 1 3.5 3 3.5 5a5 5 0 1 1-10 0c0-4 3-6 5-10z" />
    </>
  ),
  clock: (
    <>
      <circle cx="12" cy="12" r="8" />
      <path d="M12 8v4l2.5 2" />
    </>
  ),
  arrow: (
    <>
      <path d="M5 12h13" />
      <path d="m13 6 6 6-6 6" />
    </>
  ),
}

export function DashboardGlyph({ name }: { name: DashboardGlyphName }) {
  return (
    <svg
      aria-hidden="true"
      className="size-4"
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
