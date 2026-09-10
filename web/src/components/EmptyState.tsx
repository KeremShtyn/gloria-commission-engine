/** Bos tablo icin aciklayici durum; "veri yok" tek basina neden oldugunu soylemiyor. */
export function EmptyState({ title, description }: { title: string; description?: string }) {
  return (
    <div className="empty-state">
      <strong>{title}</strong>
      {description && <p>{description}</p>}
    </div>
  )
}
