/**
 * Lessons that ship with generated artwork in `public/illustrations/lessons`.
 * The cover is rendered above the lesson body; the matching diagram is
 * referenced from the lesson Markdown. Keeping the list explicit means a new
 * lesson without artwork simply renders without a cover instead of a broken
 * image.
 */
const illustratedLessons = new Set([
  'algorithm-foundations-lesson',
  'complexity-analysis-lesson',
  'data-structures-lesson',
  'relational-modeling-lesson',
  'sql-querying-lesson',
  'indexes-and-query-plans-lesson',
  'http-api-design-lesson',
  'validation-and-error-handling-lesson',
  'separation-of-concerns-lesson',
  'modular-monoliths-lesson',
  'testing-foundations-lesson',
  'debugging-and-root-cause-lesson',
  'go-foundations-lesson',
  'go-errors-and-interfaces-lesson',
  'go-concurrency-lesson',
  'go-http-services-lesson',
  'go-testing-and-tooling-lesson',
  'go-generics-lesson',
  'go-performance-lesson',
])

export function lessonCoverUrl(slug: string): string | null {
  return illustratedLessons.has(slug)
    ? `/illustrations/lessons/${slug}-cover.svg`
    : null
}
