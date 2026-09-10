import { MONTHS } from '../constants'

interface PeriodPickerProps {
  year: number
  month: number
  onYearChange: (year: number) => void
  onMonthChange: (month: number) => void
  onRefresh?: () => void
  busy?: boolean
  children?: React.ReactNode
}

/** Butun ekranlarda ayni donem secici; tekrarlanan form parcasi tek yerde. */
export function PeriodPicker({
  year,
  month,
  onYearChange,
  onMonthChange,
  onRefresh,
  busy,
  children,
}: PeriodPickerProps) {
  return (
    <div className="grid">
      <div>
        <label htmlFor="year">Yıl</label>
        <input id="year" type="number" value={year} onChange={(e) => onYearChange(Number(e.target.value))} />
      </div>

      <div>
        <label htmlFor="month">Ay</label>
        <select id="month" value={month} onChange={(e) => onMonthChange(Number(e.target.value))}>
          {MONTHS.map((m) => (
            <option key={m} value={m}>
              {String(m).padStart(2, '0')}
            </option>
          ))}
        </select>
      </div>

      {children}

      {onRefresh && (
        <div style={{ display: 'flex', alignItems: 'flex-end' }}>
          <button className="primary" onClick={onRefresh} disabled={busy}>
            {busy ? 'Yükleniyor…' : 'Getir'}
          </button>
        </div>
      )}
    </div>
  )
}
