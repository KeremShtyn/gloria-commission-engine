import type { EmployeeCommissionResponse, PagedResponse } from '../types'
import { amountClass, formatMoney } from '../utils/formatters'
import { EmptyState } from './EmptyState'
import { TableSkeleton } from './Skeleton'

/** Sunucudaki beyaz listeyle ayni; buradaki bir yazim hatasi 400 doner, sessizce siralanmaz. */
const COLUMNS = [
  { field: 'fullName', label: 'Personel', numeric: false },
  { field: 'department', label: 'Departman', numeric: false },
  { field: 'hotel', label: 'Otel', numeric: false },
  { field: 'totalSalesBase', label: 'Prime esas ciro', numeric: true },
  { field: 'totalCommission', label: 'Prim', numeric: true },
] as const

interface Props {
  data: PagedResponse<EmployeeCommissionResponse> | null
  sort: string
  selected: string | null
  loading: boolean
  /** Yeni sayfa gelirken eski liste ekranda kalir; bu bayrak yalnizca soldurur. */
  fetching: boolean
  onSortChange: (sort: string) => void
  onPageChange: (page: number) => void
  onSizeChange: (size: number) => void
  onSelect: (employeeNo: string) => void
}

export function CommissionGrid({
  data,
  sort,
  selected,
  loading,
  fetching,
  onSortChange,
  onPageChange,
  onSizeChange,
  onSelect,
}: Props) {
  const [sortField, sortDir] = sort.split(',')

  /** Ayni sutuna tekrar tiklamak yonu cevirir; baska sutun her zaman artan baslar. */
  const toggle = (field: string) =>
    onSortChange(field === sortField && sortDir === 'asc' ? `${field},desc` : `${field},asc`)

  if (loading) {
    return (
      <div className="card">
        <TableSkeleton rows={6} columns={5} />
      </div>
    )
  }

  if (!data || data.totalElements === 0) {
    return (
      <div className="card">
        <EmptyState
          title="Bu dönemde personel kaydı yok"
          description="Seçili dönem için aktarılmış satış bulunamadı; önce ilgili ayın ekstresini aktarın."
        />
      </div>
    )
  }

  // Sayfa araligi icerikten turetilir; bos sayfada "1981-13 / 13" gibi bir metin cikmasin.
  const from = data.content.length > 0 ? data.page * data.size + 1 : 0
  const to = from + data.content.length - 1
  const shownPage = Math.min(data.page + 1, data.totalPages)

  return (
    <div className="card">
      <h2>Dönem özeti ({data.totalElements} personel)</h2>
      <p className="hint">Satıra tıklayınca o personelin hesap adımları aşağıda açılır.</p>

      <div className={fetching ? 'table-scroll is-fetching' : 'table-scroll'}>
        <table>
          <thead>
            <tr>
              {COLUMNS.map((column) => (
                <th
                  key={column.field}
                  className={column.numeric ? 'num sortable' : 'sortable'}
                  aria-sort={
                    column.field === sortField
                      ? sortDir === 'desc'
                        ? 'descending'
                        : 'ascending'
                      : 'none'
                  }
                >
                  <button type="button" onClick={() => toggle(column.field)}>
                    {column.label}
                    <span aria-hidden="true">
                      {column.field === sortField ? (sortDir === 'desc' ? ' ↓' : ' ↑') : ' ↕'}
                    </span>
                  </button>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.content.length === 0 && (
              <tr>
                <td colSpan={COLUMNS.length} className="empty">
                  Bu sayfada kayıt yok.
                </td>
              </tr>
            )}
            {data.content.map((row) => (
              <tr
                key={row.employeeNo}
                className={row.employeeNo === selected ? 'row-selected' : 'row-clickable'}
                onClick={() => onSelect(row.employeeNo)}
              >
                <td>
                  <div className="cell-title">{row.fullName}</div>
                  <div className="cell-sub">{row.employeeNo}</div>
                </td>
                <td>{row.department}</td>
                <td>{row.hotel}</td>
                <td className={amountClass(row.totalSalesBase)}>{formatMoney(row.totalSalesBase)}</td>
                <td className={`${amountClass(row.totalCommission)} strong`}>
                  {formatMoney(row.totalCommission)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="pager">
        <label htmlFor="pageSize">Sayfa boyutu</label>
        <select
          id="pageSize"
          value={data.size}
          onChange={(e) => onSizeChange(Number(e.target.value))}
        >
          {[10, 20, 50].map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>

        <span>
          {data.content.length > 0 ? `${from}–${to}` : '0'} / {data.totalElements}
        </span>

        <button
          type="button"
          onClick={() => onPageChange(data.page - 1)}
          disabled={data.page === 0}
        >
          ← Önceki
        </button>
        <span>
          {shownPage} / {data.totalPages}
        </span>
        <button
          type="button"
          onClick={() => onPageChange(data.page + 1)}
          disabled={data.page + 1 >= data.totalPages}
        >
          Sonraki →
        </button>
      </div>
    </div>
  )
}
