export interface SensorData {
  timestamp: string;
  temperature: number;
  humidity: number;
}

export interface DeviceInfo {
  id: number;
  name: string;
}

export interface AggregateData {
  min: number;
  max: number;
  avg: number;
}

export interface TrendPoint {
  bucket: string;
  value: number;
}

export interface Anomaly {
  timestamp: string;
  value: number;
}

export interface DeviceSummary {
  data: SensorData[];
  aggregates: AggregateData;
}