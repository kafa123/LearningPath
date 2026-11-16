import { Link, useParams } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { ModuleDetail } from '@/shared/api/learning'
import type { ChallengeSummary } from '@/shared/api/challenges'
import { Breadcrumbs, ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'
import { LevelBadge, ModuleMeta } from './components/LearningBadges'
import { RelatedChallenges } from './components/RelatedChallenges'

export function ModulePage() {
  const { domainSlug = '', moduleSlug = '' } = useParams<{
    domainSlug: string
    moduleSlug: string
  }>()
  const { state, refresh } = useApiQuery<ModuleDetail>(
    `/api/modules/${encodeURIComponent(moduleSlug)}`,
  )
  const challengesQuery = useApiQuery<ChallengeSummary[]>('/api/challenges')

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <Breadcrumbs
        items={[
          { label: 'Learning paths', to: '/learning' },
          {
            label: state.status === 'success' ? state.data.domain.name : 'Learning path',
            to: `/learning/${domainSlug}`,
          },
          { label: state.status === 'success' ? state.data.title : 'Module' },
        ]}
      />

      {state.status === 'loading' ? <LoadingPanel label="Loading module..." /> : null}

      {state.status === 'error' ? (
        <ErrorPanel
          message={state.message}
          onRetry={refresh}
          title="Unable to load this module"
        />
      ) : null}

      {state.status === 'success' ? (
        <>
          <header>
            <div className="flex flex-wrap items-center gap-3">
              <LevelBadge level={state.data.level} />
              <ModuleMeta minutes={state.data.estimatedMinutes} />
            </div>
            <h1 className="mt-2 text-2xl font-semibold tracking-tight text-white">
              {state.data.title}
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">
              {state.data.summary}
            </p>
          </header>

          <div className="grid gap-6 lg:grid-cols-[1.4fr_1fr]">
            <section aria-labelledby="objectives-heading">
              <h2 className="text-base font-semibold text-white" id="objectives-heading">
                Learning objectives
              </h2>
              <p className="mt-1 text-xs text-slate-500">
                What you should be able to do after working through this module.
              </p>

              <ol className="mt-4 space-y-2">
                {state.data.objectives.map((objective, index) => (
                  <li
                    className="flex gap-3 rounded-md border border-white/[0.07] bg-white/[0.02] p-4"
                    key={objective}
                  >
                    <span className="grid size-5 shrink-0 place-items-center rounded-sm border border-white/[0.1] font-mono text-[10px] text-slate-400">
                      {index + 1}
                    </span>
                    <span className="text-sm leading-6 text-slate-300">{objective}</span>
                  </li>
                ))}
              </ol>

              <section aria-labelledby="competencies-heading" className="mt-8">
                <h2 className="text-base font-semibold text-white" id="competencies-heading">
                  Competencies
                </h2>
                <p className="mt-1 text-xs text-slate-500">
                  Measurable skills this module contributes to.
                </p>

                <div className="mt-4 grid gap-2 sm:grid-cols-2">
                  {state.data.competencies.map((competency) => (
                    <article
                      className="rounded-md border border-white/[0.07] bg-white/[0.02] p-4"
                      key={competency.slug}
                    >
                      <h3 className="text-sm text-slate-100">{competency.name}</h3>
                      <p className="mt-1 text-xs leading-5 text-slate-500">
                        {competency.description}
                      </p>
                    </article>
                  ))}
                </div>
              </section>
            </section>

            <div className="space-y-6">
              {challengesQuery.state.status === 'success' ? (
                <RelatedChallenges
                  challenges={challengesQuery.state.data}
                  competencySlugs={state.data.competencies.map(
                    (competency) => competency.slug,
                  )}
                  description="Challenges that assess the competencies this module teaches."
                />
              ) : null}

              <section
                aria-labelledby="materials-heading"
                className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5"
              >
                <div className="flex items-end justify-between gap-3">
                  <h2 className="text-base font-semibold text-white" id="materials-heading">
                    Materials
                  </h2>
                  <span className="text-[11px] text-slate-500">
                    {state.data.materials.length}{' '}
                    {state.data.materials.length === 1 ? 'lesson' : 'lessons'}
                  </span>
                </div>

                <ul className="mt-3 space-y-2">
                  {state.data.materials.map((material) => (
                    <li key={material.slug}>
                      <Link
                        className="group flex items-center justify-between gap-3 rounded-md border border-white/[0.07] bg-white/[0.02] px-4 py-3 transition-colors hover:border-white/[0.16] hover:bg-white/[0.03] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                        to={`/learning/${state.data.domain.slug}/${state.data.slug}/materials/${material.slug}`}
                      >
                        <span className="min-w-0">
                          <span className="block truncate text-sm text-slate-200 group-hover:text-white">
                            {material.title}
                          </span>
                          <span className="mt-0.5 block text-[11px] text-slate-500">
                            {material.kind} · {material.estimatedMinutes} min
                          </span>
                        </span>
                        <span aria-hidden="true" className="text-slate-500">
                          →
                        </span>
                      </Link>
                    </li>
                  ))}
                </ul>
              </section>

              <section
                aria-labelledby="prerequisites-heading"
                className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5"
              >
                <h2 className="text-base font-semibold text-white" id="prerequisites-heading">
                  Prerequisites
                </h2>

                {state.data.prerequisites.length === 0 ? (
                  <p className="mt-2 text-xs leading-5 text-slate-500">
                    No prior module is required. Start here.
                  </p>
                ) : (
                  <ul className="mt-3 space-y-2">
                    {state.data.prerequisites.map((prerequisite) => (
                      <li key={prerequisite.slug}>
                        <Link
                          className="flex items-center justify-between gap-3 rounded-md border border-white/[0.07] bg-white/[0.02] px-4 py-3 text-sm text-slate-300 transition-colors hover:border-white/[0.16] hover:text-white"
                          to={`/learning/${prerequisite.domainSlug}/${prerequisite.slug}`}
                        >
                          <span className="min-w-0 truncate">{prerequisite.title}</span>
                          <span aria-hidden="true" className="text-slate-500">
                            →
                          </span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            </div>
          </div>
        </>
      ) : null}
    </div>
  )
}
