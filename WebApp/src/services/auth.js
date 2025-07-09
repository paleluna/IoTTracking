import axios from 'axios';

const TOKEN_KEY = 'iot_token';
const TOKEN_EXPIRATION_KEY = 'iot_token_expiration';

export function setToken(token, expiresIn) {
  const expiration = new Date().getTime() + expiresIn * 1000;
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(TOKEN_EXPIRATION_KEY, expiration);
}

export function getToken() {
  const expiration = localStorage.getItem(TOKEN_EXPIRATION_KEY);
  if (expiration && new Date().getTime() > parseInt(expiration, 10)) {
    removeToken();
    return null;
  }
  return localStorage.getItem(TOKEN_KEY);
}

export function removeToken() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(TOKEN_EXPIRATION_KEY);
}

export async function login(username, password) {
  const params = new URLSearchParams();
  params.append('grant_type', 'password');
  params.append('client_id', 'webapp-client');
  params.append('client_secret', 'super-secret'); // This should ideally be in an env var
  params.append('username', username);
  params.append('password', password);

  const response = await axios.post(
    'http://localhost:5001/connect/token',
    params,
    {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    }
  );

  return response.data;
} 