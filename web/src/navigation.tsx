import type { ReactElement } from 'react'
import { AuditLogPage } from './pages/AuditLogPage'
import { DashboardPage } from './pages/DashboardPage'
import { ImportsPage } from './pages/ImportsPage'
import { MyCommissionPage } from './pages/MyCommissionPage'
import { PeriodSummaryPage } from './pages/PeriodSummaryPage'
import { PeriodsPage } from './pages/PeriodsPage'
import { RulesPage } from './pages/RulesPage'
import type { UserRole } from './types'

export interface AppRoute {
  path: string
  label: string
  element: ReactElement
  roles: UserRole[]
}

const ALL: UserRole[] = ['Admin', 'Accounting', 'Employee']
const STAFF: UserRole[] = ['Admin', 'Accounting']
const ADMIN: UserRole[] = ['Admin']

/**
 * Menu ve rota korumasinin tek kaynagi. Ikisi ayri listelerden beslenseydi
 * zamanla ayrisir ve menude gorunmeyen bir sayfa URL'den acilabilir hale gelirdi.
 *
 * Buradaki kisit yalnizca gorunurluk icindir; gercek yetki kontrolu sunucuda.
 * Bir sayfada rolun yapabilecegi hicbir sey yoksa sayfa hic gosterilmez:
 * yetkisiz kullaniciya bos ya da tamami pasif bir ekran acmak bilgi sizdirir
 * ve kullaniciyi yaniltir.
 */
export const APP_ROUTES: AppRoute[] = [
  { path: '/', label: 'Genel bakış', element: <DashboardPage />, roles: STAFF },
  { path: '/primim', label: 'Primim', element: <MyCommissionPage />, roles: ALL },
  { path: '/donem-ozeti', label: 'Dönem özeti', element: <PeriodSummaryPage />, roles: STAFF },
  { path: '/kurallar', label: 'Prim kuralları', element: <RulesPage />, roles: STAFF },
  { path: '/aktarim', label: 'Veri aktarımı', element: <ImportsPage />, roles: STAFF },
  { path: '/donemler', label: 'Dönemler', element: <PeriodsPage />, roles: ADMIN },
  { path: '/denetim', label: 'Denetim', element: <AuditLogPage />, roles: STAFF },
]

export const routesFor = (role: UserRole) => APP_ROUTES.filter((route) => route.roles.includes(role))

/** Rolun erisebildigi ilk sayfa; yetkisiz bir adrese gidildiginde buraya yonlendirilir. */
export const landingFor = (role: UserRole) => routesFor(role)[0]?.path ?? '/primim'
