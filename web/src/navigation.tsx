import type { ReactElement } from 'react'
import { EmployeeCommissionPage } from './pages/EmployeeCommissionPage'
import { MyCommissionPage } from './pages/MyCommissionPage'
import { RulesPage } from './pages/RulesPage'
import type { UserRole } from './types'

export interface AppRoute {
  path: string
  label: string
  element: ReactElement
  roles: UserRole[]
  /** Menude yer almaz; yalnizca baska bir sayfadan acilir (personel detayi gibi). */
  hidden?: boolean
}

const ALL: UserRole[] = ['Admin', 'Accounting', 'Employee']
const STAFF: UserRole[] = ['Admin', 'Accounting']

/**
 * Menu ve rota korumasinin tek kaynagi. Ikisi ayri listelerden beslenseydi
 * zamanla ayrisir ve menude gorunmeyen bir sayfa URL'den acilabilir hale gelirdi.
 *
 * Buradaki kisit yalnizca gorunurluk icindir; gercek yetki kontrolu sunucuda.
 * Bir sayfada rolun yapabilecegi hicbir sey yoksa sayfa hic gosterilmez:
 * yetkisiz kullaniciya bos ya da tamami pasif bir ekran acmak bilgi sizdirir
 * ve kullaniciyi yaniltir.
 *
 * Iki ekran var: kural yonetimi ve personelin kendi primi. Aktarim, donem kapatma
 * ve denetim kaydi API ucları olarak duruyor; ekrani istenmedigi icin yazilmadi.
 */
export const APP_ROUTES: AppRoute[] = [
  { path: '/kurallar', label: 'Prim kuralları', element: <RulesPage />, roles: STAFF },
  { path: '/primim', label: 'Primim', element: <MyCommissionPage />, roles: ALL },
  {
    // Personel rolu baskasinin hesabini goremez; kendi detayini /primim uzerinde gorur.
    path: '/primim/:employeeNo',
    label: 'Personel prim detayı',
    element: <EmployeeCommissionPage />,
    roles: STAFF,
    hidden: true,
  },
]

export const routesFor = (role: UserRole) => APP_ROUTES.filter((route) => route.roles.includes(role))

/** Sol menude gosterilecek sayfalar; detay gibi ic sayfalar menuye girmez. */
export const menuFor = (role: UserRole) => routesFor(role).filter((route) => !route.hidden)

/** Rolun erisebildigi ilk sayfa; yetkisiz bir adrese gidildiginde buraya yonlendirilir. */
export const landingFor = (role: UserRole) => menuFor(role)[0]?.path ?? '/primim'
