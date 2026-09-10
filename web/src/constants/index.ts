import type { RuleType, UserRole } from '../types'

export const ROLE_LABEL: Record<UserRole, string> = {
  Admin: 'Admin — kuralları yönetir',
  Accounting: 'Muhasebe — tüm personeli görür',
  Employee: 'Personel — yalnızca kendini görür',
}

export const RULE_TYPE_LABEL: Record<RuleType, string> = {
  Percentage: 'Sabit yüzde',
  Tiered: 'Kademeli barem',
  FixedAmount: 'Sabit tutar',
}

/** Prim dışı bırakılma nedenlerinin okunur karşılıkları. */
export const EXCLUSION_REASON_LABEL: Record<string, string> = {
  NO_MATCHING_RULE: 'Kural tanımlı değil',
  OUTSIDE_EMPLOYMENT: 'İstihdam dışı',
  NOT_COMMISSIONABLE: 'Prime esas değil',
  REVERSED_PAIR: 'İade ile netleşti',
}

export const MONTHS = Array.from({ length: 12 }, (_, i) => i + 1)
