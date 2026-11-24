import { Link } from 'react-router'
import type { ProgressOverview } from '@/shared/api/progress'
import type { ApiQueryState } from '@/shared/hooks/useApiQuery'
import { CompetencyBadge } from './CompetencyBadge'
import { DashboardPanel, PanelLink } from './DashboardPanel'
import { getFocusItems } from './dashboardData'

export function FocusAreasCard({
  state,
  onRetry,
}: {
  state: ApiQueryState<ProgressOverview>
  onRetry: () => void
}) {
  return (
    <DashboardPanel
      action={<PanelLink to="/practice">Practice</PanelLink>}
      title="Focus areas"
    >
      {state.status === 'loading' ? (
        <div className="space-y-3" role="status">
          {[0, 1, 2].map((index) => (
            <div className="h-3 w-full animate-pulse rounded bg-white/[0.05]" key={index} />
          ))}
        </div>
      ) : null}

      {state.status === 'error' ? (
        <div>
          <p className="text-xs text-rose-200">Focus areas are unavailable.</p>
          <button
            className="mt-2 rounded-md border border-white/10 bg-white/[0.03] px-2.5 py-1.5 text-[11px] text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white"
            onClick={onRetry}
            type="button"
          >
            Retry progress
          </button>
        </div>
      ) : null}

      {state.status === 'success' ? (
        <FocusAreasBody overview={state.data} />
      ) : null}
    </DashboardPanel>
  )
}

function FocusAreasBody({ overview }: { overview: ProgressOverview }) {
  const items = getFocusItems(overview)

  if (items.length === 0) {
    return (
      <p className="text-xs leading-5 text-slate-500">
        Nothing needs attention. Every competency is mastered.
      </p>
    )
  }

  return (
    <ul className="space-y-1">
      {items.map((item) => {
        const to = item.latestChallengeSlug
          ? `/challenges/${item.latestChallengeSlug}`
          : `/learning/${item.domain.slug}/${item.module.slug}`

        return (
          <li key={item.competency.slug}>
            <Link
              className="group flex items-start gap-3 rounded-md px-2 py-2 transition-colors hover:bg-white/[0.03] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
              to={to}
            >
              <span className="mt-1.5">
                <CompetencyBadge state={item.competency.state} />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-xs font-medium text-slate-200 group-hover:text-white">
                  {item.competency.name}
                </span>
                <span className="mt-0.5 block truncate text-[10px] text-slate-500">
                  {item.domain.name}
                  <span aria-hidden="true"> / </span>
                  {item.module.title}
                </span>
              </span>
              <span
                aria-hidden="true"
                className="mt-1 text-xs text-slate-600 transition-colors group-hover:text-slate-300"
              >
                →
              </span>
            </Link>
          </li>
        )
      })}
    </ul>
  )
}
