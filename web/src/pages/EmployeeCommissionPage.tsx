import { useCallback, useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { api, isAborted } from '../api/client'
import { CommissionDetail } from '../components/CommissionDetail'
import { StatSkeleton, TableSkeleton } from '../components/Skeleton'
import { useSession } from '../context/SessionContext'
import type { CommissionResultResponse } from '../types'

/**
 * Listeden acilan personel detayi. Kendi adresi vardir: baglanti paylasilabilir,
 * geri tusu calisir ve sayfa yenilendiginde ayni hesap acilir.
 *
 * Liste parametreleri (donem, sayfa, siralama) adres cubugunda tasinir; donus
 * baglantisi bunlari geri verir, kullanici 3. sayfadan detaya girip 1. sayfaya
 * dusmez.
 */
export function EmployeeCommissionPage() {
  const { session } = useSession()
  const { employeeNo = '' } = useParams()
  const [params] = useSearchParams()

  const year = Number(params.get('year')) || 2026
  const month = Number(params.get('month')) || 8

  const [result, setResult] = useState<CommissionResultResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(true)

  const load = useCallback(
    async (signal: AbortSignal) => {
      setBusy(true)
      setError(null)
      try {
        setResult(await api.commission(session, year, month, employeeNo, signal))
      } catch (e) {
        if (isAborted(e)) return
        setResult(null)
        setError((e as Error).message)
      } finally {
        if (!signal.aborted) setBusy(false)
      }
    },
    [session, year, month, employeeNo],
  )

  useEffect(() => {
    const controller = new AbortController()
    void load(controller.signal)
    return () => controller.abort()
  }, [load])

  const backTo = `/primim?${params}`

  return (
    <>
      <Link to={backTo} className="back-link">
        ← Dönem özetine dön
      </Link>

      <div className="page-head">
        <h1>{result?.fullName ?? employeeNo}</h1>
        <p>
          {year}-{String(month).padStart(2, '0')} dönemi hesabı; hangi satış, hangi kural, hangi oran
          ve ara toplamlar bilgisiyle.
        </p>
      </div>

      {error && <div className="alert error">{error}</div>}

      {busy && !result && (
        <>
          <StatSkeleton />
          <div className="card">
            <TableSkeleton rows={8} columns={8} />
          </div>
        </>
      )}

      {result && <CommissionDetail result={result} />}
    </>
  )
}
