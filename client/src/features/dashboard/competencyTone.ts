import type { CompetencyState } from '@/shared/api/progress'

export const stateDot: Record<CompetencyState, string> = {
  NotStarted: 'bg-slate-500',
  Learning: 'bg-sky-400',
  Practicing: 'bg-amber-400',
  AssessmentRequired: 'bg-violet-400',
  Mastered: 'bg-emerald-400',
}

export const stateText: Record<CompetencyState, string> = {
  NotStarted: 'text-slate-400',
  Learning: 'text-sky-200',
  Practicing: 'text-amber-200',
  AssessmentRequired: 'text-violet-200',
  Mastered: 'text-emerald-200',
}
