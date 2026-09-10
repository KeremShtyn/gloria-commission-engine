import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { RequireRole } from './components/RequireRole'
import { SessionProvider } from './context/SessionContext'
import { APP_ROUTES } from './navigation'

export default function App() {
  return (
    <SessionProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            {APP_ROUTES.map((route) => {
              const element = <RequireRole roles={route.roles}>{route.element}</RequireRole>

              return route.path === '/' ? (
                <Route key={route.path} index element={element} />
              ) : (
                <Route key={route.path} path={route.path.slice(1)} element={element} />
              )
            })}

            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </SessionProvider>
  )
}
