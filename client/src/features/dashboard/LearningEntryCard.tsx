import { Link } from 'react-router'

const highlights = ['5 domains', '12 modules', '12 lessons', '36 objectives']

export function LearningEntryCard() {
  return (
    <Link
      className="group flex h-full flex-col rounded-lg border border-white/[0.08] bg-white/[0.02] p-5 transition-colors hover:border-white/[0.16] hover:bg-white/[0.03] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
      to="/learning"
    >
      <div className="flex items-start justify-between gap-3">
        <span className="text-[11px] text-slate-500">Learning</span>
        <span aria-hidden="true" className="text-sm text-slate-600 group-hover:text-slate-300">
          →
        </span>
      </div>

      <h3 className="mt-3 text-sm font-semibold text-white">Start a learning path</h3>
      <p className="mt-1.5 text-xs leading-5 text-slate-400">
        Read structured lessons, review objectives, and follow prerequisites across the five
        engineering domains.
      </p>

      <ul className="mt-auto flex flex-wrap gap-x-3 gap-y-1 pt-4 text-[11px] text-slate-500">
        {highlights.map((highlight) => (
          <li key={highlight}>{highlight}</li>
        ))}
      </ul>
    </Link>
  )
}
