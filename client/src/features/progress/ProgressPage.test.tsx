import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse } from '@/test/learningFixtures'
import {
  assessmentHistoryFixture,
  progressOverviewFixture,
} from '@/test/progressFixtures'

function routeFetch(input: RequestInfo | URL) {
  const url = String(input)

  if (url.includes('/api/progress')) {
    return Promise.resolve(jsonResponse(progressOverviewFixture))
  }

  if (url.includes('/api/assessments')) {
    return Promise.resolve(jsonResponse(assessmentHistoryFixture))
  }

  return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
}

describe('ProgressPage', () => {
  it('renders the summary, competency evidence, and assessment history', async () => {
    vi.stubGlobal('fetch', vi.fn(routeFetch))

    renderApp(['/progress'])

    expect(
      await screen.findByRole('heading', { name: 'Evidence, not activity.' }),
    ).toBeInTheDocument()

    expect(await screen.findByText('Problem decomposition')).toBeInTheDocument()
    expect(screen.getByText('Correctness reasoning')).toBeInTheDocument()
    expect(screen.getByText('Asymptotic analysis')).toBeInTheDocument()
    expect(screen.getByText(/Take an examination for a mapped challenge/i)).toBeInTheDocument()
    expect(screen.getAllByText('Mastered').length).toBeGreaterThanOrEqual(2)
    expect(screen.getByText('Assessment required')).toBeInTheDocument()

    expect(screen.getByText('Recent assessments')).toBeInTheDocument()
    expect(screen.getByText('Two Sum')).toBeInTheDocument()
    expect(screen.getByText('Balanced Brackets')).toBeInTheDocument()
    expect(screen.getByText('Standard examination')).toBeInTheDocument()
    expect(screen.getByText('WrongAnswer')).toBeInTheDocument()
  })

  it('shows an error for progress and retries it', async () => {
    const user = userEvent.setup()
    let progressCalls = 0
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input)

      if (url.includes('/api/progress')) {
        progressCalls += 1

        return progressCalls === 1
          ? Promise.reject(new TypeError('Failed to fetch'))
          : Promise.resolve(jsonResponse(progressOverviewFixture))
      }

      if (url.includes('/api/assessments')) {
        return Promise.resolve(jsonResponse(assessmentHistoryFixture))
      }

      return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/progress'])

    expect(await screen.findByText('Unable to load progress')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('Problem decomposition')).toBeInTheDocument()
  })

  it('shows an empty assessment state when no evidence exists', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input)

        if (url.includes('/api/progress')) {
          return Promise.resolve(jsonResponse(progressOverviewFixture))
        }

        if (url.includes('/api/assessments')) {
          return Promise.resolve(jsonResponse([]))
        }

        return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
      }),
    )

    renderApp(['/progress'])

    expect(await screen.findByText('No assessments yet.')).toBeInTheDocument()

    const history = screen
      .getByRole('heading', { name: 'Recent assessments' })
      .closest('section')

    expect(history).not.toBeNull()
    expect(within(history as HTMLElement).getByRole('link', { name: 'Browse challenges' })).toHaveAttribute(
      'href',
      '/challenges',
    )
  })
})
