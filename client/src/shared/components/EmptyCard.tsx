interface EmptyCardProps {
  title: string
  description: string
  phase: string
}

export function EmptyCard({ title, description, phase }: EmptyCardProps) {
  return (
    <article className="flex min-h-40 flex-col rounded-2xl border border-dashed border-white/10 bg-white/[0.018] p-5">
      <span className="w-fit rounded-md border border-white/[0.07] bg-white/[0.035] px-2 py-1 text-[9px] font-semibold tracking-[0.16em] text-slate-500 uppercase">
        {phase}
      </span>
      <h2 className="mt-4 text-sm font-semibold text-slate-200">{title}</h2>
      <p className="mt-2 text-xs leading-5 text-slate-500">{description}</p>
    </article>
  )
}
