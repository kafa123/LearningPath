import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { twoSumSummary } from '@/test/challengeFixtures'
import { jsonResponse } from '@/test/learningFixtures'
import {
  completedExaminationFixture,
  examinationDetailFixture,
  examinationSummaryFixture,
} from '@/test/progressFixtures'

vi.mock('@/shared/components/CodeEditor', () => ({
  CodeEditor: ({
    filePath,
    value,
    onChange,
  }: {
    filePath: string
    value: string
    onChange: (value: string) => void
  }) => (
    <textarea
      aria-label={`Code editor for ${filePath}`}
      onChange={(event) => onChange(event.target.value)}
      value={value}
    />
  ),
}))

function routeFetch(input: RequestInfo | URL, init?: RequestInit) {
  const url = String(input)

  if (url.includes('/api/challenges')) {
    return Promise.resolve(jsonResponse([twoSumSummary]))
  }

  if (url.includes('/finish')) {
    return Promise.resolve(jsonResponse(completedExaminationFixture))
  }

  if (url.includes('/api/examinations/exam-1')) {
    return Promise.resolve(jsonResponse(examinationDetailFixture))
  }

  if (url.includes('/api/examinations')) {
    if (init?.method === 'POST') {
      return Promise.resolve(jsonResponse(examinationDetailFixture, 201))
    }

    return Promise.resolve(jsonResponse([examinationSummaryFixture]))
  }

  return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
}

describe('ExaminationsPage', () => {
  it('lists sessions and opens a newly started examination', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('fetch', vi.fn(routeFetch))

    const { router } = renderApp(['/examinations'])

    expect(
      await screen.findByRole('heading', { name: 'Prove competency under constraints.' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Standard examination')).toBeInTheDocument()
    expect(screen.getByText('1 attempt')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Start examination' }))

    expect(
      await screen.findByRole('heading', { name: 'Two Sum examination' }, { timeout: 5000 }),
    ).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/examinations/exam-1')
  })

  it('shows the server rejection when an examination cannot start', async () => {
    const user = userEvent.setup()
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input)

        if (url.includes('/api/challenges')) {
          return Promise.resolve(jsonResponse([twoSumSummary]))
        }

        if (url.includes('/api/examinations') && init?.method === 'POST') {
          return Promise.resolve(
            jsonResponse(
              {
                title: 'Examination rejected.',
                detail: 'This challenge does not define hidden assessment tests.',
              },
              400,
            ),
          )
        }

        if (url.includes('/api/examinations')) {
          return Promise.resolve(jsonResponse([]))
        }

        return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
      }),
    )

    renderApp(['/examinations'])

    await screen.findByRole('heading', { name: 'Prove competency under constraints.' })
    await user.click(screen.getByRole('button', { name: 'Start examination' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'This challenge does not define hidden assessment tests.',
    )
  })

  it('shows an empty history state', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input)

        if (url.includes('/api/challenges')) {
          return Promise.resolve(jsonResponse([twoSumSummary]))
        }

        if (url.includes('/api/examinations')) {
          return Promise.resolve(jsonResponse([]))
        }

        return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
      }),
    )

    renderApp(['/examinations'])

    expect(await screen.findByText('No examinations yet.')).toBeInTheDocument()
  })
})
