import { useRef, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import {
  importChallengePackage,
  importLearningContent,
  type ChallengePackageImportResult,
  type LearningContentImportResult,
} from '@/shared/api/authoring'
import { ApiError } from '@/shared/api/http'

type RequestState<T> =
  | { status: 'idle' }
  | { status: 'pending' }
  | { status: 'success'; result: T }
  | { status: 'error'; message: string }

function describeError(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Unable to reach the PSEL API.'
}

function SuccessPanel({
  children,
  title,
}: {
  children: ReactNode
  title: string
}) {
  return (
    <div
      className="mt-4 rounded-md border border-emerald-400/20 bg-emerald-400/[0.04] p-4"
      role="status"
    >
      <p className="text-sm font-medium text-emerald-100">{title}</p>
      <p className="mt-1 text-xs leading-5 text-slate-400">{children}</p>
    </div>
  )
}

function ErrorNotice({ message }: { message: string }) {
  return (
    <div
      className="mt-4 rounded-md border border-rose-400/20 bg-rose-400/[0.04] p-4"
      role="alert"
    >
      <p className="text-xs leading-5 text-rose-200">{message}</p>
    </div>
  )
}

function LearningImportSection() {
  const [file, setFile] = useState<File | null>(null)
  const [state, setState] = useState<RequestState<LearningContentImportResult>>({
    status: 'idle',
  })
  const inputRef = useRef<HTMLInputElement>(null)

  const pending = state.status === 'pending'

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!file || pending) {
      return
    }

    setState({ status: 'pending' })

    try {
      const result = await importLearningContent(file)
      setState({ status: 'success', result })
      setFile(null)

      if (inputRef.current) {
        inputRef.current.value = ''
      }
    } catch (error) {
      setState({ status: 'error', message: describeError(error) })
    }
  }

  return (
    <section
      aria-labelledby="authoring-learning-heading"
      className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5 sm:p-6"
    >
      <h2 className="text-base font-semibold text-white" id="authoring-learning-heading">
        Learning content
      </h2>
      <p className="mt-1 max-w-2xl text-xs leading-5 text-slate-500">
        Upload a JSON content pack. Domains, modules, competencies, materials, and
        objectives are matched by slug and updated in place. Nothing is deleted.
      </p>

      <form className="mt-4 space-y-3" onSubmit={handleSubmit}>
        <label className="block max-w-md">
          <span className="mb-1 block text-xs text-slate-500">Content pack (.json)</span>
          <input
            accept=".json,application/json"
            className="block w-full cursor-pointer rounded-md border border-white/10 bg-slate-950/60 text-xs text-slate-400 file:mr-3 file:cursor-pointer file:border-0 file:bg-white/[0.06] file:px-3 file:py-2 file:text-xs file:text-slate-200 hover:file:bg-white/[0.1] focus:border-white/25 focus:outline-none"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            ref={inputRef}
            type="file"
          />
        </label>

        <button
          className="rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
          disabled={!file || pending}
          type="submit"
        >
          {pending ? 'Importing...' : 'Import content'}
        </button>
      </form>

      {state.status === 'success' ? (
        <SuccessPanel title="Content pack applied.">
          {state.result.domainCount}{' '}
          {state.result.domainCount === 1 ? 'domain' : 'domains'} ·{' '}
          {state.result.moduleCount}{' '}
          {state.result.moduleCount === 1 ? 'module' : 'modules'} ·{' '}
          {state.result.added} added · {state.result.updated} updated
        </SuccessPanel>
      ) : null}

      {state.status === 'error' ? <ErrorNotice message={state.message} /> : null}
    </section>
  )
}

function ChallengeImportSection() {
  const [file, setFile] = useState<File | null>(null)
  const [replace, setReplace] = useState(false)
  const [state, setState] = useState<RequestState<ChallengePackageImportResult>>({
    status: 'idle',
  })
  const inputRef = useRef<HTMLInputElement>(null)

  const pending = state.status === 'pending'

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!file || pending) {
      return
    }

    setState({ status: 'pending' })

    try {
      const result = await importChallengePackage(file, { replace })
      setState({ status: 'success', result })
      setFile(null)
      setReplace(false)

      if (inputRef.current) {
        inputRef.current.value = ''
      }
    } catch (error) {
      setState({ status: 'error', message: describeError(error) })
    }
  }

  return (
    <section
      aria-labelledby="authoring-challenge-heading"
      className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5 sm:p-6"
    >
      <h2 className="text-base font-semibold text-white" id="authoring-challenge-heading">
        Challenge package
      </h2>
      <p className="mt-1 max-w-2xl text-xs leading-5 text-slate-500">
        Upload one .zip archive with a single challenge.json. The package is validated
        before it replaces anything, and it becomes available without restarting the API.
      </p>

      <form className="mt-4 space-y-3" onSubmit={handleSubmit}>
        <label className="block max-w-md">
          <span className="mb-1 block text-xs text-slate-500">Package archive (.zip)</span>
          <input
            accept=".zip,application/zip"
            className="block w-full cursor-pointer rounded-md border border-white/10 bg-slate-950/60 text-xs text-slate-400 file:mr-3 file:cursor-pointer file:border-0 file:bg-white/[0.06] file:px-3 file:py-2 file:text-xs file:text-slate-200 hover:file:bg-white/[0.1] focus:border-white/25 focus:outline-none"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            ref={inputRef}
            type="file"
          />
        </label>

        <label className="flex w-fit items-center gap-2 text-xs text-slate-400">
          <input
            checked={replace}
            className="size-3.5 rounded border-white/20 bg-slate-950/60 accent-emerald-400"
            onChange={(event) => setReplace(event.target.checked)}
            type="checkbox"
          />
          Replace the existing package with the same slug
        </label>

        <button
          className="rounded-md border border-white/10 bg-white/[0.03] px-3 py-1.5 text-xs font-medium text-slate-200 transition-colors hover:border-white/[0.18] hover:text-white disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
          disabled={!file || pending}
          type="submit"
        >
          {pending ? 'Uploading...' : 'Upload package'}
        </button>
      </form>

      {state.status === 'success' ? (
        <SuccessPanel
          title={`${state.result.replaced ? 'Package replaced' : 'Package installed'}: ${
            state.result.title
          }`}
        >
          <code className="text-slate-400">{state.result.slug}</code> ·{' '}
          {state.result.domainSlug} · {state.result.challengeCount}{' '}
          {state.result.challengeCount === 1 ? 'challenge' : 'challenges'} in the catalog
        </SuccessPanel>
      ) : null}

      {state.status === 'error' ? <ErrorNotice message={state.message} /> : null}
    </section>
  )
}

export function AuthoringPage() {
  return (
    <div className="mx-auto max-w-4xl space-y-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight text-white">Authoring</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
          Import learning content and install challenge packages on this local workspace.
          Every upload is validated by the server before it is written.
        </p>
      </header>

      <LearningImportSection />
      <ChallengeImportSection />
    </div>
  )
}
