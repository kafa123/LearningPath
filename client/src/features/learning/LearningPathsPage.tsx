import { Link } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { LearningPathSummary } from '@/shared/api/learning'
import { ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

function LearningPathCard({ path }: { path: LearningPathSummary }) {
  return (
    <Link
      className="group flex h-full flex-col rounded-2xl border border-white/10 bg-slate-900/55 p-5 transition hover:-translate-y-0.5 hover:border-emerald-400/30 hover:bg-slate-900/80 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
      to={`/learning/${path.slug}`}
    >
      <div className="flex items-start justify-between gap-3">
        <span className="rounded-full border border-white/10 bg-white/[0.04] px-2.5 py-1 text-[10px] font-semibold tracking-[0.16em] text-slate-400 uppercase">
          Learning path
        </span>
        <span className="text-[11px] font-medium text-slate-500">
          {path.modules.length} {path.modules.length === 1 ? 'module' : 'modules'}
        </span>
      </div>

      <h2 className="mt-5 text-lg font-semibold text-white transition group-hover:text-emerald-100">
        {path.name}
      </h2>
      <p className="mt-2 text-xs leading-5 text-slate-400">{path.description}</p>

      <ul className="mt-5 space-y-2 border-t border-white/[0.07] pt-4">
        {path.modules.slice(0, 3).map((module) => (
          <li className="flex items-center gap-2 text-xs text-slate-400" key={module.slug}>
            <span aria-hidden="true" className="size-1 rounded-full bg-emerald-400/70" />
            <span className="truncate">{module.title}</span>
          </li>
        ))}
      </ul>

      <span className="mt-auto inline-flex items-center gap-1.5 pt-5 text-xs font-medium text-emerald-300">
        Browse path
        <span aria-hidden="true" className="transition group-hover:translate-x-0.5">
          →
        </span>
      </span>
    </Link>
  )
}

export function LearningPathsPage() {
  const { state, refresh } = useApiQuery<LearningPathSummary[]>('/api/learning-paths')

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <header>
        <p className="text-[10px] font-semibold tracking-[0.24em] text-emerald-300 uppercase">
          Learning paths
        </p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
          Build understanding from the ground up.
        </h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
          Five domains connect theory to practice. Each module lists objectives,
          competencies, prerequisites, and a lesson you can read in the lab.
        </p>
      </header>

      {state.status === 'loading' ? <LoadingPanel /> : null}

      {state.status === 'error' ? (
        <ErrorPanel message={state.message} onRetry={refresh} />
      ) : null}

      {state.status === 'success' ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {state.data.map((path) => (
            <LearningPathCard key={path.slug} path={path} />
          ))}
        </div>
      ) : null}
    </div>
  )
}
