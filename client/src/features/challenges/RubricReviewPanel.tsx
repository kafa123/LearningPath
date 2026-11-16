import { useEffect, useState } from 'react'
import type { ChallengeRubric } from '@/shared/api/challenges'
import { ApiError } from '@/shared/api/http'
import {
  getRubricReview,
  saveRubricReview,
  type RubricReview,
} from '@/shared/api/rubric'

function bandDot(band: string) {
  if (band === 'Exemplary') {
    return 'bg-emerald-400'
  }

  if (band === 'Proficient') {
    return 'bg-sky-400'
  }

  if (band === 'Developing') {
    return 'bg-amber-400'
  }

  return 'bg-rose-400'
}

export function RubricReviewPanel({
  rubric,
  submissionId,
}: {
  rubric: ChallengeRubric
  submissionId: string
}) {
  const [points, setPoints] = useState<Record<string, number>>({})
  const [note, setNote] = useState('')
  const [review, setReview] = useState<RubricReview | null>(null)
  const [status, setStatus] = useState<'idle' | 'loading' | 'saving'>('loading')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    let active = true

    async function load() {
      try {
        const existing = await getRubricReview(submissionId, controller.signal)

        if (!active) {
          return
        }

        setReview(existing)
        setNote(existing.note ?? '')
        setPoints(
          Object.fromEntries(
            existing.criteria.map((criterion) => [criterion.id, criterion.points]),
          ),
        )
      } catch (loadError) {
        if (!active || controller.signal.aborted) {
          return
        }

        if (loadError instanceof ApiError && loadError.status === 404) {
          setReview(null)
        } else {
          setError(
            loadError instanceof Error
              ? loadError.message
              : 'The rubric review could not be retrieved.',
          )
        }
      } finally {
        if (active) {
          setStatus('idle')
        }
      }
    }

    void load()

    return () => {
      active = false
      controller.abort()
    }
  }, [submissionId])

  function updatePoints(criterionId: string, value: number, maxPoints: number) {
    const clamped = Number.isFinite(value)
      ? Math.min(Math.max(Math.trunc(value), 0), maxPoints)
      : 0

    setPoints((current) => ({ ...current, [criterionId]: clamped }))
  }

  async function save() {
    if (status === 'saving') {
      return
    }

    setStatus('saving')
    setError(null)

    try {
      const saved = await saveRubricReview(submissionId, {
        criteria: rubric.criteria.map((criterion) => ({
          id: criterion.id,
          points: points[criterion.id] ?? 0,
        })),
        note: note.trim().length > 0 ? note.trim() : null,
      })

      setReview(saved)
      setPoints(
        Object.fromEntries(
          saved.criteria.map((criterion) => [criterion.id, criterion.points]),
        ),
      )
    } catch (saveError) {
      setError(
        saveError instanceof Error
          ? saveError.message
          : 'The rubric review could not be saved.',
      )
    } finally {
      setStatus('idle')
    }
  }

  return (
    <section
      aria-label="Rubric self-review"
      className="rounded-md border border-white/[0.07] bg-white/[0.02] p-4"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-white">Rubric self-review</h2>
          <p className="mt-1 max-w-xl text-[11px] leading-5 text-slate-500">
            Score each criterion honestly. The server computes the weighted score and band from
            the challenge rubric. This review is advisory and never changes your assessment result.
          </p>
        </div>

        {review ? (
          <div className="flex items-center gap-2">
            <span className="inline-flex items-center gap-1.5 text-xs font-medium text-slate-300">
              <span
                aria-hidden="true"
                className={['size-1.5 rounded-full', bandDot(review.band)].join(' ')}
              />
              {review.band}
            </span>
            <span className="text-sm font-semibold text-white">{review.score}/100</span>
          </div>
        ) : null}
      </div>

      {status === 'loading' ? (
        <p className="mt-3 text-xs text-slate-500">Loading your review...</p>
      ) : (
        <>
          <div className="mt-4 space-y-2">
            {rubric.criteria.map((criterion) => {
              const value = points[criterion.id] ?? 0
              const inputId = `rubric-${criterion.id}`

              return (
                <div
                  className="rounded-sm border border-white/[0.07] bg-white/[0.02] px-3 py-2.5"
                  key={criterion.id}
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <label className="text-xs text-slate-200" htmlFor={inputId}>
                      {criterion.title}
                    </label>
                    <div className="flex items-center gap-1.5 text-[11px] text-slate-500">
                      <input
                        aria-describedby={criterion.description ? `${inputId}-hint` : undefined}
                        className="w-16 rounded-sm border border-white/10 bg-slate-950/60 px-2 py-1 text-right font-mono text-xs text-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
                        id={inputId}
                        max={criterion.maxPoints}
                        min={0}
                        onChange={(event) => {
                          updatePoints(criterion.id, event.target.valueAsNumber, criterion.maxPoints)
                        }}
                        type="number"
                        value={value}
                      />
                      <span>/ {criterion.maxPoints}</span>
                      <span className="text-slate-600">x{criterion.weight}</span>
                    </div>
                  </div>
                  {criterion.description ? (
                    <p
                      className="mt-1 text-[11px] leading-5 text-slate-500"
                      id={`${inputId}-hint`}
                    >
                      {criterion.description}
                    </p>
                  ) : null}
                </div>
              )
            })}
          </div>

          <div className="mt-4">
            <label className="text-xs text-slate-500" htmlFor="rubric-note">
              Review note
            </label>
            <textarea
              className="mt-1 w-full rounded-sm border border-white/10 bg-slate-950/60 px-3 py-2 text-xs text-slate-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300"
              id="rubric-note"
              maxLength={2000}
              onChange={(event) => setNote(event.target.value)}
              placeholder="What did you do well, and what would you change next time?"
              rows={3}
              value={note}
            />
          </div>

          {error ? (
            <p
              className="mt-3 rounded-sm border border-rose-400/20 bg-rose-400/[0.04] px-3 py-2 text-xs text-rose-200"
              role="alert"
            >
              {error}
            </p>
          ) : null}

          <div className="mt-3 flex flex-wrap items-center gap-3">
            <button
              className={[
                'rounded-md border px-3 py-1.5 text-xs font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-300',
                status === 'saving'
                  ? 'cursor-not-allowed border-white/10 bg-white/[0.03] text-slate-500'
                  : 'border-emerald-400/30 bg-emerald-400/10 text-emerald-100 hover:bg-emerald-400/15',
              ].join(' ')}
              disabled={status === 'saving'}
              onClick={() => {
                void save()
              }}
              type="button"
            >
              {status === 'saving'
                ? 'Saving...'
                : review
                  ? 'Update self-review'
                  : 'Save self-review'}
            </button>
            <span className="text-[11px] text-slate-500">
              Weighted score appears after you save.
            </span>
          </div>
        </>
      )}
    </section>
  )
}
