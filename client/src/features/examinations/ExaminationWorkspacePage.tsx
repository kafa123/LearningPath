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

function statusTone(status: string) {
  if (status === 'InProgress') {
    return 'border-emerald-400/25 bg-emerald-400/10 text-emerald-100'
  }

  if (status === 'Expired') {
    return 'border-amber-300/25 bg-amber-300/10 text-amber-100'
  }

  return 'border-white/15 bg-white/[0.05] text-slate-300'
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

      <header className="rounded-2xl border border-white/10 bg-slate-900/45 p-5">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className={[
              'rounded-full border px-2.5 py-1 text-[10px] font-semibold tracking-[0.14em] uppercase',
              statusTone(detail.status),
            ].join(' ')}
          >
            {detail.status}
          </span>
          <span className="rounded-full border border-white/10 bg-white/[0.04] px-2.5 py-1 text-[10px] font-medium tracking-[0.14em] text-slate-400 uppercase">
            {detail.variantLabel}
          </span>
          {detail.timeLimitSeconds != null ? (
            <span className="text-[11px] text-slate-500">
              Limit {formatDuration(detail.timeLimitSeconds)}
            </span>
          ) : null}
          {isActive && remainingSeconds != null ? (
            <span
              aria-live="polite"
              className="ml-auto rounded-full border border-emerald-400/25 bg-emerald-400/10 px-3 py-1 font-mono text-xs font-semibold text-emerald-100"
            >
              {formatDuration(remainingSeconds)} remaining
            </span>
          ) : null}
        </div>

        <h1 className="mt-4 text-2xl font-semibold tracking-tight text-white sm:text-3xl">
          {detail.challengeTitle} examination
        </h1>
        <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">
          The server selected this variant and its time limit. Hidden tests are used for
          scoring, and hints are not available during an examination.
        </p>

        {isActive ? (
          <button
            className="mt-4 rounded-lg border border-white/10 bg-white/[0.04] px-3 py-1.5 text-xs font-medium text-slate-200 transition hover:border-white/20 hover:text-white disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
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
            className="mt-3 rounded-lg border border-rose-400/20 bg-rose-400/[0.06] px-3 py-2 text-xs text-rose-100"
            role="alert"
          >
            {actionError}
          </p>
        ) : null}
      </header>

      {!isActive ? (
        <div
          className="rounded-2xl border border-amber-300/20 bg-amber-300/[0.06] p-5"
          role="status"
        >
          <p className="text-sm font-medium text-amber-100">
            {detail.status === 'Expired'
              ? 'The time limit for this examination has expired.'
              : 'This examination is complete.'}
          </p>
          <p className="mt-1 text-xs leading-5 text-slate-400">
            Submissions are closed. Review the assessment history or start a new examination
            to try again.
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
