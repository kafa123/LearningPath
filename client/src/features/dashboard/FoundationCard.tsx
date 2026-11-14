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
    <section className="rounded-2xl border border-white/10 bg-slate-900/45 p-5 sm:p-6">
      <p className="text-[10px] font-semibold tracking-[0.22em] text-slate-500 uppercase">
        Phase 1 foundation
      </p>
      <h2 className="mt-2 text-lg font-semibold text-white">What is ready</h2>

      <ul className="mt-5 space-y-3.5">
        {foundationItems.map((item) => (
          <li className="flex gap-3" key={item.title}>
            <span
              aria-hidden="true"
              className="mt-0.5 grid size-5 shrink-0 place-items-center rounded-full border border-emerald-400/25 bg-emerald-400/10 text-emerald-300"
            >
              <svg
                className="size-3"
                fill="none"
                stroke="currentColor"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="2"
                viewBox="0 0 24 24"
              >
                <path d="m5 12 4 4L19 6" />
              </svg>
            </span>
            <span>
              <span className="block text-sm font-medium text-slate-200">{item.title}</span>
              <span className="mt-0.5 block text-xs leading-5 text-slate-500">{item.detail}</span>
            </span>
          </li>
        ))}
      </ul>
    </section>
  )
}
