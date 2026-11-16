import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse, learningPathFixture } from '@/test/learningFixtures'
import { twoSumSummary } from '@/test/challengeFixtures'

describe('LearningPathPage', () => {
  it('connects modules to the challenges that assess their competencies', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input)

        if (url.includes('/api/challenges')) {
          return Promise.resolve(jsonResponse([twoSumSummary]))
        }

        return Promise.resolve(jsonResponse(learningPathFixture))
      }),
    )

    renderApp(['/learning/programming-fundamentals'])

    expect(
      await screen.findByRole('heading', { name: 'Programming Fundamentals' }),
    ).toBeInTheDocument()

    expect(screen.getByText(/No prerequisites · 1 challenge/)).toBeInTheDocument()
    expect(screen.getByText(/1 prerequisite · 1 challenge/)).toBeInTheDocument()

    expect(
      screen.getByRole('heading', { name: 'Practice this path' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /two sum/i })).toHaveAttribute(
      'href',
      '/challenges/two-sum',
    )
  })
})
