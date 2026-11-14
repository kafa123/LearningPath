export function PageLoading({ label = 'Loading...' }: { label?: string }) {
  return (
    <div aria-live="polite" className="mx-auto max-w-4xl" role="status">
      <p className="text-sm text-slate-400">{label}</p>
    </div>
  )
}
