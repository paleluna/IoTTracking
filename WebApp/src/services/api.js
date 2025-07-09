import axios, { AxiosRequestConfig } from 'axios';
import { getToken } from './auth';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000'; // Ocelot gateway

const api = axios.create({
  baseURL: API_URL,
});

api.interceptors.request.use((config: AxiosRequestConfig) => {
  const token = getToken();
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export async function get<T>(url: string): Promise<T> {
  const { data } = await api.get<T>(url);
  return data;
}

// Можно добавить post/put/delete с типами аналогично