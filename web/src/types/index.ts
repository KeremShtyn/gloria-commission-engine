export type UserRole = 'Admin' | 'Accounting' | 'EmployeeResponse'

export type RuleType = 'Percentage' | 'Tiered' | 'FixedAmount'

export interface RuleTierResponse {
  id?: number
  minAmount: number
  maxAmount: number | null
  rate: number
}

export interface CommissionRuleResponse {
  id: number
  code: string
  name: string
  ruleType: RuleType
  sourceSystem: string | null
  departmentCode: string | null
  productGroup: string | null
  productCode: string | null
  hotel: string | null
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

export type CommissionRuleRequest = Omit<CommissionRuleResponse, 'id' | 'tiers'> & { tiers: RuleTierResponse[] }

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
    timestamp: string
    path: string
  }
}
