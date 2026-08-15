# Generates gRPC-Web TypeScript/JS client stubs from the .proto files in proto/.
# Requires: npm install (installs grpc-tools, which bundles protoc, and protoc-gen-grpc-web,
# which downloads the official protoc-gen-grpc-web plugin binary as a postinstall step).
#
# NOTE: grpc-tools versions newer than 1.11.x ship a protoc.exe on Windows that requires
# ucrtbased.dll (the Visual Studio Debug Universal CRT), which is not present on a normal
# dev machine without Visual Studio's debug tooling installed. package.json therefore pins
# grpc-tools to 1.11.0, which ships a correctly built release protoc.exe. See README.md.

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$protoc = Join-Path $root "node_modules\grpc-tools\bin\protoc.exe"
$grpcWebPlugin = Join-Path $root "node_modules\protoc-gen-grpc-web\bin\protoc-gen-grpc-web.exe"

if (-not (Test-Path $protoc)) {
    throw "protoc.exe not found at $protoc - run 'npm install' first."
}
if (-not (Test-Path $grpcWebPlugin)) {
    throw "protoc-gen-grpc-web.exe not found at $grpcWebPlugin - run 'npm install' first."
}

$outDir = Join-Path $root "src\app\grpc"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

& $protoc `
    "--plugin=protoc-gen-grpc-web=$grpcWebPlugin" `
    "--js_out=import_style=commonjs,binary:$outDir" `
    "--grpc-web_out=import_style=typescript,mode=grpcwebtext:$outDir" `
    "-I" "proto" `
    "proto/movements.proto" "proto/accounts.proto" "proto/users.proto"

if ($LASTEXITCODE -ne 0) {
    throw "protoc codegen failed with exit code $LASTEXITCODE"
}

Write-Host "gRPC-Web client stubs generated in $outDir"
