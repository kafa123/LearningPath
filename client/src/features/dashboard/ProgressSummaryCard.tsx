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
    <article className="flex min-h-40 flex-col rounded-lg border border-white/[0.08] bg-white/[0.02] p-5">
      <div className="flex items-start justify-between gap-3">
        <span className="text-[11px] text-slate-500">Progress</span>
        <Link
          aria-label="Open progress"
          className="text-sm text-slate-600 transition-colors hover:text-slate-300 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
          to="/progress"
        >
          →
        </Link>
      </div>

      <Link
        className="mt-3 text-sm font-semibold text-white transition-colors hover:text-slate-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
        to="/progress"
      >
        Competency overview
      </Link>

      {state.status === 'loading' ? (
        <p className="mt-1.5 text-xs text-slate-500">Loading competency progress...</p>
      ) : null}

      {state.status === 'error' ? (
        <div className="mt-1.5">
          <p className="text-xs text-rose-200">Progress is unavailable.</p>
          <button
            className="mt-2 rounded-md border border-white/10 bg-white/[0.03] px-2 py-1 text-[11px] text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            onClick={onRetry}
            type="button"
          >
            Retry progress
          </button>
        </div>
      ) : null}

      {state.status === 'success' ? (
        <>
          <p className="mt-1.5 text-xs leading-5 text-slate-400">
            {state.data.summary.mastered} of {state.data.summary.total} competencies mastered.
          </p>
          <dl className="mt-auto grid grid-cols-3 gap-3 pt-4">
            <div>
              <dt className="text-[10px] text-slate-500">Mastered</dt>
              <dd className="mt-0.5 text-sm font-medium text-slate-200">
                {state.data.summary.mastered}
              </dd>
            </div>
            <div>
              <dt className="text-[10px] text-slate-500">Required</dt>
              <dd className="mt-0.5 text-sm font-medium text-slate-200">
                {state.data.summary.assessmentRequired}
              </dd>
            </div>
            <div>
              <dt className="text-[10px] text-slate-500">Learning</dt>
              <dd className="mt-0.5 text-sm font-medium text-slate-200">
                {state.data.summary.learning + state.data.summary.practicing}
              </dd>
            </div>
          </dl>
        </>
      ) : null}
    </article>
  )
}
