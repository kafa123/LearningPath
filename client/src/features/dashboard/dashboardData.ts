import type {
  AssessmentHistoryItem,
  CompetencyProgress,
  CompetencyState,
  DomainProgress,
  ModuleProgress,
  ProgressOverview,
} from '@/shared/api/progress'

export interface FocusItem {
  competency: CompetencyProgress
  module: ModuleProgress
  domain: DomainProgress
  latestChallengeSlug: string | null
}

export interface DomainSummary {
  domain: DomainProgress
  total: number
  mastered: number
  counts: Record<CompetencyState, number>
}

export interface ContinueModule {
  module: ModuleProgress
  domain: DomainProgress
  total: number
  mastered: number
  next: FocusItem | null
}

export interface AssessmentStats {
  total: number
  accepted: number
  passRate: number
  streak: number
}

const priority: Record<CompetencyState, number> = {
  AssessmentRequired: 0,
  Practicing: 1,
  Learning: 2,
  NotStarted: 3,
  Mastered: 4,
}

function collect(overview: ProgressOverview): FocusItem[] {
  const items: FocusItem[] = []

  for (const domain of overview.domains) {
    for (const module of domain.modules) {
      for (const competency of module.competencies) {
        items.push({
          competency,
          module,
          domain,
          latestChallengeSlug: competency.evidence[0]?.challengeSlug ?? null,
        })
      }
    }
  }

  return items
}

function byPriority(a: FocusItem, b: FocusItem) {
  return priority[a.competency.state] - priority[b.competency.state]
}

export function getRecommendedNext(overview: ProgressOverview): FocusItem | null {
  return (
    collect(overview)
      .filter((item) => item.competency.state !== 'Mastered')
      .sort(byPriority)[0] ?? null
  )
}

export function getFocusItems(
  overview: ProgressOverview,
  limit = 3,
): FocusItem[] {
  return collect(overview)
    .filter((item) => item.competency.state !== 'Mastered')
    .sort(byPriority)
    .slice(0, limit)
}

export function getDomainSummaries(overview: ProgressOverview): DomainSummary[] {
  return overview.domains.map((domain) => {
    const counts: Record<CompetencyState, number> = {
      NotStarted: 0,
      Learning: 0,
      Practicing: 0,
      AssessmentRequired: 0,
      Mastered: 0,
    }

    let total = 0

    for (const module of domain.modules) {
      for (const competency of module.competencies) {
        counts[competency.state] += 1
        total += 1
      }
    }

    return { domain, total, mastered: counts.Mastered, counts }
  })
}

export function getContinueModule(
  overview: ProgressOverview,
): ContinueModule | null {
  const candidates: ContinueModule[] = []

  for (const domain of overview.domains) {
    for (const module of domain.modules) {
      const competencies = module.competencies
      const mastered = competencies.filter(
        (competency) => competency.state === 'Mastered',
      ).length

      if (mastered === competencies.length) {
        continue
      }

      const next =
        competencies
          .filter((competency) => competency.state !== 'Mastered')
          .map<FocusItem>((competency) => ({
            competency,
            module,
            domain,
            latestChallengeSlug: competency.evidence[0]?.challengeSlug ?? null,
          }))
          .sort(byPriority)[0] ?? null

      candidates.push({
        module,
        domain,
        total: competencies.length,
        mastered,
        next,
      })
    }
  }

  return (
    candidates.sort((a, b) => {
      const aStarted = a.mastered > 0 ? 1 : 0
      const bStarted = b.mastered > 0 ? 1 : 0

      if (aStarted !== bStarted) {
        return bStarted - aStarted
      }

      return b.total - b.mastered - (a.total - a.mastered)
    })[0] ?? null
  )
}

export function getAssessmentStats(
  items: AssessmentHistoryItem[],
  now = new Date(),
): AssessmentStats {
  const accepted = items.filter((item) => item.isAccepted).length
  const days = new Set(
    items.map((item) => dayKey(new Date(item.createdAtUtc))),
  )

  let streak = 0
  const cursor = new Date(now)

  if (!days.has(dayKey(cursor))) {
    cursor.setDate(cursor.getDate() - 1)
  }

  while (days.has(dayKey(cursor)) && streak < 365) {
    streak += 1
    cursor.setDate(cursor.getDate() - 1)
  }

  return {
    total: items.length,
    accepted,
    passRate:
      items.length === 0 ? 0 : Math.round((accepted / items.length) * 100),
    streak,
  }
}

export function dayKey(date: Date): string {
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`
}

export function formatRelativeTime(value: string, now = new Date()): string {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return 'Unknown'
  }

  const diffMs = now.getTime() - date.getTime()
  const minutes = Math.floor(diffMs / 60000)

  if (minutes < 1) {
    return 'Just now'
  }

  if (minutes < 60) {
    return `${minutes}m ago`
  }

  const hours = Math.floor(minutes / 60)

  if (hours < 24) {
    return `${hours}h ago`
  }

  if (hours < 48) {
    return 'Yesterday'
  }

  return new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
  }).format(date)
}

export function formatRemaining(seconds: number | null | undefined): string {
  if (seconds == null || seconds <= 0) {
    return 'No time limit'
  }

  const minutes = Math.ceil(seconds / 60)

  return `${minutes} min left`
}
