import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import api from '../api/client'
import StatusBadge from '../components/StatusBadge'

function toDateInput(iso) {
  if (!iso) return ''
  return new Date(iso).toISOString().slice(0, 10)
}

export default function RequestDetailPage() {
  const { id } = useParams()
  const [request, setRequest] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [form, setForm] = useState({ description: '', budget: '', deadline: '', estimatedTime: '', estimatedCost: '' })
  const [savingError, setSavingError] = useState('')
  const [actionError, setActionError] = useState('')
  const [message, setMessage] = useState('')

  const token = localStorage.getItem('token')
  const user = JSON.parse(localStorage.getItem('user') || 'null')

  function hydrate(data) {
    setRequest(data)
    setForm({
      description: data.description ?? '',
      budget: data.budget ?? '',
      deadline: toDateInput(data.deadline),
      estimatedTime: data.estimatedTime ?? '',
      estimatedCost: data.estimatedCost ?? '',
    })
  }

  function load() {
    return api.get(`/api/Request/${id}`).then(r => hydrate(r.data))
  }

  useEffect(() => {
    if (!token) { setLoading(false); return }
    load()
      .catch(err => {
        if (err.response?.status === 404) setError('Request not found.')
        else if (err.response?.status === 403) setError('You do not have access to this request.')
        else setError('Could not load this request.')
      })
      .finally(() => setLoading(false))
  }, [id, token])

  if (!token) {
    return (
      <div style={{ maxWidth: 720, margin: '0 auto' }}>
        <p className="muted">Please <Link to="/login">log in</Link> to view this request.</p>
      </div>
    )
  }
  if (loading) return <p className="loading-text">Loading…</p>
  if (error) return (
    <div>
      <p className="error-text" style={{ marginBottom: '1rem' }}>{error}</p>
      <Link to="/requests">← Back to requests</Link>
    </div>
  )

  const isArtist = user?.id === request.artistId
  const isRequester = user?.id === request.requesterId
  const isPending = request.status === 'Pending'
  const canAccept = isArtist && isPending && request.estimatedCost != null && !!request.estimatedTime

  async function handleSave(e) {
    e.preventDefault()
    setSavingError('')
    const body = isArtist
      ? { description: form.description, estimatedTime: form.estimatedTime || null, estimatedCost: form.estimatedCost === '' ? null : Number(form.estimatedCost) }
      : { budget: form.budget === '' ? null : Number(form.budget), deadline: form.deadline ? new Date(form.deadline).toISOString() : null }
    try {
      await api.put(`/api/Request/${id}`, body)
      await load()
    } catch (err) {
      const msg = err.response?.data
      setSavingError(typeof msg === 'string' ? msg : 'Failed to save changes.')
    }
  }

  async function doAction(action) {
    setActionError('')
    try {
      await api.post(`/api/Request/${id}/${action}`)
      await load()
    } catch (err) {
      const msg = err.response?.data
      setActionError(typeof msg === 'string' ? msg : 'Action failed.')
    }
  }

  async function sendMessage(e) {
    e.preventDefault()
    if (!message.trim()) return
    try {
      await api.post(`/api/Request/${id}/messages`, { content: message })
      setMessage('')
      await load()
    } catch {
      // non-critical; ignore
    }
  }

  const labelStyle = { fontSize: '0.72rem', color: '#6E6785', textTransform: 'uppercase', letterSpacing: '0.04em', marginBottom: '0.2rem' }
  const cardStyle = { background: '#fff', border: '1px solid #DDD6F7', borderRadius: 12, padding: '1.25rem 1.4rem', marginBottom: '1.25rem' }

  return (
    <div style={{ maxWidth: 720, margin: '0 auto' }}>
      <Link to="/requests" style={{ fontSize: '0.875rem', color: '#6E6785' }}>← Back to requests</Link>

      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '1rem', margin: '1rem 0 1.5rem' }}>
        <h1 style={{ fontSize: '1.6rem', margin: 0 }}>{request.title}</h1>
        <StatusBadge status={request.status} />
      </div>

      {/* Parties */}
      <div style={cardStyle}>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1.5rem' }}>
          <div>
            <p style={labelStyle}>Requester</p>
            <p style={{ margin: 0, color: '#1F1B2D' }}>{request.requesterUsername}</p>
            {isArtist && (
              <p className="muted" style={{ margin: '0.15rem 0 0', fontSize: '0.82rem' }}>{request.requesterEmail}</p>
            )}
          </div>
          <div>
            <p style={labelStyle}>Artist</p>
            <p style={{ margin: 0, color: '#1F1B2D' }}>{request.artistUsername}</p>
          </div>
        </div>
      </div>

      {/* Details / editable fields */}
      <form onSubmit={handleSave} style={cardStyle}>
        <div style={{ marginBottom: '1rem' }}>
          <p style={labelStyle}>Description</p>
          {isArtist && isPending ? (
            <textarea
              value={form.description}
              onChange={e => setForm({ ...form, description: e.target.value })}
              rows={3}
              style={{ resize: 'vertical', width: '100%' }}
            />
          ) : (
            <p style={{ margin: 0, color: '#1F1B2D', lineHeight: 1.6 }}>{request.description || <span className="muted">—</span>}</p>
          )}
        </div>

        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1.25rem', marginBottom: '1rem' }}>
          <div style={{ flex: '1 1 140px' }}>
            <p style={labelStyle}>Budget</p>
            {isRequester && isPending ? (
              <input type="number" min="0" step="0.01" value={form.budget}
                onChange={e => setForm({ ...form, budget: e.target.value })} placeholder="USD" />
            ) : (
              <p style={{ margin: 0 }}>{request.budget != null ? `$${request.budget}` : <span className="muted">—</span>}</p>
            )}
          </div>
          <div style={{ flex: '1 1 140px' }}>
            <p style={labelStyle}>Deadline</p>
            {isRequester && isPending ? (
              <input type="date" value={form.deadline}
                onChange={e => setForm({ ...form, deadline: e.target.value })} />
            ) : (
              <p style={{ margin: 0 }}>{request.deadline ? new Date(request.deadline).toLocaleDateString() : <span className="muted">—</span>}</p>
            )}
          </div>
        </div>

        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1.25rem' }}>
          <div style={{ flex: '1 1 140px' }}>
            <p style={labelStyle}>Estimated time</p>
            {isArtist && isPending ? (
              <input type="text" value={form.estimatedTime}
                onChange={e => setForm({ ...form, estimatedTime: e.target.value })} placeholder="e.g. 2 weeks" />
            ) : (
              <p style={{ margin: 0 }}>{request.estimatedTime || <span className="muted">—</span>}</p>
            )}
          </div>
          <div style={{ flex: '1 1 140px' }}>
            <p style={labelStyle}>Estimated cost</p>
            {isArtist && isPending ? (
              <input type="number" min="0" step="0.01" value={form.estimatedCost}
                onChange={e => setForm({ ...form, estimatedCost: e.target.value })} placeholder="USD" />
            ) : (
              <p style={{ margin: 0 }}>{request.estimatedCost != null ? `$${request.estimatedCost}` : <span className="muted">—</span>}</p>
            )}
          </div>
        </div>

        {isPending && (isArtist || isRequester) && (
          <div style={{ marginTop: '1.1rem' }}>
            {savingError && <p className="error-text">{savingError}</p>}
            <button type="submit" className="btn-secondary">Save changes</button>
            {isArtist && (
              <p className="muted" style={{ fontSize: '0.78rem', marginTop: '0.5rem' }}>
                Set an estimated cost and time before accepting.
              </p>
            )}
          </div>
        )}
      </form>

      {/* Status actions */}
      {(isArtist || isRequester) && isPending && (
        <div style={{ ...cardStyle, display: 'flex', gap: '0.6rem', flexWrap: 'wrap', alignItems: 'center' }}>
          {isArtist && (
            <>
              <button className="btn-primary" disabled={!canAccept} onClick={() => doAction('accept')}>Accept</button>
              <button className="btn-secondary" onClick={() => doAction('decline')}>Decline</button>
            </>
          )}
          {isRequester && (
            <button className="btn-secondary" onClick={() => doAction('withdraw')}>Withdraw request</button>
          )}
          {actionError && <p className="error-text" style={{ margin: 0 }}>{actionError}</p>}
        </div>
      )}
      {isArtist && request.status === 'Accepted' && (
        <div style={{ ...cardStyle, display: 'flex', gap: '0.6rem', alignItems: 'center' }}>
          <button className="btn-primary" onClick={() => doAction('complete')}>Mark completed</button>
          {actionError && <p className="error-text" style={{ margin: 0 }}>{actionError}</p>}
        </div>
      )}

      {/* Messaging */}
      <div style={cardStyle}>
        <h2 style={{ fontSize: '1.05rem', margin: '0 0 0.9rem', color: '#1F1B2D' }}>Messages</h2>
        {request.messages.length === 0 ? (
          <p className="muted" style={{ marginBottom: '1rem' }}>No messages yet. Start the conversation below.</p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.6rem', marginBottom: '1rem' }}>
            {request.messages.map(m => {
              const mine = m.senderId === user?.id
              return (
                <div key={m.id} style={{ alignSelf: mine ? 'flex-end' : 'flex-start', maxWidth: '80%' }}>
                  <div style={{
                    background: mine ? '#5B3FD6' : '#F3EEFF',
                    color: mine ? '#fff' : '#1F1B2D',
                    borderRadius: 10,
                    padding: '0.55rem 0.8rem',
                    fontSize: '0.88rem',
                    lineHeight: 1.45,
                  }}>
                    {m.content}
                  </div>
                  <p style={{ fontSize: '0.7rem', color: '#6E6785', margin: '0.2rem 0 0', textAlign: mine ? 'right' : 'left' }}>
                    {m.senderUsername} · {new Date(m.createdAt).toLocaleString()}
                  </p>
                </div>
              )
            })}
          </div>
        )}
        <form onSubmit={sendMessage} style={{ display: 'flex', gap: '0.6rem' }}>
          <input
            type="text"
            value={message}
            onChange={e => setMessage(e.target.value)}
            placeholder="Write a message…"
            style={{ flex: 1 }}
          />
          <button type="submit" className="btn-primary" style={{ flexShrink: 0 }}>Send</button>
        </form>
      </div>

      {/* Activity log */}
      <div style={cardStyle}>
        <h2 style={{ fontSize: '1.05rem', margin: '0 0 0.9rem', color: '#1F1B2D' }}>Activity log</h2>
        {request.logs.length === 0 ? (
          <p className="muted">No activity yet.</p>
        ) : (
          <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
            {request.logs.map(l => (
              <li key={l.id} style={{ fontSize: '0.85rem', color: '#1F1B2D', display: 'flex', justifyContent: 'space-between', gap: '1rem' }}>
                <span>{l.action}</span>
                <span className="muted" style={{ fontSize: '0.75rem', whiteSpace: 'nowrap' }}>
                  {new Date(l.createdAt).toLocaleString()}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
