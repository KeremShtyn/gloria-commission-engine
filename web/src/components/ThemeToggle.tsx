import { useTheme } from '../context/ThemeContext'

export function ThemeToggle() {
  const { theme, toggle } = useTheme()

  return (
    <button
      type="button"
      className="ghost"
      onClick={toggle}
      title={theme === 'light' ? 'Koyu temaya geç' : 'Açık temaya geç'}
      aria-label={theme === 'light' ? 'Koyu temaya geç' : 'Açık temaya geç'}
    >
      {theme === 'light' ? '🌙' : '☀️'}
    </button>
  )
}
