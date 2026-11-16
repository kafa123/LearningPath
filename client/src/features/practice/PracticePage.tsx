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

const statusDot: Record<PracticeStatus, string> = {
  'not-started': 'bg-slate-500',
  'in-progress': 'bg-amber-400',
  solved: 'bg-emerald-400',
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
        'group flex h-full flex-col rounded-lg border p-5 transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300',
        featured
          ? 'border-white/[0.14] bg-white/[0.03] hover:border-white/[0.22]'
          : 'border-white/[0.08] bg-white/[0.02] hover:border-white/[0.16] hover:bg-white/[0.03]',
      ].join(' ')}
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
        <span className="ml-auto inline-flex items-center gap-1.5 text-slate-500">
          <span aria-hidden="true" className={['size-1.5 rounded-full', statusDot[status]].join(' ')} />
          {statusLabels[status]}
        </span>
      </div>

      {reason ? <p className="mt-3 text-[11px] text-slate-500">{reason}</p> : null}

      <h3 className="mt-2 text-sm font-semibold text-white transition-colors group-hover:text-slate-200">
        {challenge.title}
      </h3>
      <p className="mt-1.5 text-xs leading-5 text-slate-400">{challenge.summary}</p>
      <p className="mt-2 text-[11px] text-slate-500">
        {humanizeSlug(challenge.domainSlug)} · {challenge.topic}
      </p>

      {competencies.length > 0 ? (
        <ul className="mt-2 flex flex-wrap gap-x-3 gap-y-1 text-[11px] text-slate-500" aria-label="Mapped competencies">
          {competencies.map((competency) => (
            <li key={competency}>{competency}</li>
          ))}
        </ul>
      ) : null}

      <div className="mt-auto flex flex-wrap items-center gap-x-3 gap-y-1 border-t border-white/[0.07] pt-3 text-[11px] text-slate-500">
        {languages.map((language) => (
          <span key={language.id}>{language.name}</span>
        ))}
        <span>{challenge.estimatedMinutes} min</span>
        <span className="ml-auto inline-flex items-center gap-1 text-slate-400 group-hover:text-slate-200">
          {actionLabel(status)}
          <span aria-hidden="true">→</span>
        </span>
      </div>
    </Link>
  )
}

function SummaryTile({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-4">
      <p className="text-xs text-slate-500">{label}</p>
      <p className="mt-1.5 text-2xl font-semibold text-white">{value}</p>
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
          <h1 className="text-2xl font-semibold tracking-tight text-white">Practice</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
            Open a realistic task, use hints when you need them, and run the public tests until
            the behavior is right. Submit only when you are ready for the full assessment.
          </p>
        </div>
        <Link
          className="rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
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
        <p className="rounded-lg border border-white/[0.08] bg-white/[0.02] px-4 py-3 text-xs text-slate-400">
          Progress-aware recommendations are temporarily unavailable. Challenge practice still
          works.
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
        <div className="rounded-lg border border-dashed border-white/[0.1] bg-white/[0.01] p-10 text-center">
          <p className="text-sm text-slate-300">Nothing to practice yet.</p>
          <p className="mt-1 text-xs text-slate-500">
            No challenge currently declares a trusted execution harness. You can still browse
            the catalog.
          </p>
          <Link
            className="mt-4 inline-flex rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            to="/challenges"
          >
            Browse challenges
          </Link>
        </div>
      ) : null}

      {challengesQuery.state.status === 'success' && recommendations.length > 0 ? (
        <section aria-labelledby="recommended-practice-heading">
          <div>
            <h2 className="text-base font-semibold text-white" id="recommended-practice-heading">
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
              <h2 className="text-base font-semibold text-white" id="practice-challenges-heading">
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

          <div className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-4">
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[minmax(0,1.5fr)_repeat(3,minmax(0,1fr))]">
              <label className="block">
                <span className="mb-1 block text-xs text-slate-500">Search</span>
                <input
                  className="w-full rounded-md border border-white/10 bg-slate-950/60 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-600 focus:border-white/25 focus:outline-none"
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Title, topic, or competency"
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
              <span>{filtered.length} matching</span>
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
            <div className="rounded-lg border border-dashed border-white/[0.1] bg-white/[0.01] p-10 text-center">
              <p className="text-sm text-slate-300">
                No practice challenges match those filters.
              </p>
              <button
                className="mt-4 rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
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
