import { useState } from 'react'
import { Modal } from './Modal'
import { RULE_TYPE_LABEL } from '../constants'
import { formatMoney, formatPercent } from '../utils/formatters'
import type {
  CommissionRuleRequest,
  CommissionRuleResponse,
  LookupResponse,
  RuleTierResponse,
  RuleType,
} from '../types'

const EMPTY: CommissionRuleRequest = {
  code: '',
  name: '',
  ruleType: 'Percentage',
  sourceSystem: null,
  departmentId: null,
  productGroupId: null,
  hotelId: null,
  productCode: null,
  rate: null,
  fixedAmount: null,
  multiplyByQuantity: false,
  tierApplication: 'WholeAmount',
  priority: 10,
  effectiveFrom: '2026-01-01',
  effectiveTo: null,
  isActive: true,
  tiers: [],
}

/** Bos string yerine null gonderilir; null "hepsi" anlamina gelir. */
const orNull = (value: string) => (value.trim() === '' ? null : value.trim())

export function toRequest(rule: CommissionRuleResponse): CommissionRuleRequest {
  return {
    code: rule.code,
    name: rule.name,
    ruleType: rule.ruleType,
    sourceSystem: rule.sourceSystem,
    departmentId: rule.departmentId,
    productGroupId: rule.productGroupId,
    hotelId: rule.hotelId,
    productCode: rule.productCode,
    rate: rule.rate,
    fixedAmount: rule.fixedAmount,
    multiplyByQuantity: rule.multiplyByQuantity,
    tierApplication: rule.tierApplication,
    priority: rule.priority,
    effectiveFrom: rule.effectiveFrom,
    effectiveTo: rule.effectiveTo,
    isActive: rule.isActive,
    tiers: rule.tiers.map((t) => ({ ...t })),
  }
}

interface RuleFormModalProps {
  rule: CommissionRuleResponse | null
  departments: LookupResponse[]
  hotels: LookupResponse[]
  groups: LookupResponse[]
  readOnly: boolean
  onClose: () => void
  onSubmit: (body: CommissionRuleRequest) => Promise<void>
}

export function RuleFormModal({
  rule,
  departments,
  hotels,
  groups,
  readOnly,
  onClose,
  onSubmit,
}: RuleFormModalProps) {
  const [form, setForm] = useState<CommissionRuleRequest>(rule ? toRequest(rule) : EMPTY)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const patch = (changes: Partial<CommissionRuleRequest>) =>
    setForm((current) => ({ ...current, ...changes }))

  const updateTier = (index: number, changes: Partial<RuleTierResponse>) =>
    patch({ tiers: form.tiers.map((t, i) => (i === index ? { ...t, ...changes } : t)) })

  const addTier = () =>
    patch({
      tiers: [
        ...form.tiers,
        { minAmount: form.tiers.at(-1)?.maxAmount ?? 0, maxAmount: null, rate: 0.05 },
      ],
    })

  const submit = async () => {
    setBusy(true)
    setError(null)
    try {
      await onSubmit(form)
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal
      title={rule ? `Kural: ${rule.code}` : 'Yeni prim kuralı'}
      subtitle={
        readOnly
          ? 'Kural yönetimi Admin rolüne açıktır; bu görünüm salt okunur.'
          : 'Boş bırakılan eşleştirme alanı "hepsi" anlamına gelir.'
      }
      onClose={onClose}
      footer={
        <>
          <button onClick={onClose} disabled={busy}>
            {readOnly ? 'Kapat' : 'Vazgeç'}
          </button>
          {!readOnly && (
            <button className="primary" onClick={submit} disabled={busy}>
              {busy ? 'Kaydediliyor…' : rule ? 'Değişikliği kaydet' : 'Kuralı kaydet'}
            </button>
          )}
        </>
      }
    >
      {error && <div className="alert error">{error}</div>}

      {rule && (
        <div className="detail-strip">
          <div>
            <span>Durum</span>
            <strong>{rule.isActive ? 'Aktif' : 'Pasif'}</strong>
          </div>
          <div>
            <span>Değer</span>
            <strong>
              {rule.ruleType === 'Percentage' && rule.rate != null && formatPercent(rule.rate)}
              {rule.ruleType === 'FixedAmount' &&
                rule.fixedAmount != null &&
                `${formatMoney(rule.fixedAmount)}${rule.multiplyByQuantity ? ' / adet' : ''}`}
              {rule.ruleType === 'Tiered' && rule.tiers.map((t) => formatPercent(t.rate)).join(' / ')}
            </strong>
          </div>
          <div>
            <span>Eşleşme</span>
            <strong>
              {[rule.sourceSystem, rule.productGroupCode, rule.productCode, rule.departmentCode, rule.hotelCode]
                .filter(Boolean)
                .join(' · ') || 'Tüm satışlar'}
            </strong>
          </div>
        </div>
      )}

      <fieldset disabled={readOnly}>
        <div className="grid">
          <div>
            <label htmlFor="code">Kural kodu</label>
            <input id="code" value={form.code} onChange={(e) => patch({ code: e.target.value })} placeholder="GOLF-PCT" />
          </div>
          <div>
            <label htmlFor="name">Kural adı</label>
            <input id="name" value={form.name} onChange={(e) => patch({ name: e.target.value })} placeholder="Golf dersi satışı" />
          </div>
          <div>
            <label htmlFor="ruleType">Hesaplama tipi</label>
            <select
              id="ruleType"
              value={form.ruleType}
              onChange={(e) => patch({ ruleType: e.target.value as RuleType })}
            >
              {(Object.keys(RULE_TYPE_LABEL) as RuleType[]).map((type) => (
                <option key={type} value={type}>
                  {RULE_TYPE_LABEL[type]}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="priority">Öncelik</label>
            <input
              id="priority"
              type="number"
              value={form.priority}
              onChange={(e) => patch({ priority: Number(e.target.value) })}
            />
          </div>
        </div>

        <h3>Eşleştirme</h3>
        <div className="grid">
          <div>
            <label htmlFor="sourceSystem">Kaynak sistem</label>
            <select
              id="sourceSystem"
              value={form.sourceSystem ?? ''}
              onChange={(e) => patch({ sourceSystem: orNull(e.target.value) })}
            >
              <option value="">Hepsi</option>
              <option value="Pms">PMS (Fidelio)</option>
              <option value="Pos">POS (Flyby)</option>
              <option value="Erp">ERP (Oracle JDE)</option>
            </select>
          </div>
          <div>
            <label htmlFor="productGroupId">Ürün grubu</label>
            <select
              id="productGroupId"
              value={form.productGroupId ?? ''}
              onChange={(e) => patch({ productGroupId: orNull(e.target.value) })}
            >
              <option value="">Hepsi</option>
              {groups.map((group) => (
                <option key={group.id} value={group.id}>
                  {group.code}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="departmentId">Departman</label>
            <select
              id="departmentId"
              value={form.departmentId ?? ''}
              onChange={(e) => patch({ departmentId: orNull(e.target.value) })}
            >
              <option value="">Hepsi</option>
              {departments.map((department) => (
                <option key={department.id} value={department.id}>
                  {department.code}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="hotelId">Otel</label>
            <select
              id="hotelId"
              value={form.hotelId ?? ''}
              onChange={(e) => patch({ hotelId: orNull(e.target.value) })}
            >
              <option value="">Hepsi</option>
              {hotels.map((hotel) => (
                <option key={hotel.id} value={hotel.id}>
                  {hotel.code} — {hotel.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="productCode">Ürün kodu</label>
            <input
              id="productCode"
              value={form.productCode ?? ''}
              onChange={(e) => patch({ productCode: orNull(e.target.value) })}
              placeholder="Hepsi"
            />
          </div>
        </div>

        <h3>Hesaplama</h3>
        <div className="grid">
          {form.ruleType === 'Percentage' && (
            <div>
              <label htmlFor="rate">Oran (%)</label>
              <input
                id="rate"
                type="number"
                step="0.1"
                value={form.rate == null ? '' : form.rate * 100}
                onChange={(e) => patch({ rate: e.target.value === '' ? null : Number(e.target.value) / 100 })}
                placeholder="6"
              />
            </div>
          )}

          {form.ruleType === 'FixedAmount' && (
            <>
              <div>
                <label htmlFor="fixedAmount">Tutar (TRY)</label>
                <input
                  id="fixedAmount"
                  type="number"
                  value={form.fixedAmount ?? ''}
                  onChange={(e) =>
                    patch({ fixedAmount: e.target.value === '' ? null : Number(e.target.value) })
                  }
                  placeholder="50"
                />
              </div>
              <div className="checkbox-line">
                <input
                  id="multiplyByQuantity"
                  type="checkbox"
                  checked={form.multiplyByQuantity}
                  onChange={(e) => patch({ multiplyByQuantity: e.target.checked })}
                />
                <label htmlFor="multiplyByQuantity">Adetle çarp</label>
              </div>
            </>
          )}

          {form.ruleType === 'Tiered' && (
            <div>
              <label htmlFor="tierApplication">Barem uygulaması</label>
              <select
                id="tierApplication"
                value={form.tierApplication}
                onChange={(e) =>
                  patch({ tierApplication: e.target.value as CommissionRuleRequest['tierApplication'] })
                }
              >
                <option value="WholeAmount">Hedef aşılırsa oran tüm ciroya</option>
                <option value="Marginal">Dilimli (her kademe kendi aralığına)</option>
              </select>
            </div>
          )}

          <div>
            <label htmlFor="effectiveFrom">Yürürlük başlangıcı</label>
            <input
              id="effectiveFrom"
              type="date"
              value={form.effectiveFrom}
              onChange={(e) => patch({ effectiveFrom: e.target.value })}
            />
          </div>
          <div>
            <label htmlFor="effectiveTo">Yürürlük bitişi</label>
            <input
              id="effectiveTo"
              type="date"
              value={form.effectiveTo ?? ''}
              onChange={(e) => patch({ effectiveTo: orNull(e.target.value) })}
            />
          </div>
        </div>

        {form.ruleType === 'Tiered' && (
          <>
            <h3>Kademeler</h3>
            <p className="hint">Aylık net ciroya göre uygulanır. Kademeler arasında boşluk bırakılamaz.</p>

            {form.tiers.map((tier, index) => (
              <div className="tier-row" key={index}>
                <input
                  type="number"
                  value={tier.minAmount}
                  onChange={(e) => updateTier(index, { minAmount: Number(e.target.value) })}
                  placeholder="Alt sınır"
                />
                <input
                  type="number"
                  value={tier.maxAmount ?? ''}
                  onChange={(e) =>
                    updateTier(index, { maxAmount: e.target.value === '' ? null : Number(e.target.value) })
                  }
                  placeholder="Üst sınır (boş = sınırsız)"
                />
                <input
                  type="number"
                  step="0.1"
                  value={tier.rate * 100}
                  onChange={(e) => updateTier(index, { rate: Number(e.target.value) / 100 })}
                  placeholder="Oran %"
                />
                <button
                  type="button"
                  className="danger"
                  onClick={() => patch({ tiers: form.tiers.filter((_, i) => i !== index) })}
                >
                  Sil
                </button>
              </div>
            ))}

            <button type="button" onClick={addTier}>
              + Kademe ekle
            </button>
          </>
        )}
      </fieldset>
    </Modal>
  )
}
