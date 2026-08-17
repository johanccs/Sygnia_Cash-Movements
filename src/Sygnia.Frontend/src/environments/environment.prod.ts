export const environment = {
  production: true,
  // The `production` build config (used by the frontend Dockerfile / docker-compose /
  // run-dockerhub.ps1) always talks to the `presentation` container, published on the host
  // at :8080 (see docker-compose.yml). This is a different target from environment.ts's
  // :5058, which matches Sygnia.Presentation running natively via run-local.ps1.
  grpcUrl: 'http://localhost:8080',
};
