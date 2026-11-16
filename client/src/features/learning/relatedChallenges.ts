import type { ChallengeSummary } from '@/shared/api/challenges'

const difficultyRank: Record<string, number> = {
  Beginner: 0,
  Intermediate: 1,
  Advanced: 2,
}

/**
 * Challenges declare the seeded competencies they assess. Learning modules declare
 * the competencies they teach, so the intersection is the practice link between the
 * two catalogs.
 */
export function relatedChallenges(
  challenges: ChallengeSummary[],
  competencySlugs: readonly string[],
): ChallengeSummary[] {
  const wanted = new Set(competencySlugs)

  if (wanted.size === 0) {
    return []
  }

  return challenges
    .filter((challenge) =>
      challenge.competencySlugs.some((slug) => wanted.has(slug)),
    )
    .sort(
      (left, right) =>
        (difficultyRank[left.difficulty] ?? 3) - (difficultyRank[right.difficulty] ?? 3) ||
        left.title.localeCompare(right.title),
    )
}

export function uniqueCompetencySlugs(
  modules: readonly { competencySlugs: string[] }[],
): string[] {
  return [...new Set(modules.flatMap((module) => module.competencySlugs))]
}
