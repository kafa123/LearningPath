import { useCallback, useEffect, useState } from 'react'
import { getHealth, type HealthResponse } from '@/shared/api/health'

export type HealthState =
  | { status: 'loading' }
  | { status: 'success'; data: HealthResponse }
  | { status: 'error'; message: string }

export function useHealth() {
  const [state, setState] = useState<HealthState>({ status: 'loading' })
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    getHealth(controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) {
          setState({ status: 'success', data })
        }
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) {
          return
        }

        setState({
          status: 'error',
          message:
            error instanceof Error
              ? error.message
              : 'The health check failed for an unknown reason.',
        })
      })

    return () => controller.abort()
  }, [refreshToken])

  const refresh = useCallback(() => {
    setState({ status: 'loading' })
    setRefreshToken((current) => current + 1)
  }, [])

  return { state, refresh }
}
