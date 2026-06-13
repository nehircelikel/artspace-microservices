import { Link } from 'react-router-dom'
import StarRating from './StarRating'
const ArtworkItem = ({ art, navigate, rating, showDesc, showRating }) =>
  <Link
    to={`/artworks/${art.id}`}
    style={{ textDecoration: 'none', color: 'inherit' }}
  >
    <div className="card">
      <div className="top" style={{
        backgroundImage: "url(" + art.imageUrl + ")"
      }}>
        <div className="header">
          {art.category && (
            <span style={{
              display: 'inline-block',
              background: '#F3EEFF',
              color: '#5B3FD6',
              border: '1px solid #DDD6F7',
              borderRadius: 99,
              fontSize: '0.72rem',
              fontWeight: 600,
              padding: '0.15rem 0.6rem',
              marginBottom: '0.5rem',
              letterSpacing: '0.01em',
            }}>
              {art.category}
            </span>
          )}
        </div>

        <div className="center">
        </div>
        <div className="footer">
          <h3 style={{ fontSize: '0.975rem', marginBottom: '0.2rem' }}>
            {art.title}
          </h3>
        </div>
      </div>
      <div style={{ padding: '1rem 1.1rem 1.1rem' }}>
        <div className="detail-line">
          <p style={{ fontSize: '0.8rem' }}>
            <span
              onClick={e => { e.preventDefault(); e.stopPropagation(); navigate(`/artists/${art.artistUsername}`) }}
              style={{ fontWeight: 600, cursor: 'pointer' }}
            >
              {art.artistUsername}
            </span>
          </p>


          {showRating && <div style={{ marginBottom: '0.4rem' }}>
            {rating?.ratingCount > 0 ? (
              <StarRating
                value={rating.averageRating}
                count={rating.ratingCount}
                size={14}
              />
            ) : (
              <span style={{ fontSize: '0.78rem', color: '#A39DB8' }}>No ratings yet</span>
            )}
          </div>}
        </div>
        {showDesc && art.description && (
          <p style={{ fontSize: '0.85rem', color: '#4B4669', lineHeight: 1.45 }}>
            {art.description.length > 90
              ? art.description.slice(0, 90) + '…'
              : art.description}
          </p>
        )}
      </div>
    </div>
  </Link>

export default ArtworkItem
