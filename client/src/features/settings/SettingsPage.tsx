import type { HealthStatus } from '@/shared/api/health'
import type { ExecutionRuntimeInfo } from '@/shared/api/runtime'
import { useApiQuery, type ApiQueryState } from '@/shared/hooks/useApiQuery'
import { useHealth, type HealthState } from '@/features/dashboard/useHealth'
import { Breadcrumbs, ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'
import { useTheme } from '@/shared/theme/ThemeContext'

const healthStyles: Record<HealthStatus, string> = {
  Healthy: 'border-emerald-400/25 bg-emerald-400/10 text-emerald-200',
  Degraded: 'border-amber-400/25 bg-amber-400/10 text-amber-200',
  Unhealthy: 'border-rose-400/25 bg-rose-400/10 text-rose-200',
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

function formatBytes(bytes: number): string {
  if (bytes >= 1024 * 1024) {
    return `${(bytes / (1024 * 1024)).toFixed(1)} MiB`
  }

  return `${Math.round(bytes / 1024)} KiB`
}

function StatTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-white/[0.07] bg-white/[0.025] p-3">
      <dt className="text-[10px] font-medium tracking-[0.16em] text-slate-500 uppercase">
        {label}
      </dt>
      <dd className="mt-1.5 font-mono text-sm text-slate-100">{value}</dd>
    </div>
  )
}

function HealthSection({
  onRetry,
  state,
}: {
  onRetry: () => void
  state: HealthState
}) {
  return (
    <section
      aria-labelledby="settings-health-heading"
      className="rounded-2xl border border-white/10 bg-slate-900/55 p-5 sm:p-6"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-[10px] font-semibold tracking-[0.22em] text-emerald-300 uppercase">
            System status
          </p>
          <h2 className="mt-2 text-lg font-semibold text-white" id="settings-health-heading">
            API health
          </h2>
        </div>

        {state.status === 'success' ? (
          <span
            className={[
              'inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[10px] font-semibold tracking-[0.14em] uppercase',
              healthStyles[state.data.status],
            ].join(' ')}
          >
            <span aria-hidden="true" className="size-1.5 rounded-full bg-current" />
            {state.data.status}
          </span>
        ) : null}
        {state.status === 'loading' ? (
          <span className="rounded-full border border-sky-400/25 bg-sky-400/10 px-2.5 py-1 text-[10px] font-semibold tracking-[0.14em] text-sky-200 uppercase">
            Checking
          </span>
        ) : null}
        {state.status === 'error' ? (
          <span className="rounded-full border border-rose-400/25 bg-rose-400/10 px-2.5 py-1 text-[10px] font-semibold tracking-[0.14em] text-rose-200 uppercase">
            Unavailable
          </span>
        ) : null}
      </div>

      {state.status === 'loading' ? (
        <p aria-live="polite" className="mt-4 text-sm text-slate-400" role="status">
          Checking local services...
        </p>
      ) : null}

      {state.status === 'error' ? (
        <div className="mt-4" role="alert">
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

      {state.status === 'success' ? (
        <div className="mt-4" role="status">
          <p className="text-xs text-slate-500">
            The API and local database responded successfully.
          </p>
          <dl className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <StatTile label="API" value={state.data.status} />
            <StatTile
              label="Database"
              value={
                state.data.checks.find((check) => check.name === 'database')?.status ??
                'Not reported'
              }
            />
            <StatTile label="Version" value={state.data.version} />
            <StatTile label="Checked" value={formatTimestamp(state.data.timestampUtc)} />
          </dl>
          <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
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
    </section>
  )
}

function RuntimeSection({
  onRetry,
  state,
}: {
  onRetry: () => void
  state: ApiQueryState<ExecutionRuntimeInfo>
}) {
  return (
    <section
      aria-labelledby="settings-runtime-heading"
      className="rounded-2xl border border-white/10 bg-slate-900/55 p-5 sm:p-6"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-[10px] font-semibold tracking-[0.22em] text-emerald-300 uppercase">
            Execution runtime
          </p>
          <h2 className="mt-2 text-lg font-semibold text-white" id="settings-runtime-heading">
            Runtimes and limits
          </h2>
          <p className="mt-1 max-w-xl text-xs leading-5 text-slate-500">
            Values below are safe diagnostics reported by the server. They are read-only here.
          </p>
        </div>

        {state.status === 'success' ? (
          <span
            className={[
              'rounded-full border px-2.5 py-1 text-[10px] font-semibold tracking-[0.14em] uppercase',
              state.data.executionEnabled
                ? 'border-emerald-400/25 bg-emerald-400/10 text-emerald-200'
                : 'border-amber-300/25 bg-amber-300/10 text-amber-100',
            ].join(' ')}
          >
            {state.data.executionEnabled ? 'Execution enabled' : 'Execution disabled'}
          </span>
        ) : null}
      </div>

      {state.status === 'loading' ? (
        <div className="mt-4">
          <LoadingPanel label="Loading runtime diagnostics..." />
        </div>
      ) : null}

      {state.status === 'error' ? (
        <div className="mt-4">
          <ErrorPanel
            backLabel="Back to dashboard"
            backTo="/"
            message={state.message}
            onRetry={onRetry}
            title="Unable to load runtime diagnostics"
          />
        </div>
      ) : null}

      {state.status === 'success' ? (
        <div className="mt-5 space-y-6">
          <div>
            <h3 className="text-[11px] font-semibold tracking-[0.16em] text-slate-400 uppercase">
              Languages
            </h3>
            <ul className="mt-3 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {state.data.languages.map((language) => (
                <li
                  className="rounded-xl border border-white/[0.07] bg-white/[0.025] p-3"
                  key={language.id}
                >
                  <p className="text-sm font-medium text-slate-100">{language.name}</p>
                  <p className="mt-1 text-[10px] tracking-[0.12em] text-slate-500 uppercase">
                    {language.id}
                  </p>
                  <code className="mt-2 block truncate text-xs text-slate-400" title={language.image}>
                    {language.image}
                  </code>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h3 className="text-[11px] font-semibold tracking-[0.16em] text-slate-400 uppercase">
              Limits
            </h3>
            <dl className="mt-3 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <StatTile label="Time limit" value={`${state.data.limits.timeLimitSeconds}s`} />
              <StatTile label="Memory" value={`${state.data.limits.memoryLimitMb} MB`} />
              <StatTile label="CPU" value={state.data.limits.cpuLimit.toFixed(1)} />
              <StatTile label="PIDs" value={String(state.data.limits.pidsLimit)} />
              <StatTile label="Max output" value={formatBytes(state.data.limits.maxOutputBytes)} />
              <StatTile
                label="Concurrent"
                value={String(state.data.limits.maxConcurrentExecutions)}
              />
              <StatTile label="Queue" value={String(state.data.limits.maxQueuedExecutions)} />
            </dl>
          </div>
        </div>
      ) : null}
    </section>
  )
}

export function SettingsPage() {
  const health = useHealth()
  const runtimeQuery = useApiQuery<ExecutionRuntimeInfo>('/api/runtime')
  const { theme, setTheme } = useTheme()

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <Breadcrumbs items={[{ label: 'Settings' }]} />

      <header>
        <p className="text-[10px] font-semibold tracking-[0.24em] text-emerald-300 uppercase">
          Settings
        </p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-white sm:text-4xl">
          Know your runtime.
        </h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
          Inspect the local API, execution languages, and resource limits. Configuration stays
          server-owned through appsettings and environment variables, so nothing here can weaken
          the execution sandbox.
        </p>
      </header>

      <div className="rounded-2xl border border-sky-400/20 bg-sky-400/[0.06] px-4 py-3 text-xs leading-5 text-sky-100">
        This page is read-only. Change runtime images and limits in{' '}
        <code className="text-sky-100">appsettings.json</code> or the corresponding environment
        variables, then restart the API.
      </div>

      <section
        aria-labelledby="settings-appearance-heading"
        className="flex flex-wrap items-center justify-between gap-4 rounded-2xl border border-white/10 bg-slate-900/55 px-5 py-4"
      >
        <div>
          <h2 className="text-sm font-semibold text-white" id="settings-appearance-heading">
            Appearance
          </h2>
          <p className="mt-1 text-xs text-slate-500">
            Choose a theme for this browser. The preference stays on this device.
          </p>
        </div>
        <div className="flex gap-2" role="group" aria-label="Color theme">
          {(['light', 'dark'] as const).map((option) => {
            const isActive = theme === option

            return (
              <button
                aria-pressed={isActive}
                className={[
                  'rounded-lg border px-3 py-1.5 text-xs font-medium capitalize transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300',
                  isActive
                    ? 'border-emerald-400/30 bg-emerald-400/10 text-emerald-100'
                    : 'border-white/10 bg-white/[0.04] text-slate-300 hover:border-white/20 hover:text-white',
                ].join(' ')}
                key={option}
                onClick={() => setTheme(option)}
                type="button"
              >
                {option}
              </button>
            )
          })}
        </div>
      </section>

      <HealthSection onRetry={health.refresh} state={health.state} />
      <RuntimeSection onRetry={runtimeQuery.refresh} state={runtimeQuery.state} />
    </div>
  )
}
