import ArtworkItem from './ArtworkItem'

export default function ArtworksTab({ gallery, loading, page, setPage, username }) {
  if (loading) return <p className="loading-text">Loading artworks…</p>

  if (gallery.items.length === 0) {
    return (
      <div className="empty-state">
        <p className="empty-title">{username} hasn't shared any artworks yet.</p>
        <p className="empty-sub">Check back later for new works.</p>
      </div>
    )
  }

  return (
    <div>
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
        gap: '1.1rem',
      }}>
        {gallery.items.map(art => (
          <ArtworkItem key={art.id} art={art} />
        ))}
      </div>

      {/* Pagination */}
      {gallery.totalPages > 1 && (
        <div style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: '1rem',
          marginTop: '1.75rem',
        }}>
          <button
            className="btn-secondary"
            disabled={page <= 1}
            onClick={() => setPage(p => Math.max(1, p - 1))}
            style={{ opacity: page <= 1 ? 0.5 : 1 }}
          >
            ← Prev
          </button>
          <span className="muted" style={{ fontSize: '0.85rem' }}>
            Page {page} of {gallery.totalPages}
          </span>
          <button
            className="btn-secondary"
            disabled={page >= gallery.totalPages}
            onClick={() => setPage(p => Math.min(gallery.totalPages, p + 1))}
            style={{ opacity: page >= gallery.totalPages ? 0.5 : 1 }}
          >
            Next →
          </button>
        </div>
      )}
    </div>
  )
}
