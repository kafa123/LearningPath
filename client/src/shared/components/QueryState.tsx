import { Link } from 'react-router'

interface BreadcrumbItem {
  label: string
  to?: string
}

export function Breadcrumbs({ items }: { items: BreadcrumbItem[] }) {
  return (
    <nav aria-label="Breadcrumb">
      <ol className="flex flex-wrap items-center gap-2 text-[11px] text-slate-500">
        {items.map((item, index) => (
          <li className="flex items-center gap-2" key={`${item.label}-${index}`}>
            {index > 0 ? <span aria-hidden="true">/</span> : null}
            {item.to ? (
              <Link
                className="transition hover:text-emerald-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                to={item.to}
              >
                {item.label}
              </Link>
            ) : (
              <span aria-current="page" className="text-slate-300">
                {item.label}
              </span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  )
}

interface LoadingPanelProps {
  label?: string
}

export function LoadingPanel({ label = 'Loading learning content...' }: LoadingPanelProps) {
  return (
    <div
      aria-live="polite"
      className="rounded-2xl border border-white/10 bg-slate-900/45 p-6"
      role="status"
    >
      <p className="text-sm text-slate-400">{label}</p>
      <div className="mt-4 space-y-2.5">
        <div className="h-2.5 w-2/3 animate-pulse rounded-full bg-white/10" />
        <div className="h-2.5 w-1/2 animate-pulse rounded-full bg-white/[0.07]" />
        <div className="h-2.5 w-3/5 animate-pulse rounded-full bg-white/[0.05]" />
      </div>
    </div>
  )
}

interface ErrorPanelProps {
  title?: string
  message: string
  onRetry?: () => void
  backTo?: string
  backLabel?: string
}

export function ErrorPanel({
  title = 'Unable to load learning content',
  message,
  onRetry,
  backTo = '/learning',
  backLabel = 'Back to learning paths',
}: ErrorPanelProps) {
  return (
    <div className="rounded-2xl border border-rose-400/20 bg-rose-400/[0.06] p-6" role="alert">
      <p className="text-sm font-medium text-rose-100">{title}</p>
      <p className="mt-2 max-w-xl text-xs leading-5 text-slate-400">{message}</p>
      <div className="mt-4 flex flex-wrap gap-3">
        {onRetry ? (
          <button
            className="rounded-lg border border-rose-300/20 bg-rose-400/10 px-3 py-1.5 text-xs font-medium text-rose-100 transition hover:bg-rose-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-rose-300"
            onClick={onRetry}
            type="button"
          >
            Try again
          </button>
        ) : null}
        <Link
          className="rounded-lg border border-white/10 bg-white/[0.04] px-3 py-1.5 text-xs font-medium text-slate-200 transition hover:border-white/20 hover:text-white"
          to={backTo}
        >
          {backLabel}
        </Link>
      </div>
    </div>
  )
}
