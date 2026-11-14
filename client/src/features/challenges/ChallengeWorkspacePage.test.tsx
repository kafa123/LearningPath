import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { orderRevenueDetail, twoSumDetail } from '@/test/challengeFixtures'
import { jsonResponse } from '@/test/learningFixtures'

vi.mock('@/shared/components/CodeEditor', () => ({
  CodeEditor: ({
    filePath,
    language,
    value,
    onChange,
  }: {
    filePath: string
    language: string
    value: string
    onChange: (value: string) => void
  }) => (
    <textarea
      aria-label={`Code editor for ${filePath}`}
      data-language={language}
      onChange={(event) => onChange(event.target.value)}
      value={value}
    />
  ),
}))

const csharpStarter = twoSumDetail.languages[0].starterFiles[0].content
const pythonStarter = twoSumDetail.languages[1].starterFiles[0].content

describe('ChallengeWorkspacePage', () => {
  it('renders the statement, hints, public tests, and starter code', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(twoSumDetail)))

    renderApp(['/challenges/two-sum'])

    expect(
      await screen.findByRole('heading', { name: 'Two Sum', level: 1 }, { timeout: 5000 }),
    ).toBeInTheDocument()
    expect(screen.getByText('Learning objectives')).toBeInTheDocument()
    expect(screen.getByText('Constraints')).toBeInTheDocument()
    expect(screen.getByText('Examples')).toBeInTheDocument()
    expect(screen.getByText('Public tests')).toBeInTheDocument()
    expect(screen.getByText('Example 1')).toBeInTheDocument()
    expect(screen.getByText('Show hints')).toBeInTheDocument()

    const editor = screen.getByLabelText('Code editor for Solution.cs')
    expect(editor).toHaveValue(csharpStarter)
    expect(editor).toHaveAttribute('data-language', 'csharp')

    const csharpButton = screen.getByRole('button', { name: 'C#' })
    const pythonButton = screen.getByRole('button', { name: 'Python' })
    expect(csharpButton).toHaveAttribute('aria-pressed', 'true')
    expect(pythonButton).toHaveAttribute('aria-pressed', 'false')

    expect(screen.getByRole('button', { name: 'Run' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Submit' })).toBeEnabled()
    expect(screen.getByText(/Run checks the public tests/i)).toBeInTheDocument()
    expect(screen.getByText('Starter code')).toBeInTheDocument()
  })

  it('tracks edits and resets the current file to its starter content', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(twoSumDetail)))

    renderApp(['/challenges/two-sum'])

    const editor = await screen.findByLabelText('Code editor for Solution.cs')
    expect(screen.getByRole('button', { name: 'Reset file' })).toBeDisabled()

    await user.clear(editor)
    await user.type(editor, 'fixed code')

    expect(screen.getByText('1 file modified')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Reset file' })).toBeEnabled()
    expect(screen.getByRole('img', { name: 'Modified' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Reset file' }))

    expect(editor).toHaveValue(csharpStarter)
    expect(screen.getByText('Starter code')).toBeInTheDocument()
  })

  it('switches languages and preserves per-language drafts', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(twoSumDetail)))

    renderApp(['/challenges/two-sum'])

    await screen.findByLabelText('Code editor for Solution.cs')

    await user.click(screen.getByRole('button', { name: 'Python' }))

    const pythonEditor = screen.getByLabelText('Code editor for solution.py')
    expect(pythonEditor).toHaveAttribute('data-language', 'python')
    expect(pythonEditor).toHaveValue(
      twoSumDetail.languages[1].starterFiles[0].content,
    )
    expect(screen.getByRole('button', { name: 'Python' })).toHaveAttribute(
      'aria-pressed',
      'true',
    )
    expect(screen.getByRole('button', { name: 'Run' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Submit' })).toBeEnabled()

    await user.type(pythonEditor, '# draft')

    expect(screen.getByText('1 file modified')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'C#' }))

    expect(screen.getByLabelText('Code editor for Solution.cs')).toHaveValue(csharpStarter)

    await user.click(screen.getByRole('button', { name: 'Python' }))

    expect(screen.getByLabelText('Code editor for solution.py')).toHaveValue(
      `${twoSumDetail.languages[1].starterFiles[0].content}# draft`,
    )
  })

  it('resets every modified file at once', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(twoSumDetail)))

    renderApp(['/challenges/two-sum'])

    const editor = await screen.findByLabelText('Code editor for Solution.cs')
    await user.type(editor, '// note')
    await user.click(screen.getByRole('button', { name: 'Python' }))
    await user.type(screen.getByLabelText('Code editor for solution.py'), '# note')

    expect(screen.getByText('2 files modified')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Reset all files' }))

    expect(screen.getByText('Starter code')).toBeInTheDocument()
    expect(screen.getByLabelText('Code editor for solution.py')).toHaveValue(
      twoSumDetail.languages[1].starterFiles[0].content,
    )
  })

  it('runs the active language and renders the terminal execution result', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(
      async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input)

        if (url === '/api/challenges/two-sum') {
          return jsonResponse(twoSumDetail)
        }

        if (url === '/api/submissions' && init?.method === 'POST') {
          return jsonResponse(
            {
              id: 'sub-1',
              status: 'Queued',
              createdAtUtc: '2026-11-08T00:00:00Z',
            },
            202,
          )
        }

        if (url === '/api/submissions/sub-1') {
          return jsonResponse({
            id: 'sub-1',
            challengeSlug: 'two-sum',
            languageId: 'csharp',
            status: 'Succeeded',
            isTerminal: true,
            createdAtUtc: '2026-11-08T00:00:00Z',
            startedAtUtc: '2026-11-08T00:00:01Z',
            completedAtUtc: '2026-11-08T00:00:02Z',
            durationMs: 42,
            exitCode: 0,
            standardOutput: '0,1\n',
            standardError: '',
            failureMessage: null,
            outputTruncated: false,
            purpose: 'Run',
          })
        }

        if (url === '/api/submissions/sub-1/results') {
          return jsonResponse({
            submissionId: 'sub-1',
            challengeSlug: 'two-sum',
            languageId: 'csharp',
            purpose: 'Run',
            outcome: 'Accepted',
            isAccepted: true,
            score: 100,
            passedTests: 2,
            totalTests: 2,
            publicPassed: 2,
            publicTotal: 2,
            hiddenPassed: 0,
            hiddenTotal: 0,
            publicTests: [
              {
                name: 'Example 1',
                description: 'The first pair adds up to the target.',
                status: 'Passed',
              },
              {
                name: 'Example 2',
                description: 'The matching pair is not at the start.',
                status: 'Passed',
              },
            ],
            createdAtUtc: '2026-11-08T00:00:02Z',
          })
        }

        throw new Error(`Unexpected request: ${url}`)
      },
    )
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/challenges/two-sum'])

    await screen.findByLabelText('Code editor for Solution.cs')
    await user.click(screen.getByRole('button', { name: 'Run' }))

    expect(await screen.findByText('Succeeded')).toBeInTheDocument()
    expect(screen.getByText(/0,1/)).toBeInTheDocument()
    expect(screen.getByText('42 ms')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Run' })).toBeEnabled()

    const runAssessment = await screen.findByRole('region', { name: 'Assessment result' })
    expect(runAssessment).toHaveTextContent('Accepted')
    expect(runAssessment).toHaveTextContent('100%')
    expect(runAssessment).toHaveTextContent('Run assessment')

    const postCall = fetchMock.mock.calls.find((call) => call[1]?.method === 'POST')
    expect(JSON.parse(String(postCall?.[1]?.body))).toEqual({
      challengeSlug: 'two-sum',
      languageId: 'csharp',
      files: [{ path: 'Solution.cs', content: csharpStarter }],
      purpose: 'Run',
    })
  })

  it('runs the active Python language and sends its own starter file', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(
      async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input)

        if (url === '/api/challenges/two-sum') {
          return jsonResponse(twoSumDetail)
        }

        if (url === '/api/submissions' && init?.method === 'POST') {
          return jsonResponse(
            {
              id: 'sub-py',
              status: 'Queued',
              createdAtUtc: '2026-11-08T00:00:00Z',
            },
            202,
          )
        }

        if (url === '/api/submissions/sub-py') {
          return jsonResponse({
            id: 'sub-py',
            challengeSlug: 'two-sum',
            languageId: 'python',
            status: 'Succeeded',
            isTerminal: true,
            createdAtUtc: '2026-11-08T00:00:00Z',
            startedAtUtc: '2026-11-08T00:00:01Z',
            completedAtUtc: '2026-11-08T00:00:02Z',
            durationMs: 38,
            exitCode: 0,
            standardOutput: '',
            standardError: '',
            failureMessage: null,
            outputTruncated: false,
            purpose: 'Run',
          })
        }

        if (url === '/api/submissions/sub-py/results') {
          return jsonResponse({
            submissionId: 'sub-py',
            challengeSlug: 'two-sum',
            languageId: 'python',
            purpose: 'Run',
            outcome: 'Accepted',
            isAccepted: true,
            score: 100,
            passedTests: 2,
            totalTests: 2,
            publicPassed: 2,
            publicTotal: 2,
            hiddenPassed: 0,
            hiddenTotal: 0,
            publicTests: [
              {
                name: 'Example 1',
                description: 'The first pair adds up to the target.',
                status: 'Passed',
              },
              {
                name: 'Example 2',
                description: 'The matching pair is not at the start.',
                status: 'Passed',
              },
            ],
            createdAtUtc: '2026-11-08T00:00:02Z',
          })
        }

        throw new Error(`Unexpected request: ${url}`)
      },
    )
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/challenges/two-sum'])

    await screen.findByLabelText('Code editor for Solution.cs')
    await user.click(screen.getByRole('button', { name: 'Python' }))
    await user.click(screen.getByRole('button', { name: 'Run' }))

    expect(await screen.findByText('Succeeded')).toBeInTheDocument()
    expect(
      await screen.findByRole('region', { name: 'Assessment result' }),
    ).toHaveTextContent('Accepted')

    const postCall = fetchMock.mock.calls.find((call) => call[1]?.method === 'POST')
    expect(JSON.parse(String(postCall?.[1]?.body))).toEqual({
      challengeSlug: 'two-sum',
      languageId: 'python',
      files: [{ path: 'solution.py', content: pythonStarter }],
      purpose: 'Run',
    })
  })

  it('submits the active language and renders the assessment without hidden tests', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(
      async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input)

        if (url === '/api/challenges/two-sum') {
          return jsonResponse(twoSumDetail)
        }

        if (url === '/api/submissions' && init?.method === 'POST') {
          return jsonResponse(
            {
              id: 'sub-2',
              status: 'Queued',
              createdAtUtc: '2026-11-08T00:00:00Z',
            },
            202,
          )
        }

        if (url === '/api/submissions/sub-2') {
          return jsonResponse({
            id: 'sub-2',
            challengeSlug: 'two-sum',
            languageId: 'csharp',
            status: 'Succeeded',
            isTerminal: true,
            createdAtUtc: '2026-11-08T00:00:00Z',
            startedAtUtc: '2026-11-08T00:00:01Z',
            completedAtUtc: '2026-11-08T00:00:02Z',
            durationMs: 57,
            exitCode: 0,
            standardOutput: 'all tests ran\n',
            standardError: '',
            failureMessage: null,
            outputTruncated: false,
            purpose: 'Submit',
          })
        }

        if (url === '/api/submissions/sub-2/results') {
          return jsonResponse({
            submissionId: 'sub-2',
            challengeSlug: 'two-sum',
            languageId: 'csharp',
            purpose: 'Submit',
            outcome: 'Accepted',
            isAccepted: true,
            score: 100,
            passedTests: 4,
            totalTests: 4,
            publicPassed: 2,
            publicTotal: 2,
            hiddenPassed: 2,
            hiddenTotal: 2,
            publicTests: [
              {
                name: 'Example 1',
                description: 'The first pair adds up to the target.',
                status: 'Passed',
              },
              {
                name: 'Example 2',
                description: 'The matching pair is not at the start.',
                status: 'Passed',
              },
            ],
            createdAtUtc: '2026-11-08T00:00:02Z',
          })
        }

        throw new Error(`Unexpected request: ${url}`)
      },
    )
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/challenges/two-sum'])

    await screen.findByLabelText('Code editor for Solution.cs')
    await user.click(screen.getByRole('button', { name: 'Submit' }))

    const assessment = await screen.findByRole('region', { name: 'Assessment result' })
    expect(assessment).toHaveTextContent('Accepted')
    expect(assessment).toHaveTextContent('Submit assessment')
    expect(assessment).toHaveTextContent('4 / 4')
    expect(assessment).toHaveTextContent('2 / 2')
    expect(assessment).toHaveTextContent('Hidden tests are scored on the server')
    expect(assessment).not.toHaveTextContent('Duplicate values')
    expect(assessment).not.toHaveTextContent('Negative numbers')

    const postCall = fetchMock.mock.calls.find((call) => call[1]?.method === 'POST')
    expect(JSON.parse(String(postCall?.[1]?.body))).toEqual({
      challengeSlug: 'two-sum',
      languageId: 'csharp',
      files: [{ path: 'Solution.cs', content: csharpStarter }],
      purpose: 'Submit',
    })
  })

  it('shows the server rejection when a run cannot be started', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(
      async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input)

        if (url === '/api/challenges/two-sum') {
          return jsonResponse(twoSumDetail)
        }

        if (url === '/api/submissions' && init?.method === 'POST') {
          return jsonResponse(
            {
              title: 'Submission rejected.',
              detail: 'This challenge cannot be executed with the selected language.',
            },
            400,
          )
        }

        throw new Error(`Unexpected request: ${url}`)
      },
    )
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/challenges/two-sum'])

    await screen.findByLabelText('Code editor for Solution.cs')
    await user.click(screen.getByRole('button', { name: 'Run' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'This challenge cannot be executed with the selected language.',
    )
    expect(screen.getByRole('button', { name: 'Run' })).toBeEnabled()
  })

  it('opens a SQL challenge in the SQL editor and submits query.sql', async () => {
    const user = userEvent.setup()
    const sqlStarter = orderRevenueDetail.languages[0].starterFiles[0].content
    const fetchMock = vi.fn(
      async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input)

        if (url === '/api/challenges/order-revenue') {
          return jsonResponse(orderRevenueDetail)
        }

        if (url === '/api/submissions' && init?.method === 'POST') {
          return jsonResponse(
            {
              id: 'sub-sql',
              status: 'Queued',
              createdAtUtc: '2026-11-13T00:00:00Z',
            },
            202,
          )
        }

        if (url === '/api/submissions/sub-sql') {
          return jsonResponse({
            id: 'sub-sql',
            challengeSlug: 'order-revenue',
            languageId: 'sql',
            status: 'Succeeded',
            isTerminal: true,
            createdAtUtc: '2026-11-13T00:00:00Z',
            startedAtUtc: '2026-11-13T00:00:01Z',
            completedAtUtc: '2026-11-13T00:00:02Z',
            durationMs: 35,
            exitCode: 0,
            standardOutput: '',
            standardError: '',
            failureMessage: null,
            outputTruncated: false,
            purpose: 'Run',
          })
        }

        if (url === '/api/submissions/sub-sql/results') {
          return jsonResponse({
            submissionId: 'sub-sql',
            challengeSlug: 'order-revenue',
            languageId: 'sql',
            purpose: 'Run',
            outcome: 'Accepted',
            isAccepted: true,
            score: 100,
            passedTests: 2,
            totalTests: 2,
            publicPassed: 2,
            publicTotal: 2,
            hiddenPassed: 0,
            hiddenTotal: 0,
            publicTests: [
              {
                name: 'Standard customer set',
                description: 'Every seeded customer is returned with completed revenue.',
                status: 'Passed',
              },
              {
                name: 'Single customer',
                description: 'One customer with one completed order produces one row.',
                status: 'Passed',
              },
            ],
            createdAtUtc: '2026-11-13T00:00:02Z',
          })
        }

        throw new Error(`Unexpected request: ${url}`)
      },
    )
    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/challenges/order-revenue'])

    const editor = await screen.findByLabelText('Code editor for query.sql')
    expect(editor).toHaveAttribute('data-language', 'sql')
    expect(editor).toHaveValue(sqlStarter)

    await user.click(screen.getByRole('button', { name: 'Run' }))

    expect(await screen.findByText('Succeeded')).toBeInTheDocument()
    expect(
      await screen.findByRole('region', { name: 'Assessment result' }),
    ).toHaveTextContent('Accepted')

    const postCall = fetchMock.mock.calls.find((call) => call[1]?.method === 'POST')
    expect(JSON.parse(String(postCall?.[1]?.body))).toEqual({
      challengeSlug: 'order-revenue',
      languageId: 'sql',
      files: [{ path: 'query.sql', content: sqlStarter }],
      purpose: 'Run',
    })
  })

  it('shows an error state and can retry loading the challenge', async () => {
    const user = userEvent.setup()
    const fetchMock = vi
      .fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(jsonResponse(twoSumDetail))

    vi.stubGlobal('fetch', fetchMock)

    renderApp(['/challenges/two-sum'])

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Unable to load this challenge')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(
      await screen.findByRole('heading', { name: 'Two Sum', level: 1 }),
    ).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
