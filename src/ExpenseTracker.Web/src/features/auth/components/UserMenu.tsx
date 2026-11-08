import { useEffect, useId, useRef, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useSession } from '@/shared/auth/useSession'
import { LogOutIcon, UserIcon } from '@/shared/ui/Icons'

const initials = (name: string) =>
  name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]!.toUpperCase()).join('') || '?'

/** Avatar button in the header: who is signed in, their role, and the account / sign-out links. */
export function UserMenu() {
  const { user } = useSession()
  const { pathname } = useLocation()
  // Remember the page the menu was opened on, so navigating anywhere closes it.
  const [openOn, setOpenOn] = useState<string | null>(null)
  const open = openOn === pathname
  const root = useRef<HTMLDivElement>(null)
  const menuId = useId()

  // Close on outside click and Escape.
  useEffect(() => {
    if (!open) return
    const onPointer = (e: PointerEvent) => {
      if (!root.current?.contains(e.target as Node)) setOpenOn(null)
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpenOn(null)
    }
    document.addEventListener('pointerdown', onPointer)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('pointerdown', onPointer)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  if (!user) return null
  const primaryRole = user.roles[0]

  return (
    <div className="user-menu" ref={root}>
      <button
        type="button"
        className="user-menu__trigger"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={menuId}
        onClick={() => setOpenOn(open ? null : pathname)}
        title={`${user.displayName} (${user.email})`}
      >
        <span className="avatar" aria-hidden="true">
          {initials(user.displayName)}
        </span>
        <span className="user-menu__name">{user.displayName}</span>
      </button>

      {open && (
        <div className="user-menu__panel glass" id={menuId} role="menu">
          <div className="user-menu__who">
            <span className="user-menu__display">{user.displayName}</span>
            <span className="user-menu__email">{user.email}</span>
            {primaryRole && <span className={`role-badge role-badge--${primaryRole.toLowerCase()}`}>{primaryRole}</span>}
          </div>
          <Link to="/account" className="user-menu__item" role="menuitem">
            <UserIcon size={16} /> Account &amp; API keys
          </Link>
          <Link to="/logout" className="user-menu__item user-menu__item--danger" role="menuitem">
            <LogOutIcon size={16} /> Sign out
          </Link>
        </div>
      )}
    </div>
  )
}
