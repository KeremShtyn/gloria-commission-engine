import { useEffect, useRef, useState } from 'react'
import { ROLE_LABEL } from '../constants'
import { useSession } from '../context/SessionContext'
import type { EmployeeResponse, UserRole } from '../types'

/**
 * Kullanici ve rol secimi. Case'de gercek kimlik dogrulama olmadigi icin
 * bunlar ekrandan degistirilebiliyor; sayfanin ustunde surekli durmalari yerine
 * gercek uygulamalardaki kullanici menusune benzer bir yerde toplandi.
 */
export function UserMenu({ employees }: { employees: EmployeeResponse[] }) {
  const { session, setUserId, setRole, setEmployeeNo } = useSession()
  const [open, setOpen] = useState(false)
  const wrapper = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return

    const onPointerDown = (event: MouseEvent) => {
      if (!wrapper.current?.contains(event.target as Node)) setOpen(false)
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }

    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)

    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  const roleShort: Record<UserRole, string> = {
    Admin: 'Admin',
    Accounting: 'Muhasebe',
    Employee: 'Personel',
  }

  return (
    <div className="topbar-wrap" ref={wrapper}>
      <button
        type="button"
        className="user-chip"
        onClick={() => setOpen((o) => !o)}
        aria-expanded={open}
        aria-haspopup="true"
      >
        <span className="avatar">{session.userId.slice(0, 1)}</span>
        <span className="user-chip-text">
          <strong>{session.userId}</strong>
          <span>{roleShort[session.role]}</span>
        </span>
      </button>

      {open && (
        <div className="user-menu" role="menu">
          <div>
            <label htmlFor="session-user">Kullanıcı</label>
            <input id="session-user" value={session.userId} onChange={(e) => setUserId(e.target.value)} />
          </div>

          <div>
            <label htmlFor="session-role">Rol</label>
            <select
              id="session-role"
              value={session.role}
              onChange={(e) => setRole(e.target.value as UserRole)}
            >
              {(Object.keys(ROLE_LABEL) as UserRole[]).map((role) => (
                <option key={role} value={role}>
                  {ROLE_LABEL[role]}
                </option>
              ))}
            </select>
          </div>

          {session.role === 'Employee' && (
            <div>
              <label htmlFor="session-employee">Personel</label>
              <select
                id="session-employee"
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

          <p className="user-menu-note">
            Kimlik HTTP başlığından okunur; yetki kontrolü sunucuda yapılır. Rol değiştiğinde
            o rolün açılış sayfasına dönülür.
          </p>
        </div>
      )}
    </div>
  )
}
