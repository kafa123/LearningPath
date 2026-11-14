import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse, learningPathFixture } from '@/test/learningFixtures'

describe('LearningPathsPage', () => {
  it('lists the learning paths returned by the API', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse([learningPathFixture])))

    renderApp(['/learning'])

    expect(
      await screen.findByRole('heading', {
        name: 'Build understanding from the ground up.',
      }),
    ).toBeInTheDocument()

    const pathLink = await screen.findByRole('link', {
      name: /programming fundamentals/i,
    })
    expect(pathLink).toHaveAttribute('href', '/learning/programming-fundamentals')
    expect(screen.getByText('2 modules')).toBeInTheDocument()
  })

  it('shows an error state and retries the request', async () => {
    const fetchMock = vi
      .fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(jsonResponse([learningPathFixture]))

    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()

    renderApp(['/learning'])

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Unable to reach the PSEL API.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(
      await screen.findByRole('link', { name: /programming fundamentals/i }),
    ).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
