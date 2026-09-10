import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useSession } from '../context/SessionContext'
import { APP_ROUTES, landingFor, routesFor } from '../navigation'
import type { EmployeeResponse } from '../types'
import { ThemeToggle } from './ThemeToggle'
import { UserMenu } from './UserMenu'

export function Layout() {
  const { session } = useSession()
  const [employees, setEmployees] = useState<EmployeeResponse[]>([])
  const navigate = useNavigate()
  const location = useLocation()
  const previousRole = useRef(session.role)

  useEffect(() => {
    api
      .employees({ userId: session.userId, role: 'Admin', employeeNo: null })
      .then(setEmployees)
      .catch(() => setEmployees([]))
  }, [session.userId])

  /*
   * Rol degisince o rolun acilis sayfasina donulur. Aksi halde kullanici onceki
   * rolun sayfasinda, onceki rolun verisiyle kalir; yetkisi kalmadiysa yonlendirme
   * sayfa icinde gerceklesir ve bir an eski icerik gorunur.
   */
  useEffect(() => {
    if (previousRole.current === session.role) return

    previousRole.current = session.role
    navigate(landingFor(session.role), { replace: true })
  }, [session.role, navigate])

  // Menu ile rota korumasi ayni listeden beslenir; ayrisamazlar.
  const visible = routesFor(session.role)
  const current = APP_ROUTES.find((route) => location.pathname.startsWith(route.path))

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <span className="brand-mark">G</span>
          <div>
            <strong>Gloria Hotels</strong>
            <span>Prim Motoru</span>
          </div>
        </div>

        <nav className="sidebar-nav">
          {visible.map((route) => (
            <NavLink
              key={route.path}
              to={route.path}
              className={({ isActive }) => (isActive ? 'active' : undefined)}
            >
              {route.label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-foot">
          Prim kuralları veritabanında tutulur.
          <br />
          Yeni kalem eklemek kod değişikliği gerektirmez.
        </div>
      </aside>

      <div className="content-column">
        <header className="topbar">
          <span className="topbar-title">{current?.label ?? 'Gloria Prim Motoru'}</span>

          <div className="topbar-actions">
            <ThemeToggle />
            <UserMenu employees={employees} />
          </div>
        </header>

        <main>
          {/*
            Kimlik degisince sayfalar bastan kurulur: onceki rolden kalan yuklenmis
            veri, filtre ve bildirim mesaji temizlenir.
          */}
          <Outlet key={`${session.role}-${session.employeeNo ?? ''}`} />
        </main>
      </div>
    </div>
  )
}
