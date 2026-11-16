import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import type { ChallengeSummary } from '@/shared/api/challenges'
import {
  createExamination,
  type ExaminationSessionSummary,
} from '@/shared/api/progress'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import { ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

function statusDot(status: string) {
  if (status === 'InProgress') {
    return 'bg-emerald-400'
  }

  if (status === 'Expired') {
    return 'bg-amber-400'
  }

  return 'bg-slate-500'
}

function formatStarted(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function ExaminationsPage() {
  const navigate = useNavigate()
  const challengesQuery = useApiQuery<ChallengeSummary[]>('/api/challenges')
  const sessionsQuery = useApiQuery<ExaminationSessionSummary[]>('/api/examinations')
  const [selectedSlug, setSelectedSlug] = useState('')
  const [starting, setStarting] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const challenges = useMemo(
    () => (challengesQuery.state.status === 'success' ? challengesQuery.state.data : []),
    [challengesQuery.state],
  )
  const sessions = useMemo(
    () => (sessionsQuery.state.status === 'success' ? sessionsQuery.state.data : []),
    [sessionsQuery.state],
  )
  const activeSlug = selectedSlug || challenges[0]?.slug || ''

  async function startExamination() {
    if (!activeSlug || starting) {
      return
    }

    setStarting(true)
    setActionError(null)

    try {
      const session = await createExamination(activeSlug)
      navigate(`/examinations/${session.id}`)
    } catch (error) {
      setActionError(
        error instanceof Error
          ? error.message
          : 'The examination could not be started.',
      )
    } finally {
      setStarting(false)
    }
  }

  function refreshAll() {
    challengesQuery.refresh()
    sessionsQuery.refresh()
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight text-white">Examinations</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
          The server selects a variant and time limit, runs the hidden assessment tests, and never
          exposes hints. Finish whenever you are ready, or let the limit expire.
        </p>
      </header>

      <section
        aria-labelledby="start-examination-heading"
        className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5"
      >
        <h2 className="text-sm font-semibold text-white" id="start-examination-heading">
          Start an examination
        </h2>
        <p className="mt-1 max-w-2xl text-xs leading-5 text-slate-500">
          Choose a challenge with hidden tests. Challenges that cannot be executed are rejected by
          the server with a clear message.
        </p>

        <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end">
          <label className="flex-1">
            <span className="mb-1 block text-xs text-slate-500">Challenge</span>
            <select
              className="w-full rounded-md border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 focus:border-white/25 focus:outline-none"
              disabled={challenges.length === 0 || starting}
              onChange={(event) => setSelectedSlug(event.target.value)}
              value={activeSlug}
            >
              {challenges.length === 0 ? (
                <option value="">No challenges available</option>
              ) : null}
              {challenges.map((challenge) => (
                <option key={challenge.slug} value={challenge.slug}>
                  {challenge.title}
                </option>
              ))}
            </select>
          </label>

          <button
            className="rounded-md border border-emerald-400/30 bg-emerald-400/10 px-4 py-2 text-xs font-medium text-emerald-100 transition-colors hover:bg-emerald-400/15 disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            disabled={!activeSlug || starting}
            onClick={() => {
              void startExamination()
            }}
            type="button"
          >
            {starting ? 'Starting...' : 'Start examination'}
          </button>
        </div>

        {actionError ? (
          <p
            className="mt-3 rounded-md border border-rose-400/20 bg-rose-400/[0.04] px-3 py-2 text-xs text-rose-200"
            role="alert"
          >
            {actionError}
          </p>
        ) : null}
      </section>

      <section aria-labelledby="examination-history-heading">
        <div className="flex items-end justify-between gap-4">
          <div>
            <h2 className="text-base font-semibold text-white" id="examination-history-heading">
              Examination history
            </h2>
            <p className="mt-1 text-xs text-slate-500">
              Newest sessions first. Resume an active session or review a completed attempt.
            </p>
          </div>
          <button
            className="rounded-md border border-white/10 bg-white/[0.03] px-2.5 py-1 text-[11px] text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            onClick={refreshAll}
            type="button"
          >
            Refresh
          </button>
        </div>

        {sessionsQuery.state.status === 'loading' ? (
          <div className="mt-4">
            <LoadingPanel label="Loading examination history..." />
          </div>
        ) : null}

        {sessionsQuery.state.status === 'error' ? (
          <div className="mt-4">
            <ErrorPanel
              backLabel="Back to dashboard"
              backTo="/"
              message={sessionsQuery.state.message}
              onRetry={sessionsQuery.refresh}
              title="Unable to load examination history"
            />
          </div>
        ) : null}

        {sessionsQuery.state.status === 'success' && sessions.length === 0 ? (
          <div className="mt-4 rounded-lg border border-dashed border-white/[0.1] bg-white/[0.01] p-8 text-center">
            <p className="text-sm text-slate-300">No examinations yet.</p>
            <p className="mt-1 text-xs text-slate-500">
              Start one above to generate assessment evidence.
            </p>
          </div>
        ) : null}

        {sessionsQuery.state.status === 'success' && sessions.length > 0 ? (
          <ul className="mt-4 space-y-2">
            {sessions.map((session) => (
              <li key={session.id}>
                <Link
                  className="group flex flex-wrap items-center gap-x-3 gap-y-1 rounded-lg border border-white/[0.08] bg-white/[0.02] p-4 text-[11px] transition-colors hover:border-white/[0.16] hover:bg-white/[0.03] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                  to={`/examinations/${session.id}`}
                >
                  <span className="inline-flex items-center gap-1.5 text-slate-400">
                    <span
                      aria-hidden="true"
                      className={['size-1.5 rounded-full', statusDot(session.status)].join(' ')}
                    />
                    {session.status}
                  </span>
                  <span className="text-sm text-slate-100">{session.challengeTitle}</span>
                  <span className="text-slate-500">{session.variantLabel}</span>
                  <span className="text-slate-500">
                    {session.attemptCount}{' '}
                    {session.attemptCount === 1 ? 'attempt' : 'attempts'}
                  </span>
                  {session.timeLimitSeconds != null ? (
                    <span className="text-slate-500">
                      Limit {Math.round(session.timeLimitSeconds / 60)} min
                    </span>
                  ) : null}
                  <span className="ml-auto text-slate-500">
                    {formatStarted(session.startedAtUtc)}
                  </span>
                  <span className="text-slate-400 group-hover:text-slate-200">Open →</span>
                </Link>
              </li>
            ))}
          </ul>
        ) : null}
      </section>
    </div>
  )
}
