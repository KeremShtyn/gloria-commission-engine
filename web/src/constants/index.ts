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

export const SOURCE_LABEL: Record<string, string> = {
  Pms: 'PMS (Fidelio)',
  Pos: 'POS (Flyby)',
  Erp: 'ERP (Oracle JDE)',
}

/** Prim dışı bırakılma nedenlerinin okunur karşılıkları. */
export const EXCLUSION_REASON_LABEL: Record<string, string> = {
  NO_MATCHING_RULE: 'Kural tanımlı değil',
  OUTSIDE_EMPLOYMENT: 'İstihdam dışı',
  NOT_COMMISSIONABLE: 'Prime esas değil',
  REVERSED_PAIR: 'İade ile netleşti',
}

/** Ham satırın aktarım sonucu. */
export const STAGING_STATUS_LABEL: Record<string, string> = {
  Pending: 'Bekliyor',
  Processed: 'Aktarıldı',
  Failed: 'Hatalı',
  Duplicate: 'Mükerrer',
}

export const MONTHS = Array.from({ length: 12 }, (_, i) => i + 1)
