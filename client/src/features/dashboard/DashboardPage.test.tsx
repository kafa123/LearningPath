import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse } from '@/test/learningFixtures'
import {
  assessmentHistoryFixture,
  examinationSummaryFixture,
  progressOverviewFixture,
} from '@/test/progressFixtures'

const healthResponse = {
  status: 'Healthy',
  timestampUtc: '2026-11-07T10:00:00Z',
  version: '0.1.0.0',
  totalDurationMs: 1.2,
  checks: [{ name: 'database', status: 'Healthy', durationMs: 0.5 }],
}

function successResponse() {
  return {
    ok: true,
    status: 200,
    json: async () => healthResponse,
  } as Response
}

function supportResponse(
  input: RequestInfo | URL,
  examinations: unknown[] = [],
) {
  const url = String(input)

  if (url.includes('/api/progress')) {
    return Promise.resolve(jsonResponse(progressOverviewFixture))
  }

  if (url.includes('/api/assessments')) {
    return Promise.resolve(jsonResponse(assessmentHistoryFixture))
  }

  if (url.includes('/api/examinations')) {
    return Promise.resolve(jsonResponse(examinations))
  }

  return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
}

describe('DashboardPage', () => {
  it('shows a loading state and then the healthy result', async () => {
    let resolveHealth!: (response: Response) => void
    const healthPromise = new Promise<Response>((resolve) => {
      resolveHealth = resolve
    })

    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) =>
        String(input).includes('/api/health')
          ? healthPromise
          : supportResponse(input),
      ),
    )

    renderApp()

    expect(screen.getByText('Checking local services...')).toBeInTheDocument()

    resolveHealth(successResponse())

    expect(
      await screen.findByText('The API and local database responded successfully.'),
    ).toBeInTheDocument()
    expect(screen.getByText('0.1.0.0')).toBeInTheDocument()
    expect(screen.getByText('Database')).toBeInTheDocument()
  })

  it('shows key metrics, the recommended next step, and recent activity', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) =>
        String(input).includes('/api/health')
          ? Promise.resolve(successResponse())
          : supportResponse(input),
      ),
    )

    renderApp()

    expect(
      await screen.findByText('The API and local database responded successfully.'),
    ).toBeInTheDocument()
    expect(screen.getByText('0.1.0.0')).toBeInTheDocument()

    const healthCard = screen
      .getByRole('heading', { name: 'Local lab health' })
      .closest('section')

    expect(healthCard).not.toBeNull()
    expect(within(healthCard as HTMLElement).getByText('Database')).toBeInTheDocument()
    expect(within(healthCard as HTMLElement).getAllByText('Healthy')).toHaveLength(3)

    const metrics = screen.getByRole('region', { name: 'Key metrics' })

    expect(within(metrics).getByText('Mastered')).toBeInTheDocument()
    expect(within(metrics).getByText('25% of competencies mastered')).toBeInTheDocument()
    expect(within(metrics).getByText('50')).toBeInTheDocument()
    expect(within(metrics).getByText('%')).toBeInTheDocument()
    expect(within(metrics).getByText('1 of 2 attempts accepted')).toBeInTheDocument()

    expect((await screen.findAllByText('Correctness reasoning')).length).toBeGreaterThan(0)
    expect(
      screen.getAllByText('Take an examination for a mapped challenge.').length,
    ).toBeGreaterThan(0)
    expect(screen.getAllByText('Algorithm Foundations').length).toBeGreaterThan(0)

    expect(screen.getByText('Two Sum')).toBeInTheDocument()
    expect(screen.getByText('Exam')).toBeInTheDocument()
    expect(screen.getByText('Balanced Brackets')).toBeInTheDocument()
  })

  it('surfaces an active examination as a resume banner', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) =>
        String(input).includes('/api/health')
          ? Promise.resolve(successResponse())
          : supportResponse(input, [examinationSummaryFixture]),
      ),
    )

    renderApp()

    const banner = await screen.findByRole('region', {
      name: 'Active examination',
    })

    expect(within(banner).getByText('Examination in progress')).toBeInTheDocument()
    expect(within(banner).getByText('Two Sum')).toBeInTheDocument()
    expect(within(banner).getByText('9 min left')).toBeInTheDocument()
    expect(
      within(banner).getByRole('link', { name: 'Resume examination' }),
    ).toHaveAttribute('href', '/examinations/exam-1')
  })

  it('shows an error state and can retry the health check', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input)

      if (url.includes('/api/health')) {
        return Promise.resolve(
          fetchMock.mock.calls.filter((call) =>
            String(call[0]).includes('/api/health'),
          ).length === 1
            ? Promise.reject(new TypeError('Failed to fetch'))
            : Promise.resolve(successResponse()),
        )
      }

      return supportResponse(input)
    })

    vi.stubGlobal('fetch', fetchMock)

    const user = userEvent.setup()
    renderApp()

    expect(await screen.findByText('The API is currently unavailable.')).toBeInTheDocument()
    expect(screen.getByText('Unable to reach the PSEL API.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Retry health check' }))

    expect(
      await screen.findByText('The API and local database responded successfully.'),
    ).toBeInTheDocument()

    const healthCalls = fetchMock.mock.calls.filter((call) =>
      String(call[0]).includes('/api/health'),
    )
    expect(healthCalls).toHaveLength(2)
  })
})
