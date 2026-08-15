# SygniaFrontend

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 18.0.7.

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via a platform of your choice. To use this command, you need to first add a package that implements end-to-end testing capabilities.

## gRPC-Web client codegen

The `.proto` files in `proto/` are copied verbatim from `Sygnia.Backend/src/Sygnia.Presentation/Protos/`
(the backend is the source of truth — never hand-edit the copies here). Generated TypeScript/JS client
stubs live in `src/app/grpc/` and are checked into source control, so a normal `npm ci && npm start`
works without anyone needing `protoc` installed.

To regenerate the stubs after a proto file changes:

```bash
npm install
npm run gen:proto
```

`npm install` pulls in everything codegen needs:
- `grpc-tools` (devDependency) bundles a `protoc.exe` binary. **This is pinned to exactly `1.11.0`** —
  newer `grpc-tools` releases (1.12.x, 1.13.x) ship a Windows `protoc.exe` that requires
  `ucrtbased.dll` (the Visual Studio *Debug* Universal CRT), which is not present on a normal
  dev machine and makes `protoc.exe` fail to start with `STATUS_DLL_NOT_FOUND`. `1.11.0` ships a
  correctly built release binary. If you ever bump this version, verify `protoc.exe --version`
  actually runs on Windows before relying on it.
- `protoc-gen-grpc-web` (devDependency, the `protoc-gen-grpc-web-npm` package) downloads the
  official `protoc-gen-grpc-web` plugin binary for your platform as a postinstall step. This is
  the plugin that turns protobuf definitions into gRPC-Web client stubs; it's a separate binary
  from `protoc` itself and is not bundled by `grpc-tools`.

`npm run gen:proto` runs `scripts/gen-proto.ps1`, which invokes `protoc.exe` directly (via
`grpc-tools`) with the `protoc-gen-grpc-web` plugin, producing for each proto file:
- `*_pb.js` + `*_pb.d.ts` — protobuf message classes (from `google-protobuf`'s `js_out`)
- `*ServiceClientPb.ts` — the gRPC-Web client stub (from `grpc-web`'s `grpc-web_out`)

## Dev server / google-protobuf CommonJS

`angular.json`'s `serve.options.prebundle.exclude: ["google-protobuf", "google-protobuf/google/protobuf/timestamp_pb.js"]`
and `build.options.allowedCommonJsDependencies` are load-bearing, not optional tidiness. `google-protobuf`'s
generated CommonJS code uses a dynamic-require pattern that Vite's dev-time dependency pre-bundler cannot
statically resolve, which crashes `ng serve` before the app even bootstraps (production `ng build` was always
fine, since the production build doesn't go through Vite's pre-bundler). The fix is excluding `google-protobuf`
from pre-bundling. Do not remove either setting without re-verifying `ng serve` still renders both `/` and a
gRPC-dependent route such as `/accounts`.

## Known branding gaps

There is no real Sygnia logo asset available, so the nav bar uses a text wordmark instead of an image logo.
`public/favicon.ico` is also still the unmodified Angular CLI default favicon, pending a real Sygnia brand asset.

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
