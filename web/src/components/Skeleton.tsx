/**
 * Yukleniyor durumu. Bos ekran gosterip icerigi aniden yerlestirmek yerine
 * beklenen duzenin iskeleti cizilir; sayfa zipladigi icin okuma kesilmez.
 */
export function TableSkeleton({ rows = 6, columns = 5 }: { rows?: number; columns?: number }) {
  return (
    <div className="skeleton-table" aria-busy="true" aria-label="Yükleniyor">
      <div className="skeleton-row head">
        {Array.from({ length: columns }, (_, i) => (
          <div key={i} className="skeleton skeleton-cell" style={{ maxWidth: i === 0 ? 140 : undefined }} />
        ))}
      </div>

      {Array.from({ length: rows }, (_, row) => (
        <div key={row} className="skeleton-row">
          {Array.from({ length: columns }, (_, col) => (
            <div
              key={col}
              className="skeleton skeleton-cell"
              style={{ maxWidth: col === 0 ? 140 : undefined, opacity: 1 - row * 0.08 }}
            />
          ))}
        </div>
      ))}
    </div>
  )
}

export function StatSkeleton({ count = 4 }: { count?: number }) {
  return (
    <div className="stat-grid">
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className="stat-card">
          <div className="skeleton" style={{ height: 10, width: '55%', marginBottom: 10 }} />
          <div className="skeleton" style={{ height: 24, width: '75%' }} />
        </div>
      ))}
    </div>
  )
}
