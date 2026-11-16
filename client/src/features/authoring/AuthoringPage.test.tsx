import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse } from '@/test/learningFixtures'

const learningResult = {
  added: 3,
  updated: 1,
  domainCount: 2,
  moduleCount: 1,
}

const challengeResult = {
  slug: 'balanced-brackets',
  title: 'Balanced Brackets',
  domainSlug: 'programming-fundamentals',
  challengeCount: 8,
  replaced: false,
}

function learningPack() {
  return new File(['{"schemaVersion":1}'], 'learning.json', {
    type: 'application/json',
  })
}

function challengeArchive() {
  return new File(['PK'], 'package.zip', { type: 'application/zip' })
}

describe('AuthoringPage', () => {
  it('renders both upload forms with disabled submit buttons', () => {
    vi.stubGlobal('fetch', vi.fn())
    renderApp(['/authoring'])

    expect(
      screen.getByRole('heading', { name: 'Learning content' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Challenge package' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Import content' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Upload package' })).toBeDisabled()
  })

  it('imports a learning content pack and reports the result', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(learningResult))
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/authoring'])

    await user.upload(screen.getByLabelText('Content pack (.json)'), learningPack())
    await user.click(screen.getByRole('button', { name: 'Import content' }))

    const panel = await screen.findByText('Content pack applied.')
    expect(panel.parentElement).toHaveTextContent(
      '2 domains · 1 module · 3 added · 1 updated',
    )

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/authoring/learning-content',
      expect.objectContaining({
        method: 'POST',
        body: expect.any(FormData),
      }),
    )
  })

  it('shows the server message when a content pack is rejected', async () => {
    const user = userEvent.setup()
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse(
          {
            title: 'Learning content import failed.',
            detail: 'domains[0].slug is required.',
          },
          400,
        ),
      ),
    )

    renderApp(['/authoring'])

    await user.upload(screen.getByLabelText('Content pack (.json)'), learningPack())
    await user.click(screen.getByRole('button', { name: 'Import content' }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('domains[0].slug is required.')
  })

  it('uploads a challenge package and reports the catalog size', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(challengeResult))
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/authoring'])

    await user.upload(
      screen.getByLabelText('Package archive (.zip)'),
      challengeArchive(),
    )
    await user.click(screen.getByRole('button', { name: 'Upload package' }))

    const panel = await screen.findByText('Package installed: Balanced Brackets')
    expect(panel.parentElement).toHaveTextContent(
      'balanced-brackets · programming-fundamentals · 8 challenges in the catalog',
    )

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/authoring/challenge-packages',
      expect.objectContaining({
        method: 'POST',
        body: expect.any(FormData),
      }),
    )
  })

  it('requests replacement when overwriting an existing package', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ ...challengeResult, replaced: true }),
    )
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/authoring'])

    await user.upload(
      screen.getByLabelText('Package archive (.zip)'),
      challengeArchive(),
    )
    await user.click(
      screen.getByLabelText('Replace the existing package with the same slug'),
    )
    await user.click(screen.getByRole('button', { name: 'Upload package' }))

    expect(
      await screen.findByText('Package replaced: Balanced Brackets'),
    ).toBeInTheDocument()

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/authoring/challenge-packages?replace=true',
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it('shows the conflict message when replacement is not enabled', async () => {
    const user = userEvent.setup()
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse(
          {
            title: 'Challenge package conflict.',
            detail: "A package already exists for 'balanced-brackets'.",
          },
          409,
        ),
      ),
    )

    renderApp(['/authoring'])

    await user.upload(
      screen.getByLabelText('Package archive (.zip)'),
      challengeArchive(),
    )
    await user.click(screen.getByRole('button', { name: 'Upload package' }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent("A package already exists for 'balanced-brackets'.")
  })
})
