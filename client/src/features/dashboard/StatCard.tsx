import type { ReactNode } from 'react'
import { DashboardGlyph, type DashboardGlyphName } from './DashboardGlyph'

export type StatTone = 'emerald' | 'sky' | 'violet' | 'amber'

const iconTone: Record<StatTone, string> = {
  emerald: 'border-emerald-400/20 bg-emerald-400/10 text-emerald-300',
  sky: 'border-sky-400/20 bg-sky-400/10 text-sky-300',
  violet: 'border-violet-400/20 bg-violet-400/10 text-violet-300',
  amber: 'border-amber-400/20 bg-amber-400/10 text-amber-300',
}

const barTone: Record<StatTone, string> = {
  emerald: 'bg-emerald-400/80',
  sky: 'bg-sky-400/80',
  violet: 'bg-violet-400/80',
  amber: 'bg-amber-400/80',
}

export function StatCard({
  label,
  value,
  suffix,
  hint,
  glyph,
  tone,
  progress,
  loading = false,
}: {
  label: string
  value: ReactNode
  suffix?: string
  hint: string
  glyph: DashboardGlyphName
  tone: StatTone
  progress?: number
  loading?: boolean
}) {
  return (
    <article className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-4">
      <div className="flex items-center justify-between gap-3">
        <p className="text-[10px] font-medium tracking-[0.12em] text-slate-500 uppercase">
          {label}
        </p>
        <span
          className={[
            'grid size-7 place-items-center rounded-md border',
            iconTone[tone],
          ].join(' ')}
        >
          <DashboardGlyph name={glyph} />
        </span>
      </div>

      <p className="mt-3 flex items-baseline gap-1 text-2xl font-semibold tracking-tight text-white">
        {loading ? (
          <span className="inline-block h-7 w-16 animate-pulse rounded bg-white/[0.06]" />
        ) : (
          value
        )}
        {suffix && !loading ? (
          <span className="text-sm font-normal text-slate-500">{suffix}</span>
        ) : null}
      </p>

      <p className="mt-1 truncate text-[11px] leading-5 text-slate-500">{hint}</p>

      {typeof progress === 'number' ? (
        <div
          aria-hidden="true"
          className="mt-3 h-1 overflow-hidden rounded-full bg-white/[0.06]"
        >
          <div
            className={[
              'h-full rounded-full transition-[width] duration-500',
              barTone[tone],
            ].join(' ')}
            style={{ width: `${Math.max(0, Math.min(100, progress))}%` }}
          />
        </div>
      ) : null}
    </article>
  )
}
