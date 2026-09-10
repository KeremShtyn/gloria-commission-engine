import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import { useSession } from '../context/SessionContext'
import type { PeriodResponse } from '../types'
import { formatDateTime } from '../utils/formatters'

export function PeriodsPage() {
  const { session, isAdmin } = useSession()

  const [periods, setPeriods] = useState<PeriodResponse[]>([])
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      setPeriods(await api.periods(session))
      setError(null)
    } catch (e) {
      setError((e as Error).message)
    }
  }, [session])

  useEffect(() => {
    void load()
  }, [load])

  const toggle = async (period: PeriodResponse) => {
    setError(null)
    setNotice(null)
    try {
      if (period.status === 'Closed') {
        await api.reopenPeriod(session, period.year, period.month)
        setNotice(`${period.key} yeniden açıldı. Bu işlem denetim kaydına düştü.`)
      } else {
        await api.closePeriod(session, period.year, period.month)
        setNotice(`${period.key} kapatıldı. Bu döneme ait kayıtlar artık değiştirilemez.`)
      }
      await load()
    } catch (e) {
      setError((e as Error).message)
    }
  }

  return (
    <>
      <div className="page-head">
        <h1>Dönemler</h1>
        <p>
          Hesabı biten dönem kapatılır; kapalı dönemin satış ve prim kayıtları değiştirilemez.
          Kontrol veri erişim katmanında, hangi servisten gelinirse gelinsin geçerli.
        </p>
      </div>

      {error && <div className="alert error">{error}</div>}
      {notice && <div className="alert info">{notice}</div>}
      {!isAdmin && (
        <div className="alert info">Dönem kapatma ve açma Admin rolüne açıktır.</div>
      )}

      <div className="card">
        <h2>Dönem listesi ({periods.length})</h2>
        <p className="hint">Dönem kaydı ilk hesaplamada veya kapatma sırasında oluşur.</p>

        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Dönem</th>
                <th>Durum</th>
                <th>Kapatan</th>
                <th>Kapatılma zamanı</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {periods.map((period) => (
                <tr key={period.key}>
                  <td>
                    <strong>{period.key}</strong>
                  </td>
                  <td>
                    <span className={period.status === 'Closed' ? 'badge warn' : 'badge'}>
                      {period.status === 'Closed' ? 'Kapalı' : 'Açık'}
                    </span>
                  </td>
                  <td>{period.closedBy ?? '—'}</td>
                  <td>{formatDateTime(period.closedAtUtc)}</td>
                  <td className="num">
                    <button onClick={() => toggle(period)} disabled={!isAdmin}>
                      {period.status === 'Closed' ? 'Yeniden aç' : 'Dönemi kapat'}
                    </button>
                  </td>
                </tr>
              ))}
              {periods.length === 0 && (
                <tr>
                  <td colSpan={5} className="empty">
                    Henüz dönem oluşmadı.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </>
  )
}
