import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { BarChart, DivergingBarChart, StackedBarChart } from '../components/Charts'
import { EmptyState } from '../components/EmptyState'
import { PeriodPicker } from '../components/PeriodPicker'
import { StatSkeleton, TableSkeleton } from '../components/Skeleton'
import { useSession } from '../context/SessionContext'
import type {
  EmployeeCommissionResponse,
  ImportBatchResponse,
  PeriodSummaryResponse,
  ReconciliationResponse,
} from '../types'
import { amountClass, formatMoney } from '../utils/formatters'

/** Personel listesini verilen anahtara gore toplar; departman ve otel dagilimi ayni islem. */
function sumBy(employees: EmployeeCommissionResponse[], key: 'department' | 'hotel') {
  const totals = new Map<string, { value: number; count: number }>()

  for (const employee of employees) {
    if (employee.totalCommission === 0) continue

    const current = totals.get(employee[key]) ?? { value: 0, count: 0 }
    totals.set(employee[key], {
      value: current.value + employee.totalCommission,
      count: current.count + 1,
    })
  }

  return [...totals.entries()]
    .map(([label, { value, count }]) => ({ label, value, sublabel: `${count} personel` }))
    .sort((a, b) => b.value - a.value)
}

export function DashboardPage() {
  const { session } = useSession()
  const [year, setYear] = useState(2026)
  const [month, setMonth] = useState(8)

  const [summary, setSummary] = useState<PeriodSummaryResponse | null>(null)
  const [reconciliation, setReconciliation] = useState<ReconciliationResponse | null>(null)
  const [batches, setBatches] = useState<ImportBatchResponse[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [loaded, setLoaded] = useState(false)

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
      setLoaded(true)
    }
  }, [session, year, month])

  useEffect(() => {
    void load()
  }, [load])

  const employees = useMemo(() => summary?.employees ?? [], [summary])
  const byDepartment = useMemo(() => sumBy(employees, 'department'), [employees])
  const byHotel = useMemo(() => sumBy(employees, 'hotel'), [employees])

  const topEarners = useMemo(
    () => [...employees].sort((a, b) => b.totalCommission - a.totalCommission).slice(0, 5),
    [employees],
  )

  const differences = useMemo(
    () =>
      (reconciliation?.groups ?? [])
        .filter((group) => group.difference !== 0)
        .map((group) => ({ label: group.productGroup, value: group.difference }))
        .sort((a, b) => b.value - a.value),
    [reconciliation],
  )

  const importRows = useMemo(
    () =>
      batches
        // Satiri olmayan parti kalite bilgisi tasimaz; bos bar cizmek yaniltir.
        .filter((batch) => batch.totalRows > 0)
        .slice(0, 6)
        .map((batch) => ({
        label: batch.fileName,
        sublabel: `${batch.sourceSystem} · ${batch.importedBy}`,
        segments: [
          { key: 'imported', label: 'Yazılan', value: batch.importedRows },
          { key: 'duplicate', label: 'Mükerrer', value: batch.duplicateRows },
          { key: 'failed', label: 'Hatalı', value: batch.failedRows },
        ],
      })),
    [batches],
  )

  const failedRows = batches.reduce((total, batch) => total + batch.failedRows, 0)

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

      {!loaded && <StatSkeleton />}

      {loaded && (
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
              tüm aktarımlar · <Link to="/aktarim">detay</Link>
            </small>
          </div>
        </div>
      )}

      <div className="two-column">
        <div className="card">
          <h2>Departman bazında prim</h2>
          <p className="hint">Dönemde hak edilen primin departmanlara dağılımı.</p>

          {!loaded ? (
            <TableSkeleton rows={4} columns={3} />
          ) : (
            <BarChart items={byDepartment} format={formatMoney} emptyText="Bu dönemde prim hesaplanmadı." />
          )}
        </div>

        <div className="card">
          <h2>Otel bazında prim</h2>
          <p className="hint">Otel personelin özelliğidir, satış kaydının değil.</p>

          {!loaded ? (
            <TableSkeleton rows={3} columns={3} />
          ) : (
            <BarChart items={byHotel} format={formatMoney} emptyText="Bu dönemde prim hesaplanmadı." />
          )}
        </div>
      </div>

      <div className="two-column">
        <div className="card">
          <h2>En çok prim hak edenler</h2>
          <p className="hint">Dönemin ilk beşi.</p>

          {!loaded ? (
            <TableSkeleton rows={5} columns={3} />
          ) : topEarners.length === 0 ? (
            <EmptyState
              title="Bu dönemde prim hesaplanmadı"
              description="Seçili ay için prime esas satış bulunmuyor ya da hesaplama henüz çalışmadı."
            />
          ) : (
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
                        <div className="cell-title">{employee.fullName}</div>
                        <div className="cell-sub">{employee.employeeNo}</div>
                      </td>
                      <td>
                        <span className="badge muted">{employee.department}</span>
                      </td>
                      <td className={`${amountClass(employee.totalCommission)} strong`}>
                        {formatMoney(employee.totalCommission)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="card">
          <h2>ERP mutabakat farkı</h2>
          <p className="hint">
            Operasyonel ciro (PMS + POS) eksi muhasebeleşmiş ciro (ERP). Sağa taşan grupta ERP
            eksik kalmış, sola taşan grupta ERP fazla görünüyor. ERP prim tabanı değil,
            doğrulama katmanıdır.
          </p>

          {!loaded ? (
            <TableSkeleton rows={6} columns={3} />
          ) : (
            <DivergingBarChart
              items={differences}
              format={formatMoney}
              emptyText="Fark bulunan ürün grubu yok."
            />
          )}

          {reconciliation && (
            <p className="hint" style={{ marginTop: 16, marginBottom: 0 }}>
              Muhasebeleşmemiş kayıt: <strong>{reconciliation.unpostedCount}</strong> · Eşleşmeyen
              iade: <strong>{reconciliation.unmatchedRefundCount}</strong>
            </p>
          )}
        </div>
      </div>

      <div className="card">
        <h2>Aktarım kalitesi</h2>
        <p className="hint">
          Her dosyanın satırları: veritabanına yazılan, mükerrer olduğu için atlanan ve
          ayrıştırılamadığı için hata tablosuna düşen. Hatalı satır dosyayı reddettirmez.
        </p>

        {!loaded ? (
          <TableSkeleton rows={3} columns={3} />
        ) : (
          <StackedBarChart rows={importRows} emptyText="Henüz aktarım yapılmadı." />
        )}
      </div>
    </>
  )
}
