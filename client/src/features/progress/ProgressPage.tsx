import { Link } from 'react-router'
import {
  type AssessmentHistoryItem,
  type CompetencyProgress,
  type CompetencyState,
  type ProgressOverview,
} from '@/shared/api/progress'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import { Breadcrumbs, ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

const stateDot: Record<CompetencyState, string> = {
  NotStarted: 'bg-slate-500',
  Learning: 'bg-sky-400',
  Practicing: 'bg-amber-400',
  AssessmentRequired: 'bg-violet-400',
  Mastered: 'bg-emerald-400',
}

function StateBadge({ state }: { state: CompetencyState }) {
  return (
    <span className="inline-flex shrink-0 items-center gap-1.5 text-[11px] text-slate-400">
      <span aria-hidden="true" className={['size-1.5 rounded-full', stateDot[state]].join(' ')} />
      {state}
    </span>
  )
}

function SummaryTile({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-4">
      <p className="text-xs text-slate-500">{label}</p>
      <p className="mt-1.5 text-2xl font-semibold text-white">{value}</p>
    </div>
  )
}

function CompetencyRow({ competency }: { competency: CompetencyProgress }) {
  return (
    <li className="rounded-md border border-white/[0.07] bg-white/[0.02] p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <p className="text-sm text-slate-200">{competency.name}</p>
          <p className="mt-1 max-w-2xl text-[11px] leading-5 text-slate-500">
            {competency.description}
          </p>
        </div>
        <StateBadge state={competency.state} />
      </div>

      <p className="mt-3 text-[11px] leading-5 text-slate-400">
        <span className="text-slate-300">Next: </span>
        {competency.nextAction}
      </p>

      {competency.evidence.length > 0 ? (
        <ul className="mt-2.5 flex flex-wrap gap-x-3 gap-y-1 text-[11px] text-slate-500">
          {competency.evidence.map((evidence, index) => (
            <li key={`${evidence.challengeSlug}-${evidence.createdAtUtc}-${index}`}>
              {evidence.challengeTitle} · {evidence.purpose} · {evidence.outcome}
              {evidence.isExamination ? ' · exam' : ''}
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-2.5 text-[11px] text-slate-600">No assessment evidence yet.</p>
      )}
    </li>
  )
}

function ProgressOverviewSection({ overview }: { overview: ProgressOverview }) {
  const { summary } = overview

  return (
    <div className="space-y-6">
      <div className="grid gap-3 sm:grid-cols-3 xl:grid-cols-6">
        <SummaryTile label="Competencies" value={summary.total} />
        <SummaryTile label="Mastered" value={summary.mastered} />
        <SummaryTile label="Assessment required" value={summary.assessmentRequired} />
        <SummaryTile label="Practicing" value={summary.practicing} />
        <SummaryTile label="Learning" value={summary.learning} />
        <SummaryTile label="Not started" value={summary.notStarted} />
      </div>

      {overview.domains.map((domain) => (
        <section
          aria-labelledby={`progress-domain-${domain.slug}`}
          className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5"
          key={domain.slug}
        >
          <h2 className="text-sm font-semibold text-white" id={`progress-domain-${domain.slug}`}>
            {domain.name}
          </h2>
          <p className="mt-1 text-xs leading-5 text-slate-500">{domain.description}</p>

          <div className="mt-4 space-y-4">
            {domain.modules.map((module) => (
              <div key={module.slug}>
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="text-xs font-medium text-slate-300">{module.title}</h3>
                  <span className="text-[10px] text-slate-500">{module.level}</span>
                </div>
                <ul className="mt-2 space-y-2">
                  {module.competencies.map((competency) => (
                    <CompetencyRow competency={competency} key={competency.slug} />
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

function AssessmentHistory({ items }: { items: AssessmentHistoryItem[] }) {
  if (items.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-white/[0.1] bg-white/[0.01] p-6 text-center">
        <p className="text-sm text-slate-300">No assessments yet.</p>
        <p className="mt-1 text-xs text-slate-500">
          Run a mapped challenge, then submit it to create evidence.
        </p>
        <Link
          className="mt-4 inline-flex rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
          to="/challenges"
        >
          Browse challenges
        </Link>
      </div>
    )
  }

  return (
    <ul className="space-y-2">
      {items.map((item) => (
        <li
          className="flex flex-wrap items-center gap-x-3 gap-y-1 rounded-md border border-white/[0.07] bg-white/[0.02] px-3 py-2.5 text-[11px]"
          key={item.submissionId}
        >
          <span className="inline-flex items-center gap-1.5">
            <span
              aria-hidden="true"
              className={[
                'size-1.5 rounded-full',
                item.isAccepted ? 'bg-emerald-400' : 'bg-rose-400',
              ].join(' ')}
            />
            <span className="text-slate-300">{item.outcome}</span>
          </span>
          <span className="text-slate-200">{item.challengeTitle}</span>
          <span className="text-slate-500">{item.purpose}</span>
          <span className="text-slate-500">
            {item.passedTests}/{item.totalTests} tests
          </span>
          {item.isExamination ? (
            <span className="text-slate-500">{item.examinationVariant ?? 'Examination'}</span>
          ) : null}
          <span className="ml-auto text-slate-500">
            {new Intl.DateTimeFormat(undefined, {
              dateStyle: 'medium',
              timeStyle: 'short',
            }).format(new Date(item.createdAtUtc))}
          </span>
        </li>
      ))}
    </ul>
  )
}

export function ProgressPage() {
  const progressQuery = useApiQuery<ProgressOverview>('/api/progress')
  const assessmentsQuery = useApiQuery<AssessmentHistoryItem[]>('/api/assessments?limit=10')

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <Breadcrumbs items={[{ label: 'Progress' }]} />

      <header>
        <h1 className="text-2xl font-semibold tracking-tight text-white">Progress</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
          Competency states are derived from append-only assessments. A lesson view never
          counts; accepted submissions and examinations do.
        </p>
      </header>

      {progressQuery.state.status === 'loading' ? (
        <LoadingPanel label="Loading competency progress..." />
      ) : null}

      {progressQuery.state.status === 'error' ? (
        <ErrorPanel
          backLabel="Back to dashboard"
          backTo="/"
          message={progressQuery.state.message}
          onRetry={progressQuery.refresh}
          title="Unable to load progress"
        />
      ) : null}

      {progressQuery.state.status === 'success' ? (
        <ProgressOverviewSection overview={progressQuery.state.data} />
      ) : null}

      <section aria-labelledby="assessment-history-heading">
        <div className="flex items-end justify-between gap-4">
          <div>
            <h2 className="text-base font-semibold text-white" id="assessment-history-heading">
              Recent assessments
            </h2>
            <p className="mt-1 text-xs text-slate-500">
              Newest evidence first, including whether it came from an examination.
            </p>
          </div>
          <Link
            className="rounded-md border border-white/10 bg-white/[0.03] px-2.5 py-1 text-[11px] text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            to="/examinations"
          >
            Examinations
          </Link>
        </div>

        <div className="mt-4">
          {assessmentsQuery.state.status === 'loading' ? (
            <LoadingPanel label="Loading assessment history..." />
          ) : null}

          {assessmentsQuery.state.status === 'error' ? (
            <ErrorPanel
              backLabel="Back to dashboard"
              backTo="/"
              message={assessmentsQuery.state.message}
              onRetry={assessmentsQuery.refresh}
              title="Unable to load assessment history"
            />
          ) : null}

          {assessmentsQuery.state.status === 'success' ? (
            <AssessmentHistory items={assessmentsQuery.state.data} />
          ) : null}
        </div>
      </section>
    </div>
  )
}
