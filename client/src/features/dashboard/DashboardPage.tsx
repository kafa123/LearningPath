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
        <h1 className="text-2xl font-semibold tracking-tight text-white">Dashboard</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
          Learning, challenges, examinations, and evidence share one progress model.
        </p>
      </header>

      <div className="grid gap-4 xl:grid-cols-[1.35fr_1fr]">
        <HealthCard onRetry={refresh} state={state} />
        <FoundationCard />
      </div>

      <section aria-labelledby="learning-overview-heading">
        <div className="flex items-end justify-between gap-4">
          <div>
            <h2 className="text-base font-semibold text-white" id="learning-overview-heading">
              Overview
            </h2>
            <p className="mt-1 text-xs text-slate-500">
              Open a path or challenge, or review recent evidence.
            </p>
          </div>
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
