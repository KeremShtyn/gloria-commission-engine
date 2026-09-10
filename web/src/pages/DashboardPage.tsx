import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { PeriodPicker } from '../components/PeriodPicker'
import { useSession } from '../context/SessionContext'
import type { ImportBatchResponse, PeriodSummaryResponse, ReconciliationResponse } from '../types'
import { formatDateTime, formatMoney } from '../utils/formatters'

export function DashboardPage() {
  const { session } = useSession()
  const [year, setYear] = useState(2026)
  const [month, setMonth] = useState(8)

  const [summary, setSummary] = useState<PeriodSummaryResponse | null>(null)
  const [reconciliation, setReconciliation] = useState<ReconciliationResponse | null>(null)
  const [batches, setBatches] = useState<ImportBatchResponse[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setBusy(true)
    setError(null)
    try {
      const [s, r, b] = await Promise.all([
        api.periodSummary(session, year, month),
        api.reconciliation(session, year, month),
        api.importBatches(session),
      ])
      setSummary(s)
      setReconciliation(r)
      setBatches(b)
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }, [session, year, month])

  useEffect(() => {
    void load()
  }, [load])

  const failedRows = batches.reduce((total, b) => total + b.failedRows, 0)
  const topEarners = [...(summary?.employees ?? [])]
    .sort((a, b) => b.totalCommission - a.totalCommission)
    .slice(0, 5)

  return (
    <>
      <div className="page-head">
        <h1>Genel bakış</h1>
        <p>Dönemin prim, mutabakat ve veri kalitesi özeti.</p>
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

      <div className="stat-grid">
        <div className="stat-card">
          <span>Hak edilen toplam prim</span>
          <strong className="accent">{formatMoney(summary?.totalCommission ?? 0)}</strong>
          <small>{summary?.period ?? '—'} dönemi</small>
        </div>

        <div className="stat-card">
          <span>Prime esas net ciro</span>
          <strong>{formatMoney(summary?.totalSalesBase ?? 0)}</strong>
          <small>iadeler düşülmüş</small>
        </div>

        <div className="stat-card">
          <span>Prim alan personel</span>
          <strong>{summary?.employeeCount ?? 0}</strong>
          <small>{summary?.closed ? 'dönem kapalı' : 'dönem açık'}</small>
        </div>

        <div className="stat-card">
          <span>Hatalı satır</span>
          <strong className={failedRows > 0 ? 'warn' : undefined}>{failedRows}</strong>
          <small>
            <Link to="/aktarim">aktarım detayı</Link>
          </small>
        </div>
      </div>

      <div className="two-column">
        <div className="card">
          <h2>En çok prim hak edenler</h2>
          <p className="hint">Dönemin ilk beşi.</p>

          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Personel</th>
                  <th>Departman</th>
                  <th className="num">Prim</th>
                </tr>
              </thead>
              <tbody>
                {topEarners.map((employee) => (
                  <tr key={employee.employeeNo}>
                    <td>
                      <strong>{employee.fullName}</strong>
                      <div className="muted">{employee.employeeNo}</div>
                    </td>
                    <td>{employee.department}</td>
                    <td className="num">{formatMoney(employee.totalCommission)}</td>
                  </tr>
                ))}
                {topEarners.length === 0 && (
                  <tr>
                    <td colSpan={3} className="empty">
                      Bu dönemde prim hesaplanmadı.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card">
          <h2>ERP mutabakatı</h2>
          <p className="hint">
            Operasyonel ciro (PMS + POS) ile muhasebeleşmiş ciro (ERP) farkı. ERP prim tabanı değil,
            doğrulama katmanıdır.
          </p>

          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Ürün grubu</th>
                  <th className="num">Operasyonel</th>
                  <th className="num">ERP</th>
                  <th className="num">Fark</th>
                </tr>
              </thead>
              <tbody>
                {(reconciliation?.groups ?? []).map((group) => (
                  <tr key={group.productGroup}>
                    <td>{group.productGroup}</td>
                    <td className="num">{formatMoney(group.operationalRevenue)}</td>
                    <td className="num">{formatMoney(group.accountedRevenue)}</td>
                    <td className="num">{formatMoney(group.difference)}</td>
                  </tr>
                ))}
                {(reconciliation?.groups.length ?? 0) === 0 && (
                  <tr>
                    <td colSpan={4} className="empty">
                      Veri yok.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {reconciliation && (
            <p className="hint" style={{ marginTop: 12 }}>
              Muhasebeleşmemiş kayıt: <strong>{reconciliation.unpostedCount}</strong> ·
              {' '}Eşleşmeyen iade: <strong>{reconciliation.unmatchedRefundCount}</strong>
            </p>
          )}
        </div>
      </div>

      <div className="card">
        <h2>Son veri aktarımları</h2>
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Kaynak</th>
                <th>Dosya</th>
                <th className="num">Yazılan</th>
                <th className="num">Mükerrer</th>
                <th className="num">Hatalı</th>
                <th>Yükleyen</th>
                <th>Zaman</th>
              </tr>
            </thead>
            <tbody>
              {batches.slice(0, 5).map((batch) => (
                <tr key={batch.id}>
                  <td>
                    <span className="badge">{batch.sourceSystem}</span>
                  </td>
                  <td>{batch.fileName}</td>
                  <td className="num">{batch.importedRows}</td>
                  <td className="num">{batch.duplicateRows}</td>
                  <td className="num">{batch.failedRows}</td>
                  <td>{batch.importedBy}</td>
                  <td>{formatDateTime(batch.startedAtUtc)}</td>
                </tr>
              ))}
              {batches.length === 0 && (
                <tr>
                  <td colSpan={7} className="empty">
                    Henüz aktarım yapılmadı.
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
