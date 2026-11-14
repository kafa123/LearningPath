import { render } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { appRoutes } from '@/app/routes'

export function renderApp(initialEntries: string[] = ['/']) {
  const router = createMemoryRouter(appRoutes, { initialEntries })

  return {
    router,
    ...render(<RouterProvider router={router} />),
  }
}
