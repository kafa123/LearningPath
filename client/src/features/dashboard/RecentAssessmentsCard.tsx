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
    <article className="flex min-h-40 flex-col rounded-2xl border border-sky-400/25 bg-sky-400/[0.06] p-5">
      <div className="flex items-start justify-between gap-3">
        <span className="rounded-full border border-sky-400/25 bg-sky-400/10 px-2.5 py-1 text-[10px] font-semibold tracking-[0.16em] text-sky-200 uppercase">
          Assessments
        </span>
        <Link
          aria-label="Open assessment history"
          className="text-sm text-sky-300 transition hover:text-sky-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-300"
          to="/progress"
        >
          →
        </Link>
      </div>

      <Link
        className="mt-4 text-base font-semibold text-white transition hover:text-sky-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-300"
        to="/progress"
      >
        Recent assessments
      </Link>

      {state.status === 'loading' ? (
        <p className="mt-2 text-xs text-slate-400">Loading assessment history...</p>
      ) : null}

      {state.status === 'error' ? (
        <div className="mt-2">
          <p className="text-xs text-rose-200">Assessment history is unavailable.</p>
          <button
            className="mt-2 rounded-md border border-white/10 bg-white/[0.04] px-2 py-1 text-[10px] font-medium text-slate-200 transition hover:border-white/20 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-300"
            onClick={onRetry}
            type="button"
          >
            Retry assessments
          </button>
        </div>
      ) : null}

      {state.status === 'success' && state.data.length === 0 ? (
        <p className="mt-2 text-xs leading-5 text-slate-500">
          No assessments yet. Submit a mapped challenge to create evidence.
        </p>
      ) : null}

      {state.status === 'success' && state.data.length > 0 ? (
        <ul className="mt-auto space-y-2 pt-3">
          {state.data.slice(0, 3).map((item) => (
            <li
              className="flex items-center gap-2 rounded-lg bg-white/[0.03] px-2.5 py-1.5"
              key={item.submissionId}
            >
              <span
                className={[
                  'size-1.5 shrink-0 rounded-full',
                  item.isAccepted ? 'bg-emerald-300' : 'bg-rose-300',
                ].join(' ')}
              />
              <span className="truncate text-[11px] text-slate-300">
                {item.challengeTitle}
              </span>
              {item.isExamination ? (
                <span className="ml-auto text-[9px] tracking-[0.12em] text-violet-200 uppercase">
                  Exam
                </span>
              ) : null}
            </li>
          ))}
        </ul>
      ) : null}
    </article>
  )
}
