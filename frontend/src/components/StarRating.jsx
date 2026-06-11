// Renders a 0–5 star rating with fractional fill (e.g. 4.2 → four full stars
// plus one 40%-filled star). The fill is achieved by overlaying a clipped gold
// star row on top of a grey base row.
export default function StarRating({ value = 0, count, size = 16, showValue = true }) {
  const clamped = Math.max(0, Math.min(5, Number(value) || 0))
  const fillPercent = (clamped / 5) * 100
  const stars = '★★★★★'

  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}>
      <span
        style={{
          position: 'relative',
          display: 'inline-block',
          fontSize: size,
          lineHeight: 1,
          letterSpacing: '0.05em',
        }}
        aria-label={`${clamped.toFixed(1)} out of 5 stars`}
      >
        {/* Base (empty) layer */}
        <span style={{ color: '#DDD6F7' }}>{stars}</span>
        {/* Filled layer, clipped to the rating percentage */}
        <span
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: `${fillPercent}%`,
            overflow: 'hidden',
            whiteSpace: 'nowrap',
            color: '#F5A623',
          }}
        >
          {stars}
        </span>
      </span>
      {showValue && (
        <span style={{ fontSize: '0.8rem', color: '#6E6785' }}>
          {clamped.toFixed(1)}
          {count != null && ` (${count})`}
        </span>
      )}
    </span>
  )
}
