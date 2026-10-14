# Security Policy

## Supported versions

Security fixes are released for the latest minor version of each package on NuGet
(`CSharpEssentials.LoggerHelper`, its `Sink.*` packages, `CSharpEssentials.LoggerHelper.Dashboard`
and `CSharpEssentials.HttpHelper`).

| Version | Supported |
|---------|-----------|
| 5.x (latest minor) | ✅ |
| < 5.0   | ❌ |

## Reporting a vulnerability

Please **do not open a public issue** for security problems.

Report it privately through GitHub:
[**Report a vulnerability**](https://github.com/alexbypa/CSharp.Essentials/security/advisories/new)
(Security tab → Advisories → Report a vulnerability).

Please include:

- the affected package and version;
- a description of the issue and its impact;
- steps or a minimal project to reproduce it.

You can expect an acknowledgement within 7 days. Once the issue is confirmed, a fix is released
as a patch version and credited in [CHANGELOG.md](CHANGELOG.md) unless you prefer to stay anonymous.

## Scope

In scope: code in this repository and the packages published from it, for example a secret that
bypasses `SensitiveDataMasking`, log injection, or credentials leaking into logs or the Dashboard.

Out of scope: vulnerabilities in third-party dependencies (Serilog, Polly, sink libraries) — please
report those upstream; Dependabot keeps them updated here.
