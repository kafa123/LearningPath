import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <div className="mx-auto max-w-2xl py-12">
      <p className="font-mono text-xs tracking-[0.24em] text-emerald-300 uppercase">404</p>
      <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white">Page not found</h1>
      <p className="mt-3 text-sm leading-6 text-slate-400">
        The requested route does not exist.
      </p>
      <Link
        className="mt-6 inline-flex rounded-lg border border-white/10 bg-white/[0.04] px-3.5 py-2 text-xs font-medium text-slate-200 transition hover:border-white/20 hover:text-white"
        to="/"
      >
        Return to dashboard
      </Link>
    </div>
  )
}
