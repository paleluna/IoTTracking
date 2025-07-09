import axios, { AxiosInstance } from 'axios';
import { getToken } from './auth';
import { DeviceInfo, DeviceSummary, SensorData, TrendPoint, Anomaly } from '../types/api.types';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

const api: AxiosInstance = axios.create({
  baseURL: API_URL,
});

api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// API endpoints with types
export const getDevices = async (): Promise<DeviceInfo[]> => {
  const { data } = await api.get('/devices');
  return data;
};

export const getDeviceSummary = async (deviceId: number): Promise<DeviceSummary> => {
  const { data } = await api.get(`/devices/${deviceId}/summary`);
  return data;
};

export const getDeviceData = async (deviceId: number, limit: number = 100): Promise<SensorData[]> => {
  const { data } = await api.get(`/devices/${deviceId}/data`, { params: { limit } });
  return data;
};

export const getTrend = async (
  deviceId: number, 
  metric: 'temperature' | 'humidity' = 'temperature',
  bucket: string = '5 minutes',
  period: string = 'day'
): Promise<TrendPoint[]> => {
  const { data } = await api.get(`/devices/${deviceId}/trend`, {
    params: { metric, bucket, period }
  });
  return data;
};

export const getAnomalies = async (
  deviceId: number,
  metric: 'temperature' | 'humidity' = 'temperature',
  period: string = 'hour',
  threshold: number = 2.0
): Promise<Anomaly[]> => {
  const { data } = await api.get(`/devices/${deviceId}/anomalies`, {
    params: { metric, period, threshold }
  });
  return data;
};