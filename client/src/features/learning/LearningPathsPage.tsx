import { Link } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { LearningPathSummary } from '@/shared/api/learning'
import type { ChallengeSummary } from '@/shared/api/challenges'
import { ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'
import { relatedChallenges, uniqueCompetencySlugs } from './relatedChallenges'

function LearningPathCard({
  path,
  challengeCount,
}: {
  path: LearningPathSummary
  challengeCount: number
}) {
  return (
    <Link
      className="group flex h-full flex-col rounded-lg border border-white/[0.08] bg-white/[0.02] p-5 transition-colors hover:border-white/[0.16] hover:bg-white/[0.03] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
      to={`/learning/${path.slug}`}
    >
      <div className="flex items-start justify-between gap-3 text-[11px]">
        <span className="text-slate-500">Learning path</span>
        <span className="text-slate-500">
          {path.modules.length} {path.modules.length === 1 ? 'module' : 'modules'}
          {challengeCount > 0
            ? ` · ${challengeCount} challenge${challengeCount === 1 ? '' : 's'}`
            : ''}
        </span>
      </div>

      <h2 className="mt-3 text-sm font-semibold text-white transition-colors group-hover:text-slate-200">
        {path.name}
      </h2>
      <p className="mt-1.5 text-xs leading-5 text-slate-400">{path.description}</p>

      <ul className="mt-4 space-y-1.5 border-t border-white/[0.07] pt-3 text-[11px] text-slate-500">
        {path.modules.slice(0, 3).map((module) => (
          <li className="flex items-center gap-2" key={module.slug}>
            <span aria-hidden="true" className="size-1 rounded-full bg-slate-500" />
            <span className="truncate">{module.title}</span>
          </li>
        ))}
      </ul>

      <span className="mt-auto inline-flex items-center gap-1 pt-4 text-[11px] text-slate-400">
        Browse path
        <span aria-hidden="true">→</span>
      </span>
    </Link>
  )
}

export function LearningPathsPage() {
  const { state, refresh } = useApiQuery<LearningPathSummary[]>('/api/learning-paths')
  const challengesQuery = useApiQuery<ChallengeSummary[]>('/api/challenges')
  const challenges = challengesQuery.state.status === 'success' ? challengesQuery.state.data : []

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight text-white">Learning paths</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
          Five domains connect theory to practice. Each module lists objectives, competencies,
          prerequisites, and a lesson you can read in the lab.
        </p>
      </header>

      {state.status === 'loading' ? <LoadingPanel /> : null}

      {state.status === 'error' ? (
        <ErrorPanel message={state.message} onRetry={refresh} />
      ) : null}

      {state.status === 'success' ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {state.data.map((path) => (
            <LearningPathCard
              challengeCount={
                relatedChallenges(challenges, uniqueCompetencySlugs(path.modules)).length
              }
              key={path.slug}
              path={path}
            />
          ))}
        </div>
      ) : null}
    </div>
  )
}
