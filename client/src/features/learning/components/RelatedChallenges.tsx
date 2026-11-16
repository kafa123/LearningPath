import { useId } from 'react'
import { Link } from 'react-router'
import type { ChallengeSummary } from '@/shared/api/challenges'
import { relatedChallenges } from '../relatedChallenges'

const difficultyDot: Record<string, string> = {
  Beginner: 'bg-emerald-400',
  Intermediate: 'bg-sky-400',
  Advanced: 'bg-amber-400',
}

interface RelatedChallengesProps {
  challenges: ChallengeSummary[]
  competencySlugs: readonly string[]
  description?: string
  heading?: string
}

/**
 * Lists the challenges whose assessed competencies overlap the given set. The
 * component renders nothing when the intersection is empty.
 */
export function RelatedChallenges({
  challenges,
  competencySlugs,
  description = 'Tasks that assess the competencies covered above.',
  heading = 'Practice these competencies',
}: RelatedChallengesProps) {
  const headingId = useId()
  const related = relatedChallenges(challenges, competencySlugs)

  if (related.length === 0) {
    return null
  }

  return (
    <section
      aria-labelledby={headingId}
      className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5"
    >
      <div className="flex items-end justify-between gap-3">
        <h2 className="text-base font-semibold text-white" id={headingId}>
          {heading}
        </h2>
        <span className="text-[11px] text-slate-500">{related.length}</span>
      </div>
      <p className="mt-1 text-xs leading-5 text-slate-500">{description}</p>

      <ul className="mt-3 space-y-2">
        {related.map((challenge) => {
          const executable = challenge.languages.some((language) => language.canExecute)

          return (
            <li key={challenge.slug}>
              <Link
                className="group flex items-center justify-between gap-3 rounded-md border border-white/[0.07] bg-white/[0.02] px-4 py-3 transition-colors hover:border-white/[0.16] hover:bg-white/[0.03] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                to={`/challenges/${challenge.slug}`}
              >
                <span className="min-w-0">
                  <span className="block truncate text-sm text-slate-200 group-hover:text-white">
                    {challenge.title}
                  </span>
                  <span className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-[11px] text-slate-500">
                    <span className="inline-flex items-center gap-1.5">
                      <span
                        aria-hidden="true"
                        className={[
                          'size-1.5 rounded-full',
                          difficultyDot[challenge.difficulty] ?? 'bg-slate-500',
                        ].join(' ')}
                      />
                      {challenge.difficulty}
                    </span>
                    <span>{challenge.type}</span>
                    <span>{challenge.estimatedMinutes} min</span>
                    {executable ? null : <span>Read-only</span>}
                  </span>
                </span>
                <span aria-hidden="true" className="text-slate-500">
                  →
                </span>
              </Link>
            </li>
          )
        })}
      </ul>
    </section>
  )
}
