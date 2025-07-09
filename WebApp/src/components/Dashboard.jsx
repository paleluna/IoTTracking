import React, { useEffect, useState } from 'react';
import { get } from '../services/api';
import DeviceChart from './DeviceChart';
import { removeToken } from '../services/auth';
import {
  Box,
  Button,
  Flex,
  List,
  ListItem,
  Heading,
  Spinner,
  ButtonGroup,
  Stat,
  StatLabel,
  StatNumber,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Text,
  Select,
  FormControl,
  FormLabel,
} from '@chakra-ui/react';

function Dashboard() {
  const [devices, setDevices] = useState([]);
  const [selectedDevice, setSelectedDevice] = useState(null);
  const [view, setView] = useState('realtime'); // realtime | aggregates | anomalies
  const [aggregates, setAggregates] = useState(null);
  const [anomalies, setAnomalies] = useState(null);

  // Генерируем список месяцев без дубликатов
  const months = React.useMemo(() => {
    const set = new Set();
    const currentDate = new Date();
    for (let i = 0; i < 12; i++) {
      const date = new Date(currentDate);
      date.setMonth(currentDate.getMonth() - i);
      set.add(date.toISOString().slice(0, 7));
    }
    return Array.from(set);
  }, []);

  const [selectedMonth, setSelectedMonth] = useState(months[0]);

  useEffect(() => {
    const fetchDevices = async () => {
      try {
        const data = await get('/devices');
        setDevices(data);
      } catch (err) {
        console.error(err);
      }
    };
    fetchDevices();
  }, []);

  useEffect(() => {
    if (!selectedDevice || view === 'realtime') return;

    let intervalId;

    const fetchAnalytics = async () => {
      try {
        if (view === 'aggregates') {
          const data = await get(`/devices/${selectedDevice.id}/aggregates?period=day`);
          setAggregates(data);
        } else if (view === 'anomalies') {
          const data = await get(`/devices/${selectedDevice.id}/anomalies?metric=temperature&period=day&threshold=0.8`);
          const filtered = data.filter((a) => {
            const val = Number(a.value);
            return val <= 15 || val >= 26;
          });
          setAnomalies(filtered);
        }
      } catch (e) {
        console.error(e);
      }
    };

    fetchAnalytics();
    // poll every 5s for anomalies/aggregates update
    intervalId = setInterval(fetchAnalytics, 5000);

    return () => clearInterval(intervalId);
  }, [selectedDevice, view]);

  const handleLogout = () => {
    removeToken();
    window.location.reload();
  };

  return (
    <Box p={6}>
      <Flex justify="space-between" align="center" mb={4}>
        <Heading size="lg">Dashboard</Heading>
        <Button colorScheme="red" variant="outline" onClick={handleLogout}>
          Logout
        </Button>
      </Flex>

      <Flex gap={6}>
        <Box minW="200px" borderWidth={1} borderRadius="md" p={4}>
          {devices.length === 0 ? (
            <Spinner />
          ) : (
            <List spacing={2}>
              {devices.map((d) => (
                <ListItem
                  key={d.id}
                  cursor="pointer"
                  p={2}
                  borderRadius="md"
                  bg={d.id === selectedDevice?.id ? 'teal.50' : 'transparent'}
                  color={d.id === selectedDevice?.id ? 'teal.700' : 'inherit'}
                  fontWeight={d.id === selectedDevice?.id ? 'bold' : 'normal'}
                  _hover={{ bg: 'teal.50' }}
                  onClick={() => setSelectedDevice(d)}
                >
                  {d.name}
                </ListItem>
              ))}
            </List>
          )}
        </Box>

        <Box flex="1" borderWidth={1} borderRadius="md" p={4}>
          {!selectedDevice && (
            <Heading size="md" textAlign="center" color="gray.400">
              Select device to view data
            </Heading>
          )}

          {selectedDevice && (
            <>
              <Flex align="center" mb={4} gap={4}>
                <ButtonGroup size="sm" colorScheme="teal" isAttached>
                  <Button
                    variant={view === 'realtime' ? 'solid' : 'outline'}
                    onClick={() => setView('realtime')}
                  >
                    Real-time
                  </Button>
                  <Button
                    variant={view === 'aggregates' ? 'solid' : 'outline'}
                    onClick={() => setView('aggregates')}
                  >
                    Aggregates
                  </Button>
                  <Button
                    variant={view === 'anomalies' ? 'solid' : 'outline'}
                    onClick={() => setView('anomalies')}
                  >
                    Anomalies
                  </Button>
                </ButtonGroup>

                {view === 'realtime' && (
                  <FormControl maxW="220px" borderWidth={1} borderRadius="md" p={2} ml={4}>
                    <FormLabel fontSize="sm" mb={1} htmlFor="month-select">Select month</FormLabel>
                    <Select
                      id="month-select"
                      value={selectedMonth}
                      onChange={(e) => setSelectedMonth(e.target.value)}
                    >
                      {months.map((month) => (
                        <option key={month} value={month}>
                          {month}
                        </option>
                      ))}
                    </Select>
                  </FormControl>
                )}
              </Flex>

              {view === 'realtime' && <DeviceChart deviceId={selectedDevice.id} month={selectedMonth} />}

              {view === 'aggregates' && aggregates && (
                <Flex gap={6} justify="center">
                  <Stat>
                    <StatLabel>Min (°C)</StatLabel>
                    <StatNumber>{Number(aggregates.min).toFixed(1)}</StatNumber>
                  </Stat>
                  <Stat>
                    <StatLabel>Avg (°C)</StatLabel>
                    <StatNumber>{Number(aggregates.avg).toFixed(1)}</StatNumber>
                  </Stat>
                  <Stat>
                    <StatLabel>Max (°C)</StatLabel>
                    <StatNumber>{Number(aggregates.max).toFixed(1)}</StatNumber>
                  </Stat>
                </Flex>
              )}

              {view === 'anomalies' && anomalies && (
                anomalies.length === 0 ? (
                  <Text textAlign="center" color="gray.500">No anomalies detected</Text>
                ) : (
                  <Table size="sm">
                    <Thead>
                      <Tr>
                        <Th>Timestamp</Th>
                        <Th isNumeric>Value (°C)</Th>
                      </Tr>
                    </Thead>
                    <Tbody>
                      {anomalies.map((a, idx) => (
                        <Tr key={idx}>
                          <Td>{new Date(a.timestamp).toLocaleString()}</Td>
                          <Td isNumeric>{a.value.toFixed(1)}</Td>
                        </Tr>
                      ))}
                    </Tbody>
                  </Table>
                )
              )}
            </>
          )}
        </Box>
      </Flex>
    </Box>
  );
}

export default Dashboard;
