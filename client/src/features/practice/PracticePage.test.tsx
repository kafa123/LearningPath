import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import {
  apiValidationSummary,
  orderRevenueSummary,
  twoSumSummary,
} from '@/test/challengeFixtures'
import { jsonResponse } from '@/test/learningFixtures'
import {
  assessmentHistoryFixture,
  progressOverviewFixture,
} from '@/test/progressFixtures'

function practiceFetch(input: RequestInfo | URL) {
  const url = String(input)

  if (url.includes('/api/challenges')) {
    return Promise.resolve(jsonResponse([twoSumSummary, apiValidationSummary, orderRevenueSummary]))
  }

  if (url.includes('/api/progress')) {
    return Promise.resolve(jsonResponse(progressOverviewFixture))
  }

  if (url.includes('/api/assessments')) {
    return Promise.resolve(jsonResponse(assessmentHistoryFixture))
  }

  return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
}

describe('PracticePage', () => {
  it('recommends executable challenges, excludes read-only packages, and shows status', async () => {
    vi.stubGlobal('fetch', vi.fn(practiceFetch))

    renderApp(['/practice'])

    expect(
      await screen.findByRole('heading', {
        name: 'Practice deliberately. Then prove it.',
      }),
    ).toBeInTheDocument()

    expect(screen.getByText('Practice-ready challenges')).toBeInTheDocument()
    expect(await screen.findByText(/1 catalog challenge is read-only/i)).toBeInTheDocument()

    const recommendations = screen
      .getByRole('heading', { name: 'Recommended next' })
      .closest('section')

    expect(recommendations).not.toBeNull()
    expect(
      within(recommendations as HTMLElement).getByRole('link', { name: /two sum/i }),
    ).toHaveAttribute('href', '/challenges/two-sum')
    expect(within(recommendations as HTMLElement).getByText('Ready for assessment')).toBeInTheDocument()
    expect(within(recommendations as HTMLElement).getByText('Practice again')).toBeInTheDocument()

    expect(
      screen.getByRole('link', { name: /completed revenue per customer/i }),
    ).toHaveAttribute('href', '/challenges/order-revenue')
    expect(
      screen.queryByRole('link', { name: /add request validation to an endpoint/i }),
    ).not.toBeInTheDocument()

    expect(screen.getAllByText('Solved').length).toBeGreaterThanOrEqual(2)
  })

  it('filters the practice list by difficulty', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('fetch', vi.fn(practiceFetch))

    renderApp(['/practice'])

    expect(await screen.findByText('Practice challenges')).toBeInTheDocument()

    const section = screen
      .getByRole('heading', { name: 'Practice challenges' })
      .closest('section') as HTMLElement

    expect(within(section).getByRole('link', { name: /two sum/i })).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Difficulty'), 'Intermediate')

    expect(screen.getByText('1 matching')).toBeInTheDocument()
    expect(within(section).queryByRole('link', { name: /two sum/i })).not.toBeInTheDocument()
    expect(
      within(section).getByRole('link', { name: /completed revenue per customer/i }),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Clear filters' }))

    expect(screen.getByText('2 matching')).toBeInTheDocument()
    expect(within(section).getByRole('link', { name: /two sum/i })).toBeInTheDocument()
  })

  it('shows an empty state when every catalog challenge is read-only', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        if (String(input).includes('/api/challenges')) {
          return Promise.resolve(jsonResponse([apiValidationSummary]))
        }

        return practiceFetch(input)
      }),
    )

    renderApp(['/practice'])

    expect(await screen.findByText('Nothing to practice yet.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Browse challenges' })).toHaveAttribute(
      'href',
      '/challenges',
    )
  })

  it('shows an error state and retries the challenge request', async () => {
    const user = userEvent.setup()
    let challengeCalls = 0
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      if (String(input).includes('/api/challenges')) {
        challengeCalls += 1

        return challengeCalls === 1
          ? Promise.reject(new TypeError('Failed to fetch'))
          : Promise.resolve(jsonResponse([twoSumSummary]))
      }

      return practiceFetch(input)
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/practice'])

    expect(await screen.findByText('Unable to load practice challenges')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect((await screen.findAllByRole('link', { name: /two sum/i })).length).toBeGreaterThan(0)
  })
})
