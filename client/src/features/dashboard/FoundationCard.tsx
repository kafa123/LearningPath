const foundationItems = [
  {
    title: 'API and SQLite',
    detail: 'Migration-backed local persistence is initialized on startup.',
  },
  {
    title: 'Application shell',
    detail: 'Navigation is in place for the learning and assessment phases.',
  },
  {
    title: 'Automated tests',
    detail: 'xUnit integration tests and Vitest component tests are configured.',
  },
  {
    title: 'Architecture baseline',
    detail: 'Module boundaries, decisions, and execution security are documented.',
  },
]

export function FoundationCard() {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-5 sm:p-6">
      <h2 className="text-base font-semibold text-white">Foundation</h2>

      <ul className="mt-4 space-y-3">
        {foundationItems.map((item) => (
          <li className="flex gap-2.5" key={item.title}>
            <span
              aria-hidden="true"
              className="mt-2 size-1 shrink-0 rounded-full bg-slate-500"
            />
            <span>
              <span className="block text-sm text-slate-200">{item.title}</span>
              <span className="mt-0.5 block text-xs leading-5 text-slate-500">{item.detail}</span>
            </span>
          </li>
        ))}
      </ul>
    </section>
  )
}
