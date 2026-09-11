import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api, isAborted } from '../api/client'
import { CommissionDetail } from '../components/CommissionDetail'
import { CommissionGrid } from '../components/CommissionGrid'
import { PeriodPicker } from '../components/PeriodPicker'
import { StatSkeleton, TableSkeleton } from '../components/Skeleton'
import { useSession } from '../context/SessionContext'
import type { CommissionResultResponse, PeriodSummaryResponse } from '../types'
import { formatMoney } from '../utils/formatters'

const DEFAULT_SORT = 'fullName,asc'
const DEFAULT_SIZE = 20

/**
 * Iki farkli ekran, tek adres:
 * - Yetkili rol donemin personel listesini gorur, detaya listeden gider.
 * - Personel rolu baskasini sorgulayamaz; dogrudan kendi hesabini gorur.
 */
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

  const locked = !canSeeAllEmployees

  const [result, setResult] = useState<CommissionResultResponse | null>(null)
  const [summary, setSummary] = useState<PeriodSummaryResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [gridBusy, setGridBusy] = useState(false)

  /** Personel rolunde secim yok; kendi adi gosterilir. */
  const ownLabel = result ? `${result.fullName} — ${result.department}` : (session.employeeNo ?? '')

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

  /** "Getir" dugmesi ayni parametrelerle yeniden yuklemeyi tetikler. */
  const [reloadToken, setReloadToken] = useState(0)
  const refresh = () => setReloadToken((token) => token + 1)

  const loadSummary = useCallback(
    async (signal: AbortSignal) => {
      if (locked) return

      setGridBusy(true)
      try {
        // Onceki liste ekranda kalir, yalnizca soldurulur: her sayfa gecisinde
        // iskelete donmek listeyi titretiyordu.
        setSummary(await api.periodSummary(session, year, month, { page, size, sort }, signal))
        setError(null)
      } catch (e) {
        if (isAborted(e)) return
        setSummary(null)
        setError((e as Error).message)
      } finally {
        if (!signal.aborted) setGridBusy(false)
      }
    },
    [locked, session, year, month, page, size, sort],
  )

  const loadOwn = useCallback(
    async (signal: AbortSignal) => {
      if (!locked || !session.employeeNo) {
        setResult(null)
        return
      }

      setBusy(true)
      setError(null)
      try {
        setResult(await api.commission(session, year, month, session.employeeNo, signal))
      } catch (e) {
        if (isAborted(e)) return
        setResult(null)
        setError((e as Error).message)
      } finally {
        if (!signal.aborted) setBusy(false)
      }
    },
    [locked, session, year, month],
  )

  /*
   * Istek iptal edilebilir olmali: kullanici hizlica sayfa degistirdiginde
   * yolda kalan eski cevap yeni sayfanin uzerine yazabilirdi.
   */
  useEffect(() => {
    const controller = new AbortController()
    void loadSummary(controller.signal)
    return () => controller.abort()
  }, [loadSummary, reloadToken])

  useEffect(() => {
    const controller = new AbortController()
    void loadOwn(controller.signal)
    return () => controller.abort()
  }, [loadOwn, reloadToken])

  /*
   * Adres cubugundan gelen sayfa listenin disindaysa son sayfaya cekilir;
   * ?page=99 ile paylasilan bir baglanti bos tablo acmasin.
   */
  useEffect(() => {
    const totalPages = summary?.employees.totalPages ?? 0
    if (totalPages > 0 && page > totalPages - 1) update({ page: String(totalPages - 1) })
  }, [summary, page, update])

  return (
    <>
      <div className="page-head">
        <h1>{locked ? 'Primim' : 'Dönem özeti'}</h1>
        <p>
          {locked
            ? 'Hesap; hangi satış, hangi kural, hangi oran ve ara toplamlar bilgisiyle gösterilir. Personel rolünde yalnızca kendi priminiz görüntülenir.'
            : 'Dönemin tüm personeli ve hak ettiği prim. Bir personelin hesaplama adımları için satırdaki detay bağlantısını kullanın.'}
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
            loading={gridBusy && !summary}
            fetching={gridBusy}
            // Liste durumu detay adresine tasinir; donus baglantisi ayni sayfayi geri acar.
            detailHref={(employeeNo) => `/primim/${employeeNo}?${params}`}
            onSortChange={(value) => update({ sort: value, page: null })}
            onPageChange={(value) => update({ page: String(value) })}
            onSizeChange={(value) => update({ size: String(value), page: null })}
          />
        </>
      )}

      {locked && busy && !result && (
        <>
          <StatSkeleton />
          <div className="card">
            <TableSkeleton rows={8} columns={8} />
          </div>
        </>
      )}

      {locked && result && <CommissionDetail result={result} />}
    </>
  )
}
