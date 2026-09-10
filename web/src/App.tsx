import { useEffect, useState } from 'react'
import { api, type Session } from './api/client'
import { CommissionPage } from './pages/CommissionPage'
import { RulesPage } from './pages/RulesPage'
import type { EmployeeResponse, UserRole } from './types'

const ROLE_LABEL: Record<UserRole, string> = {
  Admin: 'Admin (kuralları yönetir)',
  Accounting: 'Muhasebe (tüm personeli görür)',
  EmployeeResponse: 'Personel (yalnızca kendini görür)',
}

type Tab = 'rules' | 'commission'

export default function App() {
  const [role, setRole] = useState<UserRole>('Admin')
  const [userId, setUserId] = useState('kerem')
  const [employeeNo, setEmployeeNo] = useState('P1001')
  const [employees, setEmployees] = useState<EmployeeResponse[]>([])
  const [tab, setTab] = useState<Tab>('rules')

  const session: Session = {
    userId,
    role,
    employeeNo: role === 'EmployeeResponse' ? employeeNo : null,
  }

  useEffect(() => {
    api
      .employees({ userId, role: 'Admin', employeeNo: null })
      .then(setEmployees)
      .catch(() => setEmployees([]))
  }, [userId])

  return (
    <>
      <header className="app-header">
        <h1>Gloria Hotels &amp; Resorts · Prim Motoru</h1>
        <nav>
          <button className={tab === 'rules' ? 'active' : ''} onClick={() => setTab('rules')}>
            Prim kuralları
          </button>
          <button className={tab === 'commission' ? 'active' : ''} onClick={() => setTab('commission')}>
            Prim detayı
          </button>
        </nav>
      </header>

      <div className="session-bar">
        <div>
          <label htmlFor="userId">Kullanıcı (audit log aktörü)</label>
          <input id="userId" value={userId} onChange={(e) => setUserId(e.target.value)} />
        </div>
        <div style={{ minWidth: 250 }}>
          <label htmlFor="role">Rol</label>
          <select id="role" value={role} onChange={(e) => setRole(e.target.value as UserRole)}>
            {(Object.keys(ROLE_LABEL) as UserRole[]).map((r) => (
              <option key={r} value={r}>
                {ROLE_LABEL[r]}
              </option>
            ))}
          </select>
        </div>
        {role === 'EmployeeResponse' && (
          <div style={{ minWidth: 220 }}>
            <label htmlFor="employeeNo">Personel</label>
            <select
              id="employeeNo"
              value={employeeNo}
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
      </div>

      <main>
        {tab === 'rules' ? (
          <RulesPage session={session} />
        ) : (
          <CommissionPage key={`${role}-${employeeNo}`} session={session} />
        )}
      </main>
    </>
  )
}
