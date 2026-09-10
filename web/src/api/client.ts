import type {
  ApiError,
  AuditLogResponse,
  CommissionResultResponse,
  CommissionRuleRequest,
  CommissionRuleResponse,
  EmployeeResponse,
  ImportBatchResponse,
  ImportErrorResponse,
  ImportSummaryResponse,
  LookupResponse,
  PagedResponse,
  PeriodResponse,
  PeriodSummaryResponse,
  ReconciliationResponse,
  StagingRowResponse,
  UserRole,
} from '../types'

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
    if (body?.error?.message) {
      const details = body.error.details?.map((d) => `${d.field}: ${d.message}`).join(' · ')
      message = details ? `${body.error.message} — ${details}` : body.error.message
    }
  } catch {
    // Gövdesiz hata cevaplari icin varsayilan mesaj kalir.
  }
  throw new Error(message)
}

const get = <T>(session: Session, path: string) =>
  fetch(`${BASE_URL}${path}`, { headers: headers(session) }).then(handle<T>)

export const api = {
  // --- Kurallar ---
  listRules: (session: Session) => get<CommissionRuleResponse[]>(session, '/api/v1/commission-rules'),

  createRule: (session: Session, body: CommissionRuleRequest) =>
    fetch(`${BASE_URL}/api/v1/commission-rules`, {
      method: 'POST',
      headers: headers(session, true),
      body: JSON.stringify(body),
    }).then(handle<CommissionRuleResponse>),

  updateRule: (session: Session, id: string, body: CommissionRuleRequest) =>
    fetch(`${BASE_URL}/api/v1/commission-rules/${id}`, {
      method: 'PUT',
      headers: headers(session, true),
      body: JSON.stringify(body),
    }).then(handle<CommissionRuleResponse>),

  deactivateRule: (session: Session, id: string) =>
    fetch(`${BASE_URL}/api/v1/commission-rules/${id}`, {
      method: 'DELETE',
      headers: headers(session),
    }).then(handle<void>),

  // --- Prim ---
  commission: (session: Session, year: number, month: number, employeeNo: string) =>
    get<CommissionResultResponse>(
      session,
      `/api/v1/commissions/${year}/${month}/employees/${employeeNo}`,
    ),

  periodSummary: (session: Session, year: number, month: number) =>
    get<PeriodSummaryResponse>(session, `/api/v1/commissions/${year}/${month}`),

  reconciliation: (session: Session, year: number, month: number) =>
    get<ReconciliationResponse>(session, `/api/v1/commissions/${year}/${month}/reconciliation`),

  // --- Donem ---
  periods: (session: Session) => get<PeriodResponse[]>(session, '/api/v1/periods'),

  closePeriod: (session: Session, year: number, month: number) =>
    fetch(`${BASE_URL}/api/v1/periods/${year}/${month}/close`, {
      method: 'POST',
      headers: headers(session),
    }).then(handle<PeriodResponse>),

  reopenPeriod: (session: Session, year: number, month: number) =>
    fetch(`${BASE_URL}/api/v1/periods/${year}/${month}/reopen`, {
      method: 'POST',
      headers: headers(session),
    }).then(handle<PeriodResponse>),

  // --- Aktarim ---
  importBatches: (session: Session) => get<ImportBatchResponse[]>(session, '/api/v1/imports'),

  importErrors: (session: Session, batchId: string) =>
    get<ImportErrorResponse[]>(session, `/api/v1/imports/${batchId}/errors`),

  stagingRows: (session: Session, batchId: string) =>
    get<StagingRowResponse[]>(session, `/api/v1/imports/${batchId}/staging`),

  uploadImport: (session: Session, source: string, file: File) => {
    const form = new FormData()
    form.append('file', file)

    return fetch(`${BASE_URL}/api/v1/imports/${source}`, {
      method: 'POST',
      headers: headers(session),
      body: form,
    }).then(handle<ImportSummaryResponse>)
  },

  // --- Denetim ---
  auditLogs: (session: Session, page: number, size: number, entityName?: string) => {
    const query = new URLSearchParams({ page: String(page), size: String(size) })
    if (entityName) query.set('entityName', entityName)
    return get<PagedResponse<AuditLogResponse>>(session, `/api/v1/audit-logs?${query}`)
  },

  // --- Referans ---
  employees: (session: Session) => get<EmployeeResponse[]>(session, '/api/v1/employees'),
  departments: (session: Session) => get<LookupResponse[]>(session, '/api/v1/departments'),
  hotels: (session: Session) => get<LookupResponse[]>(session, '/api/v1/hotels'),
  productGroups: (session: Session) => get<LookupResponse[]>(session, '/api/v1/product-groups'),
}
