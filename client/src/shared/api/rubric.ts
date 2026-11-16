import { getJson, putJson } from '@/shared/api/http'

export interface RubricCriterionInput {
  id: string
  points: number
}

export interface SaveRubricReviewRequest {
  criteria: RubricCriterionInput[]
  note?: string | null
}

export interface RubricCriterionReview {
  id: string
  title: string
  description?: string | null
  maxPoints: number
  weight: number
  points: number
}

export interface RubricReview {
  submissionId: string
  challengeSlug: string
  score: number
  band: string
  note?: string | null
  criteria: RubricCriterionReview[]
  createdAtUtc: string
  updatedAtUtc: string
}

export function getRubricReview(
  submissionId: string,
  signal?: AbortSignal,
): Promise<RubricReview> {
  return getJson<RubricReview>(
    `/api/submissions/${encodeURIComponent(submissionId)}/rubric`,
    { signal },
  )
}

export function saveRubricReview(
  submissionId: string,
  request: SaveRubricReviewRequest,
  signal?: AbortSignal,
): Promise<RubricReview> {
  return putJson<RubricReview>(
    `/api/submissions/${encodeURIComponent(submissionId)}/rubric`,
    request,
    { signal },
  )
}
