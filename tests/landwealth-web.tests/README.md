# landwealth-web.tests

Frontend unit and component tests for `landwealth-web` are executed using **Vitest** and **React Testing Library**.

## Running Tests
To run all frontend tests:
```bash
cd src/landwealth-web
npm run test
```

## Test Structure
Tests are co-located alongside features and utility modules (e.g., `src/utils/currencyFormatter.test.ts`).
Vitest environment is configured with `jsdom` and `@testing-library/jest-dom` in `src/landwealth-web/vite.config.ts`.
