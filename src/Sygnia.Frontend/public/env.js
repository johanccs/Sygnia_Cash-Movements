// Runtime config, overwritten by nginx's docker-entrypoint script (see nginx.conf /
// Dockerfile) from the GRPC_URL env var at container start. This checked-in copy is the
// default used by `ng serve` and any container started without GRPC_URL set.
window.__env = {
  grpcUrl: 'http://localhost:5058',
};
