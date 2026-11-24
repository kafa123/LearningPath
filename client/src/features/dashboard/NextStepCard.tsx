import { Link } from 'react-router'
import type { ProgressOverview } from '@/shared/api/progress'
import type { ApiQueryState } from '@/shared/hooks/useApiQuery'
import { CompetencyBadge } from './CompetencyBadge'
import { DashboardPanel, PanelLink } from './DashboardPanel'
import { getRecommendedNext } from './dashboardData'

export function NextStepCard({
  state,
  onRetry,
}: {
  state: ApiQueryState<ProgressOverview>
  onRetry: () => void
}) {
  return (
    <DashboardPanel
      action={<PanelLink to="/practice">Practice queue</PanelLink>}
      description="The highest-priority competency based on your assessment evidence."
      title="Next up"
    >
      {state.status === 'loading' ? (
        <div className="space-y-3" role="status">
          <div className="h-3 w-40 animate-pulse rounded bg-white/[0.06]" />
          <div className="h-5 w-56 animate-pulse rounded bg-white/[0.06]" />
          <div className="h-3 w-full animate-pulse rounded bg-white/[0.04]" />
          <div className="h-3 w-2/3 animate-pulse rounded bg-white/[0.04]" />
        </div>
      ) : null}

      {state.status === 'error' ? (
        <div>
          <p className="text-xs text-rose-200">Progress is unavailable.</p>
          <p className="mt-1 text-[11px] leading-5 text-slate-500">{state.message}</p>
          <button
            className="mt-3 rounded-md border border-white/10 bg-white/[0.03] px-2.5 py-1.5 text-[11px] text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            onClick={onRetry}
            type="button"
          >
            Retry progress
          </button>
        </div>
      ) : null}

      {state.status === 'success' ? (
        <NextStepBody overview={state.data} />
      ) : null}
    </DashboardPanel>
  )
}

function NextStepBody({ overview }: { overview: ProgressOverview }) {
  const next = getRecommendedNext(overview)

  if (!next) {
    return (
      <div className="rounded-md border border-dashed border-emerald-400/20 bg-emerald-400/[0.03] p-4">
        <p className="text-sm text-emerald-100">Every competency is mastered.</p>
        <p className="mt-1 text-xs leading-5 text-slate-400">
          Keep the evidence current with another challenge or a timed examination.
        </p>
        <Link
          className="mt-3 inline-flex rounded-md border border-emerald-400/30 bg-emerald-400/10 px-3 py-1.5 text-xs font-medium text-emerald-100 transition-colors hover:bg-emerald-400/15"
          to="/challenges"
        >
          Browse challenges
        </Link>
      </div>
    )
  }

  const target = next.latestChallengeSlug
    ? `/challenges/${next.latestChallengeSlug}`
    : `/learning/${next.domain.slug}/${next.module.slug}`

  return (
    <div className="rounded-md border border-white/[0.07] bg-gradient-to-br from-emerald-400/[0.05] via-transparent to-transparent p-4">
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-[11px] text-slate-500">
        <span>{next.domain.name}</span>
        <span aria-hidden="true">/</span>
        <span>{next.module.title}</span>
        <span className="ml-auto">
          <CompetencyBadge state={next.competency.state} />
        </span>
      </div>

      <h3 className="mt-2.5 text-base font-semibold text-white">
        {next.competency.name}
      </h3>
      <p className="mt-1 text-xs leading-5 text-slate-400">
        {next.competency.nextAction}
      </p>

      <div className="mt-4 flex flex-wrap items-center gap-2">
        <Link
          className="inline-flex items-center gap-1.5 rounded-md border border-emerald-400/30 bg-emerald-400/10 px-3 py-1.5 text-xs font-medium text-emerald-100 transition-colors hover:bg-emerald-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
          to={target}
        >
          {next.latestChallengeSlug ? 'Continue challenge' : 'Open module'}
          <span aria-hidden="true">→</span>
        </Link>
        <Link
          className="rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
          to={`/learning/${next.domain.slug}/${next.module.slug}`}
        >
          Review module
        </Link>
      </div>
    </div>
  )
}
