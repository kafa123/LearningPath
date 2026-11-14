import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse, learningPathFixture } from '@/test/learningFixtures'
import {
  assessmentHistoryFixture,
  progressOverviewFixture,
} from '@/test/progressFixtures'

const healthResponse = {
  status: 'Healthy',
  timestampUtc: '2026-11-07T10:00:00Z',
  version: '0.1.0.0',
  totalDurationMs: 1.2,
  checks: [{ name: 'database', status: 'Healthy', durationMs: 0.5 }],
}

function routeFetch(input: RequestInfo | URL) {
  const url =
    typeof input === 'string'
      ? input
      : input instanceof URL
        ? input.toString()
        : input.url

  if (url.includes('/api/health')) {
    return Promise.resolve(jsonResponse(healthResponse))
  }

  if (url.includes('/api/learning-paths')) {
    return Promise.resolve(jsonResponse([learningPathFixture]))
  }

  if (url.includes('/api/progress')) {
    return Promise.resolve(jsonResponse(progressOverviewFixture))
  }

  if (url.includes('/api/assessments')) {
    return Promise.resolve(jsonResponse(assessmentHistoryFixture))
  }

  return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(routeFetch))
})

describe('AppShell', () => {
  it('renders the primary navigation', () => {
    renderApp()

    const navigation = screen.getByRole('navigation', { name: 'Main navigation' })

    expect(navigation).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: /dashboard/i })).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: /learning paths/i })).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: /^challenges$/i })).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: /practice/i })).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: /examinations/i })).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: /progress/i })).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: /settings/i })).toBeInTheDocument()
  })

  it('navigates to the learning page without a full reload', async () => {
    const user = userEvent.setup()
    const { router } = renderApp()

    await user.click(screen.getByRole('link', { name: /learning paths/i }))

    expect(
      await screen.findByRole('heading', {
        name: 'Build understanding from the ground up.',
      }),
    ).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/learning')
  })

  it('renders a not-found view for unknown routes', () => {
    renderApp(['/does-not-exist'])

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })

  it('toggles the color theme and remembers the choice', async () => {
    const user = userEvent.setup()
    renderApp()

    await waitFor(() => {
      expect(document.documentElement.dataset.theme).toBe('dark')
    })

    await user.click(screen.getByRole('button', { name: 'Switch to light theme' }))

    expect(document.documentElement.dataset.theme).toBe('light')
    expect(window.localStorage.getItem('psel-theme')).toBe('light')
    expect(
      screen.getByRole('button', { name: 'Switch to dark theme' }),
    ).toBeInTheDocument()
  })
})
