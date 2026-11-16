import { Link } from 'react-router'
import type { AssessmentHistoryItem } from '@/shared/api/progress'
import type { ApiQueryState } from '@/shared/hooks/useApiQuery'

export function RecentAssessmentsCard({
  state,
  onRetry,
}: {
  state: ApiQueryState<AssessmentHistoryItem[]>
  onRetry: () => void
}) {
  return (
    <article className="flex min-h-40 flex-col rounded-lg border border-white/[0.08] bg-white/[0.02] p-5">
      <div className="flex items-start justify-between gap-3">
        <span className="text-[11px] text-slate-500">Assessments</span>
        <Link
          aria-label="Open assessment history"
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
        Recent assessments
      </Link>

      {state.status === 'loading' ? (
        <p className="mt-1.5 text-xs text-slate-500">Loading assessment history...</p>
      ) : null}

      {state.status === 'error' ? (
        <div className="mt-1.5">
          <p className="text-xs text-rose-200">Assessment history is unavailable.</p>
          <button
            className="mt-2 rounded-md border border-white/10 bg-white/[0.03] px-2 py-1 text-[11px] text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            onClick={onRetry}
            type="button"
          >
            Retry assessments
          </button>
        </div>
      ) : null}

      {state.status === 'success' && state.data.length === 0 ? (
        <p className="mt-1.5 text-xs leading-5 text-slate-500">
          No assessments yet. Submit a mapped challenge to create evidence.
        </p>
      ) : null}

      {state.status === 'success' && state.data.length > 0 ? (
        <ul className="mt-auto space-y-1.5 pt-3">
          {state.data.slice(0, 3).map((item) => (
            <li className="flex items-center gap-2 text-[11px]" key={item.submissionId}>
              <span
                aria-hidden="true"
                className={[
                  'size-1.5 shrink-0 rounded-full',
                  item.isAccepted ? 'bg-emerald-400' : 'bg-rose-400',
                ].join(' ')}
              />
              <span className="truncate text-slate-300">{item.challengeTitle}</span>
              {item.isExamination ? (
                <span className="ml-auto text-[10px] text-slate-500">Exam</span>
              ) : null}
            </li>
          ))}
        </ul>
      ) : null}
    </article>
  )
}
