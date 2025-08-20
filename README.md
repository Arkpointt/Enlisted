# Enlisted

A professional, production-ready Bannerlord mod project. This repository contains the Enlisted mod source code targeting .NET Framework 4.7.2 and .NET Standard 2.0 helper libraries.

Project highlights:
- Clean project structure and save-system integration
- Clear separation of models, behaviors, and utilities
- Compatible with Mount & Blade II: Bannerlord modding environment

Requirements
- Visual Studio 2019 or later (or JetBrains Rider)
- .NET Framework 4.7.2 developer pack
- Bannerlord SDK/binaries available locally for references

Building
1. Open the solution/project in Visual Studio.
2. Ensure references to TaleWorlds.* and other game DLLs point to your local Bannerlord installation (typically under Steam's Mount & Blade II Bannerlord/bin/Win64_Shipping_Client).
3. Build the Enlisted project in Release.
4. Deploy the resulting module files per Bannerlord modding conventions.

Notes
- This repository intentionally ignores the DECOMPILE folder and other heavy or vendor sources. You should depend on game-provided DLLs instead of embedding decompiled code.
- Target frameworks: .NET Framework 4.7.2, .NET Standard 2.0 (where applicable).

Contributing
- Fork the repo and create feature branches from main.
- Follow the coding style in .editorconfig.
- Submit PRs with a concise description, rationale, and testing notes.

License
This project is licensed under the MIT License. See LICENSE for details.