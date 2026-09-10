import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import { EmptyState } from '../components/EmptyState'
import { Modal } from '../components/Modal'
import { TableSkeleton } from '../components/Skeleton'
import { useSession } from '../context/SessionContext'
import type { AuditLogResponse, PagedResponse } from '../types'
import { formatDateTime } from '../utils/formatters'

const ENTITY_FILTERS = [
  { value: '', label: 'Tümü' },
  { value: 'CommissionRule', label: 'Prim kuralı' },
  { value: 'CommissionRuleTier', label: 'Kademe' },
  { value: 'SaleRecord', label: 'Satış kaydı' },
  { value: 'Period', label: 'Dönem' },
]

const ACTION_LABEL: Record<string, string> = {
  Create: 'Oluşturma',
  Update: 'Güncelleme',
  Delete: 'Silme',
}

/** JSON gövdesini okunur hale getirir; bozuksa ham metni döndürür. */
function pretty(value?: string) {
  if (!value) return '—'
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

export function AuditLogPage() {
  const { session } = useSession()

  const [page, setPage] = useState(0)
  const [entityName, setEntityName] = useState('')
  const [data, setData] = useState<PagedResponse<AuditLogResponse> | null>(null)
  const [selected, setSelected] = useState<AuditLogResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loaded, setLoaded] = useState(false)

  const load = useCallback(async () => {
    try {
      setData(await api.auditLogs(session, page, 20, entityName || undefined))
      setError(null)
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setLoaded(true)
    }
  }, [session, page, entityName])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <>
      <div className="page-head">
        <h1>Denetim kayıtları</h1>
        <p>
          Prim kuralı, satış kaydı ve dönem üzerinde yapılan her değişiklik: kim, ne zaman,
          eski değer, yeni değer. Kayıt işlemle aynı transaction'da yazılır.
        </p>
      </div>

      {error && <div className="alert error">{error}</div>}

      <div className="card">
        <div className="grid">
          <div>
            <label htmlFor="entityName">Kayıt tipi</label>
            <select
              id="entityName"
              value={entityName}
              onChange={(e) => {
                setPage(0)
                setEntityName(e.target.value)
              }}
            >
              {ENTITY_FILTERS.map((filter) => (
                <option key={filter.value} value={filter.value}>
                  {filter.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      <div className="card">
        <h2>Değişiklik geçmişi ({data?.totalElements ?? 0})</h2>

        {!loaded ? (
          <TableSkeleton rows={8} columns={6} />
        ) : (data?.content.length ?? 0) === 0 ? (
          <EmptyState
            title="Kayıt bulunamadı"
            description="Seçili kayıt tipinde henüz bir değişiklik yapılmamış."
          />
        ) : (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Zaman</th>
                <th>Kayıt</th>
                <th>İşlem</th>
                <th>Kim</th>
                <th>Rol</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(data?.content ?? []).map((log) => (
                <tr key={log.id}>
                  <td>{formatDateTime(log.changedAtUtc)}</td>
                  <td>{log.entityName}</td>
                  <td>
                    <span className="badge muted">{ACTION_LABEL[log.action] ?? log.action}</span>
                  </td>
                  <td>{log.changedBy}</td>
                  <td>{log.changedByRole}</td>
                  <td className="num">
                    <button onClick={() => setSelected(log)}>Detay</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        )}

        {data && data.totalPages > 1 && (
          <div className="pager">
            <button onClick={() => setPage((p) => Math.max(0, p - 1))} disabled={page === 0}>
              ← Önceki
            </button>
            <span>
              {data.page + 1} / {data.totalPages}
            </span>
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={page + 1 >= data.totalPages}
            >
              Sonraki →
            </button>
          </div>
        )}
      </div>

      {selected && (
        <Modal
          title={`${selected.entityName} · ${ACTION_LABEL[selected.action] ?? selected.action}`}
          subtitle={`${selected.changedBy} (${selected.changedByRole}) · ${formatDateTime(selected.changedAtUtc)}`}
          onClose={() => setSelected(null)}
          footer={<button onClick={() => setSelected(null)}>Kapat</button>}
        >
          <div className="two-column">
            <div>
              <h3>Eski değer</h3>
              <pre className="json-view">{pretty(selected.oldValues)}</pre>
            </div>
            <div>
              <h3>Yeni değer</h3>
              <pre className="json-view">{pretty(selected.newValues)}</pre>
            </div>
          </div>
          <p className="hint">Kayıt kimliği: {selected.entityId}</p>
        </Modal>
      )}
    </>
  )
}
