import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { ChallengeSummary } from '@/shared/api/challenges'
import { ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

const allValue = 'all'

const difficultyDot: Record<string, string> = {
  Beginner: 'bg-emerald-400',
  Intermediate: 'bg-sky-400',
  Advanced: 'bg-amber-400',
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
      className="group flex h-full flex-col rounded-lg border border-white/[0.08] bg-white/[0.02] p-5 transition-colors hover:border-white/[0.16] hover:bg-white/[0.03] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
      to={`/challenges/${challenge.slug}`}
    >
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[11px]">
        <span className="inline-flex items-center gap-1.5 text-slate-400">
          <span
            aria-hidden="true"
            className={[
              'size-1.5 rounded-full',
              difficultyDot[challenge.difficulty] ?? 'bg-slate-500',
            ].join(' ')}
          />
          {challenge.difficulty}
        </span>
        <span className="text-slate-500">{challenge.type}</span>
        <span className="ml-auto text-slate-500">{challenge.estimatedMinutes} min</span>
      </div>

      <h2 className="mt-3 text-sm font-semibold text-white transition-colors group-hover:text-slate-200">
        {challenge.title}
      </h2>
      <p className="mt-1.5 text-xs leading-5 text-slate-400">{challenge.summary}</p>
      <p className="mt-2 text-[11px] text-slate-500">
        {humanizeSlug(challenge.domainSlug)} · {challenge.topic}
      </p>

      <div className="mt-auto flex flex-wrap items-center gap-x-3 gap-y-1 border-t border-white/[0.07] pt-3 text-[11px] text-slate-500">
        {challenge.languages.map((language) => (
          <span key={language.id}>{language.name}</span>
        ))}
        <span className="ml-auto inline-flex items-center gap-1 text-slate-400 group-hover:text-slate-200">
          Open workspace
          <span aria-hidden="true">→</span>
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
        <h1 className="text-2xl font-semibold tracking-tight text-white">Challenges</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
          Browse every validated challenge package. Executable languages run in an isolated
          container; read-only packages remain available for review.
        </p>
      </header>

      <section
        aria-label="Challenge filters"
        className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-4"
      >
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[minmax(0,1.5fr)_repeat(3,minmax(0,1fr))]">
          <label className="block">
            <span className="mb-1 block text-xs text-slate-500">Search</span>
            <input
              className="w-full rounded-md border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-600 focus:border-white/25 focus:outline-none"
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Title, topic, or domain"
              type="search"
              value={query}
            />
          </label>

          <label className="block">
            <span className="mb-1 block text-xs text-slate-500">Domain</span>
            <select
              className="w-full rounded-md border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 focus:border-white/25 focus:outline-none"
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
            <span className="mb-1 block text-xs text-slate-500">Difficulty</span>
            <select
              className="w-full rounded-md border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 focus:border-white/25 focus:outline-none"
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
            <span className="mb-1 block text-xs text-slate-500">Type</span>
            <select
              className="w-full rounded-md border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 focus:border-white/25 focus:outline-none"
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
              className="rounded-md border border-white/10 bg-white/[0.03] px-2.5 py-1 text-[11px] text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
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
        <div className="rounded-lg border border-dashed border-white/[0.1] bg-white/[0.01] p-10 text-center">
          <p className="text-sm text-slate-300">No challenges match those filters.</p>
          <button
            className="mt-4 rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
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
