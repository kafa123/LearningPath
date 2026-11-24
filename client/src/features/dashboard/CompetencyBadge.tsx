import type { CompetencyState } from '@/shared/api/progress'
import { stateDot, stateText } from './competencyTone'

export function CompetencyBadge({ state }: { state: CompetencyState }) {
  return (
    <span
      className={[
        'inline-flex shrink-0 items-center gap-1.5 rounded-full border border-white/[0.08] bg-white/[0.03] px-2 py-0.5 text-[10px] font-medium',
        stateText[state],
      ].join(' ')}
    >
      <span aria-hidden="true" className={['size-1.5 rounded-full', stateDot[state]].join(' ')} />
      {state}
    </span>
  )
}
