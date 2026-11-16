interface EmptyCardProps {
  title: string
  description: string
  phase: string
}

export function EmptyCard({ title, description, phase }: EmptyCardProps) {
  return (
    <article className="flex min-h-40 flex-col rounded-lg border border-dashed border-white/[0.1] bg-white/[0.01] p-5">
      <span className="w-fit text-[10px] text-slate-500">{phase}</span>
      <h2 className="mt-3 text-sm font-medium text-slate-200">{title}</h2>
      <p className="mt-1.5 text-xs leading-5 text-slate-500">{description}</p>
    </article>
  )
}
