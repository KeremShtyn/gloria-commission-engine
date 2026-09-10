import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { PeriodPicker } from '../components/PeriodPicker'
import { useSession } from '../context/SessionContext'
import type { PeriodSummaryResponse } from '../types'
import { amountClass, formatMoney } from '../utils/formatters'

export function PeriodSummaryPage() {
  const { session } = useSession()
  const navigate = useNavigate()

  const [year, setYear] = useState(2026)
  const [month, setMonth] = useState(8)
  const [summary, setSummary] = useState<PeriodSummaryResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setBusy(true)
    setError(null)
    try {
      setSummary(await api.periodSummary(session, year, month))
    } catch (e) {
      setSummary(null)
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }, [session, year, month])

  useEffect(() => {
    void load()
  }, [load])

  const rows = [...(summary?.employees ?? [])].sort((a, b) => b.totalCommission - a.totalCommission)

  return (
    <>
      <div className="page-head">
        <h1>Dönem özeti</h1>
        <p>Tüm personelin dönem primi. Satıra tıklayarak hesaplama detayına gidebilirsiniz.</p>
      </div>

      {error && <div className="alert error">{error}</div>}

      <div className="card">
        <PeriodPicker
          year={year}
          month={month}
          onYearChange={setYear}
          onMonthChange={setMonth}
          onRefresh={load}
          busy={busy}
        />
      </div>

      {summary && (
        <>
          <div className="stat-grid">
            <div className="stat-card">
              <span>Dönem</span>
              <strong style={{ fontSize: 18 }}>{summary.period}</strong>
              <small>
                <span className={summary.closed ? 'badge warn' : 'badge'}>
                  {summary.closed ? 'Kapalı' : 'Açık'}
                </span>
              </small>
            </div>
            <div className="stat-card">
              <span>Prim alan personel</span>
              <strong>{summary.employeeCount}</strong>
              <small>toplam {rows.length} personel</small>
            </div>
            <div className="stat-card">
              <span>Prime esas net ciro</span>
              <strong>{formatMoney(summary.totalSalesBase)}</strong>
            </div>
            <div className="stat-card">
              <span>Toplam prim</span>
              <strong className="accent">{formatMoney(summary.totalCommission)}</strong>
            </div>
          </div>

          <div className="card">
            <h2>Personel bazında</h2>

            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Personel</th>
                    <th>Departman</th>
                    <th>Otel</th>
                    <th className="num">Net ciro</th>
                    <th className="num">Prim</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {rows.map((employee) => (
                    <tr key={employee.employeeNo}>
                      <td>
                        <div className="cell-title">{employee.fullName}</div>
                        <div className="cell-sub">{employee.employeeNo}</div>
                      </td>
                      <td>
                        <span className="badge muted">{employee.department}</span>
                      </td>
                      <td>{employee.hotel}</td>
                      <td className={amountClass(employee.totalSalesBase)}>
                        {formatMoney(employee.totalSalesBase)}
                      </td>
                      <td className={`${amountClass(employee.totalCommission)} strong`}>
                        {formatMoney(employee.totalCommission)}
                      </td>
                      <td className="num">
                        <button
                          onClick={() =>
                            navigate(
                              `/primim?employeeNo=${employee.employeeNo}&year=${year}&month=${month}`,
                            )
                          }
                        >
                          Detay
                        </button>
                      </td>
                    </tr>
                  ))}
                  {rows.length === 0 && (
                    <tr>
                      <td colSpan={6} className="empty">
                        Bu dönemde hesaplanmış prim yok.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </>
  )
}
