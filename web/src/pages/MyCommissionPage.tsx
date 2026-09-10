import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import { PeriodPicker } from '../components/PeriodPicker'
import { StatSkeleton, TableSkeleton } from '../components/Skeleton'
import { EXCLUSION_REASON_LABEL } from '../constants'
import { useSession } from '../context/SessionContext'
import type { CommissionResultResponse, EmployeeResponse } from '../types'
import { amountClass, formatMoney, formatPercent } from '../utils/formatters'

export function MyCommissionPage() {
  const { session, canSeeAllEmployees } = useSession()
  const [params] = useSearchParams()

  const [year, setYear] = useState(Number(params.get('year')) || 2026)
  const [month, setMonth] = useState(Number(params.get('month')) || 8)
  const [employeeNo, setEmployeeNo] = useState(
    params.get('employeeNo') ?? session.employeeNo ?? 'P1001',
  )

  const [result, setResult] = useState<CommissionResultResponse | null>(null)
  const [employees, setEmployees] = useState<EmployeeResponse[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  // Personel rolunde baska bir personel sorgulanamaz; kutu kendi numarasina sabitlenir.
  const locked = !canSeeAllEmployees
  const target = locked ? (session.employeeNo ?? employeeNo) : employeeNo

  /** Personel rolunde secim yok; kendi adi gosterilir. */
  const ownLabel = result ? `${result.fullName} — ${result.department}` : target

  /*
   * Personel listesi yalnizca baskasinin primini gorebilen roller icin cekilir.
   * Personel rolu zaten kendi kaydina kilitli; listeyi getirmek gereksiz olurdu
   * ve diger calisanlarin adlarini bosuna gosterirdi.
   */
  useEffect(() => {
    if (locked) return

    api
      .employees(session)
      .then((list) =>
        setEmployees([...list].sort((a, b) => a.fullName.localeCompare(b.fullName, 'tr'))),
      )
      .catch(() => setEmployees([]))
  }, [locked, session])

  const load = useCallback(async () => {
    setBusy(true)
    setError(null)
    try {
      setResult(await api.commission(session, year, month, target))
    } catch (e) {
      setResult(null)
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }, [session, year, month, target])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <>
      <div className="page-head">
        <h1>{locked ? 'Primim' : 'Personel primi'}</h1>
        <p>
          Hesap; hangi satış, hangi kural, hangi oran ve ara toplamlar bilgisiyle gösterilir.
          {locked && ' Personel rolünde yalnızca kendi priminiz görüntülenir.'}
        </p>
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
        >
          <div style={{ minWidth: 260 }}>
            <label htmlFor="employeeNo">Personel</label>

            {locked ? (
              <input id="employeeNo" value={ownLabel} disabled />
            ) : (
              <select
                id="employeeNo"
                value={target}
                onChange={(e) => setEmployeeNo(e.target.value)}
              >
                {employees.map((employee) => (
                  <option key={employee.employeeNo} value={employee.employeeNo}>
                    {employee.fullName} — {employee.department}
                  </option>
                ))}
              </select>
            )}
          </div>
        </PeriodPicker>
      </div>

      {busy && !result && (
        <>
          <StatSkeleton />
          <div className="card">
            <TableSkeleton rows={8} columns={8} />
          </div>
        </>
      )}

      {result && (
        <>
          <div className="stat-grid">
            <div className="stat-card">
              <span>Personel</span>
              <strong style={{ fontSize: 18 }}>{result.fullName}</strong>
              <small>
                {result.employeeNo} · {result.department} · {result.hotel}
              </small>
            </div>
            <div className="stat-card">
              <span>Dönem</span>
              <strong style={{ fontSize: 18 }}>{result.period}</strong>
              <small>
                <span className={result.periodClosed ? 'badge warn' : 'badge'}>
                  {result.periodClosed ? 'Kapalı' : 'Açık'}
                </span>
              </small>
            </div>
            <div className="stat-card">
              <span>Prime esas net ciro</span>
              <strong>{formatMoney(result.totalSalesBase)}</strong>
              <small>iadeler düşülmüş</small>
            </div>
            <div className="stat-card">
              <span>Hak edilen prim</span>
              <strong className="accent">{formatMoney(result.totalCommission)}</strong>
              <small>{result.steps.length} hesaplama adımı</small>
            </div>
          </div>

          <div className="card">
            <h2>Hesaplama adımları ({result.steps.length})</h2>
            <p className="hint">
              Yeşil satırlar kademeli baremde hangi kademenin seçildiğini gösterir; prim tutarı taşımaz.
            </p>

            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th className="num">#</th>
                    <th>Kural</th>
                    <th>Kaynak / Belge</th>
                    <th>Tarih</th>
                    <th className="num">Taban</th>
                    <th className="num">Oran / Tutar</th>
                    <th className="num">Prim</th>
                    <th>Açıklama</th>
                  </tr>
                </thead>
                <tbody>
                  {result.steps.map((step) => (
                    <tr
                      key={step.order}
                      className={
                        !step.sourceDocumentNo && step.commissionAmount === 0 ? 'summary-step' : undefined
                      }
                    >
                      <td className="num">{step.order}</td>
                      <td>
                        <strong>{step.ruleCode}</strong>
                      </td>
                      <td>
                        {step.sourceDocumentNo ? (
                          <>
                            <div className="cell-title">{step.sourceDocumentNo}</div>
                            <div className="cell-sub">
                              {step.sourceSystem}
                              {step.productName ? ` · ${step.productName}` : ''}
                            </div>
                          </>
                        ) : (
                          '—'
                        )}
                      </td>
                      <td>{step.transactionDate ?? '—'}</td>
                      <td className={amountClass(step.baseAmount)}>{formatMoney(step.baseAmount)}</td>
                      <td className="num">
                        {step.appliedRate != null && formatPercent(step.appliedRate)}
                        {step.appliedFixedAmount != null && formatMoney(step.appliedFixedAmount)}
                        {step.appliedRate == null && step.appliedFixedAmount == null && '—'}
                      </td>
                      <td className={`${amountClass(step.commissionAmount)} strong`}>
                        {formatMoney(step.commissionAmount)}
                      </td>
                      <td className="note">{step.explanation}</td>
                    </tr>
                  ))}
                  {result.steps.length === 0 && (
                    <tr>
                      <td colSpan={8} className="empty">
                        Bu dönemde prime esas satış bulunamadı.
                      </td>
                    </tr>
                  )}
                </tbody>
                {result.steps.length > 0 && (
                  <tfoot>
                    <tr>
                      <td colSpan={6} className="num">
                        <strong>Toplam</strong>
                      </td>
                      <td className="num">
                        <strong>{formatMoney(result.totalCommission)}</strong>
                      </td>
                      <td />
                    </tr>
                  </tfoot>
                )}
              </table>
            </div>
          </div>

          {result.excludedSales.length > 0 && (
            <div className="card">
              <h2>Prim dışı bırakılan satışlar ({result.excludedSales.length})</h2>
              <p className="hint">Hesaba girmeyen kayıtlar sessizce atılmaz; nedeniyle listelenir.</p>

              <div className="table-scroll">
                <table>
                  <thead>
                    <tr>
                      <th>Kaynak / Belge</th>
                      <th>Tarih</th>
                      <th>Ürün</th>
                      <th className="num">Tutar</th>
                      <th>Neden</th>
                      <th>Açıklama</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.excludedSales.map((sale) => (
                      <tr key={`${sale.sourceSystem}-${sale.sourceDocumentNo}`}>
                        <td>
                          <div className="cell-title">{sale.sourceDocumentNo}</div>
                          <div className="cell-sub">{sale.sourceSystem}</div>
                        </td>
                        <td>{sale.transactionDate}</td>
                        <td>{sale.productName}</td>
                        <td className={amountClass(sale.amountTry)}>{formatMoney(sale.amountTry)}</td>
                        <td>
                          <span className="badge warn">
                            {EXCLUSION_REASON_LABEL[sale.reasonCode] ?? sale.reasonCode}
                          </span>
                        </td>
                        <td className="note">{sale.reason}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}
    </>
  )
}
