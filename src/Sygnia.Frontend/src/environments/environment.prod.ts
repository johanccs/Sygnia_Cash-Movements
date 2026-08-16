export const environment = {
  production: true,
  // Matches run-local.ps1, which runs Sygnia.Presentation natively on :5058 (not the
  // `presentation` container on :8080) so the WCF gateway's TLS endpoint is available too.
  grpcUrl: 'http://localhost:5058',
};
