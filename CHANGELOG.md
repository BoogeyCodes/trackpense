# Changelog

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- `CategorySpend.Remaining` — budget left for a category (negative when over, null when
  unbudgeted).
- `MonthlySummary.SavingsRate` — share of income kept this month (Net / Income).

## [0.1.0]

### Added
- Personal finance tracker in Blazor: transactions, budgets and a live dashboard.
- Core services over EF Core/SQLite with an IDbContextFactory for Blazor Server.
