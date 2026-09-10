import { useCallback, useEffect, useState } from 'react'
import { api, formatMoney, formatPercent, type Session } from '../api/client'
import type { CommissionResultResponse } from '../types'

const REASON_LABEL: Record<string, string> = {
  NO_MATCHING_RULE: 'Kural tanımlı değil',
  OUTSIDE_EMPLOYMENT: 'İstihdam dışı',
  NOT_COMMISSIONABLE: 'Prime esas değil',
  REVERSED_PAIR: 'İade ile netleşti',
}

export function CommissionPage({ session }: { session: Session }) {
  const [year, setYear] = useState(2026)
  const [month, setMonth] = useState(8)
  const [employeeNo, setEmployeeNo] = useState(session.employeeNo ?? 'P1001')
  const [result, setResult] = useState<CommissionResultResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  // Personel rolunde baska bir personel sorgulanamaz; kutu kendi numarasina sabitlenir.
  const locked = session.role === 'EmployeeResponse'

  useEffect(() => {
    if (locked && session.employeeNo) setEmployeeNo(session.employeeNo)
  }, [locked, session.employeeNo])

  const load = useCallback(async () => {
    setBusy(true)
    setError(null)
    try {
      setResult(await api.commission(session, year, month, employeeNo))
    } catch (e) {
      setResult(null)
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }, [session, year, month, employeeNo])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <>
      {error && <div className="alert error">{error}</div>}

      <div className="card">
        <h2>Dönem primi</h2>
        <p className="hint">
          Hesap; hangi satış, hangi kural, hangi oran ve ara toplamlar bilgisiyle birlikte gösterilir.
        </p>

        <div className="grid">
          <div>
            <label htmlFor="employeeNo">Personel no</label>
            <input
              id="employeeNo"
              value={employeeNo}
              disabled={locked}
              onChange={(e) => setEmployeeNo(e.target.value.trim().toUpperCase())}
            />
          </div>
          <div>
            <label htmlFor="year">Yıl</label>
            <input id="year" type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} />
          </div>
          <div>
            <label htmlFor="month">Ay</label>
            <select id="month" value={month} onChange={(e) => setMonth(Number(e.target.value))}>
              {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                <option key={m} value={m}>
                  {String(m).padStart(2, '0')}
                </option>
              ))}
            </select>
          </div>
          <div style={{ display: 'flex', alignItems: 'flex-end' }}>
            <button className="primary" onClick={load} disabled={busy}>
              {busy ? 'Hesaplanıyor…' : 'Hesapla'}
            </button>
          </div>
        </div>
      </div>

      {result && (
        <>
          <div className="card">
            <div className="stat-row">
              <div className="stat">
                <span>Personel</span>
                <strong style={{ fontSize: 17 }}>{result.fullName}</strong>
                <div style={{ color: 'var(--muted)', fontSize: 12 }}>
                  {result.employeeNo} · {result.department} · {result.hotel}
                </div>
              </div>
              <div className="stat">
                <span>Dönem</span>
                <strong style={{ fontSize: 17 }}>{result.period}</strong>
                <div>
                  <span className={result.periodClosed ? 'badge warn' : 'badge'}>
                    {result.periodClosed ? 'Kapalı' : 'Açık'}
                  </span>
                </div>
              </div>
              <div className="stat">
                <span>Prime esas net ciro</span>
                <strong>{formatMoney(result.totalSalesBase)}</strong>
              </div>
              <div className="stat">
                <span>Hak edilen prim</span>
                <strong style={{ color: 'var(--accent)' }}>{formatMoney(result.totalCommission)}</strong>
              </div>
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
                      className={!step.sourceDocumentNo && step.commissionAmount === 0 ? 'summary-step' : undefined}
                    >
                      <td className="num">{step.order}</td>
                      <td>
                        <strong>{step.ruleCode}</strong>
                      </td>
                      <td>
                        {step.sourceDocumentNo
                          ? `${step.sourceSystem} · ${step.sourceDocumentNo}`
                          : '—'}
                        {step.productName && (
                          <div style={{ color: 'var(--muted)', fontSize: 12 }}>{step.productName}</div>
                        )}
                      </td>
                      <td>{step.transactionDate ?? '—'}</td>
                      <td className="num">{formatMoney(step.baseAmount)}</td>
                      <td className="num">
                        {step.appliedRate != null && formatPercent(step.appliedRate)}
                        {step.appliedFixedAmount != null && formatMoney(step.appliedFixedAmount)}
                        {step.appliedRate == null && step.appliedFixedAmount == null && '—'}
                      </td>
                      <td className="num">{formatMoney(step.commissionAmount)}</td>
                      <td style={{ color: 'var(--muted)' }}>{step.explanation}</td>
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
              <p className="hint">
                Hesaba girmeyen kayıtlar sessizce atılmaz; nedeniyle birlikte burada listelenir.
              </p>

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
                          {sale.sourceSystem} · {sale.sourceDocumentNo}
                        </td>
                        <td>{sale.transactionDate}</td>
                        <td>{sale.productName}</td>
                        <td className="num">{formatMoney(sale.amountTry)}</td>
                        <td>
                          <span className="badge warn">
                            {REASON_LABEL[sale.reasonCode] ?? sale.reasonCode}
                          </span>
                        </td>
                        <td style={{ color: 'var(--muted)' }}>{sale.reason}</td>
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
