export class ApiError extends Error {
  readonly status?: number

  constructor(message: string, status?: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export async function getJson<T>(
  path: string,
  options: { signal?: AbortSignal } = {},
): Promise<T> {
  let response: Response

  try {
    response = await fetch(path, {
      headers: { Accept: 'application/json' },
      signal: options.signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error
    }

    throw new ApiError('Unable to reach the PSEL API.')
  }

  if (!response.ok) {
    throw new ApiError(`The PSEL API returned HTTP ${response.status}.`, response.status)
  }

  try {
    return (await response.json()) as T
  } catch {
    throw new ApiError('The PSEL API returned an invalid response.', response.status)
  }
}

export async function postJson<T>(
  path: string,
  body: unknown,
  options: { signal?: AbortSignal } = {},
): Promise<T> {
  let response: Response

  try {
    response = await fetch(path, {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
      signal: options.signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error
    }

    throw new ApiError('Unable to reach the PSEL API.')
  }

  if (!response.ok) {
    throw new ApiError(
      await readProblemMessage(response) ?? `The PSEL API returned HTTP ${response.status}.`,
      response.status,
    )
  }

  try {
    return (await response.json()) as T
  } catch {
    throw new ApiError('The PSEL API returned an invalid response.', response.status)
  }
}

async function readProblemMessage(response: Response): Promise<string | null> {
  try {
    const problem = (await response.json()) as { detail?: unknown; title?: unknown }

    if (typeof problem.detail === 'string' && problem.detail.trim().length > 0) {
      return problem.detail
    }

    if (typeof problem.title === 'string' && problem.title.trim().length > 0) {
      return problem.title
    }
  } catch {
    // The response was not a JSON problem document.
  }

  return null
}
