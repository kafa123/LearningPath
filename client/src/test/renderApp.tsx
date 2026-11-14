import { render } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { appRoutes } from '@/app/routes'
import { ThemeProvider } from '@/shared/theme/ThemeProvider'

export function renderApp(initialEntries: string[] = ['/']) {
  const router = createMemoryRouter(appRoutes, { initialEntries })

  return {
    router,
    ...render(
      <ThemeProvider>
        <RouterProvider router={router} />
      </ThemeProvider>,
    ),
  }
}
