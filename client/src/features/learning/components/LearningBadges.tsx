const levelDot: Record<string, string> = {
  Beginner: 'bg-emerald-400',
  Intermediate: 'bg-sky-400',
  Advanced: 'bg-amber-400',
}

export function LevelBadge({ level }: { level: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-[11px] text-slate-400">
      <span
        aria-hidden="true"
        className={['size-1.5 rounded-full', levelDot[level] ?? 'bg-slate-500'].join(' ')}
      />
      {level}
    </span>
  )
}

export function ModuleMeta({ minutes }: { minutes: number }) {
  return <span className="text-[11px] text-slate-500">{minutes} min</span>
}
