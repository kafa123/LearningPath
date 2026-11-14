import { Link } from 'react-router'

const highlights = ['6 challenges', 'C# and Python', 'Monaco editor', 'Public tests']

export function ChallengesEntryCard() {
  return (
    <Link
      className="group flex h-full flex-col rounded-2xl border border-sky-400/25 bg-sky-400/[0.06] p-5 transition hover:-translate-y-0.5 hover:border-sky-400/40 hover:bg-sky-400/[0.09] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-300"
      to="/challenges"
    >
      <div className="flex items-start justify-between gap-3">
        <span className="rounded-full border border-sky-400/25 bg-sky-400/10 px-2.5 py-1 text-[10px] font-semibold tracking-[0.16em] text-sky-200 uppercase">
          Available now
        </span>
        <span aria-hidden="true" className="text-sm text-sky-300">
          →
        </span>
      </div>

      <h3 className="mt-5 text-base font-semibold text-white group-hover:text-sky-100">
        Open a challenge workspace
      </h3>
      <p className="mt-2 text-xs leading-5 text-slate-400">
        Read statements, review examples and public tests, then edit starter files with
        language tabs and per-file drafts.
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
