import { HealthCard } from './HealthCard'
import { FoundationCard } from './FoundationCard'
import { ChallengesEntryCard } from './ChallengesEntryCard'
import { LearningEntryCard } from './LearningEntryCard'
import { ProgressSummaryCard } from './ProgressSummaryCard'
import { RecentAssessmentsCard } from './RecentAssessmentsCard'
import { useHealth } from './useHealth'
import type { AssessmentHistoryItem, ProgressOverview } from '@/shared/api/progress'
import { useApiQuery } from '@/shared/hooks/useApiQuery'

export function DashboardPage() {
  const { state, refresh } = useHealth()
  const progressQuery = useApiQuery<ProgressOverview>('/api/progress')
  const assessmentsQuery = useApiQuery<AssessmentHistoryItem[]>('/api/assessments?limit=3')

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <header>
        <p className="text-[10px] font-semibold tracking-[0.24em] text-emerald-300 uppercase">
          Phase 8 · Database Engineering
        </p>
        <h1 className="mt-3 max-w-3xl text-3xl font-semibold tracking-tight text-white sm:text-4xl">
          Evidence-based database engineering.
        </h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
          Write and test SQL against isolated challenge databases while competency progress
          stays derived from accepted submissions.
        </p>
      </header>

      <div className="grid gap-4 xl:grid-cols-[1.35fr_1fr]">
        <HealthCard onRetry={refresh} state={state} />
        <FoundationCard />
      </div>

      <section aria-labelledby="learning-overview-heading">
        <div className="flex items-end justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold text-white" id="learning-overview-heading">
              Lab overview
            </h2>
            <p className="mt-1 text-xs text-slate-500">
              Learning, challenges, examinations, and evidence now share one progress model.
            </p>
          </div>
          <span className="hidden rounded-full border border-white/10 bg-white/[0.03] px-3 py-1 text-[10px] tracking-[0.16em] text-slate-500 uppercase sm:inline">
            Derived on read
          </span>
        </div>

        <div className="mt-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <LearningEntryCard />
          <ChallengesEntryCard />
          <ProgressSummaryCard onRetry={progressQuery.refresh} state={progressQuery.state} />
          <RecentAssessmentsCard
            onRetry={assessmentsQuery.refresh}
            state={assessmentsQuery.state}
          />
        </div>
      </section>
    </div>
  )
}
