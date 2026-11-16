import type { NavGlyphName } from '@/shared/components/NavGlyph'

export interface NavigationItem {
  to: string
  label: string
  glyph: NavGlyphName
  badge?: string
  end?: boolean
}

export const navigationItems: NavigationItem[] = [
  { to: '/', label: 'Dashboard', glyph: 'dashboard', end: true },
  { to: '/learning', label: 'Learning Paths', glyph: 'learning' },
  { to: '/challenges', label: 'Challenges', glyph: 'challenges' },
  { to: '/practice', label: 'Practice', glyph: 'practice' },
  { to: '/examinations', label: 'Examinations', glyph: 'examinations' },
  { to: '/progress', label: 'Progress', glyph: 'progress' },
  { to: '/authoring', label: 'Authoring', glyph: 'authoring' },
  { to: '/settings', label: 'Settings', glyph: 'settings' },
]
