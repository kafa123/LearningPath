import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// The full suite runs many jsdom files in parallel, so a query that resolves in
// milliseconds in isolation can take longer than the 1s default to settle.
// Keep the default generous to avoid timing flakes without hiding real errors.
configure({ asyncUtilTimeout: 5000 })

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})
