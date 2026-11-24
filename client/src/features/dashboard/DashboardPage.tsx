import { Link } from 'react-router'
import type {
  AssessmentHistoryItem,
  ExaminationSessionSummary,
  ProgressOverview,
} from '@/shared/api/progress'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { ApiQueryState } from '@/shared/hooks/useApiQuery'
import { ActivityCard } from './ActivityCard'
import { ContinueLearningCard } from './ContinueLearningCard'
import { DashboardGlyph } from './DashboardGlyph'
import { DomainProgressCard } from './DomainProgressCard'
import { FocusAreasCard } from './FocusAreasCard'
import { HealthCard } from './HealthCard'
import { NextStepCard } from './NextStepCard'
import { StatCard } from './StatCard'
import { formatRemaining, getAssessmentStats } from './dashboardData'
import { useHealth } from './useHealth'

const quickActions = [
  { label: 'Resume practice', to: '/practice', primary: true },
  { label: 'Examinations', to: '/examinations', primary: false },
  { label: 'Full progress', to: '/progress', primary: false },
]

export function DashboardPage() {
  const { state: healthState, refresh: refreshHealth } = useHealth()
  const progressQuery = useApiQuery<ProgressOverview>('/api/progress')
  const assessmentsQuery = useApiQuery<AssessmentHistoryItem[]>(
    '/api/assessments?limit=30',
  )
  const examinationsQuery = useApiQuery<ExaminationSessionSummary[]>(
    '/api/examinations?limit=5',
  )

  const progress =
    progressQuery.state.status === 'success' ? progressQuery.state.data : null
  const assessments =
    assessmentsQuery.state.status === 'success' ? assessmentsQuery.state.data : null
  const stats = getAssessmentStats(assessments ?? [])
  const summary = progress?.summary
  const masteredPercent =
    summary && summary.total > 0
      ? Math.round((summary.mastered / summary.total) * 100)
      : 0
  const assessmentHint =
    assessmentsQuery.state.status === 'error'
      ? 'Assessment history is unavailable.'
      : assessments && assessments.length > 0
        ? `${stats.accepted} of ${assessments.length} attempts accepted`
        : 'No attempts recorded yet'

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-medium tracking-[0.14em] text-slate-500 uppercase">
            Workspace overview
          </p>
          <h1 className="mt-2 text-2xl font-semibold tracking-tight text-white">
            Dashboard
          </h1>
          <p className="mt-1.5 text-sm text-slate-400">
            {new Intl.DateTimeFormat(undefined, {
              weekday: 'long',
              month: 'long',
              day: 'numeric',
            }).format(new Date())}
            <span aria-hidden="true"> · </span>
            Learning, challenges, and examinations share one progress model.
          </p>
        </div>

        <nav aria-label="Quick actions" className="flex flex-wrap items-center gap-2">
          {quickActions.map((action) => (
            <Link
              className={
                action.primary
                  ? 'rounded-md border border-emerald-400/30 bg-emerald-400/10 px-3 py-1.5 text-xs font-medium text-emerald-100 transition-colors hover:bg-emerald-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300'
                  : 'rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300'
              }
              key={action.to}
              to={action.to}
            >
              {action.label}
            </Link>
          ))}
        </nav>
      </header>

      <ActiveExamBanner state={examinationsQuery.state} />

      <section aria-label="Key metrics" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          glyph="mastered"
          hint={`${masteredPercent}% of competencies mastered`}
          label="Mastered"
          loading={progressQuery.state.status === 'loading'}
          progress={masteredPercent}
          suffix={summary ? `/ ${summary.total}` : undefined}
          tone="emerald"
          value={summary?.mastered ?? 0}
        />
        <StatCard
          glyph="assessment"
          hint={assessmentHint}
          label="Pass rate"
          loading={assessmentsQuery.state.status === 'loading'}
          progress={stats.passRate}
          suffix="%"
          tone="sky"
          value={stats.passRate}
        />
        <StatCard
          glyph="clock"
          hint="Competencies waiting on a timed examination"
          label="Assessment ready"
          loading={progressQuery.state.status === 'loading'}
          tone="violet"
          value={summary?.assessmentRequired ?? 0}
        />
        <StatCard
          glyph="streak"
          hint={`${stats.total} recorded attempt${stats.total === 1 ? '' : 's'}`}
          label="Activity streak"
          loading={assessmentsQuery.state.status === 'loading'}
          suffix={stats.streak === 1 ? 'day' : 'days'}
          tone="amber"
          value={stats.streak}
        />
      </section>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          <NextStepCard onRetry={progressQuery.refresh} state={progressQuery.state} />
          <DomainProgressCard onRetry={progressQuery.refresh} state={progressQuery.state} />
          <ActivityCard onRetry={assessmentsQuery.refresh} state={assessmentsQuery.state} />
        </div>

        <div className="space-y-4">
          <ContinueLearningCard
            onRetry={progressQuery.refresh}
            state={progressQuery.state}
          />
          <FocusAreasCard onRetry={progressQuery.refresh} state={progressQuery.state} />
          <HealthCard onRetry={refreshHealth} state={healthState} />
        </div>
      </div>
    </div>
  )
}

function ActiveExamBanner({
  state,
}: {
  state: ApiQueryState<ExaminationSessionSummary[]>
}) {
  if (state.status !== 'success') {
    return null
  }

  const active = state.data.find((session) => session.isActive)

  if (!active) {
    return null
  }

  return (
    <section
      aria-label="Active examination"
      className="flex flex-wrap items-center gap-3 rounded-lg border border-emerald-400/25 bg-emerald-400/[0.06] px-4 py-3 sm:px-5"
    >
      <span className="grid size-9 shrink-0 place-items-center rounded-md border border-emerald-400/20 bg-emerald-400/10 text-emerald-200">
        <DashboardGlyph name="clock" />
      </span>

      <div className="min-w-0 flex-1">
        <p className="text-[10px] font-medium tracking-[0.12em] text-emerald-300/80 uppercase">
          Examination in progress
        </p>
        <p className="mt-0.5 truncate text-sm font-medium text-white">
          {active.challengeTitle}
          <span className="font-normal text-slate-400">
            <span aria-hidden="true"> · </span>
            {active.variantLabel}
          </span>
        </p>
      </div>

      <p className="text-xs text-emerald-100">{formatRemaining(active.remainingSeconds)}</p>

      <Link
        className="rounded-md border border-emerald-400/30 bg-emerald-400/15 px-3 py-1.5 text-xs font-medium text-emerald-50 transition-colors hover:bg-emerald-400/20 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
        to={`/examinations/${active.id}`}
      >
        Resume examination
      </Link>
    </section>
  )
}
