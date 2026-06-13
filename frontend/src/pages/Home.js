import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { artworkAPI } from '../api/api';

export default function Home(){
    const [artworks, setArtworks] = useState([]);
    const [category, setCategory] = useState('');
    const [keyword, setKeyword] = useState('');
    const navigate = useNavigate();

     useEffect(() => {
    fetchArtworks();
  }, []);

  const fetchArtworks = async () => {
    try {
      const res = await artworkAPI.getAll({ category, keyword });
      setArtworks(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const handleSearch = (e) => {
    e.preventDefault();
    fetchArtworks();
  };

  return (
    <div style={styles.container}>
      <form onSubmit={handleSearch} style={styles.searchBar}>
        <input
          style={styles.searchInput}
          type="text"
          placeholder="Search artworks..."
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
        />
        <select style={styles.select} value={category} onChange={(e) => setCategory(e.target.value)}>
          <option value="">All Categories</option>
<option value="Painting">Painting</option>
<option value="Sculpture">Sculpture</option>
<option value="Photography">Photography</option>
<option value="Digital">Digital</option>
<option value="Other">Other</option>
        </select>
        <button style={styles.searchButton} type="submit">Search</button>
      </form>

      <div style={styles.grid}>
        {artworks.length === 0 ? (
          <p style={styles.empty}>No artworks found.</p>
        ) : (
          artworks.map((artwork) => (
            <div
              key={artwork.id}
              style={styles.card}
              onClick={() => navigate(`/artwork/${artwork.id}`)}
            >
              <img src={artwork.imageUrl} alt={artwork.title} style={styles.image} />
              <div style={styles.info}>
                <h3 style={styles.artTitle}>{artwork.title}</h3>
                <p style={styles.artist}>@{artwork.artistUsername}</p>
                <span style={styles.category}>{artwork.category}</span>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

const styles = {
  container: { maxWidth: '1200px', margin: '0 auto', padding: '2rem' },
  searchBar: { display: 'flex', gap: '1rem', marginBottom: '2rem' },
  searchInput: { flex: 1, padding: '0.75rem', borderRadius: '8px', border: '1px solid #ddd', fontSize: '14px' },
  select: { padding: '0.75rem', borderRadius: '8px', border: '1px solid #ddd', fontSize: '14px' },
  searchButton: { padding: '0.75rem 1.5rem', background: '#333', color: '#fff', border: 'none', borderRadius: '8px', cursor: 'pointer' },
  grid: { columns: '3 300px', gap: '1rem' },
  card: { breakInside: 'avoid', marginBottom: '1rem', background: '#fff', borderRadius: '12px', overflow: 'hidden', cursor: 'pointer', boxShadow: '0 2px 8px rgba(0,0,0,0.08)', transition: 'transform 0.2s', },
  image: { width: '100%', display: 'block' },
  info: { padding: '0.75rem' },
  artTitle: { fontSize: '15px', fontWeight: '600', marginBottom: '0.25rem' },
  artist: { fontSize: '13px', color: '#888', marginBottom: '0.5rem' },
  category: { fontSize: '12px', background: '#f0f0f0', padding: '0.2rem 0.6rem', borderRadius: '20px' },
  empty: { textAlign: 'center', color: '#888', gridColumn: '1/-1' }
}