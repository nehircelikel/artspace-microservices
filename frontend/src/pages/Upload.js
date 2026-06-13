import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { artworkAPI } from '../api/api';

export default function Upload() {
  const [form, setForm] = useState({
    title: '',
    description: '',
    imageUrl: '',
    category: 'Painting'
  });
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      await artworkAPI.create(form);
      navigate('/');
    } catch (err) {
      setError('Failed to upload artwork.');
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h2 style={styles.title}>Upload Artwork</h2>
        {error && <p style={styles.error}>{error}</p>}
        <form onSubmit={handleSubmit}>
          <input style={styles.input} type="text" name="title" placeholder="Title" onChange={handleChange} required />
          <textarea style={styles.textarea} name="description" placeholder="Description" onChange={handleChange} required />
          <input style={styles.input} type="text" name="imageUrl" placeholder="Image URL" onChange={handleChange} required />
          <select style={styles.input} name="category" onChange={handleChange}>
            <option value="Painting">Painting</option>
            <option value="Sculpture">Sculpture</option>
            <option value="Photography">Photography</option>
            <option value="Digital">Digital</option>
            <option value="Other">Other</option>
          </select>
          <button style={styles.button} type="submit">Upload</button>
        </form>
      </div>
    </div>
  );
}

const styles = {
  container: { display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' },
  card: { background: '#fff', padding: '2rem', borderRadius: '12px', width: '400px', boxShadow: '0 2px 12px rgba(0,0,0,0.1)' },
  title: { marginBottom: '1.5rem', textAlign: 'center' },
  input: { width: '100%', padding: '0.75rem', marginBottom: '1rem', borderRadius: '8px', border: '1px solid #ddd', fontSize: '14px' },
  textarea: { width: '100%', padding: '0.75rem', marginBottom: '1rem', borderRadius: '8px', border: '1px solid #ddd', fontSize: '14px', minHeight: '100px' },
  button: { width: '100%', padding: '0.75rem', background: '#333', color: '#fff', border: 'none', borderRadius: '8px', cursor: 'pointer', fontSize: '15px' },
  error: { color: 'red', marginBottom: '1rem', textAlign: 'center' }
};