import { getJson } from '@/shared/api/http'

export interface RuntimeLanguage {
  id: string
  name: string
  image: string
}

export interface RuntimeLimits {
  timeLimitSeconds: number
  memoryLimitMb: number
  cpuLimit: number
  pidsLimit: number
  maxOutputBytes: number
  maxConcurrentExecutions: number
  maxQueuedExecutions: number
}

export interface ExecutionRuntimeInfo {
  executionEnabled: boolean
  languages: RuntimeLanguage[]
  limits: RuntimeLimits
}

export function getRuntime(signal?: AbortSignal): Promise<ExecutionRuntimeInfo> {
  return getJson<ExecutionRuntimeInfo>('/api/runtime', { signal })
}
