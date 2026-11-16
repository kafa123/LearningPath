import { Link, NavLink, Outlet } from 'react-router'
import { navigationItems } from '@/app/navigation'
import { NavGlyph } from '@/shared/components/NavGlyph'
import { ThemeToggle } from '@/shared/theme/ThemeToggle'

export function AppShell() {
  return (
    <div className="min-h-screen text-slate-100">
      <a
        className="sr-only z-50 rounded-md bg-emerald-400 px-4 py-2 font-medium text-[#04121f] focus:not-sr-only focus:fixed focus:top-4 focus:left-4"
        href="#main-content"
      >
        Skip to content
      </a>

      <div className="mx-auto flex min-h-screen w-full max-w-[1500px] flex-col lg:flex-row">
        <aside className="border-b border-white/[0.08] px-4 py-4 lg:sticky lg:top-0 lg:h-screen lg:w-60 lg:shrink-0 lg:border-r lg:border-b-0 lg:px-5 lg:py-6">
          <div className="flex items-center justify-between gap-4">
            <Link className="flex items-center gap-2.5" to="/">
              <span className="grid size-8 place-items-center rounded-md border border-white/[0.12] font-mono text-[11px] font-semibold text-slate-300">
                PS
              </span>
              <span className="text-sm font-semibold tracking-tight text-slate-100">PSEL</span>
            </Link>
            <ThemeToggle />
          </div>

          <nav
            aria-label="Main navigation"
            className="mt-6 flex gap-1 overflow-x-auto pb-1 lg:flex-col lg:gap-0.5 lg:overflow-visible lg:pb-0"
          >
            {navigationItems.map((item) => (
              <NavLink
                className={({ isActive }) =>
                  [
                    'flex shrink-0 items-center gap-2.5 rounded-md px-2.5 py-2 text-sm transition-colors',
                    isActive
                      ? 'bg-white/[0.06] text-slate-100'
                      : 'text-slate-400 hover:bg-white/[0.03] hover:text-slate-200',
                  ].join(' ')
                }
                end={item.end}
                key={item.to}
                to={item.to}
              >
                <NavGlyph name={item.glyph} />
                <span className="whitespace-nowrap">{item.label}</span>
                {item.badge ? (
                  <span className="ml-auto hidden text-[10px] text-slate-600 sm:inline">
                    {item.badge}
                  </span>
                ) : null}
              </NavLink>
            ))}
          </nav>

          <div className="mt-auto hidden pt-8 lg:block">
            <p className="text-[11px] leading-5 text-slate-600">
              Local-first software engineering lab.
            </p>
          </div>
        </aside>

        <main
          className="min-w-0 flex-1 px-4 py-8 sm:px-6 lg:px-10 lg:py-10"
          id="main-content"
        >
          <Outlet />
        </main>
      </div>
    </div>
  )
}
