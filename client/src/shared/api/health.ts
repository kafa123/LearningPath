import { getJson } from '@/shared/api/http'

export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy'

export interface HealthCheck {
  name: string
  status: HealthStatus
  durationMs: number
}

export interface HealthResponse {
  status: HealthStatus
  timestampUtc: string
  version: string
  totalDurationMs: number
  checks: HealthCheck[]
}

export function getHealth(signal?: AbortSignal): Promise<HealthResponse> {
  return getJson<HealthResponse>('/api/health', { signal })
}
