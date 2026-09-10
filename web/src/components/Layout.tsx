import { useEffect, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { api } from '../api/client'
import { ROLE_LABEL } from '../constants'
import { useSession } from '../context/SessionContext'
import type { EmployeeResponse, UserRole } from '../types'

interface NavItem {
  to: string
  label: string
  /** Menude gorunmesi icin gereken yetki; bos ise herkese acik. */
  requires?: 'admin' | 'allEmployees'
}

const NAV_ITEMS: NavItem[] = [
  { to: '/', label: 'Genel bakış', requires: 'allEmployees' },
  { to: '/primim', label: 'Primim' },
  { to: '/donem-ozeti', label: 'Dönem özeti', requires: 'allEmployees' },
  { to: '/kurallar', label: 'Prim kuralları' },
  { to: '/aktarim', label: 'Veri aktarımı', requires: 'allEmployees' },
  { to: '/donemler', label: 'Dönemler' },
  { to: '/denetim', label: 'Denetim', requires: 'allEmployees' },
]

export function Layout() {
  const { session, setUserId, setRole, setEmployeeNo, canSeeAllEmployees, isAdmin } = useSession()
  const [employees, setEmployees] = useState<EmployeeResponse[]>([])

  useEffect(() => {
    api
      .employees({ userId: session.userId, role: 'Admin', employeeNo: null })
      .then(setEmployees)
      .catch(() => setEmployees([]))
  }, [session.userId])

  const visible = NAV_ITEMS.filter((item) => {
    if (item.requires === 'admin') return isAdmin
    if (item.requires === 'allEmployees') return canSeeAllEmployees
    return true
  })

  return (
    <div className="app">
      <header className="app-header">
        <div className="brand">
          <span className="brand-mark">G</span>
          <div>
            <strong>Gloria Hotels &amp; Resorts</strong>
            <span>Prim Motoru</span>
          </div>
        </div>

        <nav>
          {visible.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) => (isActive ? 'active' : undefined)}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </header>

      <div className="session-bar">
        <div>
          <label htmlFor="userId">Kullanıcı</label>
          <input id="userId" value={session.userId} onChange={(e) => setUserId(e.target.value)} />
        </div>

        <div style={{ minWidth: 260 }}>
          <label htmlFor="role">Rol</label>
          <select id="role" value={session.role} onChange={(e) => setRole(e.target.value as UserRole)}>
            {(Object.keys(ROLE_LABEL) as UserRole[]).map((role) => (
              <option key={role} value={role}>
                {ROLE_LABEL[role]}
              </option>
            ))}
          </select>
        </div>

        {session.role === 'Employee' && (
          <div style={{ minWidth: 240 }}>
            <label htmlFor="employeeNo">Personel</label>
            <select
              id="employeeNo"
              value={session.employeeNo ?? ''}
              onChange={(e) => setEmployeeNo(e.target.value)}
            >
              {employees.map((employee) => (
                <option key={employee.employeeNo} value={employee.employeeNo}>
                  {employee.employeeNo} — {employee.fullName}
                </option>
              ))}
            </select>
          </div>
        )}

        <p className="session-hint">
          Kimlik HTTP başlığından okunur; yetki kontrolü sunucuda yapılır.
        </p>
      </div>

      <main>
        <Outlet />
      </main>
    </div>
  )
}
