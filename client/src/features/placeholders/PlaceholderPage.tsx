import { Link } from 'react-router'

interface PlaceholderPageProps {
  title: string
  phase: string
  description: string
  planned: string
}

export function PlaceholderPage({ title, phase, description, planned }: PlaceholderPageProps) {
  return (
    <div className="mx-auto max-w-3xl">
      <p className="text-[10px] font-semibold tracking-[0.24em] text-emerald-300 uppercase">
        {phase}
      </p>
      <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
        {title}
      </h1>
      <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">{description}</p>

      <div className="mt-8 rounded-2xl border border-dashed border-white/10 bg-white/[0.018] p-6">
        <p className="text-sm font-medium text-slate-200">This area is intentionally empty.</p>
        <p className="mt-2 max-w-xl text-xs leading-6 text-slate-500">{planned}</p>
        <Link
          className="mt-5 inline-flex items-center gap-2 rounded-lg border border-emerald-400/20 bg-emerald-400/10 px-3.5 py-2 text-xs font-medium text-emerald-100 transition hover:bg-emerald-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
          to="/"
        >
          <span aria-hidden="true">←</span>
          Back to dashboard
        </Link>
      </div>
    </div>
  )
}
