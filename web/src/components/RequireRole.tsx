import { Navigate } from 'react-router-dom'
import { useSession } from '../context/SessionContext'
import { landingFor } from '../navigation'
import type { UserRole } from '../types'

/**
 * Rotayi role gore korur. Menuden gizlemek yetmez: adres cubuguna elle yazan
 * kullanici da sayfayi gormemeli. Yonlendirme, rolun erisebildigi ilk sayfaya yapilir.
 */
export function RequireRole({ roles, children }: { roles: UserRole[]; children: React.ReactNode }) {
  const { session } = useSession()

  if (!roles.includes(session.role)) {
    return <Navigate to={landingFor(session.role)} replace />
  }

  return <>{children}</>
}
