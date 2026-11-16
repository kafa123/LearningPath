import { Link } from 'react-router'

interface BreadcrumbItem {
  label: string
  to?: string
}

export function Breadcrumbs({ items }: { items: BreadcrumbItem[] }) {
  return (
    <nav aria-label="Breadcrumb">
      <ol className="flex flex-wrap items-center gap-2 text-xs text-slate-500">
        {items.map((item, index) => (
          <li className="flex items-center gap-2" key={`${item.label}-${index}`}>
            {index > 0 ? <span aria-hidden="true">/</span> : null}
            {item.to ? (
              <Link
                className="transition-colors hover:text-slate-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                to={item.to}
              >
                {item.label}
              </Link>
            ) : (
              <span aria-current="page" className="text-slate-400">
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
      className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5"
      role="status"
    >
      <p className="text-sm text-slate-400">{label}</p>
      <div className="mt-4 space-y-2">
        <div className="h-2 w-2/3 animate-pulse rounded-sm bg-white/[0.06]" />
        <div className="h-2 w-1/2 animate-pulse rounded-sm bg-white/[0.05]" />
        <div className="h-2 w-3/5 animate-pulse rounded-sm bg-white/[0.04]" />
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
    <div className="rounded-lg border border-rose-400/20 bg-rose-400/[0.04] p-5" role="alert">
      <p className="text-sm font-medium text-rose-200">{title}</p>
      <p className="mt-1.5 max-w-xl text-xs leading-5 text-slate-500">{message}</p>
      <div className="mt-4 flex flex-wrap gap-2">
        {onRetry ? (
          <button
            className="rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
            onClick={onRetry}
            type="button"
          >
            Try again
          </button>
        ) : null}
        <Link
          className="rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white"
          to={backTo}
        >
          {backLabel}
        </Link>
      </div>
    </div>
  )
}
