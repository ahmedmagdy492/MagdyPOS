# Contributing to MagdyPOS / المساهمة في MagdyPOS

Thanks for helping! / شكرًا لمساعدتك!

## Getting started / البداية

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Fork and clone the repo, then run:
   ```bash
   dotnet run
   ```
3. Open the URL printed in the terminal. On first run you'll be asked to create the admin account.
   The SQLite database (`magdypos.db`) is created and migrated automatically.

## Making changes / إجراء التعديلات

- Open an issue first for large changes so we can agree on the approach.
- Keep pull requests focused on one thing.
- The UI is Arabic (RTL) — keep new screens consistent with the existing layout.
- Changed a model? Add a migration:
  ```bash
  dotnet ef migrations add DescribeYourChange
  ```
- Make sure `dotnet build` passes before opening the PR.

## Good first contributions / مساهمات مناسبة للبداية

- English translation / localization of the UI
- Screenshots and docs
- Receipt printing improvements
- Tests for controllers and reports

Look for issues labelled `good first issue`.
