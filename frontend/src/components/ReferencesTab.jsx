import ReferenceItem from './ReferenceItem'

export default function ReferencesTab({ references, loading, username }) {
  if (loading) return <p className="loading-text">Loading reference arts…</p>

  if (references.length === 0) {
    return (
      <div className="empty-state">
        <p className="empty-title">{username} has no completed commissions yet.</p>
        <p className="empty-sub">Accepted commission deliveries appear here as reference arts.</p>
      </div>
    )
  }

  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '1.1rem' }}>
      {references.map(r => (
        <ReferenceItem key={r.id} reference={r} showArtist={false} />
      ))}
    </div>
  )
}
