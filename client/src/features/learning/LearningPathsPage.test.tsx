import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse, learningPathFixture } from '@/test/learningFixtures'
import { twoSumSummary } from '@/test/challengeFixtures'

describe('LearningPathsPage', () => {
  it('lists the learning paths with their related practice challenges', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input)

        if (url.includes('/api/challenges')) {
          return Promise.resolve(jsonResponse([twoSumSummary]))
        }

        return Promise.resolve(jsonResponse([learningPathFixture]))
      }),
    )

    renderApp(['/learning'])

    expect(
      await screen.findByRole('heading', {
        name: 'Learning paths',
      }),
    ).toBeInTheDocument()

    const pathLink = await screen.findByRole('link', {
      name: /programming fundamentals/i,
    })
    expect(pathLink).toHaveAttribute('href', '/learning/programming-fundamentals')
    expect(screen.getByText(/2 modules · 1 challenge/)).toBeInTheDocument()
  })

  it('shows an error state and retries the request', async () => {
    let learningPathCalls = 0

    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input)

        if (url.includes('/api/challenges')) {
          return Promise.resolve(jsonResponse([twoSumSummary]))
        }

        learningPathCalls += 1

        return learningPathCalls === 1
          ? Promise.reject(new TypeError('Failed to fetch'))
          : Promise.resolve(jsonResponse([learningPathFixture]))
      }),
    )

    const user = userEvent.setup()

    renderApp(['/learning'])

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Unable to reach the PSEL API.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(
      await screen.findByRole('link', { name: /programming fundamentals/i }),
    ).toBeInTheDocument()
    expect(learningPathCalls).toBe(2)
  })
})
