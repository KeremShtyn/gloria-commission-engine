/**
 * Basit grafikler. Kutuphane eklenmedi: buradaki uc bicimin hepsi
 * oranli genislikten ibaret, CSS ile ciziliyor. Cizgi grafik gerekseydi
 * kutuphane sart olurdu; bar icin 100 KB bagimlilik tasimaya degmez.
 *
 * Erisilebilirlik: her satirin sayisal degeri metin olarak da yaziliyor,
 * grafigi okuyamayan da bilgiyi kaybetmiyor.
 */

interface BarItem {
  label: string
  value: number
  sublabel?: string
}

interface BarChartProps {
  items: BarItem[]
  format: (value: number) => string
  emptyText?: string
}

/** Yatay bar: kategoriler arasi buyukluk karsilastirmasi. */
export function BarChart({ items, format, emptyText = 'Veri yok.' }: BarChartProps) {
  if (items.length === 0) return <p className="hint">{emptyText}</p>

  const max = Math.max(...items.map((i) => Math.abs(i.value)), 1)

  return (
    <ul className="bar-chart">
      {items.map((item) => (
        <li key={item.label}>
          <div className="bar-label">
            <span className="bar-name">{item.label}</span>
            {item.sublabel && <span className="bar-sub">{item.sublabel}</span>}
          </div>

          <div className="bar-track">
            <div
              className="bar-fill"
              style={{ width: `${(Math.abs(item.value) / max) * 100}%` }}
            />
          </div>

          <span className="bar-value">{format(item.value)}</span>
        </li>
      ))}
    </ul>
  )
}

/**
 * Sifir cizgisinin iki yanina uzanan bar: isaretin kendisi bilgi tasiyor.
 * ERP mutabakatinda "hangi grupta ERP fazla, hangisinde eksik" sorusunun cevabi.
 */
export function DivergingBarChart({ items, format, emptyText = 'Veri yok.' }: BarChartProps) {
  if (items.length === 0) return <p className="hint">{emptyText}</p>

  const max = Math.max(...items.map((i) => Math.abs(i.value)), 1)

  return (
    <ul className="bar-chart diverging">
      {items.map((item) => {
        const ratio = (Math.abs(item.value) / max) * 50
        const negative = item.value < 0

        return (
          <li key={item.label}>
            <div className="bar-label">
              <span className="bar-name">{item.label}</span>
            </div>

            <div className="bar-track diverging-track">
              <span className="zero-line" />
              <div
                className={negative ? 'bar-fill negative' : 'bar-fill positive'}
                style={{
                  width: `${ratio}%`,
                  left: negative ? `${50 - ratio}%` : '50%',
                }}
              />
            </div>

            <span className={negative ? 'bar-value negative' : 'bar-value'}>
              {format(item.value)}
            </span>
          </li>
        )
      })}
    </ul>
  )
}

export interface StackedSegment {
  key: string
  label: string
  value: number
}

interface StackedBarChartProps {
  rows: { label: string; sublabel?: string; segments: StackedSegment[] }[]
  emptyText?: string
}

/** Yigilmis bar: bir butunun parcalari (aktarilan / mukerrer / hatali). */
export function StackedBarChart({ rows, emptyText = 'Veri yok.' }: StackedBarChartProps) {
  if (rows.length === 0) return <p className="hint">{emptyText}</p>

  const legend = rows[0]?.segments ?? []

  return (
    <>
      <ul className="bar-chart stacked">
        {rows.map((row) => {
          const total = row.segments.reduce((sum, s) => sum + s.value, 0) || 1

          return (
            <li key={row.label}>
              <div className="bar-label">
                <span className="bar-name">{row.label}</span>
                {row.sublabel && <span className="bar-sub">{row.sublabel}</span>}
              </div>

              <div className="bar-track stacked-track">
                {row.segments.map((segment) => (
                  <div
                    key={segment.key}
                    className={`bar-segment ${segment.key}`}
                    style={{ width: `${(segment.value / total) * 100}%` }}
                    title={`${segment.label}: ${segment.value}`}
                  />
                ))}
              </div>

              <span className="bar-value">{total}</span>
            </li>
          )
        })}
      </ul>

      <div className="chart-legend">
        {legend.map((segment) => (
          <span key={segment.key}>
            <i className={`swatch ${segment.key}`} />
            {segment.label}
          </span>
        ))}
      </div>
    </>
  )
}
