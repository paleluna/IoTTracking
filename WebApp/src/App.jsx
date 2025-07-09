import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { get } from './services/api';

function App() {
  // Пример запроса устройств через React-Query
  const { data, isLoading, error } = useQuery({
    queryKey: ['devices'],
    queryFn: () => get('/devices'),
  });

  if (isLoading) return <div>Загрузка...</div>;
  if (error) return <div>Ошибка: {(error as Error).message}</div>;

  return (
    <div>
      <h1>Устройства</h1>
      <ul>
        {Array.isArray(data) && data.map((d: any) => (
          <li key={d.id}>{d.name}</li>
        ))}
      </ul>
    </div>
  );
}

export default App;