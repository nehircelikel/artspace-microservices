const IMAGE_URL_RE = /^https?:\/\/\S+$/i

// A bare URL (e.g. an artist's deliverable link) renders as an inline image preview with the
// link beneath; anything else renders as plain text.
export default function MessageContent({ content }) {
  if (IMAGE_URL_RE.test(content.trim())) {
    return (
      <a href={content} target="_blank" rel="noreferrer" style={{ color: 'inherit', textDecoration: 'none', display: 'block' }}>
        <img src={content} alt="shared"
          style={{ maxWidth: '100%', maxHeight: 240, borderRadius: 8, display: 'block', marginBottom: '0.3rem' }}
          onError={e => { e.target.style.display = 'none' }} />
        <span style={{ fontSize: '0.72rem', opacity: 0.85, wordBreak: 'break-all' }}>{content}</span>
      </a>
    )
  }
  return content
}
