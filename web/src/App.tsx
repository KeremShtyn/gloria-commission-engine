import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { SessionProvider } from './context/SessionContext'
import { AuditLogPage } from './pages/AuditLogPage'
import { DashboardPage } from './pages/DashboardPage'
import { ImportsPage } from './pages/ImportsPage'
import { MyCommissionPage } from './pages/MyCommissionPage'
import { PeriodSummaryPage } from './pages/PeriodSummaryPage'
import { PeriodsPage } from './pages/PeriodsPage'
import { RulesPage } from './pages/RulesPage'

export default function App() {
  return (
    <SessionProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<DashboardPage />} />
            <Route path="primim" element={<MyCommissionPage />} />
            <Route path="donem-ozeti" element={<PeriodSummaryPage />} />
            <Route path="kurallar" element={<RulesPage />} />
            <Route path="aktarim" element={<ImportsPage />} />
            <Route path="donemler" element={<PeriodsPage />} />
            <Route path="denetim" element={<AuditLogPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </SessionProvider>
  )
}
