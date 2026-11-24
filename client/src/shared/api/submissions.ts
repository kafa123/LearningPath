import { ApiError, getJson, postJson } from '@/shared/api/http'

export type SubmissionPurpose = 'Run' | 'Submit'

export interface SubmissionFileInput {
  path: string
  content: string
}

export interface CreateSubmissionRequest {
  challengeSlug: string
  languageId: string
  files: SubmissionFileInput[]
  purpose: SubmissionPurpose
  examinationId?: string
}

export interface SubmissionCreated {
  id: string
  status: string
  createdAtUtc: string
}

export interface SubmissionDetail {
  id: string
  challengeSlug: string
  languageId: string
  status: string
  isTerminal: boolean
  createdAtUtc: string
  startedAtUtc?: string | null
  completedAtUtc?: string | null
  durationMs?: number | null
  exitCode?: number | null
  standardOutput: string
  standardError: string
  failureMessage?: string | null
  outputTruncated: boolean
  purpose: string
}

export interface AssessmentTestResult {
  name: string
  description?: string | null
  status: string
}

export interface AssessmentResult {
  submissionId: string
  challengeSlug: string
  languageId: string
  purpose: string
  outcome: string
  isAccepted: boolean
  score: number
  passedTests: number
  totalTests: number
  publicPassed: number
  publicTotal: number
  hiddenPassed: number
  hiddenTotal: number
  publicTests: AssessmentTestResult[]
  createdAtUtc: string
}

export interface LatestSubmission {
  submission: SubmissionDetail
  files: SubmissionFileInput[]
}

export function createSubmission(
  request: CreateSubmissionRequest,
  signal?: AbortSignal,
): Promise<SubmissionCreated> {
  return postJson<SubmissionCreated>('/api/submissions', request, { signal })
}

export function getSubmission(
  id: string,
  signal?: AbortSignal,
): Promise<SubmissionDetail> {
  return getJson<SubmissionDetail>(`/api/submissions/${encodeURIComponent(id)}`, {
    signal,
  })
}

export function getAssessmentResult(
  id: string,
  signal?: AbortSignal,
): Promise<AssessmentResult> {
  return getJson<AssessmentResult>(
    `/api/submissions/${encodeURIComponent(id)}/results`,
    { signal },
  )
}

export async function getLatestSubmission(
  challengeSlug: string,
  languageId: string,
  signal?: AbortSignal,
): Promise<LatestSubmission | null> {
  const query = new URLSearchParams({ challengeSlug, languageId })

  try {
    return await getJson<LatestSubmission>(`/api/submissions/latest?${query}`, {
      signal,
    })
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null
    }

    throw error
  }
}
