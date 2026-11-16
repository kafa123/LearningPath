import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <div className="mx-auto max-w-2xl py-12">
      <p className="font-mono text-xs text-slate-500">404</p>
      <h1 className="mt-2 text-2xl font-semibold tracking-tight text-white">Page not found</h1>
      <p className="mt-2 text-sm leading-6 text-slate-400">
        The requested route does not exist.
      </p>
      <Link
        className="mt-6 inline-flex rounded-md border border-white/10 bg-white/[0.03] px-3.5 py-2 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white"
        to="/"
      >
        Return to dashboard
      </Link>
    </div>
  )
}
