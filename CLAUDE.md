# ThoseDaysApp

Self-hosted menstrual cycle tracker PWA — personal health data stays under user control.

## Stack
- Backend: ASP.NET Core + PostgreSQL
- Frontend: React + TypeScript (PWA)
- Auth: OIDC SSO with email/password fallback
- Observability: Serilog + OpenTelemetry → Seq
- Docker Compose deployment

## Key facts
- Generates 15-cycle predictions via statistical averaging
- SVG charts are hand-rolled (no charting library)
- WCAG AA accessibility target
