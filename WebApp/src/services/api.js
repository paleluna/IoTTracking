import axios from 'axios';
import { getToken } from './auth';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

const api = axios.create({
  baseURL: API_URL,
});

// Перехватчик для добавления токена в каждый запрос
api.interceptors.request.use(
  (config) => {
    const token = getToken();
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Универсальная функция для GET-запросов
export const get = async (url, params) => {
  const { data } = await api.get(url, { params });
  return data;
};

// Функции для конкретных эндпоинтов
export const getDevices = () => get('/devices');

export const getDeviceSummary = (deviceId) => get(`/devices/${deviceId}/summary`);

export const getDeviceData = (deviceId, month) => {
    const params = {};
    if (month) {
        // Форматируем в YYYY-MM
        params.month = month.toISOString().slice(0, 7);
    }
    return get(`/devices/${deviceId}/data`, params);
};

export const getTrend = (deviceId, from, to) =>
  get(`/devices/${deviceId}/trend`, { from, to });

export const getAnomalies = (deviceId, from, to) =>
  get(`/devices/${deviceId}/anomalies`, { from, to });