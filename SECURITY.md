# Security Policy

## Reporting a vulnerability

Please **do not** open a public issue for security problems. Use GitHub's
[private vulnerability reporting](../../security/advisories/new) instead.

## Before deploying

MagdyPOS ships with development defaults in `appsettings.json`. Before using it with real data:

- Change `Jwt:Key` to a long random secret.
- Change or remove the `Seed` passwords.
- Run it behind HTTPS if it is reachable from other machines.
- Back up `magdypos.db` regularly (the `updater` tool copies it to a USB drive).
