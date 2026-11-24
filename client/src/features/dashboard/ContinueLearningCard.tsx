import { Link } from 'react-router'
import type { ProgressOverview } from '@/shared/api/progress'
import type { ApiQueryState } from '@/shared/hooks/useApiQuery'
import { CompetencyBadge } from './CompetencyBadge'
import { DashboardPanel, PanelLink } from './DashboardPanel'
import { getContinueModule } from './dashboardData'

export function ContinueLearningCard({
  state,
  onRetry,
}: {
  state: ApiQueryState<ProgressOverview>
  onRetry: () => void
}) {
  return (
    <DashboardPanel
      action={<PanelLink to="/learning">All paths</PanelLink>}
      title="Continue learning"
    >
      {state.status === 'loading' ? (
        <div className="space-y-3" role="status">
          <div className="h-3 w-1/3 animate-pulse rounded bg-white/[0.05]" />
          <div className="h-4 w-3/4 animate-pulse rounded bg-white/[0.06]" />
          <div className="h-1.5 w-full animate-pulse rounded bg-white/[0.04]" />
        </div>
      ) : null}

      {state.status === 'error' ? (
        <div>
          <p className="text-xs text-rose-200">Learning progress is unavailable.</p>
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
        <ContinueLearningBody overview={state.data} />
      ) : null}
    </DashboardPanel>
  )
}

function ContinueLearningBody({ overview }: { overview: ProgressOverview }) {
  const next = getContinueModule(overview)

  if (!next) {
    return (
      <div className="rounded-md border border-dashed border-white/[0.1] bg-white/[0.01] p-4 text-center">
        <p className="text-sm text-slate-300">All modules mastered.</p>
        <p className="mt-1 text-xs text-slate-500">
          Keep the knowledge fresh with challenges and examinations.
        </p>
        <Link
          className="mt-3 inline-flex rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white"
          to="/learning"
        >
          Review learning paths
        </Link>
      </div>
    )
  }

  const percent = next.total === 0 ? 0 : Math.round((next.mastered / next.total) * 100)

  return (
    <div>
      <p className="text-[10px] text-slate-500">
        {next.domain.name}
        <span aria-hidden="true"> · </span>
        {next.module.level}
      </p>
      <Link
        className="mt-1 block text-sm font-semibold text-white transition-colors hover:text-slate-200"
        to={`/learning/${next.domain.slug}/${next.module.slug}`}
      >
        {next.module.title}
      </Link>

      <div className="mt-3 flex items-center gap-2">
        <div
          aria-label={`${next.mastered} of ${next.total} competencies mastered`}
          className="h-1.5 flex-1 overflow-hidden rounded-full bg-white/[0.06]"
          role="img"
        >
          <div
            className="h-full rounded-full bg-emerald-400/80"
            style={{ width: `${percent}%` }}
          />
        </div>
        <span className="shrink-0 text-[10px] text-slate-500">
          {next.mastered}/{next.total}
        </span>
      </div>

      {next.next ? (
        <div className="mt-3 rounded-md border border-white/[0.07] bg-white/[0.02] p-3">
          <div className="flex flex-wrap items-center gap-2">
            <p className="min-w-0 flex-1 truncate text-xs text-slate-200">
              {next.next.competency.name}
            </p>
            <CompetencyBadge state={next.next.competency.state} />
          </div>
          <p className="mt-1 text-[10px] leading-5 text-slate-500">
            {next.next.competency.nextAction}
          </p>
        </div>
      ) : null}

      <Link
        className="mt-3 inline-flex items-center gap-1.5 text-[11px] font-medium text-emerald-200 transition-colors hover:text-emerald-100"
        to={`/learning/${next.domain.slug}/${next.module.slug}`}
      >
        Resume module
        <span aria-hidden="true">→</span>
      </Link>
    </div>
  )
}
