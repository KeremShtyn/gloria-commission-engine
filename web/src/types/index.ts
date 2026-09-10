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

export interface EmployeeResponse {
  id: string
  employeeNo: string
  fullName: string
  department: string
  hotel: string
  hireDate: string
  terminationDate: string | null
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
