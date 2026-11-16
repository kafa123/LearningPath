import { describe, expect, it, vi } from 'vitest'
import { ApiError, getJson, postForm, postJson } from '@/shared/api/http'

describe('getJson', () => {
  it('parses a successful JSON response', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => ({ value: 42 }),
      } as Response),
    )

    await expect(getJson<{ value: number }>('/api/test')).resolves.toEqual({ value: 42 })
  })

  it('turns a non-2xx response into an ApiError with the status code', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 503,
        json: async () => ({}),
      } as Response),
    )

    await expect(getJson('/api/test')).rejects.toEqual(
      expect.objectContaining({
        name: 'ApiError',
        status: 503,
      }),
    )
  })

  it('turns a network failure into an ApiError', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await expect(getJson('/api/test')).rejects.toBeInstanceOf(ApiError)
  })

  it('turns invalid JSON into an ApiError', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => {
          throw new SyntaxError('Unexpected token')
        },
      } as unknown as Response),
    )

    await expect(getJson('/api/test')).rejects.toBeInstanceOf(ApiError)
  })
})

describe('postJson', () => {
  it('sends a JSON body and parses the response', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 202,
      json: async () => ({ id: 'submission-1' }),
    } as Response)
    vi.stubGlobal('fetch', fetchMock)

    await expect(
      postJson<{ id: string }>('/api/test', { value: 42 }),
    ).resolves.toEqual({ id: 'submission-1' })

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/test',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ value: 42 }),
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
      }),
    )
  })

  it('uses the problem detail as the error message', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 400,
        json: async () => ({
          title: 'Submission rejected.',
          detail: 'Source file paths must be relative.',
        }),
      } as Response),
    )

    await expect(postJson('/api/test', {})).rejects.toEqual(
      expect.objectContaining({
        name: 'ApiError',
        status: 400,
        message: 'Source file paths must be relative.',
      }),
    )
  })

  it('turns a network failure into an ApiError', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await expect(postJson('/api/test', {})).rejects.toBeInstanceOf(ApiError)
  })
})

describe('postForm', () => {
  it('posts a FormData body without setting a content type', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ slug: 'balanced-brackets' }),
    } as Response)
    vi.stubGlobal('fetch', fetchMock)

    const form = new FormData()
    form.append(
      'file',
      new File(['{}'], 'learning.json', { type: 'application/json' }),
    )

    await expect(postForm('/api/authoring/learning-content', form)).resolves.toEqual({
      slug: 'balanced-brackets',
    })

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/authoring/learning-content',
      expect.objectContaining({
        method: 'POST',
        body: form,
        headers: { Accept: 'application/json' },
      }),
    )
  })

  it('uses the problem detail as the error message', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 409,
        json: async () => ({
          title: 'Challenge package conflict.',
          detail: 'A package already exists.',
        }),
      } as Response),
    )

    await expect(postForm('/api/test', new FormData())).rejects.toEqual(
      expect.objectContaining({
        name: 'ApiError',
        status: 409,
        message: 'A package already exists.',
      }),
    )
  })

  it('turns a network failure into an ApiError', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await expect(postForm('/api/test', new FormData())).rejects.toBeInstanceOf(ApiError)
  })
})
