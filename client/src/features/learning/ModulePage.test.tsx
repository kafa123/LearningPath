import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse, moduleFixture } from '@/test/learningFixtures'
import { twoSumSummary } from '@/test/challengeFixtures'

describe('ModulePage', () => {
  it('renders objectives, materials, competencies, prerequisites, and practice', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input)

        if (url.includes('/api/challenges')) {
          return Promise.resolve(jsonResponse([twoSumSummary]))
        }

        return Promise.resolve(jsonResponse(moduleFixture))
      }),
    )

    renderApp(['/learning/programming-fundamentals/complexity-analysis'])

    expect(
      await screen.findByRole('heading', { name: 'Complexity Analysis' }),
    ).toBeInTheDocument()

    expect(
      screen.getByText('Count the dominant operation in a loop or recursive call.'),
    ).toBeInTheDocument()
    expect(
      screen.getByText('Compare algorithms by asymptotic growth instead of raw timing.'),
    ).toBeInTheDocument()
    expect(screen.getByText('Asymptotic analysis')).toBeInTheDocument()

    expect(
      screen.getByRole('link', { name: /measuring growth with big-o/i }),
    ).toHaveAttribute(
      'href',
      '/learning/programming-fundamentals/complexity-analysis/materials/complexity-analysis-lesson',
    )
    expect(
      screen.getByRole('link', { name: /algorithm foundations/i }),
    ).toHaveAttribute('href', '/learning/programming-fundamentals/algorithm-foundations')

    expect(
      screen.getByRole('heading', { name: 'Practice these competencies' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /two sum/i })).toHaveAttribute(
      'href',
      '/challenges/two-sum',
    )
  })

  it('shows an error state for an unknown module', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ title: 'Not found' }, 404)),
    )

    renderApp(['/learning/programming-fundamentals/does-not-exist'])

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Unable to load this module')).toBeInTheDocument()
  })
})
