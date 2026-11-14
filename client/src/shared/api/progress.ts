import { getJson, postJson } from '@/shared/api/http'
import type { ChallengeDetail } from '@/shared/api/challenges'

export type CompetencyState =
  | 'NotStarted'
  | 'Learning'
  | 'Practicing'
  | 'AssessmentRequired'
  | 'Mastered'

export interface CompetencyEvidence {
  challengeSlug: string
  challengeTitle: string
  purpose: string
  outcome: string
  score: number
  passedTests: number
  totalTests: number
  isExamination: boolean
  createdAtUtc: string
}

export interface CompetencyProgress {
  slug: string
  name: string
  description: string
  state: CompetencyState
  nextAction: string
  evidence: CompetencyEvidence[]
}

export interface ModuleProgress {
  slug: string
  title: string
  level: string
  sortOrder: number
  competencies: CompetencyProgress[]
}

export interface DomainProgress {
  slug: string
  name: string
  description: string
  sortOrder: number
  modules: ModuleProgress[]
}

export interface ProgressSummary {
  total: number
  notStarted: number
  learning: number
  practicing: number
  assessmentRequired: number
  mastered: number
}

export interface ProgressOverview {
  summary: ProgressSummary
  domains: DomainProgress[]
}

export interface AssessmentHistoryItem {
  submissionId: string
  challengeSlug: string
  challengeTitle: string
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
  isExamination: boolean
  examinationId?: string | null
  examinationVariant?: string | null
  createdAtUtc: string
}

export interface ExaminationSessionSummary {
  id: string
  challengeSlug: string
  challengeTitle: string
  variantId: string
  variantLabel: string
  timeLimitSeconds?: number | null
  status: string
  isActive: boolean
  remainingSeconds?: number | null
  startedAtUtc: string
  expiresAtUtc?: string | null
  completedAtUtc?: string | null
  attemptCount: number
}

export interface ExaminationSessionDetail {
  id: string
  challengeSlug: string
  challengeTitle: string
  variantId: string
  variantLabel: string
  timeLimitSeconds?: number | null
  status: string
  isActive: boolean
  remainingSeconds?: number | null
  startedAtUtc: string
  expiresAtUtc?: string | null
  completedAtUtc?: string | null
  challenge: ChallengeDetail
}

export function getProgress(signal?: AbortSignal): Promise<ProgressOverview> {
  return getJson<ProgressOverview>('/api/progress', { signal })
}

export function getAssessments(
  limit = 20,
  signal?: AbortSignal,
): Promise<AssessmentHistoryItem[]> {
  return getJson<AssessmentHistoryItem[]>(`/api/assessments?limit=${limit}`, { signal })
}

export function getExaminations(
  limit = 20,
  signal?: AbortSignal,
): Promise<ExaminationSessionSummary[]> {
  return getJson<ExaminationSessionSummary[]>(`/api/examinations?limit=${limit}`, {
    signal,
  })
}

export function getExamination(
  id: string,
  signal?: AbortSignal,
): Promise<ExaminationSessionDetail> {
  return getJson<ExaminationSessionDetail>(
    `/api/examinations/${encodeURIComponent(id)}`,
    { signal },
  )
}

export function createExamination(
  challengeSlug: string,
  signal?: AbortSignal,
): Promise<ExaminationSessionDetail> {
  return postJson<ExaminationSessionDetail>(
    '/api/examinations',
    { challengeSlug },
    { signal },
  )
}

export function finishExamination(
  id: string,
  signal?: AbortSignal,
): Promise<ExaminationSessionDetail> {
  return postJson<ExaminationSessionDetail>(
    `/api/examinations/${encodeURIComponent(id)}/finish`,
    {},
    { signal },
  )
}
