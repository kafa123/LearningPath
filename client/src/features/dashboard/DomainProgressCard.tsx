import type { CompetencyState, ProgressOverview } from '@/shared/api/progress'
import type { ApiQueryState } from '@/shared/hooks/useApiQuery'
import { DashboardPanel, PanelLink } from './DashboardPanel'
import { stateDot } from './competencyTone'
import { getDomainSummaries } from './dashboardData'

const legend: Array<{ state: CompetencyState; label: string }> = [
  { state: 'Mastered', label: 'Mastered' },
  { state: 'AssessmentRequired', label: 'Ready' },
  { state: 'Practicing', label: 'Practicing' },
  { state: 'Learning', label: 'Learning' },
  { state: 'NotStarted', label: 'Not started' },
]

const segmentOrder: CompetencyState[] = [
  'Mastered',
  'AssessmentRequired',
  'Practicing',
  'Learning',
  'NotStarted',
]

export function DomainProgressCard({
  state,
  onRetry,
}: {
  state: ApiQueryState<ProgressOverview>
  onRetry: () => void
}) {
  return (
    <DashboardPanel
      action={<PanelLink to="/progress">Full progress</PanelLink>}
      description="Competency states are derived from accepted assessments and examinations."
      title="Progress by domain"
    >
      {state.status === 'loading' ? (
        <div className="space-y-5" role="status">
          {[0, 1, 2].map((index) => (
            <div className="space-y-2" key={index}>
              <div className="h-3 w-2/5 animate-pulse rounded bg-white/[0.05]" />
              <div className="h-1.5 w-full animate-pulse rounded bg-white/[0.04]" />
            </div>
          ))}
        </div>
      ) : null}

      {state.status === 'error' ? (
        <div>
          <p className="text-xs text-rose-200">Progress is unavailable.</p>
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
        <DomainProgressBody overview={state.data} />
      ) : null}
    </DashboardPanel>
  )
}

function DomainProgressBody({ overview }: { overview: ProgressOverview }) {
  const domains = getDomainSummaries(overview)

  return (
    <>
      <ul className="space-y-4">
        {domains.map(({ domain, total, mastered, counts }) => (
          <li key={domain.slug}>
            <div className="flex items-baseline justify-between gap-3">
              <p className="truncate text-xs font-medium text-slate-300">
                {domain.name}
              </p>
              <p className="shrink-0 text-[11px] text-slate-500">
                {mastered}
                <span className="text-slate-600"> / {total} mastered</span>
              </p>
            </div>

            <div
              aria-label={`${domain.name}: ${mastered} of ${total} competencies mastered`}
              className="mt-2 flex h-1.5 overflow-hidden rounded-full bg-white/[0.06]"
              role="img"
            >
              {total === 0
                ? null
                : segmentOrder.map((segment) => {
                    const count = counts[segment]

                    return count === 0 ? null : (
                      <span
                        className={['h-full', stateDot[segment]].join(' ')}
                        key={segment}
                        style={{ width: `${(count / total) * 100}%` }}
                      />
                    )
                  })}
            </div>

            <div className="mt-1.5 flex flex-wrap gap-x-3 gap-y-1 text-[10px] text-slate-500">
              {counts.Mastered > 0 ? <span>{counts.Mastered} mastered</span> : null}
              {counts.AssessmentRequired > 0 ? (
                <span>{counts.AssessmentRequired} ready for assessment</span>
              ) : null}
              {counts.Practicing + counts.Learning > 0 ? (
                <span>{counts.Practicing + counts.Learning} in progress</span>
              ) : null}
              {counts.NotStarted > 0 ? (
                <span>{counts.NotStarted} not started</span>
              ) : null}
            </div>
          </li>
        ))}
      </ul>

      <ul className="mt-4 flex flex-wrap gap-x-4 gap-y-1.5 border-t border-white/[0.07] pt-3">
        {legend.map((item) => (
          <li className="inline-flex items-center gap-1.5 text-[10px] text-slate-500" key={item.state}>
            <span aria-hidden="true" className={['size-1.5 rounded-full', stateDot[item.state]].join(' ')} />
            {item.label}
          </li>
        ))}
      </ul>
    </>
  )
}
