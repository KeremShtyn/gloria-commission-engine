export type UserRole = 'Admin' | 'Accounting' | 'Employee'

export type RuleType = 'Percentage' | 'Tiered' | 'FixedAmount'

export interface RuleTier {
  id?: number
  minAmount: number
  maxAmount: number | null
  rate: number
}

export interface CommissionRule {
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
  tiers: RuleTier[]
}

export type RuleRequest = Omit<CommissionRule, 'id' | 'tiers'> & { tiers: RuleTier[] }

export interface CommissionStep {
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

export interface ExcludedSale {
  sourceSystem: string
  sourceDocumentNo: string
  transactionDate: string
  productCode: string
  productName: string
  amountTry: number
  reasonCode: string
  reason: string
}

export interface CommissionResult {
  period: string
  periodClosed: boolean
  employeeNo: string
  fullName: string
  department: string
  hotel: string
  totalSalesBase: number
  totalCommission: number
  calculatedAtUtc: string
  steps: CommissionStep[]
  excludedSales: ExcludedSale[]
}

export interface Employee {
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
