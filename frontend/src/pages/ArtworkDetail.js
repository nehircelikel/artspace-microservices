import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { artworkAPI, commentAPI } from '../api/api';
import { useAuth } from '../context/AuthContext';

export default function ArtworkDetail() {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [artwork, setArtwork] = useState(null);
  const [comments, setComments] = useState([]);
  const [rating, setRating] = useState(null);
  const [newComment, setNewComment] = useState({ content: '', rating: 5 });

  useEffect(() => {
    fetchArtwork();
    fetchComments();
    fetchRating();
  }, [id]);

  const fetchArtwork = async () => {
    try {
      const res = await artworkAPI.getById(id);
      setArtwork(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const fetchComments = async () => {
    try {
      const res = await commentAPI.getByArtwork(id);
      setComments(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const fetchRating = async () => {
    try {
      const res = await commentAPI.getRating(id);
      setRating(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const handleComment = async (e) => {
    e.preventDefault();
    if (!user) { navigate('/login'); return; }
    try {
      await commentAPI.create({
        content: newComment.content,
        rating: newComment.rating,
        artworkId: id,
        artistId: artwork.artistId
      });
      setNewComment({ content: '', rating: 5 });
      fetchComments();
      fetchRating();
    } catch (err) {
      console.error(err);
    }
  };

  if (!artwork) return <p style={{ textAlign: 'center', marginTop: '2rem' }}>Loading...</p>;

  return (
    <div style={styles.container}>
      <div style={styles.left}>
        <img src={artwork.imageUrl} alt={artwork.title} style={styles.image} />
      </div>
      <div style={styles.right}>
        <h1 style={styles.title}>{artwork.title}</h1>
        <p style={styles.artist}>@{artwork.artistUsername}</p>
        <span style={styles.category}>{artwork.category}</span>
        <p style={styles.description}>{artwork.description}</p>

        {rating && (
          <div style={styles.ratingBox}>
            ⭐ {rating.averageRating.toFixed(1)} / 5 ({rating.totalComments} comments)
          </div>
        )}

        {artwork.contactEmail && (
          <div style={styles.contact}>
            📧 Contact: {user ? artwork.contactEmail : <span onClick={() => navigate('/login')} style={styles.loginLink}>Login to view</span>}
          </div>
        )}

        <div style={styles.commentsSection}>
          <h3>Comments</h3>
          <form onSubmit={handleComment} style={styles.commentForm}>
            <textarea
              style={styles.textarea}
              placeholder="Your comment..."
              value={newComment.content}
              onChange={(e) => setNewComment({ ...newComment, content: e.target.value })}
              required
            />
            <select
              style={styles.select}
              value={newComment.rating}
              onChange={(e) => setNewComment({ ...newComment, rating: parseInt(e.target.value) })}
            >
              {[1,2,3,4,5].map(n => <option key={n} value={n}>{n} ⭐</option>)}
            </select>
            <button style={styles.button} type="submit">Add Comment</button>
          </form>

          {comments.map((c) => (
            <div key={c.id} style={styles.comment}>
              <p style={styles.commentUser}>@{c.username} — {c.rating} ⭐</p>
              <p>{c.content}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

const styles = {
  container: { display: 'flex', gap: '2rem', maxWidth: '1100px', margin: '2rem auto', padding: '0 1rem' },
  left: { flex: 1 },
  image: { width: '100%', borderRadius: '12px' },
  right: { flex: 1 },
  title: { fontSize: '24px', marginBottom: '0.5rem' },
  artist: { color: '#888', marginBottom: '0.5rem' },
  category: { background: '#f0f0f0', padding: '0.2rem 0.8rem', borderRadius: '20px', fontSize: '13px' },
  description: { marginTop: '1rem', lineHeight: '1.6', color: '#555' },
  ratingBox: { marginTop: '1rem', fontSize: '18px' },
  contact: { marginTop: '1rem', padding: '1rem', background: '#f9f9f9', borderRadius: '8px' },
  loginLink: { color: '#007bff', cursor: 'pointer' },
  commentsSection: { marginTop: '2rem' },
  commentForm: { marginTop: '1rem', marginBottom: '1.5rem' },
  textarea: { width: '100%', padding: '0.75rem', borderRadius: '8px', border: '1px solid #ddd', marginBottom: '0.5rem', fontSize: '14px', minHeight: '80px' },
  select: { padding: '0.5rem', borderRadius: '8px', border: '1px solid #ddd', marginBottom: '0.5rem' },
  button: { display: 'block', padding: '0.75rem 1.5rem', background: '#333', color: '#fff', border: 'none', borderRadius: '8px', cursor: 'pointer' },
  comment: { padding: '1rem', background: '#f9f9f9', borderRadius: '8px', marginBottom: '0.75rem' },
  commentUser: { fontWeight: '600', marginBottom: '0.25rem' }
};