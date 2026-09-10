import type { ApiError, CommissionResult, CommissionRule, Employee, RuleRequest, UserRole } from '../types'

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5199'

export interface Session {
  userId: string
  role: UserRole
  employeeNo: string | null
}

/**
 * Kimlik dogrulama yerine rol basliklari gonderilir (case kapsaminda yeterli).
 * Gercek sistemde burasi Authorization: Bearer ile degistirilir, cagiran kod aynen kalir.
 */
function headers(session: Session, json = false): HeadersInit {
  const result: Record<string, string> = {
    'X-User-Id': session.userId,
    'X-User-Role': session.role,
  }
  if (session.employeeNo) result['X-Employee-No'] = session.employeeNo
  if (json) result['Content-Type'] = 'application/json'
  return result
}

async function handle<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
  }

  let message = `İstek başarısız (${response.status})`
  try {
    const body = (await response.json()) as ApiError
    if (body?.error?.message) message = body.error.message
  } catch {
    // Gövdesiz hata cevaplari icin varsayilan mesaj kalir.
  }
  throw new Error(message)
}

export const api = {
  listRules: (session: Session) =>
    fetch(`${BASE_URL}/api/v1/commission-rules`, { headers: headers(session) }).then(
      handle<CommissionRule[]>,
    ),

  createRule: (session: Session, body: RuleRequest) =>
    fetch(`${BASE_URL}/api/v1/commission-rules`, {
      method: 'POST',
      headers: headers(session, true),
      body: JSON.stringify(body),
    }).then(handle<CommissionRule>),

  updateRule: (session: Session, id: number, body: RuleRequest) =>
    fetch(`${BASE_URL}/api/v1/commission-rules/${id}`, {
      method: 'PUT',
      headers: headers(session, true),
      body: JSON.stringify(body),
    }).then(handle<CommissionRule>),

  deactivateRule: (session: Session, id: number) =>
    fetch(`${BASE_URL}/api/v1/commission-rules/${id}`, {
      method: 'DELETE',
      headers: headers(session),
    }).then(handle<void>),

  commission: (session: Session, year: number, month: number, employeeNo: string) =>
    fetch(`${BASE_URL}/api/v1/commissions/${year}/${month}/employees/${employeeNo}`, {
      headers: headers(session),
    }).then(handle<CommissionResult>),

  employees: (session: Session) =>
    fetch(`${BASE_URL}/api/v1/employees`, { headers: headers(session) }).then(handle<Employee[]>),

  productGroups: (session: Session) =>
    fetch(`${BASE_URL}/api/v1/product-groups`, { headers: headers(session) }).then(handle<string[]>),
}

export const formatMoney = (value: number) =>
  new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(value)

export const formatPercent = (rate: number) =>
  new Intl.NumberFormat('tr-TR', { style: 'percent', maximumFractionDigits: 2 }).format(rate)
