import { describe, expect, it } from 'vitest'
import { lessonCoverUrl } from './lessonIllustrations'

describe('lessonCoverUrl', () => {
  it('returns the generated cover for an illustrated lesson', () => {
    expect(lessonCoverUrl('sql-querying-lesson')).toBe(
      '/illustrations/lessons/sql-querying-lesson-cover.svg',
    )
    expect(lessonCoverUrl('go-concurrency-lesson')).toBe(
      '/illustrations/lessons/go-concurrency-lesson-cover.svg',
    )
  })

  it('returns null for a lesson without artwork', () => {
    expect(lessonCoverUrl('a-lesson-without-art')).toBeNull()
  })
})
