export type UserRole = 'Admin' | 'Accounting' | 'Employee'

export type RuleType = 'Percentage' | 'Tiered' | 'FixedAmount'

export interface LookupResponse {
  id: string
  code: string
  name: string
}

export interface RuleTierResponse {
  id?: string
  minAmount: number
  maxAmount: number | null
  rate: number
}

export interface CommissionRuleResponse {
  id: string
  code: string
  name: string
  ruleType: RuleType
  sourceSystem: string | null
  departmentId: string | null
  productGroupId: string | null
  hotelId: string | null
  productCode: string | null
  departmentCode: string | null
  productGroupCode: string | null
  hotelCode: string | null
  rate: number | null
  fixedAmount: number | null
  multiplyByQuantity: boolean
  tierApplication: 'WholeAmount' | 'Marginal'
  priority: number
  effectiveFrom: string
  effectiveTo: string | null
  isActive: boolean
  tiers: RuleTierResponse[]
}

export type CommissionRuleRequest = Omit<
  CommissionRuleResponse,
  'id' | 'tiers' | 'departmentCode' | 'productGroupCode' | 'hotelCode'
> & { tiers: RuleTierResponse[] }

export interface CommissionStepResponse {
  order: number
  ruleCode: string
  ruleName: string
  ruleType: string
  sourceSystem?: string
  sourceDocumentNo?: string
  productName?: string
  transactionDate?: string
  baseAmount: number
  appliedRate?: number | null
  appliedFixedAmount?: number | null
  quantity: number
  commissionAmount: number
  explanation: string
}

export interface ExcludedSaleResponse {
  sourceSystem: string
  sourceDocumentNo: string
  transactionDate: string
  productCode: string
  productName: string
  amountTry: number
  reasonCode: string
  reason: string
}

export interface CommissionResultResponse {
  period: string
  periodClosed: boolean
  employeeNo: string
  fullName: string
  department: string
  hotel: string
  totalSalesBase: number
  totalCommission: number
  calculatedAtUtc: string
  steps: CommissionStepResponse[]
  excludedSales: ExcludedSaleResponse[]
}

export interface EmployeeCommissionResponse {
  employeeId: string
  employeeNo: string
  fullName: string
  department: string
  hotel: string
  totalSalesBase: number
  totalCommission: number
}

export interface PeriodSummaryResponse {
  period: string
  closed: boolean
  employeeCount: number
  totalSalesBase: number
  totalCommission: number
  employees: EmployeeCommissionResponse[]
}

export interface ReconciliationGroupResponse {
  productGroup: string
  operationalRevenue: number
  accountedRevenue: number
  difference: number
}

export interface ReconciliationResponse {
  period: string
  groups: ReconciliationGroupResponse[]
  unpostedCount: number
  unpostedAmount: number
  unmatchedRefundCount: number
  importErrors: { errorCode: string; count: number }[]
}

export interface EmployeeResponse {
  id: string
  employeeNo: string
  fullName: string
  department: string
  hotel: string
  hireDate: string
  terminationDate: string | null
}

export interface PeriodResponse {
  key: string
  year: number
  month: number
  status: 'Open' | 'Closed'
  closedAtUtc: string | null
  closedBy: string | null
}

export interface ImportBatchResponse {
  id: string
  sourceSystem: string
  fileName: string
  totalRows: number
  importedRows: number
  duplicateRows: number
  failedRows: number
  importedBy: string
  startedAtUtc: string
  completedAtUtc: string | null
}

export interface ImportErrorResponse {
  rowNumber: number
  errorCode: string
  errorMessage: string
  rawLine: string
}

export interface StagingRowResponse {
  rowNumber: number
  sourceSystem: string
  status: string
  rawLine: string
  saleRecordId?: string
  receivedAtUtc: string
  processedAtUtc: string | null
}

export interface ImportSummaryResponse {
  batchId: string
  sourceSystem: string
  fileName: string
  totalRows: number
  importedRows: number
  duplicateRows: number
  failedRows: number
  matchedReversals: number
  errors: ImportErrorResponse[]
}

export interface AuditLogResponse {
  id: string
  entityName: string
  entityId: string
  action: string
  oldValues?: string
  newValues?: string
  changedBy: string
  changedByRole: string
  changedAtUtc: string
}

export interface PagedResponse<T> {
  content: T[]
  page: number
  size: number
  totalElements: number
  totalPages: number
}

export interface ApiError {
  error: {
    code: string
    message: string
    correlationId?: string
    details?: { field: string; message: string }[]
    timestamp: string
    path: string
  }
}
