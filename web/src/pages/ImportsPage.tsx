import { useCallback, useEffect, useRef, useState } from 'react'
import { api } from '../api/client'
import { EmptyState } from '../components/EmptyState'
import { Modal } from '../components/Modal'
import { TableSkeleton } from '../components/Skeleton'
import { SOURCE_LABEL, STAGING_STATUS_LABEL } from '../constants'
import { useSession } from '../context/SessionContext'
import type { ImportBatchResponse, ImportErrorResponse, StagingRowResponse } from '../types'
import { formatDateTime } from '../utils/formatters'

type Detail = {
  batch: ImportBatchResponse
  errors: ImportErrorResponse[]
  staging: StagingRowResponse[]
} | null

export function ImportsPage() {
  const { session } = useSession()
  const fileInput = useRef<HTMLInputElement>(null)

  const [batches, setBatches] = useState<ImportBatchResponse[]>([])
  const [source, setSource] = useState('pms')
  const [detail, setDetail] = useState<Detail>(null)
  const [tab, setTab] = useState<'errors' | 'staging'>('errors')
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [loaded, setLoaded] = useState(false)

  const load = useCallback(async () => {
    try {
      setBatches(await api.importBatches(session))
      setError(null)
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setLoaded(true)
    }
  }, [session])

  useEffect(() => {
    void load()
  }, [load])

  const upload = async () => {
    const file = fileInput.current?.files?.[0]
    if (!file) {
      setError('Önce bir CSV dosyası seçin.')
      return
    }

    setBusy(true)
    setError(null)
    setNotice(null)
    try {
      const summary = await api.uploadImport(session, source, file)
      setNotice(
        `${summary.fileName}: ${summary.importedRows} satır yazıldı, ` +
          `${summary.duplicateRows} mükerrer, ${summary.failedRows} hatalı, ` +
          `${summary.matchedReversals} iade eşleşti.`,
      )
      if (fileInput.current) fileInput.current.value = ''
      await load()
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  const openDetail = async (batch: ImportBatchResponse) => {
    setError(null)
    try {
      const [errors, staging] = await Promise.all([
        api.importErrors(session, batch.id),
        api.stagingRows(session, batch.id),
      ])
      setTab(errors.length > 0 ? 'errors' : 'staging')
      setDetail({ batch, errors, staging })
    } catch (e) {
      setError((e as Error).message)
    }
  }

  return (
    <>
      <div className="page-head">
        <h1>Veri aktarımı</h1>
        <p>
          Kaynak sistem ekstreleri. Mükerrer kayıtlar yazılmaz, ayrıştırılamayan satırlar dosyayı
          reddettirmez — ayrı tabloya düşer.
        </p>
      </div>

      {error && <div className="alert error">{error}</div>}
      {notice && <div className="alert info">{notice}</div>}

      <div className="card">
        <h2>Dosya yükle</h2>
        <p className="hint">
          Üretimde bu iş zamanlanmış aktarımla otomatik yapılır; buradaki yükleme ilk kurulum ve
          düzeltme sonrası yeniden işleme içindir.
        </p>

        <div className="grid">
          <div>
            <label htmlFor="source">Kaynak sistem</label>
            <select id="source" value={source} onChange={(e) => setSource(e.target.value)}>
              <option value="pms">PMS (Fidelio)</option>
              <option value="pos">POS (Flyby)</option>
              <option value="erp">ERP (Oracle JDE)</option>
            </select>
          </div>
          <div style={{ gridColumn: 'span 2' }}>
            <label htmlFor="file">CSV dosyası</label>
            <input id="file" type="file" accept=".csv" ref={fileInput} />
          </div>
          <div style={{ display: 'flex', alignItems: 'flex-end' }}>
            <button className="primary" onClick={upload} disabled={busy}>
              {busy ? 'Aktarılıyor…' : 'Aktar'}
            </button>
          </div>
        </div>
      </div>

      <div className="card">
        <h2>Aktarım geçmişi ({batches.length})</h2>

        {!loaded ? (
          <TableSkeleton rows={5} columns={9} />
        ) : batches.length === 0 ? (
          <EmptyState
            title="Henüz aktarım yapılmadı"
            description="Yukarıdaki formdan bir CSV yükleyin ya da zamanlanmış aktarımın çalışmasını bekleyin."
          />
        ) : (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Kaynak</th>
                <th>Dosya</th>
                <th className="num">Toplam</th>
                <th className="num">Yazılan</th>
                <th className="num">Mükerrer</th>
                <th className="num">Hatalı</th>
                <th>Yükleyen</th>
                <th>Zaman</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {batches.map((batch) => (
                <tr key={batch.id}>
                  <td>
                    <span className="badge">{SOURCE_LABEL[batch.sourceSystem] ?? batch.sourceSystem}</span>
                  </td>
                  <td className="cell-title">{batch.fileName}</td>
                  <td className="num">{batch.totalRows}</td>
                  <td className="num strong">{batch.importedRows}</td>
                  <td className="num">{batch.duplicateRows}</td>
                  <td className="num">
                    {batch.failedRows > 0 ? (
                      <span className="badge warn">{batch.failedRows}</span>
                    ) : (
                      0
                    )}
                  </td>
                  <td>{batch.importedBy}</td>
                  <td>{formatDateTime(batch.startedAtUtc)}</td>
                  <td className="num">
                    <button onClick={() => openDetail(batch)}>Detay</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        )}
      </div>

      {detail && (
        <Modal
          title={detail.batch.fileName}
          subtitle={`${SOURCE_LABEL[detail.batch.sourceSystem] ?? detail.batch.sourceSystem} · ${detail.batch.totalRows} satır · ${formatDateTime(detail.batch.startedAtUtc)}`}
          onClose={() => setDetail(null)}
          width={1000}
          footer={<button onClick={() => setDetail(null)}>Kapat</button>}
        >
          <div className="tab-bar">
            <button className={tab === 'errors' ? 'active' : ''} onClick={() => setTab('errors')}>
              Hatalı satırlar ({detail.errors.length})
            </button>
            <button className={tab === 'staging' ? 'active' : ''} onClick={() => setTab('staging')}>
              Ham satırlar ({detail.staging.length})
            </button>
          </div>

          {tab === 'errors' ? (
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th className="num">Satır</th>
                    <th>Hata</th>
                    <th>Açıklama</th>
                    <th>Ham içerik</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.errors.map((row) => (
                    <tr key={row.rowNumber}>
                      <td className="num">{row.rowNumber}</td>
                      <td>
                        <span className="badge warn">{row.errorCode}</span>
                      </td>
                      <td className="note">{row.errorMessage}</td>
                      <td className="mono">{row.rawLine}</td>
                    </tr>
                  ))}
                  {detail.errors.length === 0 && (
                    <tr>
                      <td colSpan={4} className="empty">
                        Bu aktarımda hatalı satır yok.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="table-scroll">
              <p className="hint">
                Kaynağın gönderdiği hâl, hiçbir dönüşüm uygulanmadan. Ayrıştırıcı düzeltilirse aynı
                parti kaynağa dönmeden yeniden işlenebilir.
              </p>
              <table>
                <thead>
                  <tr>
                    <th className="num">Satır</th>
                    <th>Durum</th>
                    <th>Ham içerik</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.staging.slice(0, 200).map((row) => (
                    <tr key={row.rowNumber}>
                      <td className="num">{row.rowNumber}</td>
                      <td>
                        <span className={row.status === 'Processed' ? 'badge' : 'badge muted'}>
                          {STAGING_STATUS_LABEL[row.status] ?? row.status}
                        </span>
                      </td>
                      <td className="mono">{row.rawLine}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {detail.staging.length > 200 && (
                <p className="hint">İlk 200 satır gösteriliyor.</p>
              )}
            </div>
          )}
        </Modal>
      )}
    </>
  )
}
