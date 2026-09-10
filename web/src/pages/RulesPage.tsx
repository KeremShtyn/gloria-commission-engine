import { useCallback, useEffect, useState } from 'react'
import { api, formatMoney, formatPercent, type Session } from '../api/client'
import type { CommissionRule, RuleRequest, RuleTier, RuleType } from '../types'

const EMPTY: RuleRequest = {
  code: '',
  name: '',
  ruleType: 'Percentage',
  sourceSystem: null,
  departmentCode: null,
  productGroup: null,
  productCode: null,
  hotel: null,
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

const RULE_TYPE_LABEL: Record<RuleType, string> = {
  Percentage: 'Sabit yüzde',
  Tiered: 'Kademeli barem',
  FixedAmount: 'Sabit tutar',
}

/** Bos string yerine null gonderilir; null "hepsi" anlamina gelir. */
const orNull = (value: string) => (value.trim() === '' ? null : value.trim())

export function RulesPage({ session }: { session: Session }) {
  const [rules, setRules] = useState<CommissionRule[]>([])
  const [groups, setGroups] = useState<string[]>([])
  const [form, setForm] = useState<RuleRequest>(EMPTY)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    try {
      setRules(await api.listRules(session))
      setGroups(await api.productGroups(session))
      setError(null)
    } catch (e) {
      setError((e as Error).message)
    }
  }, [session])

  useEffect(() => {
    void load()
  }, [load])

  const patch = (changes: Partial<RuleRequest>) => setForm((current) => ({ ...current, ...changes }))

  const reset = () => {
    setForm(EMPTY)
    setEditingId(null)
  }

  const startEdit = (rule: CommissionRule) => {
    setEditingId(rule.id)
    setNotice(null)
    setForm({
      code: rule.code,
      name: rule.name,
      ruleType: rule.ruleType,
      sourceSystem: rule.sourceSystem,
      departmentCode: rule.departmentCode,
      productGroup: rule.productGroup,
      productCode: rule.productCode,
      hotel: rule.hotel,
      rate: rule.rate,
      fixedAmount: rule.fixedAmount,
      multiplyByQuantity: rule.multiplyByQuantity,
      tierApplication: rule.tierApplication,
      priority: rule.priority,
      effectiveFrom: rule.effectiveFrom,
      effectiveTo: rule.effectiveTo,
      isActive: rule.isActive,
      tiers: rule.tiers.map((t) => ({ ...t })),
    })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const submit = async () => {
    setBusy(true)
    setError(null)
    setNotice(null)
    try {
      if (editingId === null) {
        await api.createRule(session, form)
        setNotice(`'${form.code}' kuralı eklendi. Hesaplama bir sonraki çalıştırmada bu kuralı kullanır.`)
      } else {
        await api.updateRule(session, editingId, form)
        setNotice(`'${form.code}' kuralı güncellendi.`)
      }
      reset()
      await load()
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  const deactivate = async (rule: CommissionRule) => {
    setError(null)
    try {
      await api.deactivateRule(session, rule.id)
      setNotice(`'${rule.code}' pasife alındı. Geçmiş hesapların izlenebilirliği için kayıt silinmez.`)
      await load()
    } catch (e) {
      setError((e as Error).message)
    }
  }

  const updateTier = (index: number, changes: Partial<RuleTier>) =>
    patch({ tiers: form.tiers.map((t, i) => (i === index ? { ...t, ...changes } : t)) })

  const addTier = () => {
    const last = form.tiers.at(-1)
    patch({
      tiers: [
        ...form.tiers,
        { minAmount: last?.maxAmount ?? 0, maxAmount: null, rate: 0.05 },
      ],
    })
  }

  const canEdit = session.role === 'Admin'

  return (
    <>
      {error && <div className="alert error">{error}</div>}
      {notice && <div className="alert info">{notice}</div>}

      {!canEdit && (
        <div className="alert info">
          Kural yönetimi Admin rolüne açıktır. Şu anki rolünüzle kurallar yalnızca görüntülenebilir.
        </div>
      )}

      <div className="card">
        <h2>{editingId === null ? 'Yeni prim kuralı' : `Kural düzenle: ${form.code}`}</h2>
        <p className="hint">
          Kurallar veritabanında tutulur. Yeni bir prim kalemi eklemek veya oran değiştirmek için
          uygulamanın yeniden derlenmesi gerekmez. Boş bırakılan eşleştirme alanı "hepsi" anlamına gelir.
        </p>

        <div className="grid">
          <div>
            <label htmlFor="code">Kural kodu</label>
            <input
              id="code"
              value={form.code}
              onChange={(e) => patch({ code: e.target.value })}
              placeholder="GOLF-PCT"
            />
          </div>
          <div>
            <label htmlFor="name">Kural adı</label>
            <input
              id="name"
              value={form.name}
              onChange={(e) => patch({ name: e.target.value })}
              placeholder="Golf dersi satışı"
            />
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

        <div className="grid" style={{ marginTop: 12 }}>
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
            <label htmlFor="productGroup">Ürün grubu</label>
            <select
              id="productGroup"
              value={form.productGroup ?? ''}
              onChange={(e) => patch({ productGroup: orNull(e.target.value) })}
            >
              <option value="">Hepsi</option>
              {groups.map((group) => (
                <option key={group} value={group}>
                  {group}
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
          <div>
            <label htmlFor="departmentCode">Departman</label>
            <input
              id="departmentCode"
              value={form.departmentCode ?? ''}
              onChange={(e) => patch({ departmentCode: orNull(e.target.value) })}
              placeholder="Hepsi"
            />
          </div>
          <div>
            <label htmlFor="hotel">Otel</label>
            <input
              id="hotel"
              value={form.hotel ?? ''}
              onChange={(e) => patch({ hotel: orNull(e.target.value) })}
              placeholder="Hepsi"
            />
          </div>
        </div>

        <div className="grid" style={{ marginTop: 12 }}>
          {form.ruleType === 'Percentage' && (
            <div>
              <label htmlFor="rate">Oran (%)</label>
              <input
                id="rate"
                type="number"
                step="0.1"
                value={form.rate == null ? '' : form.rate * 100}
                onChange={(e) =>
                  patch({ rate: e.target.value === '' ? null : Number(e.target.value) / 100 })
                }
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
                  step="1"
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
                  patch({ tierApplication: e.target.value as RuleRequest['tierApplication'] })
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
          <div style={{ marginTop: 18 }}>
            <label>Kademeler (aylık net ciroya göre)</label>
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
                    updateTier(index, {
                      maxAmount: e.target.value === '' ? null : Number(e.target.value),
                    })
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
          </div>
        )}

        <div className="row-actions">
          <button className="primary" onClick={submit} disabled={busy || !canEdit}>
            {editingId === null ? 'Kuralı kaydet' : 'Değişikliği kaydet'}
          </button>
          {editingId !== null && (
            <button onClick={reset} disabled={busy}>
              Vazgeç
            </button>
          )}
        </div>
      </div>

      <div className="card">
        <h2>Tanımlı kurallar ({rules.length})</h2>
        <p className="hint">Aynı satışa birden fazla kural uyarsa yüksek öncelikli, eşitlikte daha spesifik olan uygulanır.</p>

        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Kod</th>
                <th>Ad</th>
                <th>Tip</th>
                <th>Eşleşme</th>
                <th className="num">Değer</th>
                <th className="num">Öncelik</th>
                <th>Durum</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {rules.map((rule) => (
                <tr key={rule.id} className={rule.isActive ? undefined : 'inactive'}>
                  <td>
                    <strong>{rule.code}</strong>
                  </td>
                  <td>{rule.name}</td>
                  <td>{RULE_TYPE_LABEL[rule.ruleType]}</td>
                  <td>
                    {[rule.sourceSystem, rule.productGroup, rule.productCode, rule.departmentCode, rule.hotel]
                      .filter(Boolean)
                      .join(' · ') || 'Tüm satışlar'}
                  </td>
                  <td className="num">
                    {rule.ruleType === 'Percentage' && rule.rate != null && formatPercent(rule.rate)}
                    {rule.ruleType === 'FixedAmount' &&
                      rule.fixedAmount != null &&
                      `${formatMoney(rule.fixedAmount)}${rule.multiplyByQuantity ? ' / adet' : ''}`}
                    {rule.ruleType === 'Tiered' &&
                      rule.tiers.map((t) => formatPercent(t.rate)).join(' / ')}
                  </td>
                  <td className="num">{rule.priority}</td>
                  <td>
                    <span className={rule.isActive ? 'badge' : 'badge muted'}>
                      {rule.isActive ? 'Aktif' : 'Pasif'}
                    </span>
                  </td>
                  <td className="num">
                    <button onClick={() => startEdit(rule)} disabled={!canEdit}>
                      Düzenle
                    </button>{' '}
                    <button className="danger" onClick={() => deactivate(rule)} disabled={!canEdit || !rule.isActive}>
                      Pasife al
                    </button>
                  </td>
                </tr>
              ))}
              {rules.length === 0 && (
                <tr>
                  <td colSpan={8} className="empty">
                    Henüz kural tanımlanmamış.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </>
  )
}
