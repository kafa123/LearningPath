export type Theme = 'light' | 'dark'

export const THEME_STORAGE_KEY = 'psel-theme'

function isTheme(value: string | null): value is Theme {
  return value === 'light' || value === 'dark'
}

export function readStoredTheme(): Theme | null {
  if (typeof window === 'undefined') {
    return null
  }

  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY)

    return isTheme(stored) ? stored : null
  } catch {
    return null
  }
}

function prefersLight(): boolean {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return false
  }

  return window.matchMedia('(prefers-color-scheme: light)').matches
}

export function resolveInitialTheme(): Theme {
  return readStoredTheme() ?? (prefersLight() ? 'light' : 'dark')
}

export function applyTheme(theme: Theme) {
  if (typeof document !== 'undefined') {
    document.documentElement.dataset.theme = theme
  }

  try {
    window.localStorage.setItem(THEME_STORAGE_KEY, theme)
  } catch {
    // Storage can be unavailable in private or restricted contexts.
  }
}
