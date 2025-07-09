import React, { useEffect, useState, useCallback } from 'react';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  TimeScale,
  Tooltip,
  Legend,
} from 'chart.js';
import 'chartjs-adapter-date-fns';
import { get } from '../services/api';
import { Spinner, Center, Text, Box, Button } from '@chakra-ui/react';
import { parseISO } from 'date-fns';

// Регистрируем компоненты Chart.js
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  TimeScale,
  Tooltip,
  Legend
);

function DeviceChart({ deviceId, month }) {
  // Debug: выводим props и монтирование
  useEffect(() => {
    console.log('DeviceChart mounted', { deviceId, month });
  }, [deviceId, month]);

  const [chartData, setChartData] = useState(null);
  const [error, setError] = useState(null);
  const [debugInfo, setDebugInfo] = useState(null);

  // Форматирование точки для графика
  const formatPoint = useCallback((rawPoint) => {
    if (!rawPoint?.timestamp || typeof rawPoint.temperature !== 'number') {
      console.warn('Invalid data point:', rawPoint);
      return null;
    }

    try {
      // Проверяем что timestamp - валидная дата
      const date = parseISO(rawPoint.timestamp);
      if (isNaN(date.getTime())) {
        console.warn('Invalid timestamp:', rawPoint.timestamp);
        return null;
      }

      return {
        x: rawPoint.timestamp, // Chart.js с TimeScale сам распарсит ISO строку
        y: rawPoint.temperature,
        meta: { // Дополнительные данные для отладки
          humidity: rawPoint.humidity,
          raw: rawPoint
        }
      };
    } catch (err) {
      console.warn('Error formatting point:', err);
      return null;
    }
  }, []);

  const fetchData = useCallback(async () => {
    try {
      const url = month ? 
        `/devices/${deviceId}/data?month=${month}` : 
        `/devices/${deviceId}/data`;

      console.log('Fetching:', url);
      const apiResponse = await get(url);

      // Универсальная обработка: если есть поле data, используем его, иначе сам массив
      let rawData = Array.isArray(apiResponse) ? apiResponse : (Array.isArray(apiResponse.data) ? apiResponse.data : []);
      console.log('API response:', apiResponse, 'Used for chart:', rawData);

      if (!Array.isArray(rawData)) {
        throw new Error('Data is not an array');
      }

      // Фильтруем и форматируем точки
      const points = rawData
        .map(formatPoint)
        .filter(Boolean)
        .sort((a, b) => new Date(a.x) - new Date(b.x));

      console.log('Processed points:', points);

      if (points.length === 0) {
        setError('No valid data points');
        setChartData(null);
        return;
      }

      // Находим min/max для автомасштабирования
      const temps = points.map(p => p.y);
      const minTemp = Math.floor(Math.min(...temps));
      const maxTemp = Math.ceil(Math.max(...temps));
      const padding = Math.max(2, Math.round((maxTemp - minTemp) * 0.1));

      const data = {
        datasets: [{
          label: 'Temperature',
          data: points,
          borderColor: 'rgba(75,192,192,1)',
          backgroundColor: 'rgba(75,192,192,0.2)',
          borderWidth: 1.5,
          tension: 0.2,
          pointRadius: 2,
          fill: true
        }]
      };

      setChartData(data);
      setError(null);
      setDebugInfo({ 
        pointCount: points.length,
        timeRange: {
          start: points[0].x,
          end: points[points.length - 1].x
        },
        tempRange: {
          min: minTemp,
          max: maxTemp
        }
      });

    } catch (err) {
      console.error('Error fetching data:', err);
      setError(err.message);
      setChartData(null);
    }
  }, [deviceId, month, formatPoint]);

  useEffect(() => {
    let intervalId;
    
    fetchData();

    // Polling только для real-time режима
    if (!month) {
      intervalId = setInterval(fetchData, 5000);
    }

    return () => {
      if (intervalId) clearInterval(intervalId);
    };
  }, [fetchData, month]);

  if (error) {
    return (
      <Center h="400px" flexDirection="column" gap={4}>
        <Text color="red.500">{error}</Text>
        <Button size="sm" onClick={fetchData}>Retry</Button>
        <Text fontSize="xs" color="gray.400">deviceId: {String(deviceId)}, month: {String(month)}</Text>
      </Center>
    );
  }

  if (!chartData) {
    return (
      <Center h="400px">
        <Spinner size="xl" />
        <Text fontSize="xs" color="gray.400" ml={4}>deviceId: {String(deviceId)}, month: {String(month)}</Text>
      </Center>
    );
  }

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      intersect: false,
      mode: 'index'
    },
    scales: {
      x: {
        type: 'time',
        time: {
          unit: month ? 'day' : 'minute',
          displayFormats: {
            minute: 'HH:mm',
            hour: 'HH:mm',
            day: 'MMM d'
          }
        },
        title: {
          display: true,
          text: 'Time'
        }
      },
      y: {
        title: {
          display: true,
          text: 'Temperature (°C)'
        },
        suggestedMin: debugInfo?.tempRange.min - 2,
        suggestedMax: debugInfo?.tempRange.max + 2
      }
    },
    plugins: {
      tooltip: {
        callbacks: {
          title: (items) => {
            const point = items[0].raw;
            return new Date(point.x).toLocaleString();
          },
          label: (context) => {
            const point = context.raw;
            const lines = [
              `Temperature: ${point.y.toFixed(1)}°C`,
            ];
            if (point.meta?.humidity) {
              lines.push(`Humidity: ${point.meta.humidity}%`);
            }
            return lines;
          }
        }
      }
    }
  };

  return (
    <Box w="100%" minH="420px" maxW="900px" mx="auto" borderWidth={1} borderRadius="md" p={2}>
      <Text fontSize="sm" color="gray.500" mb={1}>
        deviceId: {String(deviceId)}, month: {String(month)}
      </Text>
      <Box h="400px" w="100%" minW="320px" maxW="860px">
        <Line options={options} data={chartData} />
      </Box>
      {debugInfo && (
        <Text fontSize="xs" color="gray.500" mt={2}>
          {debugInfo.pointCount} points, 
          Range: {new Date(debugInfo.timeRange.start).toLocaleString()} - {new Date(debugInfo.timeRange.end).toLocaleString()},
          Temp: {debugInfo.tempRange.min}°C - {debugInfo.tempRange.max}°C
        </Text>
      )}
    </Box>
  );
}

export default DeviceChart;