import type { HealthState } from './useHealth'

interface HealthCardProps {
  state: HealthState
  onRetry: () => void
}

type StatusLabel = 'Healthy' | 'Degraded' | 'Unhealthy' | 'Checking' | 'Unavailable'

const statusTone: Record<StatusLabel, string> = {
  Healthy: 'text-emerald-300',
  Degraded: 'text-amber-300',
  Unhealthy: 'text-rose-300',
  Checking: 'text-slate-400',
  Unavailable: 'text-rose-300',
}

function StatusLabelText({ label }: { label: StatusLabel }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 text-[11px] font-medium ${statusTone[label]}`}
    >
      <span aria-hidden="true" className="size-1.5 rounded-full bg-current" />
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
    ? (state.data.checks.find((check) => check.name === 'database')?.status ??
      'Not reported')
    : 'Not reported'

  return (
    <section
      aria-busy={state.status === 'loading'}
      className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-4 sm:p-5"
    >
      <header className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-sm font-semibold text-white">Local lab health</h2>

        {state.status === 'loading' ? <StatusLabelText label="Checking" /> : null}
        {state.status === 'success' ? <StatusLabelText label={state.data.status} /> : null}
        {state.status === 'error' ? <StatusLabelText label="Unavailable" /> : null}
      </header>

      <div aria-live="polite" className="mt-3">
        {state.status === 'loading' ? (
          <div className="space-y-2" role="status">
            <p className="text-xs text-slate-400">Checking local services...</p>
            <div className="h-1.5 w-2/3 animate-pulse rounded bg-white/[0.06]" />
            <div className="h-1.5 w-1/2 animate-pulse rounded bg-white/[0.05]" />
          </div>
        ) : null}

        {isSuccess ? (
          <div role="status">
            <p className="text-[11px] leading-5 text-slate-400">
              The API and local database responded successfully.
            </p>

            <dl className="mt-3 space-y-1.5 border-t border-white/[0.06] pt-3 text-[11px]">
              <div className="flex items-center justify-between gap-3">
                <dt className="text-slate-500">API</dt>
                <dd className="text-slate-200">{state.data.status}</dd>
              </div>
              <div className="flex items-center justify-between gap-3">
                <dt className="text-slate-500">Database</dt>
                <dd className="text-slate-200">{databaseStatus}</dd>
              </div>
              <div className="flex items-center justify-between gap-3">
                <dt className="text-slate-500">Version</dt>
                <dd className="font-mono text-slate-300">{state.data.version}</dd>
              </div>
            </dl>

            <p className="mt-2.5 text-[10px] leading-5 text-slate-600">
              Checked {formatTimestamp(state.data.timestampUtc)}
            </p>

            <div className="mt-2 flex items-center justify-between gap-3 border-t border-white/[0.06] pt-3">
              <p className="text-[10px] text-slate-500">
                {Math.round(state.data.totalDurationMs)} ms ·{' '}
                <code className="text-slate-400">/api/health</code>
              </p>
              <button
                className="rounded-md border border-white/10 bg-white/[0.03] px-2.5 py-1 text-[11px] font-medium text-slate-300 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
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
            <p className="text-xs text-rose-200">The API is currently unavailable.</p>
            <p className="mt-1.5 text-[11px] leading-5 text-slate-500">{state.message}</p>
            <p className="mt-1 text-[11px] leading-5 text-slate-500">
              Make sure the backend is running on{' '}
              <code className="text-slate-400">http://localhost:5080</code>.
            </p>
            <button
              className="mt-3 rounded-md border border-white/10 bg-white/[0.03] px-2.5 py-1.5 text-[11px] font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
              onClick={onRetry}
              type="button"
            >
              Retry health check
            </button>
          </div>
        ) : null}
      </div>
    </section>
  )
}
