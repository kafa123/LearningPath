const levelStyles: Record<string, string> = {
  Beginner: 'border-emerald-400/25 bg-emerald-400/10 text-emerald-200',
  Intermediate: 'border-sky-400/25 bg-sky-400/10 text-sky-200',
  Advanced: 'border-amber-400/25 bg-amber-400/10 text-amber-200',
}

export function LevelBadge({ level }: { level: string }) {
  const style = levelStyles[level] ?? 'border-white/10 bg-white/[0.04] text-slate-300'

  return (
    <span
      className={`inline-flex rounded-full border px-2.5 py-1 text-[10px] font-semibold tracking-[0.14em] uppercase ${style}`}
    >
      {level}
    </span>
  )
}

export function ModuleMeta({ minutes }: { minutes: number }) {
  return (
    <span className="text-[11px] font-medium tracking-wide text-slate-500">
      {minutes} min
    </span>
  )
}
