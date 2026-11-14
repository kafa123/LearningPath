import { Link } from 'react-router'
import {
  type AssessmentHistoryItem,
  type CompetencyProgress,
  type CompetencyState,
  type ProgressOverview,
} from '@/shared/api/progress'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import { Breadcrumbs, ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

const stateStyles: Record<CompetencyState, string> = {
  NotStarted: 'border-white/10 bg-white/[0.04] text-slate-400',
  Learning: 'border-sky-400/25 bg-sky-400/10 text-sky-100',
  Practicing: 'border-amber-300/25 bg-amber-300/10 text-amber-100',
  AssessmentRequired: 'border-violet-400/25 bg-violet-400/10 text-violet-100',
  Mastered: 'border-emerald-400/25 bg-emerald-400/10 text-emerald-100',
}

function StateBadge({ state }: { state: CompetencyState }) {
  return (
    <span
      className={[
        'rounded-full border px-2.5 py-1 text-[10px] font-semibold tracking-[0.12em] uppercase',
        stateStyles[state],
      ].join(' ')}
    >
      {state}
    </span>
  )
}

function SummaryTile({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-slate-900/45 p-4">
      <p className="text-[10px] font-semibold tracking-[0.16em] text-slate-500 uppercase">
        {label}
      </p>
      <p className={['mt-2 text-2xl font-semibold', tone].join(' ')}>{value}</p>
    </div>
  )
}

function CompetencyRow({ competency }: { competency: CompetencyProgress }) {
  return (
    <li className="rounded-xl border border-white/[0.07] bg-slate-950/40 p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <p className="text-sm font-medium text-slate-100">{competency.name}</p>
          <p className="mt-1 max-w-2xl text-[11px] leading-5 text-slate-500">
            {competency.description}
          </p>
        </div>
        <StateBadge state={competency.state} />
      </div>

      <p className="mt-3 text-[11px] leading-5 text-slate-400">
        <span className="font-semibold text-slate-300">Next: </span>
        {competency.nextAction}
      </p>

      {competency.evidence.length > 0 ? (
        <ul className="mt-3 flex flex-wrap gap-2">
          {competency.evidence.map((evidence, index) => (
            <li
              className="rounded-md border border-white/[0.08] bg-white/[0.03] px-2 py-1 text-[10px] text-slate-400"
              key={`${evidence.challengeSlug}-${evidence.createdAtUtc}-${index}`}
            >
              {evidence.challengeTitle} · {evidence.purpose} · {evidence.outcome}
              {evidence.isExamination ? ' · exam' : ''}
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-[11px] text-slate-600">No assessment evidence yet.</p>
      )}
    </li>
  )
}

function ProgressOverviewSection({ overview }: { overview: ProgressOverview }) {
  const { summary } = overview

  return (
    <div className="space-y-6">
      <div className="grid gap-3 sm:grid-cols-3 xl:grid-cols-6">
        <SummaryTile label="Competencies" tone="text-white" value={summary.total} />
        <SummaryTile label="Mastered" tone="text-emerald-200" value={summary.mastered} />
        <SummaryTile
          label="Assessment required"
          tone="text-violet-200"
          value={summary.assessmentRequired}
        />
        <SummaryTile label="Practicing" tone="text-amber-200" value={summary.practicing} />
        <SummaryTile label="Learning" tone="text-sky-200" value={summary.learning} />
        <SummaryTile label="Not started" tone="text-slate-400" value={summary.notStarted} />
      </div>

      {overview.domains.map((domain) => (
        <section
          aria-labelledby={`progress-domain-${domain.slug}`}
          className="rounded-2xl border border-white/10 bg-slate-900/45 p-5"
          key={domain.slug}
        >
          <h2 className="text-base font-semibold text-white" id={`progress-domain-${domain.slug}`}>
            {domain.name}
          </h2>
          <p className="mt-1 text-xs leading-5 text-slate-500">{domain.description}</p>

          <div className="mt-4 space-y-4">
            {domain.modules.map((module) => (
              <div key={module.slug}>
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="text-sm font-medium text-slate-200">{module.title}</h3>
                  <span className="rounded-md border border-white/[0.08] bg-white/[0.03] px-2 py-0.5 text-[10px] text-slate-500">
                    {module.level}
                  </span>
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
      <div className="rounded-2xl border border-dashed border-white/10 bg-white/[0.02] p-6 text-center">
        <p className="text-sm font-medium text-slate-300">No assessments yet.</p>
        <p className="mt-1 text-xs text-slate-500">
          Run a mapped challenge, then submit it to create evidence.
        </p>
        <Link
          className="mt-4 inline-flex rounded-lg border border-emerald-400/25 bg-emerald-400/10 px-3 py-1.5 text-xs font-medium text-emerald-200 transition hover:bg-emerald-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
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
          className="flex flex-wrap items-center gap-2 rounded-xl border border-white/[0.07] bg-slate-950/40 px-3 py-2.5"
          key={item.submissionId}
        >
          <span
            className={[
              'rounded-full border px-2 py-0.5 text-[10px] font-semibold tracking-[0.1em] uppercase',
              item.isAccepted
                ? 'border-emerald-400/25 bg-emerald-400/10 text-emerald-100'
                : 'border-rose-400/25 bg-rose-400/10 text-rose-100',
            ].join(' ')}
          >
            {item.outcome}
          </span>
          <span className="text-xs font-medium text-slate-200">{item.challengeTitle}</span>
          <span className="text-[11px] text-slate-500">{item.purpose}</span>
          <span className="text-[11px] text-slate-500">
            {item.passedTests}/{item.totalTests} tests
          </span>
          {item.isExamination ? (
            <span className="rounded-md border border-violet-400/25 bg-violet-400/10 px-2 py-0.5 text-[10px] text-violet-100">
              {item.examinationVariant ?? 'Examination'}
            </span>
          ) : null}
          <span className="ml-auto text-[11px] text-slate-500">
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
        <p className="text-[10px] font-semibold tracking-[0.24em] text-emerald-300 uppercase">
          Progress
        </p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
          Evidence, not activity.
        </h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
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
            <h2 className="text-lg font-semibold text-white" id="assessment-history-heading">
              Recent assessments
            </h2>
            <p className="mt-1 text-xs text-slate-500">
              Newest evidence first, including whether it came from an examination.
            </p>
          </div>
          <Link
            className="rounded-lg border border-white/10 bg-white/[0.04] px-2.5 py-1 text-[11px] font-medium text-slate-300 transition hover:border-white/20 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
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
