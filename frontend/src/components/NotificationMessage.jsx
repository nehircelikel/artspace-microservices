import { Link } from 'react-router-dom'

const linkStyle = { color: '#5B3FD6', fontWeight: 600, textDecoration: 'none' }

// Renders a notification message with clickable references woven in:
//   • the actor's name (first occurrence of `actorUsername`) → their profile
//   • the affected object, quoted in the message as "Title", → its page
//     (currently only LinkType "request" → /requests/:linkId)
// Anything we can't resolve falls back to plain text, so a message without
// link metadata renders exactly as before.
export default function NotificationMessage({ message, actorUsername, linkType, linkId }) {
  const objectHref = linkType === 'request' && linkId ? `/requests/${linkId}` : null

  // Build an ordered list of [start, end, element] spans to linkify, then stitch
  // the message together around them. We only link the first match of each so we
  // never double-wrap or mangle overlapping ranges.
  const spans = []

  if (actorUsername) {
    const i = message.indexOf(actorUsername)
    if (i !== -1) {
      spans.push({
        start: i,
        end: i + actorUsername.length,
        el: (
          <Link to={`/users/${encodeURIComponent(actorUsername)}`} style={linkStyle}>
            {actorUsername}
          </Link>
        ),
      })
    }
  }

  if (objectHref) {
    // The object title is the text inside the first pair of double quotes.
    const open = message.indexOf('"')
    const close = open !== -1 ? message.indexOf('"', open + 1) : -1
    if (open !== -1 && close !== -1) {
      spans.push({
        start: open,
        end: close + 1,
        el: (
          <Link to={objectHref} style={linkStyle}>
            {message.slice(open, close + 1)}
          </Link>
        ),
      })
    }
  }

  if (spans.length === 0) return <>{message}</>

  spans.sort((a, b) => a.start - b.start)

  const parts = []
  let cursor = 0
  spans.forEach((s, idx) => {
    if (s.start < cursor) return // skip any overlap
    if (s.start > cursor) parts.push(message.slice(cursor, s.start))
    parts.push(<span key={idx}>{s.el}</span>)
    cursor = s.end
  })
  if (cursor < message.length) parts.push(message.slice(cursor))

  return <>{parts}</>
}
