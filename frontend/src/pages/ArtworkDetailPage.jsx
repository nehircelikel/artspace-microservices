import { useEffect, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import api from '../api/client'

export default function ArtworkDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [artwork, setArtwork] = useState(null)
  const [comments, setComments] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [newComment, setNewComment] = useState({ content: '', rating: 5 })
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')

  // Like state
  const [likeCount, setLikeCount] = useState(0)
  const [isLiked, setIsLiked] = useState(false)
  const [likeBusy, setLikeBusy] = useState(false)

  // Commission state
  const [showCommission, setShowCommission] = useState(false)
  const [commission, setCommission] = useState({ title: '', description: '', budget: '' })
  const [commissionBusy, setCommissionBusy] = useState(false)
  const [commissionError, setCommissionError] = useState('')
  const [commissionMsg, setCommissionMsg] = useState('')

  // Delete state
  const [deleteBusy, setDeleteBusy] = useState(false)

  const token = localStorage.getItem('token')
  const user = JSON.parse(localStorage.getItem('user') || 'null')

  function loadComments() {
    return api.get(`/api/Comment/artwork/${id}`).then(r => setComments(r.data))
  }

  function loadLikes() {
    // Public count
    api.get(`/api/Like/artwork/${id}/count`)
      .then(r => setLikeCount(r.data.totalLikes))
      .catch(() => {})
    // Whether current user liked it (only if logged in)
    if (token) {
      api.get(`/api/Like/artwork/${id}/status`)
        .then(r => { setIsLiked(r.data.isLiked); setLikeCount(r.data.totalLikes) })
        .catch(() => {})
    }
  }

  useEffect(() => {
    Promise.all([
      api.get(`/api/Artwork/${id}`),
      api.get(`/api/Comment/artwork/${id}`),
    ])
      .then(([artRes, commRes]) => {
        setArtwork(artRes.data)
        setComments(commRes.data)
      })
      .catch(err => {
        setError(err.response?.status === 404 ? 'Artwork not found.' : 'Could not load artwork.')
      })
      .finally(() => setLoading(false))
    loadLikes()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  async function handleToggleLike() {
    if (!token) return
    setLikeBusy(true)
    try {
      const r = await api.post(`/api/Like/artwork/${id}`)
      setIsLiked(r.data.isLiked)
      setLikeCount(r.data.totalLikes)
    } catch {
      // ignore
    } finally {
      setLikeBusy(false)
    }
  }

  async function handleDelete() {
    if (!window.confirm('Are you sure you want to delete this artwork? This action cannot be undone.')) return
    setDeleteBusy(true)
    try {
      await api.delete(`/api/Artwork/${id}`)
      navigate('/artworks')
    } catch (err) {
      alert('Failed to delete artwork.')
      setDeleteBusy(false)
    }
  }

  async function handleCommentSubmit(e) {
    e.preventDefault()
    setSubmitError('')
    setSubmitting(true)
    try {
      await api.post('/api/Comment', {
        content: newComment.content,
        rating: Number(newComment.rating),
        artworkId: id,
        artistId: artwork?.artistId,
      })
      await loadComments()
      setNewComment({ content: '', rating: 5 })
    } catch (err) {
      const msg = err.response?.data
      setSubmitError(typeof msg === 'string' ? msg : 'Failed to post comment.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleCommissionSubmit(e) {
    e.preventDefault()
    setCommissionError('')
    setCommissionMsg('')
    setCommissionBusy(true)
    try {
      await api.post('/api/Commission', {
        artistId: artwork?.artistId,
        title: commission.title,
        description: commission.description,
        budget: commission.budget ? Number(commission.budget) : null,
      })
      setCommission({ title: '', description: '', budget: '' })
      setShowCommission(false)
      setCommissionMsg('Your commission request has been sent to the artist.')
    } catch (err) {
      const msg = err.response?.data
      setCommissionError(typeof msg === 'string' ? msg : 'Failed to send commission request.')
    } finally {
      setCommissionBusy(false)
    }
  }

  if (loading) return <p className="loading-text">Loading…</p>
  if (error) return (
    <div>
      <p className="error-text" style={{ marginBottom: '1rem' }}>{error}</p>
      <Link to="/artworks">← Back to artworks</Link>
    </div>
  )

  // Can the logged-in user commission this artist? (not their own work)
  const canCommission = token && user && artwork?.artistId && user.id !== artwork.artistId

  // Is the logged-in user the owner of this artwork? (can delete)
  const isOwner = token && user && artwork?.artistId && user.id === artwork.artistId

  return (
    <div style={{ maxWidth: 740, margin: '0 auto' }}>
      <Link to="/artworks" style={{ fontSize: '0.875rem', color: '#6E6785' }}>
        ← Back to artworks
      </Link>

      <div style={{ marginTop: '1.4rem', marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.75rem', marginBottom: '0.3rem' }}>{artwork.title}</h1>
        <p style={{ color: '#6E6785', fontSize: '0.875rem' }}>
          by <strong style={{ color: '#1F1B2D' }}>{artwork.artistUsername}</strong>
          {artwork.category && <>{' · '}{artwork.category}</>}
          {' · '}{new Date(artwork.createdAt).toLocaleDateString()}
        </p>
      </div>

      {artwork.imageUrl && (
        <img
          src={artwork.imageUrl}
          alt={artwork.title}
          style={{
            maxWidth: '100%',
            maxHeight: 440,
            borderRadius: 12,
            marginBottom: '1.5rem',
            objectFit: 'cover',
            display: 'block',
            border: '1px solid #DDD6F7',
          }}
          onError={e => { e.target.style.display = 'none' }}
        />
      )}

      {/* Like + Commission action bar */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem', flexWrap: 'wrap' }}>
        <button
          onClick={handleToggleLike}
          disabled={!token || likeBusy}
          className="btn-secondary"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.4rem',
            cursor: token ? 'pointer' : 'not-allowed',
            color: isLiked ? '#E0245E' : '#6E6785',
            borderColor: isLiked ? '#E0245E' : undefined,
          }}
          title={token ? 'Like / save this artwork' : 'Log in to like'}
        >
          <span style={{ fontSize: '1.05rem' }}>{isLiked ? '♥' : '♡'}</span>
          <span>{likeCount}</span>
        </button>

        {canCommission && (
          <button
            className="btn-primary"
            onClick={() => { setShowCommission(s => !s); setCommissionMsg('') }}
          >
            {showCommission ? 'Cancel' : 'Request Commission'}
          </button>
        )}

        {isOwner && (
          <button
            onClick={handleDelete}
            disabled={deleteBusy}
            className="btn-secondary"
            style={{ color: '#E0245E', borderColor: '#E0245E', marginLeft: 'auto' }}
          >
            {deleteBusy ? 'Deleting…' : 'Delete Artwork'}
          </button>
        )}
      </div>

      {/* Commission success banner */}
      {commissionMsg && (
        <p className="success-text" style={{ marginBottom: '1.5rem' }}>{commissionMsg}</p>
      )}

      {/* Commission form */}
      {canCommission && showCommission && (
        <div style={{
          background: '#fff',
          border: '1px solid #DDD6F7',
          borderRadius: 12,
          padding: '1.5rem 1.75rem',
          marginBottom: '2rem',
          boxShadow: '0 2px 20px rgba(91, 63, 214, 0.09)',
        }}>
          <h3 style={{ fontSize: '1rem', margin: '0 0 1.1rem', color: '#1F1B2D' }}>
            Request a commission from {artwork.artistUsername}
          </h3>
          <form onSubmit={handleCommissionSubmit} className="form-stack">
            <div>
              <label className="field-label">Title</label>
              <input
                type="text"
                value={commission.title}
                onChange={e => setCommission({ ...commission, title: e.target.value })}
                placeholder="e.g. Portrait of my dog"
                required
                autoFocus
              />
            </div>
            <div>
              <label className="field-label">Description</label>
              <textarea
                value={commission.description}
                onChange={e => setCommission({ ...commission, description: e.target.value })}
                rows={3}
                placeholder="Describe what you would like the artist to create…"
                style={{ resize: 'vertical' }}
              />
            </div>
            <div>
              <label className="field-label">Budget <span className="muted">(optional, ₺)</span></label>
              <input
                type="number"
                min="0"
                value={commission.budget}
                onChange={e => setCommission({ ...commission, budget: e.target.value })}
                placeholder="e.g. 500"
                style={{ width: 'auto' }}
              />
            </div>
            {commissionError && <p className="error-text">{commissionError}</p>}
            <button type="submit" disabled={commissionBusy} className="btn-primary">
              {commissionBusy ? 'Sending…' : 'Send Request'}
            </button>
          </form>
        </div>
      )}

      {artwork.description && (
        <p style={{ marginBottom: '2rem', lineHeight: 1.7, color: '#1F1B2D', fontSize: '0.95rem' }}>
          {artwork.description}
        </p>
      )}

      <h2 style={{
        fontSize: '1.1rem',
        marginBottom: '1.1rem',
        paddingTop: '1rem',
        borderTop: '1px solid #DDD6F7',
        color: '#1F1B2D',
      }}>
        Comments{comments.length > 0 && (
          <span style={{ fontWeight: 400, color: '#6E6785', marginLeft: '0.4rem' }}>
            ({comments.length})
          </span>
        )}
      </h2>

      {comments.length === 0 ? (
        <p className="muted" style={{ marginBottom: '1.75rem' }}>No comments yet. Be the first!</p>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.7rem', marginBottom: '2rem' }}>
          {comments.map(c => (
            <div key={c.id} style={{
              background: '#fff',
              border: '1px solid #DDD6F7',
              borderRadius: 10,
              padding: '0.9rem 1.1rem',
            }}>
              <div style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '0.35rem',
              }}>
                <strong style={{ fontSize: '0.875rem', color: '#1F1B2D' }}>{c.username}</strong>
                <span style={{ color: '#8B6CFF', fontSize: '0.85rem', letterSpacing: '0.04em' }}>
                  {'★'.repeat(c.rating)}{'☆'.repeat(5 - c.rating)}
                </span>
              </div>
              <p style={{ color: '#1F1B2D', fontSize: '0.9rem', lineHeight: 1.55 }}>{c.content}</p>
              <p style={{ fontSize: '0.75rem', color: '#6E6785', marginTop: '0.4rem' }}>
                {new Date(c.createdAt).toLocaleString()}
              </p>
            </div>
          ))}
        </div>
      )}

      {token ? (
        <div style={{ borderTop: '1px solid #DDD6F7', paddingTop: '1.75rem' }}>
          <h3 style={{ fontSize: '1rem', marginBottom: '1.1rem', color: '#1F1B2D' }}>Leave a comment</h3>
          <form onSubmit={handleCommentSubmit} className="form-stack" style={{ maxWidth: 520 }}>
            <div>
              <label className="field-label">Comment</label>
              <textarea
                value={newComment.content}
                onChange={e => setNewComment({ ...newComment, content: e.target.value })}
                rows={3}
                placeholder="Share your thoughts…"
                required
                style={{ resize: 'vertical' }}
              />
            </div>
            <div>
              <label className="field-label">Rating</label>
              <select
                value={newComment.rating}
                onChange={e => setNewComment({ ...newComment, rating: e.target.value })}
                style={{ width: 'auto' }}
              >
                {[1, 2, 3, 4, 5].map(n => (
                  <option key={n} value={n}>{n} star{n > 1 ? 's' : ''}</option>
                ))}
              </select>
            </div>
            {submitError && <p className="error-text">{submitError}</p>}
            <div>
              <button type="submit" disabled={submitting} className="btn-primary">
                {submitting ? 'Posting…' : 'Post Comment'}
              </button>
            </div>
          </form>
        </div>
      ) : (
        <p className="muted" style={{ borderTop: '1px solid #DDD6F7', paddingTop: '1.1rem' }}>
          <Link to="/login">Log in</Link> to leave a comment.
        </p>
      )}
    </div>
  )
}
