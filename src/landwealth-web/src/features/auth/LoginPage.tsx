import React, { useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { Alert, Box, Button, Card, CardContent, Link, TextField, Typography } from '@mui/material';
import { authenticate, useAuth } from '../../auth/AuthContext';

export const LoginPage: React.FC<{ mode: 'login' | 'register' }> = ({ mode }) => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [error, setError] = useState<string | null>(null);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    try {
      const result = await authenticate(
        mode === 'login' ? '/api/auth/login' : '/api/auth/register',
        email,
        password,
        fullName
      );
      login(result.token, result.fullName);
      navigate('/');
    } catch {
      setError(mode === 'login' ? 'Email or password is incorrect.' : 'Could not create the account.');
    }
  };

  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
      <Card sx={{ width: 420 }}>
        <CardContent>
          <Typography variant="h5" gutterBottom>{mode === 'login' ? 'Sign in' : 'Create account'}</Typography>
          <Box component="form" onSubmit={submit} sx={{ display: 'grid', gap: 2 }}>
            {mode === 'register' && (
              <TextField label="Full name" value={fullName} onChange={(event) => setFullName(event.target.value)} required />
            )}
            <TextField label="Email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} required />
            <TextField label="Password" type="password" value={password} onChange={(event) => setPassword(event.target.value)} required />
            {error && <Alert severity="error">{error}</Alert>}
            <Button type="submit" variant="contained">{mode === 'login' ? 'Sign in' : 'Register'}</Button>
            <Link component={RouterLink} to={mode === 'login' ? '/register' : '/login'}>
              {mode === 'login' ? 'Need an account?' : 'Already registered?'}
            </Link>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};
