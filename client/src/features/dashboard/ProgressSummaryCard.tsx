import { Link } from 'react-router'
import type { ProgressOverview } from '@/shared/api/progress'
import type { ApiQueryState } from '@/shared/hooks/useApiQuery'

export function ProgressSummaryCard({
  state,
  onRetry,
}: {
  state: ApiQueryState<ProgressOverview>
  onRetry: () => void
}) {
  return (
    <article className="flex min-h-40 flex-col rounded-2xl border border-violet-400/25 bg-violet-400/[0.06] p-5">
      <div className="flex items-start justify-between gap-3">
        <span className="rounded-full border border-violet-400/25 bg-violet-400/10 px-2.5 py-1 text-[10px] font-semibold tracking-[0.16em] text-violet-200 uppercase">
          Progress
        </span>
        <Link
          aria-label="Open progress"
          className="text-sm text-violet-300 transition hover:text-violet-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-300"
          to="/progress"
        >
          →
        </Link>
      </div>

      <Link
        className="mt-4 text-base font-semibold text-white transition hover:text-violet-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-300"
        to="/progress"
      >
        Competency overview
      </Link>

      {state.status === 'loading' ? (
        <p className="mt-2 text-xs text-slate-400">Loading competency progress...</p>
      ) : null}

      {state.status === 'error' ? (
        <div className="mt-2">
          <p className="text-xs text-rose-200">Progress is unavailable.</p>
          <button
            className="mt-2 rounded-md border border-white/10 bg-white/[0.04] px-2 py-1 text-[10px] font-medium text-slate-200 transition hover:border-white/20 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-300"
            onClick={onRetry}
            type="button"
          >
            Retry progress
          </button>
        </div>
      ) : null}

      {state.status === 'success' ? (
        <>
          <p className="mt-2 text-xs leading-5 text-slate-400">
            {state.data.summary.mastered} of {state.data.summary.total} competencies mastered.
          </p>
          <dl className="mt-auto grid grid-cols-3 gap-2 pt-4 text-center">
            <div className="rounded-lg bg-white/[0.03] px-2 py-1.5">
              <dt className="text-[9px] tracking-[0.12em] text-slate-500 uppercase">Mastered</dt>
              <dd className="mt-0.5 text-sm font-semibold text-emerald-200">
                {state.data.summary.mastered}
              </dd>
            </div>
            <div className="rounded-lg bg-white/[0.03] px-2 py-1.5">
              <dt className="text-[9px] tracking-[0.12em] text-slate-500 uppercase">Required</dt>
              <dd className="mt-0.5 text-sm font-semibold text-violet-200">
                {state.data.summary.assessmentRequired}
              </dd>
            </div>
            <div className="rounded-lg bg-white/[0.03] px-2 py-1.5">
              <dt className="text-[9px] tracking-[0.12em] text-slate-500 uppercase">Learning</dt>
              <dd className="mt-0.5 text-sm font-semibold text-sky-200">
                {state.data.summary.learning + state.data.summary.practicing}
              </dd>
            </div>
          </dl>
        </>
      ) : null}
    </article>
  )
}
