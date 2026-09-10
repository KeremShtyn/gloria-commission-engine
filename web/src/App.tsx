import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { RequireRole } from './components/RequireRole'
import { SessionProvider, useSession } from './context/SessionContext'
import { ThemeProvider } from './context/ThemeContext'
import { APP_ROUTES, landingFor } from './navigation'

/** Kok adres role gore acilis sayfasina gider; her rolun gordugu ilk sayfa ayni degil. */
function LandingRedirect() {
  const { session } = useSession()
  return <Navigate to={landingFor(session.role)} replace />
}

export default function App() {
  return (
    <ThemeProvider>
      <SessionProvider>
        <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<LandingRedirect />} />

            {APP_ROUTES.map((route) => (
              <Route
                key={route.path}
                path={route.path.slice(1)}
                element={<RequireRole roles={route.roles}>{route.element}</RequireRole>}
              />
            ))}

            <Route path="*" element={<LandingRedirect />} />
          </Route>
          </Routes>
        </BrowserRouter>
      </SessionProvider>
    </ThemeProvider>
  )
}
