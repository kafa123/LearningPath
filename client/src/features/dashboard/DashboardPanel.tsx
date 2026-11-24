import type { ReactNode } from 'react'
import { Link } from 'react-router'

export function DashboardPanel({
  title,
  description,
  action,
  children,
  className = '',
}: {
  title: string
  description?: string
  action?: ReactNode
  children: ReactNode
  className?: string
}) {
  return (
    <section
      className={[
        'rounded-lg border border-white/[0.08] bg-white/[0.02]',
        className,
      ].join(' ')}
    >
      <header className="flex flex-wrap items-start justify-between gap-3 px-4 pt-4 sm:px-5 sm:pt-5">
        <div className="min-w-0">
          <h2 className="text-sm font-semibold text-white">{title}</h2>
          {description ? (
            <p className="mt-1 text-[11px] leading-5 text-slate-500">{description}</p>
          ) : null}
        </div>
        {action}
      </header>

      <div className="px-4 pt-3 pb-4 sm:px-5 sm:pt-4 sm:pb-5">{children}</div>
    </section>
  )
}

export function PanelLink({
  to,
  children,
  label,
}: {
  to: string
  children?: ReactNode
  label?: string
}) {
  return (
    <Link
      aria-label={label}
      className="inline-flex shrink-0 items-center gap-1 rounded-md px-1.5 py-1 text-[11px] text-slate-500 transition-colors hover:bg-white/[0.04] hover:text-slate-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
      to={to}
    >
      {children ?? 'View all'}
      <span aria-hidden="true">→</span>
    </Link>
  )
}
