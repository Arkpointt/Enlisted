# Contributing to Enlisted

Thanks for your interest in contributing!

Getting started
- Fork the repo and create a feature branch from main.
- Use Visual Studio 2019+ or Rider.
- Target frameworks: .NET Framework 4.7.2, .NET Standard 2.0.
- References to TaleWorlds.* must point to your local Bannerlord installation (typically Steam/…/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client).
- Do not commit decompiled/vendor sources. The DECOMPILE folder is ignored by .gitignore on purpose.

Coding style
- Follow .editorconfig settings (4-space indent, System usings first, etc.).
- Prefer small, focused changes with clear commit messages.

Pull requests
- Fill out the PR template: summary, rationale, testing notes.
- Keep PRs scope-limited and easy to review.
- Link related issues.

Branches
- main: stable, releasable branch.
- feature/* and fix/*: short-lived branches for work.

Testing
- Manually test in-game for gameplay changes.
- Include repro steps and screenshots/logs when possible.

License
- By contributing, you agree your contributions are licensed under the MIT License in this repository.
