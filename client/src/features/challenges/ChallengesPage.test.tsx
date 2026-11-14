import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { apiValidationSummary, twoSumSummary } from '@/test/challengeFixtures'
import { jsonResponse } from '@/test/learningFixtures'

describe('ChallengesPage', () => {
  it('lists challenges with metadata and workspace links', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse([twoSumSummary, apiValidationSummary])),
    )

    renderApp(['/challenges'])

    expect(
      await screen.findByRole('heading', {
        name: 'Practice with a real workspace.',
      }),
    ).toBeInTheDocument()

    const twoSumLink = await screen.findByRole('link', { name: /two sum/i })
    expect(twoSumLink).toHaveAttribute('href', '/challenges/two-sum')

    const apiLink = screen.getByRole('link', {
      name: /add request validation to an endpoint/i,
    })
    expect(apiLink).toHaveAttribute('href', '/challenges/api-validation')

    expect(screen.getByText('2 challenges')).toBeInTheDocument()
    expect(within(twoSumLink).getByText('Beginner')).toBeInTheDocument()
    expect(within(apiLink).getByText('Intermediate')).toBeInTheDocument()
  })

  it('filters by difficulty and clears the filters', async () => {
    const user = userEvent.setup()
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse([twoSumSummary, apiValidationSummary])),
    )

    renderApp(['/challenges'])

    expect(await screen.findByRole('link', { name: /two sum/i })).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Difficulty'), 'Intermediate')

    expect(screen.queryByRole('link', { name: /two sum/i })).not.toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: /add request validation to an endpoint/i }),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Clear filters' }))

    expect(screen.getByRole('link', { name: /two sum/i })).toBeInTheDocument()
    expect(screen.getByText('2 challenges')).toBeInTheDocument()
  })

  it('shows an empty state when no challenge matches', async () => {
    const user = userEvent.setup()
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse([twoSumSummary, apiValidationSummary])),
    )

    renderApp(['/challenges'])

    expect(await screen.findByRole('link', { name: /two sum/i })).toBeInTheDocument()

    await user.type(screen.getByLabelText('Search'), 'zzz')
    expect(screen.getByText('No challenges match those filters.')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /two sum/i })).not.toBeInTheDocument()
  })

  it('shows an error state and retries the request', async () => {
    const user = userEvent.setup()
    const fetchMock = vi
      .fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(jsonResponse([twoSumSummary]))

    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/challenges'])

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Unable to reach the PSEL API.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('link', { name: /two sum/i })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
