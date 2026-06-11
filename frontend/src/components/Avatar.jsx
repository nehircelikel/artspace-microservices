// Circular avatar: shows the profile picture when a URL is given, otherwise
// falls back to the first letter of the username on a branded background. If the
// image fails to load it swaps to the initial fallback.
import { useState } from 'react'

export default function Avatar({ src, username = '', size = 48 }) {
  const [failed, setFailed] = useState(false)
  const initial = (username.trim()[0] || '?').toUpperCase()
  const showImage = src && !failed

  return (
    <span
      style={{
        width: size,
        height: size,
        borderRadius: '50%',
        flexShrink: 0,
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        overflow: 'hidden',
        background: '#5B3FD6',
        color: '#fff',
        fontWeight: 700,
        fontSize: size * 0.42,
        border: '1px solid #DDD6F7',
        userSelect: 'none',
      }}
      aria-label={username || 'avatar'}
    >
      {showImage ? (
        <img
          src={src}
          alt={username}
          onError={() => setFailed(true)}
          style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
        />
      ) : (
        initial
      )}
    </span>
  )
}
