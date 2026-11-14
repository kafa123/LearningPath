import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { jsonResponse } from '@/test/learningFixtures'
import {
  completedExaminationFixture,
  examinationDetailFixture,
  expiredExaminationFixture,
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

describe('ExaminationWorkspacePage', () => {
  it('renders a hint-free examination workspace with a server-selected variant', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() => Promise.resolve(jsonResponse(examinationDetailFixture))),
    )

    renderApp(['/examinations/exam-1'])

    expect(
      await screen.findByRole('heading', { name: 'Two Sum examination' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Standard examination')).toBeInTheDocument()
    expect(screen.getByText('9:00 remaining')).toBeInTheDocument()
    expect(screen.queryByText('Show hints')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Run' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Submit for assessment' })).toBeEnabled()
    expect(
      screen.getByText(/Hints and reference solutions are unavailable/i),
    ).toBeInTheDocument()
  })

  it('finishes the examination and closes submissions', async () => {
    const user = userEvent.setup()
    let detailCalls = 0
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input)

        if (url.includes('/finish')) {
          return Promise.resolve(jsonResponse(completedExaminationFixture))
        }

        if (url.includes('/api/examinations/exam-1')) {
          detailCalls += 1

          return Promise.resolve(
            jsonResponse(
              detailCalls === 1
                ? examinationDetailFixture
                : completedExaminationFixture,
            ),
          )
        }

        return Promise.resolve(jsonResponse({ title: 'Not found' }, 404))
      }),
    )

    renderApp(['/examinations/exam-1'])

    await screen.findByRole('heading', { name: 'Two Sum examination' })
    await user.click(screen.getByRole('button', { name: 'Finish examination' }))

    expect(await screen.findByText('This examination is complete.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Submit for assessment' })).toBeDisabled()
  })

  it('closes an expired examination', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() => Promise.resolve(jsonResponse(expiredExaminationFixture))),
    )

    renderApp(['/examinations/exam-1'])

    expect(
      await screen.findByText('The time limit for this examination has expired.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Submit for assessment' })).toBeDisabled()
  })
})
