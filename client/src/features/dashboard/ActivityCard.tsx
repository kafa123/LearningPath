import { Link } from 'react-router'
import type { AssessmentHistoryItem } from '@/shared/api/progress'
import type { ApiQueryState } from '@/shared/hooks/useApiQuery'
import { DashboardPanel, PanelLink } from './DashboardPanel'
import { formatRelativeTime } from './dashboardData'

function outcomeTone(item: AssessmentHistoryItem) {
  if (item.isAccepted) {
    return 'text-emerald-200'
  }

  if (item.outcome === 'WrongAnswer') {
    return 'text-amber-200'
  }

  return 'text-rose-200'
}

export function ActivityCard({
  state,
  onRetry,
}: {
  state: ApiQueryState<AssessmentHistoryItem[]>
  onRetry: () => void
}) {
  return (
    <DashboardPanel
      action={<PanelLink to="/progress">Assessment history</PanelLink>}
      description="Your latest submissions and examination attempts."
      title="Recent activity"
    >
      {state.status === 'loading' ? (
        <div className="space-y-3" role="status">
          {[0, 1, 2, 3].map((index) => (
            <div className="flex items-center justify-between gap-4" key={index}>
              <div className="h-3 w-1/3 animate-pulse rounded bg-white/[0.05]" />
              <div className="h-3 w-10 animate-pulse rounded bg-white/[0.04]" />
            </div>
          ))}
        </div>
      ) : null}

      {state.status === 'error' ? (
        <div>
          <p className="text-xs text-rose-200">Assessment history is unavailable.</p>
          <p className="mt-1 text-[11px] leading-5 text-slate-500">{state.message}</p>
          <button
            className="mt-2 rounded-md border border-white/10 bg-white/[0.03] px-2.5 py-1.5 text-[11px] text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white"
            onClick={onRetry}
            type="button"
          >
            Retry assessments
          </button>
        </div>
      ) : null}

      {state.status === 'success' && state.data.length === 0 ? (
        <div className="rounded-md border border-dashed border-white/[0.1] bg-white/[0.01] p-5 text-center">
          <p className="text-sm text-slate-300">No assessments yet.</p>
          <p className="mt-1 text-xs text-slate-500">
            Submit a mapped challenge to create your first evidence.
          </p>
          <Link
            className="mt-3 inline-flex rounded-md border border-emerald-400/30 bg-emerald-400/10 px-3 py-1.5 text-xs font-medium text-emerald-100 transition-colors hover:bg-emerald-400/15"
            to="/challenges"
          >
            Browse challenges
          </Link>
        </div>
      ) : null}

      {state.status === 'success' && state.data.length > 0 ? (
        <ul className="divide-y divide-white/[0.06]">
          {state.data.slice(0, 5).map((item) => (
            <li
              className="flex items-center justify-between gap-4 py-2.5 first:pt-0 last:pb-0"
              key={item.submissionId}
            >
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <span
                    aria-hidden="true"
                    className={[
                      'size-1.5 shrink-0 rounded-full',
                      item.isAccepted ? 'bg-emerald-400' : 'bg-rose-400',
                    ].join(' ')}
                  />
                  <Link
                    className="truncate text-xs font-medium text-slate-200 transition-colors hover:text-white"
                    to={`/challenges/${item.challengeSlug}`}
                  >
                    {item.challengeTitle}
                  </Link>
                  {item.isExamination ? (
                    <span className="shrink-0 rounded-sm border border-violet-400/20 bg-violet-400/[0.06] px-1.5 py-0.5 text-[9px] font-medium text-violet-200">
                      Exam
                    </span>
                  ) : null}
                </div>
                <p className="mt-0.5 truncate text-[10px] text-slate-500">
                  <span className={outcomeTone(item)}>{item.outcome}</span>
                  <span aria-hidden="true"> · </span>
                  {item.passedTests}/{item.totalTests} tests
                  <span aria-hidden="true"> · </span>
                  {item.languageId}
                </p>
              </div>

              <div className="shrink-0 text-right">
                <p className={['text-xs font-semibold', outcomeTone(item)].join(' ')}>
                  {item.score}%
                </p>
                <p className="text-[10px] text-slate-600">
                  {formatRelativeTime(item.createdAtUtc)}
                </p>
              </div>
            </li>
          ))}
        </ul>
      ) : null}
    </DashboardPanel>
  )
}
