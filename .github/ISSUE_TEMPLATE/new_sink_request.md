---
name: 🔌 New Sink Plugin Proposal
about: Propose or request a new LoggerHelper sink plugin (e.g. Discord, Slack, Loki)
title: '[Sink Plugin] Add Sink.<TargetName>'
labels: ['enhancement', 'good first issue', 'help wanted']
assignees: ''
---

### 💡 Sink Plugin Proposal

**Sink Name:** (e.g. Discord, Slack, Grafana Loki, Sentry)

### Description & Use Case
Explain why this sink would be valuable and how developers would typically use it.

### Target Serilog Package
Which underlying Serilog sink package will be used? (e.g. `Serilog.Sinks.Discord`, `Serilog.Sinks.Grafana.Loki`)

### Configuration Sample
Provide an example of what the `appsettings.json` section should look like:

```json
{
  "LoggerHelper": {
    "Sinks": {
      "TargetName": {
        "WebhookUrl": "https://..."
      }
    }
  }
}
```

### Are you interested in contributing this yourself?
- [ ] Yes, I would love to submit a Pull Request! (Check out `CONTRIBUTING.md` for guidance)
- [ ] No, just requesting the feature.
