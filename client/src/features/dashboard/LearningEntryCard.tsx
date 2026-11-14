import { Link } from 'react-router'

const highlights = ['5 domains', '12 modules', '12 lessons', '36 objectives']

export function LearningEntryCard() {
  return (
    <Link
      className="group flex h-full flex-col rounded-2xl border border-emerald-400/25 bg-emerald-400/[0.06] p-5 transition hover:-translate-y-0.5 hover:border-emerald-400/40 hover:bg-emerald-400/[0.09] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
      to="/learning"
    >
      <div className="flex items-start justify-between gap-3">
        <span className="rounded-full border border-emerald-400/25 bg-emerald-400/10 px-2.5 py-1 text-[10px] font-semibold tracking-[0.16em] text-emerald-200 uppercase">
          Available now
        </span>
        <span aria-hidden="true" className="text-sm text-emerald-300">
          →
        </span>
      </div>

      <h3 className="mt-5 text-base font-semibold text-white group-hover:text-emerald-100">
        Start a learning path
      </h3>
      <p className="mt-2 text-xs leading-5 text-slate-400">
        Read structured lessons, review objectives, and follow prerequisites across the five
        engineering domains.
      </p>

      <ul className="mt-auto flex flex-wrap gap-2 pt-5">
        {highlights.map((highlight) => (
          <li
            className="rounded-md border border-white/[0.08] bg-white/[0.03] px-2 py-1 text-[10px] text-slate-400"
            key={highlight}
          >
            {highlight}
          </li>
        ))}
      </ul>
    </Link>
  )
}
