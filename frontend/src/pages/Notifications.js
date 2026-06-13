import React, { useState, useEffect } from 'react';
import { notificationAPI } from '../api/api';

export default function Notifications() {
  const [notifications, setNotifications] = useState([]);

  useEffect(() => {
    fetchNotifications();
  }, []);

  const fetchNotifications = async () => {
    try {
      const res = await notificationAPI.getMyNotifications();
      setNotifications(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const handleMarkAsRead = async (id) => {
    try {
      await notificationAPI.markAsRead(id);
      fetchNotifications();
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div style={styles.container}>
      <h2 style={styles.title}>Notifications</h2>
      {notifications.length === 0 ? (
        <p style={styles.empty}>No notifications yet.</p>
      ) : (
        notifications.map((n) => (
          <div key={n.id} style={{ ...styles.card, background: n.isRead ? '#f9f9f9' : '#fff' }}>
            <p style={styles.message}>{n.message}</p>
            <p style={styles.date}>{new Date(n.createdAt).toLocaleString()}</p>
            {!n.isRead && (
              <button style={styles.button} onClick={() => handleMarkAsRead(n.id)}>
                Mark as Read
              </button>
            )}
          </div>
        ))
      )}
    </div>
  );
}

const styles = {
  container: { maxWidth: '700px', margin: '2rem auto', padding: '0 1rem' },
  title: { marginBottom: '1.5rem' },
  empty: { color: '#888', textAlign: 'center' },
  card: { padding: '1rem', borderRadius: '12px', marginBottom: '1rem', boxShadow: '0 2px 8px rgba(0,0,0,0.08)' },
  message: { fontSize: '15px', marginBottom: '0.5rem' },
  date: { fontSize: '12px', color: '#888', marginBottom: '0.5rem' },
  button: { padding: '0.4rem 1rem', background: '#333', color: '#fff', border: 'none', borderRadius: '8px', cursor: 'pointer', fontSize: '13px' }
};
