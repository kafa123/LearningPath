import { useCallback, useEffect, useState } from 'react'
import { getJson } from '@/shared/api/http'

export type ApiQueryState<T> =
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; message: string }

/**
 * Runs a GET request for a stable URL and exposes loading, success, and error
 * states. Requests are aborted when the URL changes or the component unmounts.
 */
export function useApiQuery<T>(url: string) {
  const [state, setState] = useState<ApiQueryState<T>>({ status: 'loading' })
  const [refreshToken, setRefreshToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    getJson<T>(url, { signal: controller.signal })
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
              : 'The request failed for an unknown reason.',
        })
      })

    return () => controller.abort()
  }, [url, refreshToken])

  const refresh = useCallback(() => {
    setState({ status: 'loading' })
    setRefreshToken((current) => current + 1)
  }, [])

  return { state, refresh }
}
