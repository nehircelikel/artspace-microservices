// Colour-coded pill for an artwork request status.
const STYLES = {
  Pending: { bg: '#FEF3C7', fg: '#92400E', border: '#FCD34D' },
  Accepted: { bg: '#DCFCE7', fg: '#166534', border: '#86EFAC' },
  Declined: { bg: '#FEE2E2', fg: '#991B1B', border: '#FCA5A5' },
  Completed: { bg: '#EDE8FB', fg: '#5B3FD6', border: '#C4B5FD' },
  Withdrawn: { bg: '#F3F4F6', fg: '#4B5563', border: '#D1D5DB' },
}

export default function StatusBadge({ status }) {
  const s = STYLES[status] || STYLES.Withdrawn
  return (
    <span style={{
      background: s.bg,
      color: s.fg,
      border: `1px solid ${s.border}`,
      borderRadius: 99,
      fontSize: '0.72rem',
      fontWeight: 700,
      padding: '0.18rem 0.65rem',
      letterSpacing: '0.02em',
      whiteSpace: 'nowrap',
      flexShrink: 0,
    }}>
      {status}
    </span>
  )
}
