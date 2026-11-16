import { getJson } from '@/shared/api/http'

export interface ModuleSummary {
  slug: string
  title: string
  summary: string
  level: string
  estimatedMinutes: number
  prerequisiteSlugs: string[]
  competencySlugs: string[]
}

export interface LearningPathSummary {
  slug: string
  name: string
  description: string
  modules: ModuleSummary[]
}

export interface DomainReference {
  slug: string
  name: string
}

export interface ModuleReference {
  slug: string
  title: string
  domainSlug: string
}

export interface MaterialSummary {
  slug: string
  title: string
  kind: string
  estimatedMinutes: number
}

export interface CompetencySummary {
  slug: string
  name: string
  description: string
}

export interface ModuleDetail {
  slug: string
  title: string
  summary: string
  level: string
  estimatedMinutes: number
  domain: DomainReference
  objectives: string[]
  materials: MaterialSummary[]
  competencies: CompetencySummary[]
  prerequisites: ModuleReference[]
}

export interface MaterialDetail {
  slug: string
  title: string
  kind: string
  estimatedMinutes: number
  module: ModuleReference
  bodyMarkdown: string
}

export function getLearningPaths(signal?: AbortSignal): Promise<LearningPathSummary[]> {
  return getJson<LearningPathSummary[]>('/api/learning-paths', { signal })
}

export function getLearningPath(
  slug: string,
  signal?: AbortSignal,
): Promise<LearningPathSummary> {
  return getJson<LearningPathSummary>(
    `/api/learning-paths/${encodeURIComponent(slug)}`,
    { signal },
  )
}

export function getModule(slug: string, signal?: AbortSignal): Promise<ModuleDetail> {
  return getJson<ModuleDetail>(`/api/modules/${encodeURIComponent(slug)}`, { signal })
}

export function getMaterial(slug: string, signal?: AbortSignal): Promise<MaterialDetail> {
  return getJson<MaterialDetail>(`/api/materials/${encodeURIComponent(slug)}`, { signal })
}
