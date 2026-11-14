import { getJson } from '@/shared/api/http'

export interface ChallengeLanguageSummary {
  id: string
  name: string
}

export interface ChallengeSummary {
  slug: string
  title: string
  summary: string
  domainSlug: string
  topic: string
  difficulty: string
  type: string
  estimatedMinutes: number
  languages: ChallengeLanguageSummary[]
  prerequisiteSlugs: string[]
  competencySlugs: string[]
}

export interface ChallengeFile {
  path: string
  content: string
}

export interface ChallengeLanguage {
  id: string
  name: string
  editorLanguage: string
  starterFiles: ChallengeFile[]
  canExecute: boolean
}

export interface ChallengeExample {
  input: string
  output: string
  explanation?: string | null
}

export interface PublicTestSummary {
  name: string
  description?: string | null
}

export interface ChallengeEvaluationSummary {
  kind: string
  timeLimitSeconds?: number | null
  memoryLimitMb?: number | null
}

export interface ExaminationVariantSummary {
  id: string
  label: string
  timeLimitSeconds?: number | null
}

export interface ChallengeExaminationSummary {
  variants: ExaminationVariantSummary[]
}

export interface ChallengeDetail {
  slug: string
  title: string
  summary: string
  domainSlug: string
  topic: string
  difficulty: string
  type: string
  estimatedMinutes: number
  objectives: string[]
  prerequisiteSlugs: string[]
  competencySlugs: string[]
  constraints: string[]
  examples: ChallengeExample[]
  languages: ChallengeLanguage[]
  publicTests: PublicTestSummary[]
  evaluation: ChallengeEvaluationSummary
  examination: ChallengeExaminationSummary
  statementMarkdown: string
  hintsMarkdown?: string | null
}

export function getChallenges(signal?: AbortSignal): Promise<ChallengeSummary[]> {
  return getJson<ChallengeSummary[]>('/api/challenges', { signal })
}

export function getChallenge(slug: string, signal?: AbortSignal): Promise<ChallengeDetail> {
  return getJson<ChallengeDetail>(`/api/challenges/${encodeURIComponent(slug)}`, { signal })
}
