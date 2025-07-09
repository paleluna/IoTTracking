import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { get } from './services/api';

function App() {
  // ������ ������� ��������� ����� React-Query
  const { data, isLoading, error } = useQuery({
    queryKey: ['devices'],
    queryFn: () => get('/devices'),
  });

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <div>
      <h1>Devices</h1>
      <ul>
        {Array.isArray(data) && data.map((d) => (
          <li key={d.id}>{d.name}</li>
        ))}
      </ul>
    </div>
  );
}

export default App;