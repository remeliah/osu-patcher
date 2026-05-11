# osu! patcher

Windows launcher and runtime patcher for starting `osu!.exe` against the refx server with local runtime patches.

## Projects

- `launcher/` - Tauri desktop app and installer bundle.
- `patcher-cli/OsuPatcher.Cli/` - Windows CLI that starts osu! and injects the runtime patcher.
- `patcher/OsuPatcher.Runtime/` - .NET Framework runtime patcher DLL injected into osu!.
- `refx-pp/` - Rust FFI library used by the runtime patcher for performance calculation.

## Requirements

- Windows
- .NET SDK with .NET Framework 4.7.2 targeting support
- Rust toolchain with the `i686-pc-windows-msvc` target
- Node.js and npm
- NuGet packages restored into `packages/`

Install the Rust target if needed:

```powershell
rustup target add i686-pc-windows-msvc
```

Install launcher dependencies:

```powershell
cd launcher
npm.cmd install
```

## Build

From the repository root:

```powershell
dotnet build patcher\OsuPatcher.Runtime.sln -c Release
dotnet build patcher-cli\OsuPatcher.Cli.sln -c Release
```

Build the FFI library:

```powershell
cd refx-pp
cargo build --release --target i686-pc-windows-msvc
```

Build the launcher installer:

```powershell
cd launcher
npm.cmd run tauri:build
```

The NSIS installer is written to:

```text
launcher/src-tauri/target/release/bundle/nsis/
```

## Development

Run the launcher in development mode:

```powershell
cd launcher
npm.cmd run tauri:dev
```

The Tauri bundle expects these release artifacts to exist:

- `patcher-cli/OsuPatcher.Cli/bin/Release/patcher-cli.exe`
- `patcher/OsuPatcher.Runtime/bin/Release/OsuPatcher.Runtime.dll`
- `patcher/OsuPatcher.Runtime/bin/Release/0Harmony.dll`
- `refx-pp/target/i686-pc-windows-msvc/release/refx_ffi.dll`

## Release

1. Build all release artifacts.
2. Run `npm.cmd run tauri:build` from `launcher/`.
3. Tag the commit, for example `v0.1.0`.
4. Upload the generated NSIS installer to the GitHub release.
