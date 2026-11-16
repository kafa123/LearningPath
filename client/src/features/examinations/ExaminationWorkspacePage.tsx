import { useEffect, useState } from 'react'
import { useParams } from 'react-router'
import { ChallengeWorkspace } from '@/features/challenges/ChallengeWorkspacePage'
import {
  finishExamination,
  type ExaminationSessionDetail,
} from '@/shared/api/progress'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import { Breadcrumbs, ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

function formatDuration(totalSeconds: number) {
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${seconds.toString().padStart(2, '0')}`
}

function statusDot(status: string) {
  if (status === 'InProgress') {
    return 'bg-emerald-400'
  }

  if (status === 'Expired') {
    return 'bg-amber-400'
  }

  return 'bg-slate-500'
}

function ExaminationSession({
  detail,
  onRefresh,
}: {
  detail: ExaminationSessionDetail
  onRefresh: () => void
}) {
  const [nowMs, setNowMs] = useState(() => Date.now())
  const [finishing, setFinishing] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  useEffect(() => {
    const timer = window.setInterval(() => setNowMs(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [])

  const remainingSeconds = detail.expiresAtUtc
    ? Math.max(0, Math.ceil((Date.parse(detail.expiresAtUtc) - nowMs) / 1000))
    : (detail.remainingSeconds ?? null)
  const isActive = detail.isActive && (remainingSeconds === null || remainingSeconds > 0)

  async function finish() {
    if (finishing) {
      return
    }

    setFinishing(true)
    setActionError(null)

    try {
      await finishExamination(detail.id)
      onRefresh()
    } catch (error) {
      setActionError(
        error instanceof Error
          ? error.message
          : 'The examination could not be finished.',
      )
    } finally {
      setFinishing(false)
    }
  }

  return (
    <div className="space-y-6">
      <Breadcrumbs
        items={[
          { label: 'Examinations', to: '/examinations' },
          { label: detail.challengeTitle },
        ]}
      />

      <header className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[11px]">
          <span className="inline-flex items-center gap-1.5 text-slate-400">
            <span
              aria-hidden="true"
              className={['size-1.5 rounded-full', statusDot(detail.status)].join(' ')}
            />
            {detail.status}
          </span>
          <span className="text-slate-500">{detail.variantLabel}</span>
          {detail.timeLimitSeconds != null ? (
            <span className="text-slate-500">
              Limit {formatDuration(detail.timeLimitSeconds)}
            </span>
          ) : null}
          {isActive && remainingSeconds != null ? (
            <span
              aria-live="polite"
              className="ml-auto font-mono text-xs text-slate-300"
            >
              {formatDuration(remainingSeconds)} remaining
            </span>
          ) : null}
        </div>

        <h1 className="mt-3 text-xl font-semibold tracking-tight text-white sm:text-2xl">
          {detail.challengeTitle} examination
        </h1>
        <p className="mt-1.5 max-w-3xl text-sm leading-6 text-slate-400">
          The server selected this variant and its time limit. Hidden tests are used for scoring,
          and hints are not available during an examination.
        </p>

        {isActive ? (
          <button
            className="mt-4 rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            disabled={finishing}
            onClick={() => {
              void finish()
            }}
            type="button"
          >
            {finishing ? 'Finishing...' : 'Finish examination'}
          </button>
        ) : null}

        {actionError ? (
          <p
            className="mt-3 rounded-md border border-rose-400/20 bg-rose-400/[0.04] px-3 py-2 text-xs text-rose-200"
            role="alert"
          >
            {actionError}
          </p>
        ) : null}
      </header>

      {!isActive ? (
        <div
          className="rounded-lg border border-amber-300/20 bg-amber-300/[0.04] p-5"
          role="status"
        >
          <p className="text-sm text-amber-200">
            {detail.status === 'Expired'
              ? 'The time limit for this examination has expired.'
              : 'This examination is complete.'}
          </p>
          <p className="mt-1 text-xs leading-5 text-slate-400">
            Submissions are closed. Review the assessment history or start a new examination to
            try again.
          </p>
        </div>
      ) : null}

      <ChallengeWorkspace
        challenge={detail.challenge}
        disabledReason="This examination is closed. Start a new examination to submit again."
        examinationId={detail.id}
        key={detail.id}
        mode="examination"
        submissionAllowed={isActive}
      />
    </div>
  )
}

export function ExaminationWorkspacePage() {
  const { id = '' } = useParams<{ id: string }>()
  const { state, refresh } = useApiQuery<ExaminationSessionDetail>(
    `/api/examinations/${encodeURIComponent(id)}`,
  )

  if (state.status === 'loading') {
    return (
      <div className="mx-auto max-w-6xl">
        <LoadingPanel label="Loading examination..." />
      </div>
    )
  }

  if (state.status === 'error') {
    return (
      <div className="mx-auto max-w-6xl">
        <ErrorPanel
          backLabel="Back to examinations"
          backTo="/examinations"
          message={state.message}
          onRetry={refresh}
          title="Unable to load this examination"
        />
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-[1400px]">
      <ExaminationSession detail={state.data} onRefresh={refresh} />
    </div>
  )
}
