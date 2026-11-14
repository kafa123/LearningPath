import { Link, NavLink, Outlet } from 'react-router'
import { navigationItems } from '@/app/navigation'
import { NavGlyph } from '@/shared/components/NavGlyph'

export function AppShell() {
  return (
    <div className="min-h-screen text-slate-100">
      <a
        className="sr-only z-50 rounded-lg bg-emerald-400 px-4 py-2 font-semibold text-slate-950 focus:not-sr-only focus:fixed focus:top-4 focus:left-4"
        href="#main-content"
      >
        Skip to content
      </a>

      <div className="mx-auto flex min-h-screen w-full max-w-[1600px] flex-col lg:flex-row">
        <aside className="border-b border-white/10 bg-slate-900/55 px-4 py-4 backdrop-blur lg:sticky lg:top-0 lg:h-screen lg:w-72 lg:shrink-0 lg:border-r lg:border-b-0 lg:px-5 lg:py-8">
          <div className="flex items-center justify-between gap-4 lg:block">
            <Link className="group flex items-center gap-3" to="/">
              <span className="grid size-10 place-items-center rounded-xl border border-emerald-300/30 bg-emerald-400/10 font-mono text-sm font-bold text-emerald-300 transition group-hover:bg-emerald-400/15">
                PS
              </span>
              <span>
                <span className="block text-sm font-semibold tracking-[0.24em] text-emerald-300">
                  PSEL
                </span>
                <span className="mt-0.5 block text-xs text-slate-500">
                  Software Engineering Lab
                </span>
              </span>
            </Link>

            <span className="rounded-full border border-emerald-400/20 bg-emerald-400/10 px-2.5 py-1 text-[10px] font-semibold tracking-[0.16em] text-emerald-200 uppercase lg:hidden">
              Phase 8
            </span>
          </div>

          <nav
            aria-label="Main navigation"
            className="mt-6 flex gap-1 overflow-x-auto pb-1 lg:mt-10 lg:flex-col lg:gap-1.5 lg:overflow-visible lg:pb-0"
          >
            {navigationItems.map((item) => (
              <NavLink
                className={({ isActive }) =>
                  [
                    'group flex shrink-0 items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition',
                    isActive
                      ? 'bg-emerald-400/10 text-emerald-100 ring-1 ring-emerald-400/20'
                      : 'text-slate-400 hover:bg-white/[0.04] hover:text-slate-100',
                  ].join(' ')
                }
                end={item.end}
                key={item.to}
                to={item.to}
              >
                <NavGlyph name={item.glyph} />
                <span className="whitespace-nowrap">{item.label}</span>
                {item.badge ? (
                  <span className="ml-1 hidden rounded-md bg-white/[0.04] px-1.5 py-0.5 text-[9px] font-medium tracking-wide text-slate-500 uppercase sm:inline lg:ml-auto">
                    {item.badge}
                  </span>
                ) : null}
              </NavLink>
            ))}
          </nav>

          <div className="mt-auto hidden pt-10 lg:block">
            <div className="rounded-2xl border border-white/10 bg-white/[0.025] p-4">
              <p className="text-[10px] font-semibold tracking-[0.2em] text-emerald-300 uppercase">
                Workspace live
              </p>
              <p className="mt-2 text-xs leading-5 text-slate-500">
                Phase 8 adds isolated SQL challenges with server-owned schemas, seeds, and a
                built-in SQLite harness.
              </p>
            </div>
          </div>
        </aside>

        <main
          className="min-w-0 flex-1 px-4 py-8 sm:px-6 lg:px-10 lg:py-12"
          id="main-content"
        >
          <Outlet />
        </main>
      </div>
    </div>
  )
}
