import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { ChallengeSummary } from '@/shared/api/challenges'
import { ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

const allValue = 'all'

const difficultyStyles: Record<string, string> = {
  Beginner: 'border-emerald-400/25 bg-emerald-400/10 text-emerald-200',
  Intermediate: 'border-sky-400/25 bg-sky-400/10 text-sky-200',
  Advanced: 'border-amber-400/25 bg-amber-400/10 text-amber-200',
}

function humanizeSlug(slug: string) {
  return slug
    .split('-')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')
}

function uniqueSorted(values: string[]) {
  return [...new Set(values)].sort((left, right) => left.localeCompare(right))
}

function ChallengeCard({ challenge }: { challenge: ChallengeSummary }) {
  return (
    <Link
      className="group flex h-full flex-col rounded-2xl border border-white/10 bg-slate-900/55 p-5 transition hover:-translate-y-0.5 hover:border-emerald-400/30 hover:bg-slate-900/80 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
      to={`/challenges/${challenge.slug}`}
    >
      <div className="flex flex-wrap items-center gap-2">
        <span
          className={[
            'rounded-full border px-2.5 py-1 text-[10px] font-semibold tracking-[0.14em] uppercase',
            difficultyStyles[challenge.difficulty] ??
              'border-white/10 bg-white/[0.04] text-slate-300',
          ].join(' ')}
        >
          {challenge.difficulty}
        </span>
        <span className="rounded-full border border-white/10 bg-white/[0.04] px-2.5 py-1 text-[10px] font-medium tracking-[0.14em] text-slate-400 uppercase">
          {challenge.type}
        </span>
        <span className="ml-auto text-[11px] font-medium text-slate-500">
          {challenge.estimatedMinutes} min
        </span>
      </div>

      <h2 className="mt-5 text-lg font-semibold text-white transition group-hover:text-emerald-100">
        {challenge.title}
      </h2>
      <p className="mt-2 text-xs leading-5 text-slate-400">{challenge.summary}</p>
      <p className="mt-3 text-[11px] text-slate-500">
        {humanizeSlug(challenge.domainSlug)} · {challenge.topic}
      </p>

      <div className="mt-auto flex flex-wrap items-center gap-2 border-t border-white/[0.07] pt-4">
        {challenge.languages.map((language) => (
          <span
            className="rounded-md border border-white/[0.08] bg-white/[0.03] px-2 py-1 text-[10px] text-slate-400"
            key={language.id}
          >
            {language.name}
          </span>
        ))}
        <span className="ml-auto inline-flex items-center gap-1.5 text-xs font-medium text-emerald-300">
          Open workspace
          <span aria-hidden="true" className="transition group-hover:translate-x-0.5">
            →
          </span>
        </span>
      </div>
    </Link>
  )
}

export function ChallengesPage() {
  const { state, refresh } = useApiQuery<ChallengeSummary[]>('/api/challenges')
  const [query, setQuery] = useState('')
  const [domain, setDomain] = useState(allValue)
  const [difficulty, setDifficulty] = useState(allValue)
  const [type, setType] = useState(allValue)

  const challenges = useMemo(
    () => (state.status === 'success' ? state.data : []),
    [state],
  )

  const domains = useMemo(
    () => uniqueSorted(challenges.map((challenge) => challenge.domainSlug)),
    [challenges],
  )
  const difficulties = useMemo(
    () => uniqueSorted(challenges.map((challenge) => challenge.difficulty)),
    [challenges],
  )
  const types = useMemo(
    () => uniqueSorted(challenges.map((challenge) => challenge.type)),
    [challenges],
  )

  const filtered = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()

    return challenges.filter((challenge) => {
      const matchesQuery =
        normalizedQuery.length === 0 ||
        [challenge.title, challenge.summary, challenge.topic, challenge.domainSlug]
          .join(' ')
          .toLowerCase()
          .includes(normalizedQuery)

      return (
        matchesQuery &&
        (domain === allValue || challenge.domainSlug === domain) &&
        (difficulty === allValue || challenge.difficulty === difficulty) &&
        (type === allValue || challenge.type === type)
      )
    })
  }, [challenges, domain, difficulty, query, type])

  const hasFilters =
    query.trim().length > 0 ||
    domain !== allValue ||
    difficulty !== allValue ||
    type !== allValue

  function clearFilters() {
    setQuery('')
    setDomain(allValue)
    setDifficulty(allValue)
    setType(allValue)
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <header>
        <p className="text-[10px] font-semibold tracking-[0.24em] text-emerald-300 uppercase">
          Challenges
        </p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
          Practice with a real workspace.
        </h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
          Open a challenge to read its statement, inspect public tests, and edit starter code
          in Monaco. Isolated execution and submissions arrive in Phase 4.
        </p>
      </header>

      <section
        aria-label="Challenge filters"
        className="rounded-2xl border border-white/10 bg-slate-900/45 p-4"
      >
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[minmax(0,1.5fr)_repeat(3,minmax(0,1fr))]">
          <label className="block">
            <span className="mb-1.5 block text-[10px] font-semibold tracking-[0.16em] text-slate-500 uppercase">
              Search
            </span>
            <input
              className="w-full rounded-lg border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-600 focus:border-emerald-400/40 focus:outline-none"
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Title, topic, or domain"
              type="search"
              value={query}
            />
          </label>

          <label className="block">
            <span className="mb-1.5 block text-[10px] font-semibold tracking-[0.16em] text-slate-500 uppercase">
              Domain
            </span>
            <select
              className="w-full rounded-lg border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 focus:border-emerald-400/40 focus:outline-none"
              onChange={(event) => setDomain(event.target.value)}
              value={domain}
            >
              <option value={allValue}>All domains</option>
              {domains.map((item) => (
                <option key={item} value={item}>
                  {humanizeSlug(item)}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="mb-1.5 block text-[10px] font-semibold tracking-[0.16em] text-slate-500 uppercase">
              Difficulty
            </span>
            <select
              className="w-full rounded-lg border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 focus:border-emerald-400/40 focus:outline-none"
              onChange={(event) => setDifficulty(event.target.value)}
              value={difficulty}
            >
              <option value={allValue}>All difficulties</option>
              {difficulties.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="mb-1.5 block text-[10px] font-semibold tracking-[0.16em] text-slate-500 uppercase">
              Type
            </span>
            <select
              className="w-full rounded-lg border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 focus:border-emerald-400/40 focus:outline-none"
              onChange={(event) => setType(event.target.value)}
              value={type}
            >
              <option value={allValue}>All types</option>
              {types.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-slate-500">
          <span>
            {filtered.length} {filtered.length === 1 ? 'challenge' : 'challenges'}
          </span>
          {hasFilters ? (
            <button
              className="rounded-lg border border-white/10 bg-white/[0.04] px-2.5 py-1 text-[11px] font-medium text-slate-300 transition hover:border-white/20 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
              onClick={clearFilters}
              type="button"
            >
              Clear filters
            </button>
          ) : null}
        </div>
      </section>

      {state.status === 'loading' ? <LoadingPanel label="Loading challenges..." /> : null}

      {state.status === 'error' ? (
        <ErrorPanel
          backLabel="Back to dashboard"
          backTo="/"
          message={state.message}
          onRetry={refresh}
          title="Unable to load challenges"
        />
      ) : null}

      {state.status === 'success' && filtered.length > 0 ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {filtered.map((challenge) => (
            <ChallengeCard challenge={challenge} key={challenge.slug} />
          ))}
        </div>
      ) : null}

      {state.status === 'success' && filtered.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-white/10 bg-white/[0.02] p-10 text-center">
          <p className="text-sm font-medium text-slate-300">No challenges match those filters.</p>
          <button
            className="mt-4 rounded-lg border border-emerald-400/25 bg-emerald-400/10 px-3 py-1.5 text-xs font-medium text-emerald-200 transition hover:bg-emerald-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            onClick={clearFilters}
            type="button"
          >
            Clear filters
          </button>
        </div>
      ) : null}
    </div>
  )
}
