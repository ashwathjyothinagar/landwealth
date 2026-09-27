import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { apiClient } from '../api/client';

interface AuthState {
  token: string | null;
  name: string | null;
  login: (token: string, name: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(localStorage.getItem('landwealth_token'));
  const [name, setName] = useState<string | null>(localStorage.getItem('landwealth_name'));

  const login = (nextToken: string, nextName: string) => {
    localStorage.setItem('landwealth_token', nextToken);
    localStorage.setItem('landwealth_name', nextName);
    setToken(nextToken);
    setName(nextName);
  };

  const logout = () => {
    localStorage.removeItem('landwealth_token');
    localStorage.removeItem('landwealth_name');
    setToken(null);
    setName(null);
  };

  useEffect(() => {
    const onUnauthorized = () => logout();
    window.addEventListener('landwealth:unauthorized', onUnauthorized);
    return () => window.removeEventListener('landwealth:unauthorized', onUnauthorized);
  }, []);

  const value = useMemo(() => ({ token, name, login, logout }), [token, name]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}

export async function authenticate(path: string, email: string, password: string, fullName?: string) {
  const response = await apiClient.post(path, { email, password, fullName });
  return response.data as { token: string; fullName: string };
}
