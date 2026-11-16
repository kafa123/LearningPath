import { postForm } from '@/shared/api/http'

export interface LearningContentImportResult {
  added: number
  updated: number
  domainCount: number
  moduleCount: number
}

export interface ChallengePackageImportResult {
  slug: string
  title: string
  domainSlug: string
  challengeCount: number
  replaced: boolean
}

export function importLearningContent(
  file: File,
  options: { signal?: AbortSignal } = {},
): Promise<LearningContentImportResult> {
  const form = new FormData()
  form.append('file', file)

  return postForm<LearningContentImportResult>(
    '/api/authoring/learning-content',
    form,
    options,
  )
}

export function importChallengePackage(
  file: File,
  options: { replace?: boolean; signal?: AbortSignal } = {},
): Promise<ChallengePackageImportResult> {
  const form = new FormData()
  form.append('file', file)

  const query = options.replace ? '?replace=true' : ''

  return postForm<ChallengePackageImportResult>(
    `/api/authoring/challenge-packages${query}`,
    form,
    { signal: options.signal },
  )
}
