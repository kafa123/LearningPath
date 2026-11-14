import { lazy } from 'react'

// The lesson renderer pulls in the Markdown pipeline. Loading it on demand
// keeps the initial catalog bundle small.
export const MaterialPage = lazy(async () => {
  const module = await import('@/features/learning/MaterialPage')
  return { default: module.MaterialPage }
})

// The workspace loads Monaco on demand. Monaco is bundled locally rather than
// fetched from a CDN so the lab remains available without internet access.
export const ChallengeWorkspacePage = lazy(async () => {
  const module = await import('@/features/challenges/ChallengeWorkspacePage')
  return { default: module.ChallengeWorkspacePage }
})

// Examination mode reuses the same editor and submission pipeline, so it is
// loaded on demand exactly like a practice workspace.
export const ExaminationWorkspacePage = lazy(async () => {
  const module = await import('@/features/examinations/ExaminationWorkspacePage')
  return { default: module.ExaminationWorkspacePage }
})
