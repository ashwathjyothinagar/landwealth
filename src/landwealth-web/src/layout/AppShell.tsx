import React from 'react';
import { Link as RouterLink, Navigate, Outlet, useNavigate } from 'react-router-dom';
import { AppBar, Box, Button, Toolbar, Typography } from '@mui/material';
import LandscapeIcon from '@mui/icons-material/Landscape';
import { useAuth } from '../auth/AuthContext';

const links = [
  ['/', 'Dashboard'],
  ['/properties', 'Properties'],
  ['/accounts', 'Accounts'],
  ['/transactions', 'Transactions'],
  ['/monthly-payments', 'Bills'],
  ['/reminders', 'Reminders'],
  ['/reports', 'Reports'],
  ['/audit', 'Audit'],
] as const;

export const AppShell: React.FC = () => {
  const { token, name, logout } = useAuth();
  const navigate = useNavigate();
  if (!token) {
    return <Navigate to="/login" replace />;
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="static" elevation={0}>
        <Toolbar sx={{ gap: 1, flexWrap: 'wrap' }}>
          <LandscapeIcon />
          <Typography variant="h6" sx={{ fontWeight: 700, mr: 2 }}>LandWealth</Typography>
          {links.map(([to, label]) => (
            <Button key={to} color="inherit" component={RouterLink} to={to}>{label}</Button>
          ))}
          <Box sx={{ flexGrow: 1 }} />
          <Typography variant="body2">{name}</Typography>
          <Button color="inherit" onClick={() => { logout(); navigate('/login'); }}>Sign out</Button>
        </Toolbar>
      </AppBar>
      <Box component="main" sx={{ flexGrow: 1, p: { xs: 2, md: 4 } }}>
        <Outlet />
      </Box>
    </Box>
  );
};
