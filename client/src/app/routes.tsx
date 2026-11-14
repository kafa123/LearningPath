import { Suspense } from 'react'
import type { RouteObject } from 'react-router'
import { AppShell } from './layout/AppShell'
import {
  ChallengeWorkspacePage,
  ExaminationWorkspacePage,
  MaterialPage,
} from './lazyPages'
import { ChallengesPage } from '@/features/challenges/ChallengesPage'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { ExaminationsPage } from '@/features/examinations/ExaminationsPage'
import { LearningPathsPage } from '@/features/learning/LearningPathsPage'
import { LearningPathPage } from '@/features/learning/LearningPathPage'
import { ModulePage } from '@/features/learning/ModulePage'
import { NotFoundPage } from '@/features/not-found/NotFoundPage'
import { PlaceholderPage } from '@/features/placeholders/PlaceholderPage'
import { ProgressPage } from '@/features/progress/ProgressPage'
import { PageLoading } from '@/shared/components/PageLoading'

export const appRoutes: RouteObject[] = [
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <DashboardPage /> },
      {
        path: 'learning',
        children: [
          { index: true, element: <LearningPathsPage /> },
          { path: ':domainSlug', element: <LearningPathPage /> },
          { path: ':domainSlug/:moduleSlug', element: <ModulePage /> },
          {
            path: ':domainSlug/:moduleSlug/materials/:materialSlug',
            element: (
              <Suspense fallback={<PageLoading label="Loading material..." />}>
                <MaterialPage />
              </Suspense>
            ),
          },
        ],
      },
      {
        path: 'challenges',
        children: [
          { index: true, element: <ChallengesPage /> },
          {
            path: ':slug',
            element: (
              <Suspense fallback={<PageLoading label="Loading workspace..." />}>
                <ChallengeWorkspacePage />
              </Suspense>
            ),
          },
        ],
      },
      {
        path: 'practice',
        element: (
          <PlaceholderPage
            description="Work through realistic tasks with limited hints and public test feedback."
            phase="Phase 3–5"
            planned="Practice mode depends on challenge packages, the execution pipeline, and the assessment engine."
            title="Practice"
          />
        ),
      },
      {
        path: 'examinations',
        children: [
          { index: true, element: <ExaminationsPage /> },
          {
            path: ':id',
            element: (
              <Suspense fallback={<PageLoading label="Loading examination..." />}>
                <ExaminationWorkspacePage />
              </Suspense>
            ),
          },
        ],
      },
      { path: 'progress', element: <ProgressPage /> },
      {
        path: 'settings',
        element: (
          <PlaceholderPage
            description="Configure local runtimes, execution limits, and application preferences."
            phase="Planned"
            planned="Settings will follow the language runtime and execution phases so that only supported, server-validated options are exposed."
            title="Settings"
          />
        ),
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
