import axios from 'axios';

const API_BASE = 'http://localhost:5092';

const api = axios.create({
  baseURL: API_BASE,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const authAPI = {
  register: (data) => api.post('/api/Auth/register', data),
  login: (data) => api.post('/api/Auth/login', data),
  getProfile: (id) => api.get(`/api/Auth/profile/${id}`),
};

export const artworkAPI = {
  getAll: (params) => api.get('/api/Artwork', { params }),
  getById: (id) => api.get(`/api/Artwork/${id}`),
  getByArtist: (artistId) => api.get(`/api/Artwork/artist/${artistId}`),
  create: (data) => api.post('/api/Artwork', data),
  update: (id, data) => api.put(`/api/Artwork/${id}`, data),
  delete: (id) => api.delete(`/api/Artwork/${id}`),
};

export const commentAPI = {
  getByArtwork: (artworkId) => api.get(`/api/Comment/artwork/${artworkId}`),
  getRating: (artworkId) => api.get(`/api/Comment/artwork/${artworkId}/rating`),
  create: (data) => api.post('/api/Comment', data),
  delete: (id) => api.delete(`/api/Comment/${id}`),
};

export const notificationAPI = {
  getMyNotifications: () => api.get('/api/Notification'),
  markAsRead: (id) => api.put(`/api/Notification/${id}/read`),
};

export default api;