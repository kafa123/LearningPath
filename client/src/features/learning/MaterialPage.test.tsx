import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse, materialFixture } from '@/test/learningFixtures'

describe('MaterialPage', () => {
  it('renders the Markdown body with GitHub-flavored tables', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(materialFixture)))

    renderApp([
      '/learning/programming-fundamentals/complexity-analysis/materials/complexity-analysis-lesson',
    ])

    expect(
      await screen.findByRole(
        'heading',
        { name: 'Measuring growth with Big-O' },
        { timeout: 5000 },
      ),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Measuring growth, not seconds' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('table')).toBeInTheDocument()
    expect(screen.getByText('one pass over a collection')).toBeInTheDocument()

    expect(screen.getByRole('link', { name: 'Programming Fundamentals' })).toHaveAttribute(
      'href',
      '/learning/programming-fundamentals',
    )
    expect(screen.getByRole('link', { name: 'Complexity Analysis' })).toHaveAttribute(
      'href',
      '/learning/programming-fundamentals/complexity-analysis',
    )
  })

  it('shows an error state when the material cannot be loaded', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ title: 'Not found' }, 404)),
    )

    renderApp([
      '/learning/programming-fundamentals/complexity-analysis/materials/missing',
    ])

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Unable to load this material')).toBeInTheDocument()
  })
})
