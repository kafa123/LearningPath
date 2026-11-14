import { Link, useParams } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { LearningPathSummary } from '@/shared/api/learning'
import { ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'
import { LevelBadge, ModuleMeta } from './components/LearningBadges'

export function LearningPathPage() {
  const { domainSlug = '' } = useParams<{ domainSlug: string }>()
  const { state, refresh } = useApiQuery<LearningPathSummary>(
    `/api/learning-paths/${encodeURIComponent(domainSlug)}`,
  )

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <nav aria-label="Breadcrumb">
        <Link
          className="text-[11px] font-medium text-slate-500 transition hover:text-emerald-200"
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
            <p className="text-[10px] font-semibold tracking-[0.24em] text-emerald-300 uppercase">
              Learning path
            </p>
            <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
              {state.data.name}
            </h1>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
              {state.data.description}
            </p>
          </header>

          <section aria-labelledby="modules-heading">
            <div className="flex items-end justify-between gap-4">
              <h2 className="text-lg font-semibold text-white" id="modules-heading">
                Modules
              </h2>
              <span className="text-[11px] text-slate-500">
                {state.data.modules.length} in this path
              </span>
            </div>

            <div className="mt-4 grid gap-4 md:grid-cols-2">
              {state.data.modules.map((module) => (
                <Link
                  className="group flex h-full flex-col rounded-2xl border border-white/10 bg-slate-900/45 p-5 transition hover:-translate-y-0.5 hover:border-emerald-400/30 hover:bg-slate-900/70 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                  key={module.slug}
                  to={`/learning/${state.data.slug}/${module.slug}`}
                >
                  <div className="flex items-center justify-between gap-3">
                    <LevelBadge level={module.level} />
                    <ModuleMeta minutes={module.estimatedMinutes} />
                  </div>

                  <h3 className="mt-4 text-base font-semibold text-white group-hover:text-emerald-100">
                    {module.title}
                  </h3>
                  <p className="mt-2 text-xs leading-5 text-slate-400">{module.summary}</p>

                  <div className="mt-auto flex items-center justify-between gap-3 pt-5">
                    <span className="text-[11px] text-slate-500">
                      {module.prerequisiteSlugs.length > 0
                        ? `${module.prerequisiteSlugs.length} prerequisite${
                            module.prerequisiteSlugs.length === 1 ? '' : 's'
                          }`
                        : 'No prerequisites'}
                    </span>
                    <span aria-hidden="true" className="text-sm text-emerald-300">
                      →
                    </span>
                  </div>
                </Link>
              ))}
            </div>
          </section>
        </>
      ) : null}
    </div>
  )
}
