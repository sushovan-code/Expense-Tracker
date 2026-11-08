import { useCallback, useEffect, useLayoutEffect, useMemo, useState, type ReactNode } from 'react'
import {
  DARK_QUERY,
  THEME_STORAGE_KEY,
  ThemeContext,
  type ResolvedTheme,
  type ThemeMode,
} from '@/shared/theme/theme'
import { logger } from '@/shared/telemetry'

function readStoredMode(): ThemeMode {
  try {
    const value = localStorage.getItem(THEME_STORAGE_KEY)
    if (value === 'light' || value === 'dark') return value
  } catch {
    // Storage can be unavailable (private mode, blocked site data).
  }
  return 'system'
}

function getSystemTheme(): ResolvedTheme {
  return window.matchMedia(DARK_QUERY).matches ? 'dark' : 'light'
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setModeState] = useState<ThemeMode>(readStoredMode)
  const [systemTheme, setSystemTheme] = useState<ResolvedTheme>(getSystemTheme)

  useEffect(() => {
    const query = window.matchMedia(DARK_QUERY)
    const onChange = () => setSystemTheme(query.matches ? 'dark' : 'light')
    query.addEventListener('change', onChange)
    return () => query.removeEventListener('change', onChange)
  }, [])

  const resolvedTheme = mode === 'system' ? systemTheme : mode

  // Layout effect so the attribute is applied in the same commit; the theme toggle's
  // view transition snapshots the DOM right after that commit.
  useLayoutEffect(() => {
    document.documentElement.dataset.theme = resolvedTheme
  }, [resolvedTheme])

  const setMode = useCallback((next: ThemeMode) => {
    logger.event('app.theme_changed', `Theme changed to ${next}`, { 'app.theme.mode': next })
    setModeState(next)
    try {
      if (next === 'system') localStorage.removeItem(THEME_STORAGE_KEY)
      else localStorage.setItem(THEME_STORAGE_KEY, next)
    } catch {
      // Ignore; the choice still applies for this session.
    }
  }, [])

  const value = useMemo(() => ({ mode, resolvedTheme, setMode }), [mode, resolvedTheme, setMode])

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}
