import { useEffect, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { useParams } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { ChallengeDetail } from '@/shared/api/challenges'
import {
  createSubmission,
  getAssessmentResult,
  getSubmission,
  type AssessmentResult,
  type SubmissionDetail,
  type SubmissionPurpose,
} from '@/shared/api/submissions'
import { CodeEditor } from '@/shared/components/CodeEditor'
import { Breadcrumbs, ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

function draftKey(languageId: string, filePath: string) {
  return `${languageId}:${filePath}`
}

function MetaBadge({ children }: { children: string }) {
  return (
    <span className="rounded-full border border-white/10 bg-white/[0.04] px-2.5 py-1 text-[10px] font-medium tracking-[0.14em] text-slate-400 uppercase">
      {children}
    </span>
  )
}

function statusTone(status: string) {
  if (status === 'Succeeded' || status === 'Accepted') {
    return 'border-emerald-400/25 bg-emerald-400/10 text-emerald-100'
  }

  if (
    status === 'Queued'
    || status === 'Running'
    || status === 'WrongAnswer'
  ) {
    return 'border-amber-300/25 bg-amber-300/10 text-amber-100'
  }

  return 'border-rose-400/25 bg-rose-400/10 text-rose-100'
}

function testStatusTone(status: string) {
  if (status === 'Passed') {
    return 'text-emerald-200'
  }

  if (status === 'NotRun') {
    return 'text-slate-500'
  }

  return 'text-rose-200'
}

export function ChallengeWorkspace({
  challenge,
  mode = 'practice',
  examinationId,
  submissionAllowed = true,
  disabledReason,
}: {
  challenge: ChallengeDetail
  mode?: 'practice' | 'examination'
  examinationId?: string
  submissionAllowed?: boolean
  disabledReason?: string
}) {
  const isExamination = mode === 'examination'
  const firstLanguage = challenge.languages[0]
  const [activeLanguageId, setActiveLanguageId] = useState(firstLanguage?.id ?? '')
  const [activeFilePath, setActiveFilePath] = useState(
    firstLanguage?.starterFiles[0]?.path ?? '',
  )
  const [overrides, setOverrides] = useState<Record<string, string>>({})
  const [submissionId, setSubmissionId] = useState<string | null>(null)
  const [submission, setSubmission] = useState<SubmissionDetail | null>(null)
  const [assessment, setAssessment] = useState<AssessmentResult | null>(null)
  const [assessmentError, setAssessmentError] = useState<string | null>(null)
  const [runStatus, setRunStatus] = useState<'idle' | 'starting' | 'running' | 'error'>(
    'idle',
  )
  const [runError, setRunError] = useState<string | null>(null)
  const createController = useRef<AbortController | null>(null)

  useEffect(() => {
    return () => {
      createController.current?.abort()
    }
  }, [])

  useEffect(() => {
    if (!submissionId) {
      return
    }

    const id = submissionId
    const controller = new AbortController()
    let active = true
    let timer: number | undefined

    async function poll() {
      try {
        const detail = await getSubmission(id, controller.signal)

        if (!active) {
          return
        }

        setSubmission(detail)

        if (detail.isTerminal) {
          try {
            const result = await getAssessmentResult(id, controller.signal)

            if (!active) {
              return
            }

            setAssessment(result)
            setAssessmentError(null)
          } catch (error) {
            if (!active || controller.signal.aborted) {
              return
            }

            setAssessment(null)
            setAssessmentError(
              error instanceof Error
                ? error.message
                : 'The assessment result could not be retrieved.',
            )
          }

          if (active) {
            setRunStatus('idle')
          }

          return
        }

        setRunStatus('running')
        timer = window.setTimeout(() => {
          void poll()
        }, 1000)
      } catch (error) {
        if (!active || controller.signal.aborted) {
          return
        }

        setRunStatus('error')
        setRunError(
          error instanceof Error
            ? error.message
            : 'The execution result could not be retrieved.',
        )
      }
    }

    void poll()

    return () => {
      active = false
      controller.abort()

      if (timer !== undefined) {
        window.clearTimeout(timer)
      }
    }
  }, [submissionId])

  const activeLanguage =
    challenge.languages.find((language) => language.id === activeLanguageId) ?? firstLanguage
  const starterFile =
    activeLanguage?.starterFiles.find((file) => file.path === activeFilePath) ??
    activeLanguage?.starterFiles[0]

  if (!activeLanguage || !starterFile) {
    return (
      <div className="rounded-2xl border border-rose-400/20 bg-rose-400/[0.06] p-6" role="alert">
        <p className="text-sm font-medium text-rose-100">
          This challenge has no editable starter files.
        </p>
      </div>
    )
  }

  const activeDraftKey = draftKey(activeLanguage.id, starterFile.path)
  const content = overrides[activeDraftKey] ?? starterFile.content
  const isDirty = content !== starterFile.content
  const modifiedCount = Object.keys(overrides).length
  const isRunning = runStatus === 'starting' || runStatus === 'running'

  function selectLanguage(languageId: string) {
    const language = challenge.languages.find((candidate) => candidate.id === languageId)

    if (!language) {
      return
    }

    setActiveLanguageId(language.id)
    setActiveFilePath(language.starterFiles[0]?.path ?? '')
  }

  function updateContent(nextContent: string) {
    setOverrides((current) => {
      const next = { ...current }

      if (nextContent === starterFile.content) {
        delete next[activeDraftKey]
      } else {
        next[activeDraftKey] = nextContent
      }

      return next
    })
  }

  function resetCurrentFile() {
    setOverrides((current) => {
      const next = { ...current }
      delete next[activeDraftKey]
      return next
    })
  }

  function resetAllFiles() {
    setOverrides({})
  }

  async function startSubmission(purpose: SubmissionPurpose) {
    if (!activeLanguage.canExecute || isRunning || !submissionAllowed) {
      return
    }

    if (isExamination && purpose !== 'Submit') {
      return
    }

    createController.current?.abort()

    const controller = new AbortController()
    createController.current = controller

    setRunStatus('starting')
    setRunError(null)
    setSubmission(null)
    setSubmissionId(null)
    setAssessment(null)
    setAssessmentError(null)

    try {
      const files = activeLanguage.starterFiles.map((file) => ({
        path: file.path,
        content: overrides[draftKey(activeLanguage.id, file.path)] ?? file.content,
      }))

      const created = await createSubmission(
        {
          challengeSlug: challenge.slug,
          languageId: activeLanguage.id,
          files,
          purpose,
          ...(examinationId ? { examinationId } : {}),
        },
        controller.signal,
      )

      if (controller.signal.aborted) {
        return
      }

      setSubmissionId(created.id)
    } catch (error) {
      if (controller.signal.aborted) {
        return
      }

      setRunStatus('error')
      setRunError(
        error instanceof Error ? error.message : 'The submission could not be started.',
      )
    }
  }

  return (
    <div className="space-y-6">
      <header className="border-b border-white/10 pb-6">
        <div className="flex flex-wrap items-center gap-2">
          <MetaBadge>{challenge.difficulty}</MetaBadge>
          <MetaBadge>{challenge.type}</MetaBadge>
          <MetaBadge>{`${challenge.estimatedMinutes} min`}</MetaBadge>
          <span className="text-[11px] text-slate-500">{challenge.topic}</span>
        </div>
        <h1 className="mt-4 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
          {challenge.title}
        </h1>
        <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">{challenge.summary}</p>
      </header>

      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,440px)_minmax(0,1fr)]">
        <article className="rounded-2xl border border-white/10 bg-slate-900/45 p-5 xl:sticky xl:top-8 xl:max-h-[calc(100vh-4rem)] xl:overflow-y-auto">
          {challenge.objectives.length > 0 ? (
            <section aria-labelledby="challenge-objectives">
              <h2
                className="text-[10px] font-semibold tracking-[0.2em] text-emerald-300 uppercase"
                id="challenge-objectives"
              >
                Learning objectives
              </h2>
              <ul className="mt-3 space-y-2">
                {challenge.objectives.map((objective) => (
                  <li className="flex gap-2 text-xs leading-5 text-slate-400" key={objective}>
                    <span
                      aria-hidden="true"
                      className="mt-2 size-1 shrink-0 rounded-full bg-emerald-400/70"
                    />
                    <span>{objective}</span>
                  </li>
                ))}
              </ul>
            </section>
          ) : null}

          <section
            aria-labelledby="challenge-statement"
            className="mt-6 border-t border-white/[0.07] pt-6"
          >
            <h2
              className="text-[10px] font-semibold tracking-[0.2em] text-emerald-300 uppercase"
              id="challenge-statement"
            >
              Statement
            </h2>
            <div className="learning-prose mt-4">
              <ReactMarkdown
                components={{ h1: 'h2' }}
                remarkPlugins={[remarkGfm]}
              >
                {challenge.statementMarkdown}
              </ReactMarkdown>
            </div>
          </section>

          {challenge.constraints.length > 0 ? (
            <section
              aria-labelledby="challenge-constraints"
              className="mt-6 border-t border-white/[0.07] pt-5"
            >
              <h2 className="text-sm font-semibold text-white" id="challenge-constraints">
                Constraints
              </h2>
              <ul className="mt-3 space-y-2">
                {challenge.constraints.map((constraint) => (
                  <li
                    className="flex gap-2 text-xs leading-5 text-slate-400"
                    key={constraint}
                  >
                    <span aria-hidden="true" className="text-emerald-400/80">
                      •
                    </span>
                    <span>{constraint}</span>
                  </li>
                ))}
              </ul>
            </section>
          ) : null}

          {challenge.examples.length > 0 ? (
            <section
              aria-labelledby="challenge-examples"
              className="mt-6 border-t border-white/[0.07] pt-5"
            >
              <h2 className="text-sm font-semibold text-white" id="challenge-examples">
                Examples
              </h2>
              <div className="mt-3 space-y-3">
                {challenge.examples.map((example, index) => (
                  <div
                    className="rounded-xl border border-white/[0.08] bg-slate-950/45 p-3"
                    key={`${example.input}-${index}`}
                  >
                    <dl className="space-y-2 text-xs">
                      <div>
                        <dt className="text-[10px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                          Input
                        </dt>
                        <dd className="mt-1 font-mono text-[11px] whitespace-pre-wrap text-slate-300">
                          {example.input}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-[10px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                          Output
                        </dt>
                        <dd className="mt-1 font-mono text-[11px] whitespace-pre-wrap text-emerald-200">
                          {example.output}
                        </dd>
                      </div>
                    </dl>
                    {example.explanation ? (
                      <p className="mt-2 text-[11px] leading-5 text-slate-500">
                        {example.explanation}
                      </p>
                    ) : null}
                  </div>
                ))}
              </div>
            </section>
          ) : null}

          {!isExamination && challenge.hintsMarkdown ? (
            <details className="mt-6 rounded-xl border border-white/[0.08] bg-white/[0.02] p-4">
              <summary className="cursor-pointer text-xs font-medium text-emerald-200">
                Show hints
              </summary>
              <div className="learning-prose mt-3">
                <ReactMarkdown
                  components={{ h1: 'h2' }}
                  remarkPlugins={[remarkGfm]}
                >
                  {challenge.hintsMarkdown}
                </ReactMarkdown>
              </div>
            </details>
          ) : null}

          <section
            aria-labelledby="challenge-public-tests"
            className="mt-6 border-t border-white/[0.07] pt-5"
          >
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-sm font-semibold text-white" id="challenge-public-tests">
                Public tests
              </h2>
              <span className="rounded-full border border-white/10 bg-white/[0.04] px-2 py-0.5 text-[10px] tracking-[0.12em] text-slate-500 uppercase">
                {challenge.publicTests.length}
              </span>
            </div>
            <ul className="mt-3 space-y-2">
              {challenge.publicTests.map((test) => (
                <li
                  className="rounded-lg border border-white/[0.07] bg-slate-950/40 px-3 py-2"
                  key={test.name}
                >
                  <p className="text-xs font-medium text-slate-300">{test.name}</p>
                  {test.description ? (
                    <p className="mt-0.5 text-[11px] leading-5 text-slate-500">
                      {test.description}
                    </p>
                  ) : null}
                </li>
              ))}
            </ul>
            <p className="mt-3 text-[11px] leading-5 text-slate-500">
              Run executes these public tests. Submit also runs hidden tests and returns only
              their pass counts.
            </p>
          </section>
        </article>

        <section
          aria-label="Code workspace"
          className="overflow-hidden rounded-2xl border border-white/10 bg-slate-900/55"
        >
          <div className="border-b border-white/[0.07] px-4 py-3">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div aria-label="Language" className="flex flex-wrap gap-1" role="group">
                {challenge.languages.map((language) => {
                  const isActive = language.id === activeLanguage.id

                  return (
                    <button
                      aria-pressed={isActive}
                      className={[
                        'rounded-lg px-2.5 py-1.5 text-xs font-medium transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300',
                        isActive
                          ? 'bg-emerald-400/15 text-emerald-100 ring-1 ring-emerald-400/25'
                          : 'text-slate-400 hover:bg-white/[0.04] hover:text-slate-200',
                      ].join(' ')}
                      key={language.id}
                      onClick={() => selectLanguage(language.id)}
                      type="button"
                    >
                      {language.name}
                    </button>
                  )
                })}
              </div>

              <p aria-live="polite" className="text-[11px] text-slate-500">
                {modifiedCount === 0
                  ? 'Starter code'
                  : `${modifiedCount} ${modifiedCount === 1 ? 'file' : 'files'} modified`}
              </p>
            </div>

            <div
              aria-label="Starter files"
              className="mt-3 flex flex-wrap items-center gap-1"
              role="tablist"
            >
              {activeLanguage.starterFiles.map((file) => {
                const isActive = file.path === starterFile.path
                const isModified = Boolean(
                  overrides[draftKey(activeLanguage.id, file.path)],
                )

                return (
                  <button
                    aria-selected={isActive}
                    className={[
                      'flex items-center gap-1.5 rounded-md px-2.5 py-1 font-mono text-[11px] transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300',
                      isActive
                        ? 'bg-white/[0.07] text-slate-100'
                        : 'text-slate-500 hover:bg-white/[0.04] hover:text-slate-300',
                    ].join(' ')}
                    key={file.path}
                    onClick={() => setActiveFilePath(file.path)}
                    role="tab"
                    type="button"
                  >
                    {file.path}
                    {isModified ? (
                      <span
                        aria-label="Modified"
                        className="size-1.5 rounded-full bg-amber-300"
                        role="img"
                      />
                    ) : null}
                  </button>
                )
              })}
            </div>
          </div>

          <div className="h-[540px] bg-[#07101d]">
            <CodeEditor
              filePath={starterFile.path}
              language={activeLanguage.editorLanguage}
              onChange={updateContent}
              value={content}
            />
          </div>

          <div className="border-t border-white/[0.07]">
            <div className="flex flex-wrap items-center gap-2 p-4">
              {isExamination ? null : (
                <button
                  aria-describedby="execution-note"
                  className={[
                    'rounded-lg border px-3 py-1.5 text-xs font-medium transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300',
                    activeLanguage.canExecute && !isRunning
                      ? 'border-emerald-400/30 bg-emerald-400/10 text-emerald-100 hover:bg-emerald-400/15'
                      : 'cursor-not-allowed border-white/10 bg-white/[0.03] text-slate-500',
                  ].join(' ')}
                  disabled={!activeLanguage.canExecute || isRunning}
                  onClick={() => {
                    void startSubmission('Run')
                  }}
                  type="button"
                >
                  {isRunning ? 'Running...' : 'Run'}
                </button>
              )}
              <button
                aria-describedby="execution-note"
                className={[
                  'rounded-lg border px-3 py-1.5 text-xs font-medium transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300',
                  activeLanguage.canExecute && !isRunning && submissionAllowed
                    ? 'border-emerald-400/30 bg-emerald-400/10 text-emerald-100 hover:bg-emerald-400/15'
                    : 'cursor-not-allowed border-white/10 bg-white/[0.03] text-slate-500',
                ].join(' ')}
                disabled={!activeLanguage.canExecute || isRunning || !submissionAllowed}
                onClick={() => {
                  void startSubmission('Submit')
                }}
                type="button"
              >
                {isExamination ? 'Submit for assessment' : 'Submit'}
              </button>
              <span aria-hidden="true" className="mx-1 h-4 w-px bg-white/10" />
              <button
                className="rounded-lg border border-white/10 bg-white/[0.04] px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:border-white/20 hover:text-white disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                disabled={!isDirty}
                onClick={resetCurrentFile}
                type="button"
              >
                Reset file
              </button>
              <button
                className="rounded-lg border border-white/10 bg-white/[0.04] px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:border-white/20 hover:text-white disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                disabled={modifiedCount === 0}
                onClick={resetAllFiles}
                type="button"
              >
                Reset all files
              </button>
              <p className="w-full text-[11px] leading-5 text-slate-500" id="execution-note">
                {!submissionAllowed && disabledReason
                  ? disabledReason
                  : !activeLanguage.canExecute
                    ? 'Execution is not available for this language yet. Choose a language marked as executable.'
                    : isExamination
                      ? 'Submissions run the hidden assessment tests. Hints and reference solutions are unavailable during the examination.'
                      : 'Run checks the public tests only. Submit runs the public and hidden tests and records a score. Hidden test names and data never leave the server.'}
              </p>
            </div>

            <div aria-live="polite" className="space-y-3 border-t border-white/[0.07] px-4 py-4">
              {runStatus === 'starting' ? (
                <p className="text-xs text-slate-400">Starting an isolated execution...</p>
              ) : null}

              {runStatus === 'running' ? (
                <p className="text-xs text-slate-400">Running in an isolated container...</p>
              ) : null}

              {runStatus === 'error' && runError ? (
                <p
                  className="rounded-lg border border-rose-400/20 bg-rose-400/[0.06] px-3 py-2 text-xs text-rose-100"
                  role="alert"
                >
                  {runError}
                </p>
              ) : null}

              {submission ? (
                <section
                  aria-label="Execution result"
                  className="rounded-xl border border-white/[0.08] bg-slate-950/45 p-3"
                >
                  <div className="flex flex-wrap items-center gap-2">
                    <span
                      className={[
                        'rounded-full border px-2 py-0.5 text-[10px] font-semibold tracking-[0.12em] uppercase',
                        statusTone(submission.status),
                      ].join(' ')}
                    >
                      {submission.status}
                    </span>
                    {submission.durationMs != null ? (
                      <span className="text-[11px] text-slate-500">
                        {submission.durationMs} ms
                      </span>
                    ) : null}
                    {submission.exitCode != null ? (
                      <span className="text-[11px] text-slate-500">
                        exit code {submission.exitCode}
                      </span>
                    ) : null}
                  </div>

                  {submission.failureMessage ? (
                    <p className="mt-2 text-xs leading-5 text-rose-100">
                      {submission.failureMessage}
                    </p>
                  ) : null}

                  {submission.standardOutput ? (
                    <div className="mt-3">
                      <p className="text-[10px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                        Output
                      </p>
                      <pre className="mt-1 max-h-56 overflow-auto rounded-lg bg-black/30 p-3 font-mono text-[11px] whitespace-pre-wrap text-slate-200">
                        {submission.standardOutput}
                      </pre>
                    </div>
                  ) : null}

                  {submission.standardError ? (
                    <div className="mt-3">
                      <p className="text-[10px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                        Diagnostics
                      </p>
                      <pre className="mt-1 max-h-56 overflow-auto rounded-lg bg-black/30 p-3 font-mono text-[11px] whitespace-pre-wrap text-amber-100">
                        {submission.standardError}
                      </pre>
                    </div>
                  ) : null}

                  {!submission.standardOutput
                  && !submission.standardError
                  && !submission.failureMessage ? (
                    <p className="mt-2 text-xs text-slate-500">No output was produced.</p>
                  ) : null}

                  {submission.outputTruncated ? (
                    <p className="mt-2 text-[11px] text-amber-200">
                      Output was truncated at the server limit.
                    </p>
                  ) : null}
                </section>
              ) : null}

              {assessmentError ? (
                <p
                  className="rounded-lg border border-amber-300/20 bg-amber-300/[0.06] px-3 py-2 text-xs text-amber-100"
                  role="alert"
                >
                  {assessmentError}
                </p>
              ) : null}

              {assessment ? (
                <section
                  aria-label="Assessment result"
                  className="rounded-xl border border-white/[0.08] bg-slate-950/45 p-3"
                >
                  <div className="flex flex-wrap items-center gap-2">
                    <span
                      className={[
                        'rounded-full border px-2 py-0.5 text-[10px] font-semibold tracking-[0.12em] uppercase',
                        statusTone(assessment.outcome),
                      ].join(' ')}
                    >
                      {assessment.outcome}
                    </span>
                    <span className="text-[11px] text-slate-500">
                      {assessment.purpose === 'Submit' ? 'Submit assessment' : 'Run assessment'}
                    </span>
                    <span className="text-sm font-semibold text-white">
                      {assessment.score}%
                    </span>
                  </div>

                  <dl className="mt-3 grid grid-cols-3 gap-2 text-xs">
                    <div className="rounded-lg bg-white/[0.03] px-2.5 py-2">
                      <dt className="text-[10px] tracking-[0.14em] text-slate-500 uppercase">
                        Passed
                      </dt>
                      <dd className="mt-0.5 text-slate-200">
                        {assessment.passedTests} / {assessment.totalTests}
                      </dd>
                    </div>
                    <div className="rounded-lg bg-white/[0.03] px-2.5 py-2">
                      <dt className="text-[10px] tracking-[0.14em] text-slate-500 uppercase">
                        Public
                      </dt>
                      <dd className="mt-0.5 text-slate-200">
                        {assessment.publicPassed} / {assessment.publicTotal}
                      </dd>
                    </div>
                    <div className="rounded-lg bg-white/[0.03] px-2.5 py-2">
                      <dt className="text-[10px] tracking-[0.14em] text-slate-500 uppercase">
                        Hidden
                      </dt>
                      <dd className="mt-0.5 text-slate-200">
                        {assessment.hiddenPassed} / {assessment.hiddenTotal}
                      </dd>
                    </div>
                  </dl>

                  {assessment.publicTests.length > 0 ? (
                    <div className="mt-3">
                      <p className="text-[10px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                        Public test outcomes
                      </p>
                      <ul className="mt-1.5 space-y-1">
                        {assessment.publicTests.map((test) => (
                          <li
                            className="flex items-center justify-between gap-3 rounded-md bg-black/20 px-2.5 py-1.5 text-xs"
                            key={test.name}
                          >
                            <span className="text-slate-300">{test.name}</span>
                            <span
                              className={[
                                'font-mono text-[10px] tracking-[0.08em] uppercase',
                                testStatusTone(test.status),
                              ].join(' ')}
                            >
                              {test.status}
                            </span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  ) : null}

                  {assessment.hiddenTotal > 0 ? (
                    <p className="mt-2 text-[11px] leading-5 text-slate-500">
                      Hidden tests are scored on the server. Only their pass count is shown.
                    </p>
                  ) : null}
                </section>
              ) : null}
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}

export function ChallengeWorkspacePage() {
  const { slug = '' } = useParams<{ slug: string }>()
  const { state, refresh } = useApiQuery<ChallengeDetail>(
    `/api/challenges/${encodeURIComponent(slug)}`,
  )

  return (
    <div className="mx-auto max-w-[1400px] space-y-6">
      <Breadcrumbs
        items={[
          { label: 'Challenges', to: '/challenges' },
          {
            label: state.status === 'success' ? state.data.title : 'Workspace',
          },
        ]}
      />

      {state.status === 'loading' ? <LoadingPanel label="Loading challenge..." /> : null}

      {state.status === 'error' ? (
        <ErrorPanel
          backLabel="Back to challenges"
          backTo="/challenges"
          message={state.message}
          onRetry={refresh}
          title="Unable to load this challenge"
        />
      ) : null}

      {state.status === 'success' ? (
        <ChallengeWorkspace challenge={state.data} key={state.data.slug} />
      ) : null}
    </div>
  )
}
