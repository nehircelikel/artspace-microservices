import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { artworkAPI } from '../api/api';
import { useNavigate } from 'react-router-dom';

export default function Profile() {
  const { user } = useAuth();
  const [artworks, setArtworks] = useState([]);
  const navigate = useNavigate();

  useEffect(() => {
    if (user && user.role === 'Artist') {
      fetchMyArtworks();
    }
  }, [user]);

  const fetchMyArtworks = async () => {
    try {
      const res = await artworkAPI.getByArtist(user.id);
      setArtworks(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const handleDelete = async (id) => {
    try {
      await artworkAPI.delete(id);
      fetchMyArtworks();
    } catch (err) {
      console.error(err);
    }
  };

  if (!user) return <p style={{ textAlign: 'center', marginTop: '2rem' }}>Please login.</p>;

  return (
    <div style={styles.container}>
      <div style={styles.profileCard}>
        <h2 style={styles.username}>@{user.username}</h2>
        <p style={styles.role}>{user.role}</p>
        {user.bio && <p style={styles.bio}>{user.bio}</p>}
        {user.contactEmail && <p style={styles.contact}>📧 {user.contactEmail}</p>}
      </div>

      {user.role === 'Artist' && (
        <div>
          <h3 style={styles.sectionTitle}>My Artworks</h3>
          <div style={styles.grid}>
            {artworks.length === 0 ? (
              <p style={styles.empty}>No artworks yet.</p>
            ) : (
              artworks.map((artwork) => (
                <div key={artwork.id} style={styles.card}>
                  <img
                    src={artwork.imageUrl}
                    alt={artwork.title}
                    style={styles.image}
                    onClick={() => navigate(`/artwork/${artwork.id}`)}
                  />
                  <div style={styles.info}>
                    <h4>{artwork.title}</h4>
                    <p style={styles.category}>{artwork.category}</p>
                    <button style={styles.deleteBtn} onClick={() => handleDelete(artwork.id)}>
                      Delete
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}

const styles = {
  container: { maxWidth: '1100px', margin: '2rem auto', padding: '0 1rem' },
  profileCard: { background: '#fff', padding: '2rem', borderRadius: '12px', marginBottom: '2rem', boxShadow: '0 2px 8px rgba(0,0,0,0.08)' },
  username: { fontSize: '24px', marginBottom: '0.5rem' },
  role: { color: '#888', marginBottom: '0.5rem' },
  bio: { marginBottom: '0.5rem', color: '#555' },
  contact: { color: '#555' },
  sectionTitle: { fontSize: '20px', marginBottom: '1rem' },
  grid: { columns: '3 280px', gap: '1rem' },
  card: { breakInside: 'avoid', marginBottom: '1rem', background: '#fff', borderRadius: '12px', overflow: 'hidden', boxShadow: '0 2px 8px rgba(0,0,0,0.08)' },
  image: { width: '100%', display: 'block', cursor: 'pointer' },
  info: { padding: '0.75rem' },
  category: { fontSize: '13px', color: '#888', marginBottom: '0.5rem' },
  deleteBtn: { padding: '0.4rem 1rem', background: '#e74c3c', color: '#fff', border: 'none', borderRadius: '8px', cursor: 'pointer', fontSize: '13px' },
  empty: { color: '#888' }
};