import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import { RuleFormModal } from '../components/RuleFormModal'
import { RULE_TYPE_LABEL } from '../constants'
import { useSession } from '../context/SessionContext'
import type { CommissionRuleRequest, CommissionRuleResponse, LookupResponse } from '../types'
import { formatMoney, formatPercent } from '../utils/formatters'

type Editing = { rule: CommissionRuleResponse | null } | null

/** Kapsam alanlarini ayri rozetlere boler; nokta ile birlestirilmis metin okunmuyordu. */
const scopeChips = (rule: CommissionRuleResponse) =>
  [
    rule.sourceSystem,
    rule.productGroupCode,
    rule.productCode,
    rule.departmentCode,
    rule.hotelCode,
  ].filter((value): value is string => Boolean(value))

export function RulesPage() {
  const { session, isAdmin } = useSession()

  const [rules, setRules] = useState<CommissionRuleResponse[]>([])
  const [departments, setDepartments] = useState<LookupResponse[]>([])
  const [hotels, setHotels] = useState<LookupResponse[]>([])
  const [groups, setGroups] = useState<LookupResponse[]>([])

  const [editing, setEditing] = useState<Editing>(null)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const [r, d, h, g] = await Promise.all([
        api.listRules(session),
        api.departments(session),
        api.hotels(session),
        api.productGroups(session),
      ])
      setRules(r)
      setDepartments(d)
      setHotels(h)
      setGroups(g)
      setError(null)
    } catch (e) {
      setError((e as Error).message)
    }
  }, [session])

  useEffect(() => {
    void load()
  }, [load])

  const submit = async (body: CommissionRuleRequest) => {
    const existing = editing?.rule

    if (existing) {
      await api.updateRule(session, existing.id, body)
      setNotice(`'${body.code}' kuralı güncellendi.`)
    } else {
      await api.createRule(session, body)
      setNotice(`'${body.code}' kuralı eklendi. Sonraki hesaplamada devreye girer.`)
    }

    setEditing(null)
    await load()
  }

  const deactivate = async (rule: CommissionRuleResponse) => {
    setError(null)
    try {
      await api.deactivateRule(session, rule.id)
      setNotice(`'${rule.code}' pasife alındı. Geçmiş hesapların izlenebilirliği için silinmez.`)
      await load()
    } catch (e) {
      setError((e as Error).message)
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Prim kuralları</h1>
          <p>
            Kurallar veritabanında tutulur; yeni kalem eklemek veya oran değiştirmek için
            uygulamanın yeniden derlenmesi gerekmez.
          </p>
        </div>
        <button className="primary" onClick={() => setEditing({ rule: null })} disabled={!isAdmin}>
          + Yeni kural
        </button>
      </div>

      {error && <div className="alert error">{error}</div>}
      {notice && <div className="alert info">{notice}</div>}
      {!isAdmin && (
        <div className="alert info">
          Kural yönetimi Admin rolüne açıktır. Mevcut rolünüzle kurallar yalnızca görüntülenebilir.
        </div>
      )}

      <div className="card">
        <h2>Tanımlı kurallar ({rules.length})</h2>
        <p className="hint">
          Aynı satışa birden fazla kural uyarsa yüksek öncelikli, eşitlikte daha spesifik olan uygulanır.
        </p>

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
                  <td className="cell-title">{rule.code}</td>
                  <td>{rule.name}</td>
                  <td>{RULE_TYPE_LABEL[rule.ruleType]}</td>
                  <td>
                    {scopeChips(rule).length > 0 ? (
                      <div className="chip-row">
                        {scopeChips(rule).map((chip) => (
                          <span key={chip} className="badge muted">
                            {chip}
                          </span>
                        ))}
                      </div>
                    ) : (
                      <span className="muted">Tüm satışlar</span>
                    )}
                  </td>
                  <td className="num strong">
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
                  <td className="num row-actions-cell">
                    <button onClick={() => setEditing({ rule })}>
                      {isAdmin ? 'Düzenle' : 'Detay'}
                    </button>
                    <button
                      className="danger"
                      onClick={() => deactivate(rule)}
                      disabled={!isAdmin || !rule.isActive}
                    >
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

      {editing && (
        <RuleFormModal
          rule={editing.rule}
          departments={departments}
          hotels={hotels}
          groups={groups}
          readOnly={!isAdmin}
          onClose={() => setEditing(null)}
          onSubmit={submit}
        />
      )}
    </>
  )
}
