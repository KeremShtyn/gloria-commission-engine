import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { ROLE_LABEL } from '../constants'
import { useSession } from '../context/SessionContext'
import { landingFor, routesFor } from '../navigation'
import type { EmployeeResponse, UserRole } from '../types'

export function Layout() {
  const { session, setUserId, setRole, setEmployeeNo } = useSession()
  const [employees, setEmployees] = useState<EmployeeResponse[]>([])
  const navigate = useNavigate()
  const previousRole = useRef(session.role)

  useEffect(() => {
    api
      .employees({ userId: session.userId, role: 'Admin', employeeNo: null })
      .then(setEmployees)
      .catch(() => setEmployees([]))
  }, [session.userId])

  /*
   * Rol degisince o rolun acilis sayfasina donulur. Aksi halde kullanici
   * onceki rolun sayfasinda, onceki rolun verisiyle kalir; yetkisi kalmadiysa
   * da yonlendirme sayfa icinde gerceklesir ve bir an eski icerik gorunur.
   */
  useEffect(() => {
    if (previousRole.current === session.role) return

    previousRole.current = session.role
    navigate(landingFor(session.role), { replace: true })
  }, [session.role, navigate])

  // Menu ile rota korumasi ayni listeden beslenir; ayrisamazlar.
  const visible = routesFor(session.role)

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
          {visible.map((route) => (
            <NavLink
              key={route.path}
              to={route.path}
              end={route.path === '/'}
              className={({ isActive }) => (isActive ? 'active' : undefined)}
            >
              {route.label}
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
        {/*
          Kimlik degisince sayfalar bastan kurulur: onceki rolden kalan yuklenmis veri,
          filtre ve bildirim mesaji temizlenir.
        */}
        <Outlet key={`${session.role}-${session.employeeNo ?? ''}`} />
      </main>
    </div>
  )
}
