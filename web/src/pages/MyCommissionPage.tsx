import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import { CommissionGrid } from '../components/CommissionGrid'
import { PeriodPicker } from '../components/PeriodPicker'
import { StatSkeleton, TableSkeleton } from '../components/Skeleton'
import { EXCLUSION_REASON_LABEL } from '../constants'
import { useSession } from '../context/SessionContext'
import type { CommissionResultResponse, PeriodSummaryResponse } from '../types'
import { amountClass, formatDateTime, formatMoney, formatPercent } from '../utils/formatters'

const DEFAULT_SORT = 'fullName,asc'
const DEFAULT_SIZE = 20

export function MyCommissionPage() {
  const { session, canSeeAllEmployees } = useSession()

  /*
   * Liste durumu adres cubugunda tutulur: sayfa yenilendiginde, geri tusuna
   * basildiginda ve baglanti paylasildiginda ayni liste acilir. Bilesen state'inde
   * tutulsaydi ucu de kaybolurdu.
   */
  const [params, setParams] = useSearchParams()

  const year = Number(params.get('year')) || 2026
  const month = Number(params.get('month')) || 8
  const page = Number(params.get('page')) || 0
  const size = Number(params.get('size')) || DEFAULT_SIZE
  const sort = params.get('sort') ?? DEFAULT_SORT

  // Personel rolunde baska bir personel sorgulanamaz; secim kendi numarasina sabitlenir.
  const locked = !canSeeAllEmployees
  const selected = locked ? (session.employeeNo ?? null) : params.get('employeeNo')

  const [result, setResult] = useState<CommissionResultResponse | null>(null)
  const [summary, setSummary] = useState<PeriodSummaryResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [gridBusy, setGridBusy] = useState(false)

  /** Personel rolunde secim yok; kendi adi gosterilir. */
  const ownLabel = result ? `${result.fullName} — ${result.department}` : (selected ?? '')

  const update = useCallback(
    (changes: Record<string, string | null>) => {
      const next = new URLSearchParams(params)
      for (const [key, value] of Object.entries(changes)) {
        if (value === null) next.delete(key)
        else next.set(key, value)
      }
      setParams(next, { replace: true })
    },
    [params, setParams],
  )

  /*
   * Donem, siralama ve sayfa boyutu degisince sayfa basa doner. Aksi halde
   * 5. sayfadayken 12 kisilik bir donem secen kullanici bos bir sayfa gorurdu.
   */
  const loadSummary = useCallback(async () => {
    if (locked) return

    setGridBusy(true)
    try {
      // Onceki liste ekranda kalir, yalnizca soldurulur: her sayfa gecisinde
      // iskelete donmek listeyi titretiyordu.
      setSummary(await api.periodSummary(session, year, month, { page, size, sort }))
    } catch (e) {
      setSummary(null)
      setError((e as Error).message)
    } finally {
      setGridBusy(false)
    }
  }, [locked, session, year, month, page, size, sort])

  const loadDetail = useCallback(async () => {
    if (!selected) {
      setResult(null)
      return
    }

    setBusy(true)
    setError(null)
    try {
      setResult(await api.commission(session, year, month, selected))
    } catch (e) {
      setResult(null)
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }, [session, year, month, selected])

  useEffect(() => {
    void loadSummary()
  }, [loadSummary])

  useEffect(() => {
    void loadDetail()
  }, [loadDetail])

  const refresh = () => {
    void loadSummary()
    void loadDetail()
  }

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
          onYearChange={(value) => update({ year: String(value), page: null })}
          onMonthChange={(value) => update({ month: String(value), page: null })}
          onRefresh={refresh}
          busy={busy || gridBusy}
        >
          {locked && (
            <div style={{ minWidth: 260 }}>
              <label htmlFor="employeeNo">Personel</label>
              <input id="employeeNo" value={ownLabel} disabled />
            </div>
          )}
        </PeriodPicker>
      </div>

      {!locked && (
        <>
          <div className="stat-grid">
            <div className="stat-card">
              <span>Dönem</span>
              <strong style={{ fontSize: 18 }}>{summary?.period ?? '—'}</strong>
              <small>
                <span className={summary?.closed ? 'badge warn' : 'badge'}>
                  {summary?.closed ? 'Kapalı' : 'Açık'}
                </span>
              </small>
            </div>
            <div className="stat-card">
              <span>Prim alan personel</span>
              <strong>{summary?.employeeCount ?? 0}</strong>
              <small>toplam {summary?.employees.totalElements ?? 0} kayıt</small>
            </div>
            <div className="stat-card">
              <span>Prime esas net ciro</span>
              <strong>{formatMoney(summary?.totalSalesBase ?? 0)}</strong>
              <small>dönemin tamamı</small>
            </div>
            <div className="stat-card">
              <span>Toplam prim</span>
              <strong className="accent">{formatMoney(summary?.totalCommission ?? 0)}</strong>
              <small>dönemin tamamı</small>
            </div>
          </div>

          <CommissionGrid
            data={summary?.employees ?? null}
            sort={sort}
            selected={selected}
            loading={gridBusy && !summary}
            fetching={gridBusy}
            onSortChange={(value) => update({ sort: value, page: null })}
            onPageChange={(value) => update({ page: String(value) })}
            onSizeChange={(value) => update({ size: String(value), page: null })}
            onSelect={(employeeNo) => update({ employeeNo })}
          />
        </>
      )}

      {!locked && !selected && !gridBusy && (
        <p className="hint">Hesaplama adımlarını görmek için listeden bir personel seçin.</p>
      )}

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
          {locked && (
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
                </span>{' '}
                · hesap {formatDateTime(result.calculatedAtUtc)}
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
          )}

          <div className="card">
            <h2>
              {locked
                ? `Hesaplama adımları (${result.steps.length})`
                : `${result.fullName} — hesaplama adımları (${result.steps.length})`}
            </h2>
            <p className="hint">
              {!locked && (
                <>
                  {result.employeeNo} · {result.department} · {result.hotel} · dönem primi{' '}
                  {formatMoney(result.totalCommission)} · hesap{' '}
                  {formatDateTime(result.calculatedAtUtc)}
                  <br />
                </>
              )}
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
