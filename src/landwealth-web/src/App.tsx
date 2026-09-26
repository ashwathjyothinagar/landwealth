import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link as RouterLink } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import {
  ThemeProvider,
  CssBaseline,
  Box,
  AppBar,
  Toolbar,
  Typography,
  Container,
  Button,
  Card,
  CardContent,
  Grid,
  Chip,
} from '@mui/material';
import LandscapeIcon from '@mui/icons-material/Landscape';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import AssessmentIcon from '@mui/icons-material/Assessment';
import { theme } from './theme';
import { formatIndianRupee } from './utils/currencyFormatter';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 1000 * 60 * 5, // 5 minutes
    },
  },
});

const NavigationBar: React.FC = () => (
  <AppBar position="static" color="primary" elevation={0}>
    <Toolbar>
      <LandscapeIcon sx={{ mr: 1.5, fontSize: 28 }} />
      <Typography variant="h6" component="div" sx={{ flexGrow: 1, fontWeight: 700 }}>
        LandWealth
      </Typography>
      <Box sx={{ display: 'flex', gap: 1 }}>
        <Button color="inherit" component={RouterLink} to="/">
          Dashboard
        </Button>
        <Button color="inherit" component={RouterLink} to="/properties">
          Properties
        </Button>
        <Button color="inherit" component={RouterLink} to="/accounts">
          Accounts
        </Button>
        <Button color="inherit" component={RouterLink} to="/transactions">
          Transactions
        </Button>
        <Button color="inherit" component={RouterLink} to="/reports">
          Reports
        </Button>
      </Box>
    </Toolbar>
  </AppBar>
);

const HomeOverview: React.FC = () => (
  <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
    <Box sx={{ mb: 4 }}>
      <Typography variant="h4" gutterBottom>
        Wealth & Property Portfolio
      </Typography>
      <Typography variant="subtitle1">
        Personal asset management, land holdings, valuations, and liquid ledger.
      </Typography>
    </Box>

    <Grid container spacing={3}>
      <Grid item xs={12} md={4}>
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
              <AccountBalanceIcon color="primary" sx={{ mr: 1 }} />
              <Typography variant="subtitle2" color="text.secondary">
                Liquid Net Worth
              </Typography>
            </Box>
            <Typography variant="h5" sx={{ fontWeight: 700, color: 'primary.main' }}>
              {formatIndianRupee(0)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Bank accounts & cash in hand
            </Typography>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12} md={4}>
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
              <LandscapeIcon color="secondary" sx={{ mr: 1 }} />
              <Typography variant="subtitle2" color="text.secondary">
                Property Valuation
              </Typography>
            </Box>
            <Typography variant="h5" sx={{ fontWeight: 700, color: 'secondary.main' }}>
              {formatIndianRupee(0)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Estimated market valuation across all holdings
            </Typography>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12} md={4}>
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
              <AssessmentIcon color="success" sx={{ mr: 1 }} />
              <Typography variant="subtitle2" color="text.secondary">
                Total Net Worth
              </Typography>
            </Box>
            <Typography variant="h5" sx={{ fontWeight: 700, color: 'text.primary' }}>
              {formatIndianRupee(0)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Liquid assets + Property valuations - Liabilities
            </Typography>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12}>
        <Card sx={{ p: 2, bgcolor: '#F0FDF4', borderColor: '#BBF7D0' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Chip label="Core Axiom" color="success" size="small" />
            <Typography variant="body2" sx={{ fontWeight: 600, color: '#166534' }}>
              A Property is NOT a Bank Account.
            </Typography>
          </Box>
          <Typography variant="body2" sx={{ color: '#15803D', mt: 1 }}>
            LandWealth connects liquid accounts, real estate parcels, and categories with balanced double-entry accounting integrity.
          </Typography>
        </Card>
      </Grid>
    </Grid>
  </Container>
);

const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <Router>
          <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
            <NavigationBar />
            <Box component="main" sx={{ flexGrow: 1 }}>
              <Routes>
                <Route path="/" element={<HomeOverview />} />
                <Route path="/properties" element={<HomeOverview />} />
                <Route path="/accounts" element={<HomeOverview />} />
                <Route path="/transactions" element={<HomeOverview />} />
                <Route path="/reports" element={<HomeOverview />} />
              </Routes>
            </Box>
          </Box>
        </Router>
      </ThemeProvider>
    </QueryClientProvider>
  );
};

export default App;
