import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login, setToken } from '../services/auth';
import {
  Box,
  Button,
  FormControl,
  FormLabel,
  Input,
  Heading,
  VStack,
  Text,
} from '@chakra-ui/react';

function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState(null);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    try {
      const tokenResponse = await login(username, password);
      setToken(tokenResponse.access_token, tokenResponse.expires_in);
      navigate('/', { replace: true });
    } catch (err) {
      setError('Неверный логин или пароль');
    }
  };

  return (
    <Box maxW="md" mx="auto" mt={24} p={8} borderWidth={1} borderRadius="lg" boxShadow="lg">
      <Heading mb={6} textAlign="center">
        Login
      </Heading>
      <form onSubmit={handleSubmit}>
        <VStack spacing={4} align="stretch">
          <FormControl id="username">
            <FormLabel>Username</FormLabel>
            <Input
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="admin"
            />
          </FormControl>
          <FormControl id="password">
            <FormLabel>Password</FormLabel>
            <Input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="admin"
            />
          </FormControl>
          {error && (
            <Text color="red.500" fontSize="sm">
              {error}
            </Text>
          )}
          <Button colorScheme="teal" type="submit" width="full">
            Login
          </Button>
        </VStack>
      </form>
    </Box>
  );
}

export default Login;

// @ts-nocheck
