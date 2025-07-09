import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { get } from './services/api';

function App() {
  // ������ ������� ��������� ����� React-Query
  const { data, isLoading, error } = useQuery({
    queryKey: ['devices'],
    queryFn: () => get('/devices'),
  });

  if (isLoading) return <div>��������...</div>;
  if (error) return <div>������: {(error as Error).message}</div>;

  return (
    <div>
      <h1>����������</h1>
      <ul>
        {Array.isArray(data) && data.map((d: any) => (
          <li key={d.id}>{d.name}</li>
        ))}
      </ul>
    </div>
  );
}

export default App;