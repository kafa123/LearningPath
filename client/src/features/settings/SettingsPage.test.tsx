import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse } from '@/test/learningFixtures'

const healthResponse = {
  status: 'Healthy',
  timestampUtc: '2026-11-07T10:00:00Z',
  version: '0.1.0.0',
  totalDurationMs: 1.2,
  checks: [{ name: 'database', status: 'Healthy', durationMs: 0.5 }],
}

const runtimeResponse = {
  executionEnabled: true,
  languages: [
    { id: 'csharp', name: 'C#', image: 'mcr.microsoft.com/dotnet/sdk:10.0' },
    { id: 'python', name: 'Python', image: 'python:3.13-slim' },
    { id: 'sql', name: 'SQL', image: 'python:3.13-slim' },
  ],
  limits: {
    timeLimitSeconds: 60,
    memoryLimitMb: 512,
    cpuLimit: 1.0,
    pidsLimit: 128,
    maxOutputBytes: 65536,
    maxConcurrentExecutions: 2,
    maxQueuedExecutions: 64,
  },
}

function settingsFetch(input: RequestInfo | URL) {
  const url = String(input)

  if (url.includes('/api/health')) {
    return Promise.resolve(jsonResponse(healthResponse))
  }

  if (url.includes('/api/runtime')) {
    return Promise.resolve(jsonResponse(runtimeResponse))
  }

  return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
}

describe('SettingsPage', () => {
  it('shows API health, runtime languages, and server-owned limits as read-only', async () => {
    vi.stubGlobal('fetch', vi.fn(settingsFetch))

    renderApp(['/settings'])

    expect(
      await screen.findByRole('heading', { name: 'Settings' }),
    ).toBeInTheDocument()

    expect(screen.getByText(/this page is read-only/i)).toBeInTheDocument()

    const healthSection = screen
      .getByRole('heading', { name: 'API health' })
      .closest('section') as HTMLElement

    expect(await within(healthSection).findByText('0.1.0.0')).toBeInTheDocument()
    expect(within(healthSection).getAllByText('Healthy', { selector: 'dd' })).toHaveLength(2)

    const runtimeSection = screen
      .getByRole('heading', { name: 'Runtimes and limits' })
      .closest('section') as HTMLElement

    expect(await within(runtimeSection).findByText('Execution enabled')).toBeInTheDocument()
    expect(within(runtimeSection).getByText('C#')).toBeInTheDocument()
    expect(within(runtimeSection).getByText('Python')).toBeInTheDocument()
    expect(within(runtimeSection).getByText('SQL')).toBeInTheDocument()
    expect(
      within(runtimeSection).getAllByText('python:3.13-slim').length,
    ).toBeGreaterThanOrEqual(2)

    expect(within(runtimeSection).getByText('60s')).toBeInTheDocument()
    expect(within(runtimeSection).getByText('512 MB')).toBeInTheDocument()
    expect(within(runtimeSection).getByText('64 KiB')).toBeInTheDocument()
    expect(within(runtimeSection).getByText('2')).toBeInTheDocument()
    expect(within(runtimeSection).getByText('64')).toBeInTheDocument()
  })

  it('shows a disabled badge when execution is turned off', async () => {    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) =>
        String(input).includes('/api/runtime')
          ? Promise.resolve(jsonResponse({ ...runtimeResponse, executionEnabled: false }))
          : settingsFetch(input),
      ),
    )

    renderApp(['/settings'])

    expect(await screen.findByText('Execution disabled')).toBeInTheDocument()
  })

  it('shows an error state and retries the runtime request', async () => {
    const user = userEvent.setup()
    let runtimeCalls = 0
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        if (String(input).includes('/api/runtime')) {
          runtimeCalls += 1

          return runtimeCalls === 1
            ? Promise.reject(new TypeError('Failed to fetch'))
            : Promise.resolve(jsonResponse(runtimeResponse))
        }

        return settingsFetch(input)
      }),
    )

    renderApp(['/settings'])

    expect(
      await screen.findByText('Unable to load runtime diagnostics'),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('Execution enabled')).toBeInTheDocument()
  })

  it('applies and persists the light theme from the appearance section', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('fetch', vi.fn(settingsFetch))

    renderApp(['/settings'])

    expect(
      await screen.findByRole('heading', { name: 'Appearance' }),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'light' }))

    expect(document.documentElement.dataset.theme).toBe('light')
    expect(window.localStorage.getItem('psel-theme')).toBe('light')
    expect(screen.getByRole('button', { name: 'light' })).toHaveAttribute(
      'aria-pressed',
      'true',
    )
  })
})
