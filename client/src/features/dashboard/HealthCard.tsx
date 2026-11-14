import type { HealthState } from './useHealth'

interface HealthCardProps {
  state: HealthState
  onRetry: () => void
}

type StatusLabel = 'Healthy' | 'Degraded' | 'Unhealthy' | 'Checking' | 'Unavailable'

const statusStyles: Record<StatusLabel, string> = {
  Healthy: 'border-emerald-400/25 bg-emerald-400/10 text-emerald-200',
  Degraded: 'border-amber-400/25 bg-amber-400/10 text-amber-200',
  Unhealthy: 'border-rose-400/25 bg-rose-400/10 text-rose-200',
  Checking: 'border-sky-400/25 bg-sky-400/10 text-sky-200',
  Unavailable: 'border-rose-400/25 bg-rose-400/10 text-rose-200',
}

function StatusPill({ label }: { label: StatusLabel }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[10px] font-semibold tracking-[0.14em] uppercase ${statusStyles[label]}`}
    >
      <span className="size-1.5 rounded-full bg-current" />
      {label}
    </span>
  )
}

function formatTimestamp(value: string): string {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return 'Unknown'
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'medium',
  }).format(date)
}

export function HealthCard({ state, onRetry }: HealthCardProps) {
  const isSuccess = state.status === 'success'
  const databaseStatus = isSuccess
    ? (state.data.checks.find((check) => check.name === 'database')?.status ?? 'Not reported')
    : 'Not reported'

  return (
    <section
      aria-busy={state.status === 'loading'}
      className="relative overflow-hidden rounded-2xl border border-white/10 bg-slate-900/70 p-5 shadow-2xl shadow-black/20 sm:p-6"
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-24 -right-20 size-64 rounded-full bg-emerald-400/10 blur-3xl"
      />

      <div className="relative">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-[10px] font-semibold tracking-[0.22em] text-emerald-300 uppercase">
              System status
            </p>
            <h2 className="mt-2 text-lg font-semibold text-white">Local lab health</h2>
          </div>

          {state.status === 'loading' ? <StatusPill label="Checking" /> : null}
          {state.status === 'success' ? <StatusPill label={state.data.status} /> : null}
          {state.status === 'error' ? <StatusPill label="Unavailable" /> : null}
        </div>

        <div aria-live="polite" className="mt-5">
          {state.status === 'loading' ? (
            <div className="space-y-3" role="status">
              <p className="text-sm text-slate-400">Checking local services...</p>
              <div className="h-2.5 w-2/3 animate-pulse rounded-full bg-white/10" />
              <div className="h-2.5 w-1/2 animate-pulse rounded-full bg-white/[0.07]" />
            </div>
          ) : null}

          {state.status === 'success' ? (
            <div role="status">
              <p className="text-sm text-slate-400">
                The API and local database responded successfully.
              </p>

              <dl className="mt-5 grid gap-3 sm:grid-cols-2">
                <div className="rounded-xl border border-white/[0.07] bg-white/[0.025] p-3">
                  <dt className="text-[10px] font-medium tracking-[0.16em] text-slate-500 uppercase">
                    API
                  </dt>
                  <dd className="mt-1.5 text-sm font-medium text-slate-100">{state.data.status}</dd>
                </div>
                <div className="rounded-xl border border-white/[0.07] bg-white/[0.025] p-3">
                  <dt className="text-[10px] font-medium tracking-[0.16em] text-slate-500 uppercase">
                    Database
                  </dt>
                  <dd className="mt-1.5 text-sm font-medium text-slate-100">{databaseStatus}</dd>
                </div>
                <div className="rounded-xl border border-white/[0.07] bg-white/[0.025] p-3">
                  <dt className="text-[10px] font-medium tracking-[0.16em] text-slate-500 uppercase">
                    Version
                  </dt>
                  <dd className="mt-1.5 font-mono text-sm text-slate-200">{state.data.version}</dd>
                </div>
                <div className="rounded-xl border border-white/[0.07] bg-white/[0.025] p-3">
                  <dt className="text-[10px] font-medium tracking-[0.16em] text-slate-500 uppercase">
                    Checked
                  </dt>
                  <dd className="mt-1.5 text-sm text-slate-200">
                    {formatTimestamp(state.data.timestampUtc)}
                  </dd>
                </div>
              </dl>

              <div className="mt-4 flex items-center justify-between gap-4">
                <p className="text-xs text-slate-500">
                  {Math.round(state.data.totalDurationMs)} ms total · served by{' '}
                  <code className="text-slate-400">/api/health</code>
                </p>
                <button
                  className="rounded-lg border border-white/10 bg-white/[0.04] px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:border-white/20 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                  onClick={onRetry}
                  type="button"
                >
                  Refresh
                </button>
              </div>
            </div>
          ) : null}

          {state.status === 'error' ? (
            <div role="alert">
              <p className="text-sm text-rose-200">The API is currently unavailable.</p>
              <p className="mt-2 max-w-xl text-xs leading-5 text-slate-500">{state.message}</p>
              <p className="mt-1 max-w-xl text-xs leading-5 text-slate-500">
                Make sure the backend is running on{' '}
                <code className="text-slate-400">http://localhost:5080</code>.
              </p>
              <button
                className="mt-4 rounded-lg border border-rose-300/20 bg-rose-400/10 px-3 py-1.5 text-xs font-medium text-rose-100 transition hover:bg-rose-400/15 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-rose-300"
                onClick={onRetry}
                type="button"
              >
                Retry health check
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </section>
  )
}
