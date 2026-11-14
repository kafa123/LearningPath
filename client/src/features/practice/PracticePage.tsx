import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import type { ChallengeSummary } from '@/shared/api/challenges'
import type {
  AssessmentHistoryItem,
  CompetencyProgress,
  CompetencyState,
  ProgressOverview,
} from '@/shared/api/progress'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import { Breadcrumbs, ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

const allValue = 'all'

type PracticeStatus = 'not-started' | 'in-progress' | 'solved'

const statusStyles: Record<PracticeStatus, string> = {
  'not-started': 'border-white/10 bg-white/[0.04] text-slate-300',
  'in-progress': 'border-amber-300/25 bg-amber-300/10 text-amber-100',
  solved: 'border-emerald-400/25 bg-emerald-400/10 text-emerald-100',
}

const statusLabels: Record<PracticeStatus, string> = {
  'not-started': 'Not started',
  'in-progress': 'In progress',
  solved: 'Solved',
}

const competencyPriority: Record<CompetencyState, number> = {
  AssessmentRequired: 5,
  Practicing: 4,
  Learning: 3,
  NotStarted: 2,
  Mastered: 1,
}

const competencyReason: Record<CompetencyState, string> = {
  AssessmentRequired: 'Ready for assessment',
  Practicing: 'Keep practicing',
  Learning: 'Build on your learning',
  NotStarted: 'Start here',
  Mastered: 'Stay sharp',
}

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

function isExecutable(challenge: ChallengeSummary) {
  return challenge.languages.some((language) => language.canExecute)
}

function statusRank(status: PracticeStatus) {
  return status === 'in-progress' ? 0 : status === 'not-started' ? 1 : 2
}

function actionLabel(status: PracticeStatus) {
  if (status === 'solved') {
    return 'Practice again'
  }

  return status === 'in-progress' ? 'Continue practice' : 'Start practice'
}

interface Recommendation {
  challenge: ChallengeSummary
  reason: string
  status: PracticeStatus
  priority: number
}

interface PracticeChallengeCardProps {
  challenge: ChallengeSummary
  competencyNames: Map<string, string>
  featured?: boolean
  reason?: string
  status: PracticeStatus
}

function PracticeChallengeCard({
  challenge,
  competencyNames,
  featured = false,
  reason,
  status,
}: PracticeChallengeCardProps) {
  const languages = challenge.languages.filter((language) => language.canExecute)
  const competencies = challenge.competencySlugs
    .map((slug) => competencyNames.get(slug) ?? humanizeSlug(slug))
    .slice(0, 2)

  return (
    <Link
      className={[
        'group flex h-full flex-col rounded-2xl border p-5 transition hover:-translate-y-0.5 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300',
        featured
          ? 'border-emerald-400/25 bg-emerald-400/[0.06] hover:border-emerald-300/40 hover:bg-emerald-400/[0.09]'
          : 'border-white/10 bg-slate-900/55 hover:border-emerald-400/30 hover:bg-slate-900/80',
      ].join(' ')}
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
        <span
          className={[
            'ml-auto rounded-full border px-2.5 py-1 text-[10px] font-semibold tracking-[0.12em] uppercase',
            statusStyles[status],
          ].join(' ')}
        >
          {statusLabels[status]}
        </span>
      </div>

      {reason ? (
        <p className="mt-4 text-[10px] font-semibold tracking-[0.16em] text-emerald-300 uppercase">
          {reason}
        </p>
      ) : null}

      <h3 className="mt-3 text-lg font-semibold text-white transition group-hover:text-emerald-100">
        {challenge.title}
      </h3>
      <p className="mt-2 text-xs leading-5 text-slate-400">{challenge.summary}</p>
      <p className="mt-3 text-[11px] text-slate-500">
        {humanizeSlug(challenge.domainSlug)} · {challenge.topic}
      </p>

      {competencies.length > 0 ? (
        <ul className="mt-3 flex flex-wrap gap-1.5" aria-label="Mapped competencies">
          {competencies.map((competency) => (
            <li
              className="rounded-md border border-white/[0.08] bg-white/[0.03] px-2 py-1 text-[10px] text-slate-400"
              key={competency}
            >
              {competency}
            </li>
          ))}
        </ul>
      ) : null}

      <div className="mt-auto flex flex-wrap items-center gap-2 border-t border-white/[0.07] pt-4">
        {languages.map((language) => (
          <span
            className="rounded-md border border-white/[0.08] bg-white/[0.03] px-2 py-1 text-[10px] text-slate-400"
            key={language.id}
          >
            {language.name}
          </span>
        ))}
        <span className="text-[10px] text-slate-500">{challenge.estimatedMinutes} min</span>
        <span className="ml-auto inline-flex items-center gap-1.5 text-xs font-medium text-emerald-300">
          {actionLabel(status)}
          <span aria-hidden="true" className="transition group-hover:translate-x-0.5">
            →
          </span>
        </span>
      </div>
    </Link>
  )
}

function SummaryTile({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-slate-900/45 p-4">
      <p className="text-[10px] font-semibold tracking-[0.16em] text-slate-500 uppercase">
        {label}
      </p>
      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>
    </div>
  )
}

export function PracticePage() {
  const challengesQuery = useApiQuery<ChallengeSummary[]>('/api/challenges')
  const progressQuery = useApiQuery<ProgressOverview>('/api/progress')
  const assessmentsQuery = useApiQuery<AssessmentHistoryItem[]>('/api/assessments?limit=50')
  const [query, setQuery] = useState('')
  const [domain, setDomain] = useState(allValue)
  const [difficulty, setDifficulty] = useState(allValue)
  const [type, setType] = useState(allValue)

  const challenges = useMemo(
    () => (challengesQuery.state.status === 'success' ? challengesQuery.state.data : []),
    [challengesQuery.state],
  )
  const executableChallenges = useMemo(
    () => challenges.filter(isExecutable),
    [challenges],
  )
  const competencies = useMemo(() => {
    const map = new Map<string, CompetencyProgress>()

    if (progressQuery.state.status === 'success') {
      progressQuery.state.data.domains.forEach((progressDomain) => {
        progressDomain.modules.forEach((module) => {
          module.competencies.forEach((competency) => {
            map.set(competency.slug, competency)
          })
        })
      })
    }

    return map
  }, [progressQuery.state])
  const latestAttempts = useMemo(() => {
    const map = new Map<string, AssessmentHistoryItem>()

    if (assessmentsQuery.state.status === 'success') {
      assessmentsQuery.state.data.forEach((item) => {
        if (!map.has(item.challengeSlug)) {
          map.set(item.challengeSlug, item)
        }
      })
    }

    return map
  }, [assessmentsQuery.state])

  const competencyNames = useMemo(
    () => new Map([...competencies].map(([slug, competency]) => [slug, competency.name])),
    [competencies],
  )

  function statusFor(challenge: ChallengeSummary): PracticeStatus {
    const latest = latestAttempts.get(challenge.slug)

    if (!latest) {
      return 'not-started'
    }

    return latest.isAccepted ? 'solved' : 'in-progress'
  }

  const recommendations = useMemo<Recommendation[]>(() => {
    const scored = executableChallenges.map((challenge) => {
      const latest = latestAttempts.get(challenge.slug)
      const status: PracticeStatus = !latest
        ? 'not-started'
        : latest.isAccepted
          ? 'solved'
          : 'in-progress'
      const mapped = challenge.competencySlugs
        .map((slug) => competencies.get(slug))
        .filter((competency): competency is CompetencyProgress => competency !== undefined)
      const top = mapped.reduce<CompetencyProgress | undefined>(
        (current, competency) =>
          current === undefined ||
          competencyPriority[competency.state] > competencyPriority[current.state]
            ? competency
            : current,
        undefined,
      )

      return {
        challenge,
        priority: top === undefined ? 0 : competencyPriority[top.state],
        reason:
          status === 'in-progress'
            ? 'Continue your attempt'
            : top === undefined
              ? 'Recommended practice'
              : competencyReason[top.state],
        status,
      }
    })

    const needsWork = scored.filter(
      (item) => item.priority >= competencyPriority.Learning,
    )
    const pool = needsWork.length > 0 ? needsWork : scored

    return [...pool]
      .sort(
        (left, right) =>
          right.priority - left.priority ||
          statusRank(left.status) - statusRank(right.status) ||
          left.challenge.estimatedMinutes - right.challenge.estimatedMinutes,
      )
      .slice(0, 3)
  }, [competencies, executableChallenges, latestAttempts])

  const domains = useMemo(
    () => uniqueSorted(executableChallenges.map((challenge) => challenge.domainSlug)),
    [executableChallenges],
  )
  const difficulties = useMemo(
    () => uniqueSorted(executableChallenges.map((challenge) => challenge.difficulty)),
    [executableChallenges],
  )
  const types = useMemo(
    () => uniqueSorted(executableChallenges.map((challenge) => challenge.type)),
    [executableChallenges],
  )

  const filtered = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()

    return executableChallenges.filter((challenge) => {
      const searchable = [
        challenge.title,
        challenge.summary,
        challenge.topic,
        challenge.domainSlug,
        ...challenge.competencySlugs.map(
          (slug) => competencies.get(slug)?.name ?? humanizeSlug(slug),
        ),
      ]
        .join(' ')
        .toLowerCase()

      return (
        (normalizedQuery.length === 0 || searchable.includes(normalizedQuery)) &&
        (domain === allValue || challenge.domainSlug === domain) &&
        (difficulty === allValue || challenge.difficulty === difficulty) &&
        (type === allValue || challenge.type === type)
      )
    })
  }, [competencies, difficulty, domain, executableChallenges, query, type])

  const hasFilters =
    query.trim().length > 0 ||
    domain !== allValue ||
    difficulty !== allValue ||
    type !== allValue
  const startedCount = executableChallenges.filter(
    (challenge) => statusFor(challenge) !== 'not-started',
  ).length
  const solvedCount = executableChallenges.filter(
    (challenge) => statusFor(challenge) === 'solved',
  ).length
  const readOnlyCount = challenges.length - executableChallenges.length

  function clearFilters() {
    setQuery('')
    setDomain(allValue)
    setDifficulty(allValue)
    setType(allValue)
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <Breadcrumbs items={[{ label: 'Practice' }]} />

      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-[10px] font-semibold tracking-[0.24em] text-emerald-300 uppercase">
            Practice
          </p>
          <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
            Practice deliberately. Then prove it.
          </h1>
          <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
            Open a realistic task, use hints when you need them, and run the public tests
            until the behavior is right. Submit only when you are ready for the full
            assessment.
          </p>
        </div>
        <Link
          className="rounded-lg border border-white/10 bg-white/[0.04] px-3 py-1.5 text-xs font-medium text-slate-200 transition hover:border-white/20 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
          to="/challenges"
        >
          Browse full catalog
        </Link>
      </header>

      <section aria-label="Practice summary" className="grid gap-3 sm:grid-cols-3">
        <SummaryTile label="Practice-ready challenges" value={executableChallenges.length} />
        <SummaryTile label="Started" value={startedCount} />
        <SummaryTile label="Solved" value={solvedCount} />
      </section>

      {progressQuery.state.status === 'error' ||
      assessmentsQuery.state.status === 'error' ? (
        <p className="rounded-xl border border-amber-300/20 bg-amber-300/[0.06] px-4 py-3 text-xs text-amber-100">
          Progress-aware recommendations are temporarily unavailable. Challenge practice
          still works.
        </p>
      ) : null}

      {challengesQuery.state.status === 'loading' ? (
        <LoadingPanel label="Loading practice challenges..." />
      ) : null}

      {challengesQuery.state.status === 'error' ? (
        <ErrorPanel
          backLabel="Back to dashboard"
          backTo="/"
          message={challengesQuery.state.message}
          onRetry={challengesQuery.refresh}
          title="Unable to load practice challenges"
        />
      ) : null}

      {challengesQuery.state.status === 'success' && executableChallenges.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-white/10 bg-white/[0.02] p-10 text-center">
          <p className="text-sm font-medium text-slate-300">Nothing to practice yet.</p>
          <p className="mt-1 text-xs text-slate-500">
            No challenge currently declares a trusted execution harness. You can still
            browse the catalog.
          </p>
          <Link
            className="mt-4 inline-flex rounded-lg border border-emerald-400/25 bg-emerald-400/10 px-3 py-1.5 text-xs font-medium text-emerald-200 transition hover:bg-emerald-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            to="/challenges"
          >
            Browse challenges
          </Link>
        </div>
      ) : null}

      {challengesQuery.state.status === 'success' && recommendations.length > 0 ? (
        <section aria-labelledby="recommended-practice-heading">
          <div>
            <h2
              className="text-lg font-semibold text-white"
              id="recommended-practice-heading"
            >
              Recommended next
            </h2>
            <p className="mt-1 text-xs text-slate-500">
              Ordered by the competency evidence that needs attention next.
            </p>
          </div>

          <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {recommendations.map((recommendation) => (
              <PracticeChallengeCard
                challenge={recommendation.challenge}
                competencyNames={competencyNames}
                featured
                key={recommendation.challenge.slug}
                reason={recommendation.reason}
                status={recommendation.status}
              />
            ))}
          </div>
        </section>
      ) : null}

      {challengesQuery.state.status === 'success' && executableChallenges.length > 0 ? (
        <section aria-labelledby="practice-challenges-heading" className="space-y-4">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div>
              <h2 className="text-lg font-semibold text-white" id="practice-challenges-heading">
                Practice challenges
              </h2>
              <p className="mt-1 text-xs text-slate-500">
                Only challenges with a trusted execution harness appear here.
                {readOnlyCount > 0
                  ? ` ${readOnlyCount} catalog challenge${
                      readOnlyCount === 1 ? ' is' : 's are'
                    } read-only.`
                  : ''}
              </p>
            </div>
            <span className="text-xs text-slate-500">
              {filtered.length} of {executableChallenges.length}
            </span>
          </div>

          <div className="rounded-2xl border border-white/10 bg-slate-900/45 p-4">
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[minmax(0,1.5fr)_repeat(3,minmax(0,1fr))]">
              <label className="block">
                <span className="mb-1.5 block text-[10px] font-semibold tracking-[0.16em] text-slate-500 uppercase">
                  Search
                </span>
                <input
                  className="w-full rounded-lg border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-600 focus:border-emerald-400/40 focus:outline-none"
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Title, topic, or competency"
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
              <span>{filtered.length} matching</span>
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
          </div>

          {filtered.length > 0 ? (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {filtered.map((challenge) => (
                <PracticeChallengeCard
                  challenge={challenge}
                  competencyNames={competencyNames}
                  key={challenge.slug}
                  status={statusFor(challenge)}
                />
              ))}
            </div>
          ) : (
            <div className="rounded-2xl border border-dashed border-white/10 bg-white/[0.02] p-10 text-center">
              <p className="text-sm font-medium text-slate-300">
                No practice challenges match those filters.
              </p>
              <button
                className="mt-4 rounded-lg border border-emerald-400/25 bg-emerald-400/10 px-3 py-1.5 text-xs font-medium text-emerald-200 transition hover:bg-emerald-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                onClick={clearFilters}
                type="button"
              >
                Clear filters
              </button>
            </div>
          )}
        </section>
      ) : null}
    </div>
  )
}
