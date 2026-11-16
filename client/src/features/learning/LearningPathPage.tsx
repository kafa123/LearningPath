import { Link, useParams } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { LearningPathSummary } from '@/shared/api/learning'
import type { ChallengeSummary } from '@/shared/api/challenges'
import { ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'
import { LevelBadge, ModuleMeta } from './components/LearningBadges'
import { RelatedChallenges } from './components/RelatedChallenges'
import { relatedChallenges, uniqueCompetencySlugs } from './relatedChallenges'

function moduleChallengeSuffix(count: number) {
  return count > 0 ? ` · ${count} challenge${count === 1 ? '' : 's'}` : ''
}

export function LearningPathPage() {
  const { domainSlug = '' } = useParams<{ domainSlug: string }>()
  const { state, refresh } = useApiQuery<LearningPathSummary>(
    `/api/learning-paths/${encodeURIComponent(domainSlug)}`,
  )
  const challengesQuery = useApiQuery<ChallengeSummary[]>('/api/challenges')
  const challenges =
    challengesQuery.state.status === 'success' ? challengesQuery.state.data : []
  const challengeCounts = new Map<string, number>()

  if (state.status === 'success') {
    state.data.modules.forEach((module) => {
      challengeCounts.set(
        module.slug,
        relatedChallenges(challenges, module.competencySlugs).length,
      )
    })
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <nav aria-label="Breadcrumb">
        <Link
          className="text-xs text-slate-500 transition-colors hover:text-slate-200"
          to="/learning"
        >
          ← Learning paths
        </Link>
      </nav>

      {state.status === 'loading' ? <LoadingPanel /> : null}

      {state.status === 'error' ? (
        <ErrorPanel
          message={state.message}
          onRetry={refresh}
          title="Unable to load this learning path"
        />
      ) : null}

      {state.status === 'success' ? (
        <>
          <header>
            <h1 className="text-2xl font-semibold tracking-tight text-white">
              {state.data.name}
            </h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
              {state.data.description}
            </p>
          </header>

          <section aria-labelledby="modules-heading">
            <div className="flex items-end justify-between gap-4">
              <h2 className="text-base font-semibold text-white" id="modules-heading">
                Modules
              </h2>
              <span className="text-[11px] text-slate-500">
                {state.data.modules.length} in this path
              </span>
            </div>

            <div className="mt-4 grid gap-4 md:grid-cols-2">
              {state.data.modules.map((module) => (
                <Link
                  className="group flex h-full flex-col rounded-lg border border-white/[0.08] bg-white/[0.02] p-5 transition-colors hover:border-white/[0.16] hover:bg-white/[0.03] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                  key={module.slug}
                  to={`/learning/${state.data.slug}/${module.slug}`}
                >
                  <div className="flex items-center justify-between gap-3">
                    <LevelBadge level={module.level} />
                    <ModuleMeta minutes={module.estimatedMinutes} />
                  </div>

                  <h3 className="mt-3 text-sm font-semibold text-white group-hover:text-slate-200">
                    {module.title}
                  </h3>
                  <p className="mt-1.5 text-xs leading-5 text-slate-400">{module.summary}</p>

                  <div className="mt-auto flex items-center justify-between gap-3 pt-4 text-[11px] text-slate-500">
                    <span>
                      {module.prerequisiteSlugs.length > 0
                        ? `${module.prerequisiteSlugs.length} prerequisite${
                            module.prerequisiteSlugs.length === 1 ? '' : 's'
                          }`
                        : 'No prerequisites'}
                      {moduleChallengeSuffix(challengeCounts.get(module.slug) ?? 0)}
                    </span>
                    <span aria-hidden="true" className="text-slate-500">
                      →
                    </span>
                  </div>
                </Link>
              ))}
            </div>
          </section>

          {challengesQuery.state.status === 'success' ? (
            <RelatedChallenges
              challenges={challengesQuery.state.data}
              competencySlugs={uniqueCompetencySlugs(state.data.modules)}
              description="Challenges that assess the competencies taught across these modules."
              heading="Practice this path"
            />
          ) : null}
        </>
      ) : null}
    </div>
  )
}
