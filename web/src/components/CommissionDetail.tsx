import { EXCLUSION_REASON_LABEL } from '../constants'
import type { CommissionResultResponse } from '../types'
import { amountClass, formatDateTime, formatMoney, formatPercent } from '../utils/formatters'

/**
 * Bir personelin bir donemdeki hesabi: ozet kartlari, adim adim hesaplama ve
 * prim disi birakilan kayitlar.
 *
 * Hem kendi primini goren personel sayfasi hem de listeden acilan personel detay
 * sayfasi ayni govdeyi kullanir; iki yerde kopyalansa zamanla ayrisirdi.
 */
export function CommissionDetail({ result }: { result: CommissionResultResponse }) {
  return (
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
  )
}
