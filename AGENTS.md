# Project Rules

- **Build Directories**: You MUST NOT spawn multiple or alternate build folders for this project (e.g. `TestExtractApp`, `wpftmp` standalone instances). 
- **Single Folder**: All builds must be compiled into the single standard directory (`bin/Release/net8.0-windows/` or `bin/Debug/net8.0-windows/`). 
- **Forbidden Action**: Do not create temporary or extraneous .csproj files or build folders just to run quick tests. If you need to test code, use the main project build output or create a scratch C# script without generating entirely new compiled apps.
- **Strict UseCase Architecture**: ALL business logic, feature implementations, settings mutations, state updates, and domain operations MUST strictly go through dedicated UseCases (`Application/UseCases/`). ViewModels and Views must never bypass UseCases to call Infrastructure services or mutate domain settings directly.
- **Strict Versioning**: You MUST always maintain proper versioning (e.g., `v1.0.1-Alpha`, `1.0.1.0`) across the project. Whenever creating release builds or archives, keep `TeamSpeakOverlay.csproj` (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`, `<InformationalVersion>`), `Domain/Entities/AppVersion.cs`, UI components, and release ZIP archives properly versioned (e.g. `TeamSpeakOverlay-v1.0.1-Alpha.zip`).
