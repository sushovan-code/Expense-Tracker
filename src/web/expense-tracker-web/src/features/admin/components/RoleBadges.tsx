export function RoleBadges({ roles }: { roles: string[] }) {
  if (roles.length === 0) return <span className="cell-muted">No role</span>
  return (
    <span className="chips">
      {roles.map((role) => (
        <span key={role} className={`role-badge role-badge--${role.toLowerCase()}`}>
          {role}
        </span>
      ))}
    </span>
  )
}
