import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { useParams } from 'react-router'
import { useApiQuery } from '@/shared/hooks/useApiQuery'
import type { MaterialDetail } from '@/shared/api/learning'
import { Breadcrumbs, ErrorPanel, LoadingPanel } from '@/shared/components/QueryState'

function humanizeSlug(slug: string) {
  return slug
    .split('-')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')
}

export function MaterialPage() {
  const { domainSlug = '', moduleSlug = '', materialSlug = '' } = useParams<{
    domainSlug: string
    moduleSlug: string
    materialSlug: string
  }>()
  const { state, refresh } = useApiQuery<MaterialDetail>(
    `/api/materials/${encodeURIComponent(materialSlug)}`,
  )

  return (
    <div className="mx-auto max-w-4xl space-y-8">
      <Breadcrumbs
        items={[
          { label: 'Learning paths', to: '/learning' },
          { label: humanizeSlug(domainSlug), to: `/learning/${domainSlug}` },
          {
            label: state.status === 'success' ? state.data.module.title : 'Module',
            to: `/learning/${domainSlug}/${moduleSlug}`,
          },
          { label: state.status === 'success' ? state.data.title : 'Material' },
        ]}
      />

      {state.status === 'loading' ? <LoadingPanel label="Loading material..." /> : null}

      {state.status === 'error' ? (
        <ErrorPanel
          message={state.message}
          onRetry={refresh}
          title="Unable to load this material"
        />
      ) : null}

      {state.status === 'success' ? (
        <article>
          <header className="border-b border-white/[0.08] pb-6">
            <p className="text-[11px] text-slate-500">{state.data.kind}</p>
            <h1 className="mt-2 text-2xl font-semibold tracking-tight text-white">
              {state.data.title}
            </h1>
            <p className="mt-2 text-xs text-slate-500">
              {state.data.module.title} · {state.data.estimatedMinutes} min
            </p>
          </header>

          <div className="learning-prose mt-6">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {state.data.bodyMarkdown}
            </ReactMarkdown>
          </div>
        </article>
      ) : null}
    </div>
  )
}
