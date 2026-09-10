import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { Session } from '../api/client'
import type { UserRole } from '../types'

interface SessionContextValue {
  session: Session
  setUserId: (userId: string) => void
  setRole: (role: UserRole) => void
  setEmployeeNo: (employeeNo: string) => void

  /** Rol bazlı görünürlük. Asıl yetki kontrolü backend'de; burası yalnızca menüyü sadeleştirir. */
  canSeeAllEmployees: boolean
  isAdmin: boolean
}

const SessionContext = createContext<SessionContextValue | null>(null)

export function SessionProvider({ children }: { children: ReactNode }) {
  const [userId, setUserId] = useState('kerem')
  const [role, setRole] = useState<UserRole>('Admin')
  const [employeeNo, setEmployeeNo] = useState('P1001')

  const value = useMemo<SessionContextValue>(() => {
    const session: Session = {
      userId,
      role,
      employeeNo: role === 'Employee' ? employeeNo : null,
    }

    return {
      session,
      setUserId,
      setRole,
      setEmployeeNo,
      canSeeAllEmployees: role === 'Admin' || role === 'Accounting',
      isAdmin: role === 'Admin',
    }
  }, [userId, role, employeeNo])

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession() {
  const context = useContext(SessionContext)
  if (!context) throw new Error('useSession, SessionProvider içinde çağrılmalı.')
  return context
}

/** Personel rolündeyken seçili personel numarası — prim ekranının varsayılanı. */
export function useSelectedEmployeeNo() {
  const { session } = useSession()
  return session.employeeNo
}
