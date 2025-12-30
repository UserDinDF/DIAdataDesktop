# DIAdataDesktop 💎

DIAdataDesktop is a Windows desktop application for working with data provided by the DIA Oracle network.
The goal of this project is to make DIA data easily accessible in a structured and usable way, without having to interact directly with raw REST or GraphQL endpoints.

The application is built as a modular WPF desktop client and is continuously extended as new DIA APIs or use cases are added.

---

## Purpose

DIAdataDesktop is intended as a practical tool for:

- developers who work with DIA data
- analysts and researchers
- traders who want a clear overview of market data
- anyone who prefers a desktop application over browser dashboards or manual API calls

The focus is on clarity, performance and extensibility rather than visual effects.

---

## Covered DIA Data

The application is designed to support the full DIA stack.  
Some endpoints are already implemented, others are planned or partially integrated.

### Digital Assets Market Data
- Token price quotation by symbol
- Token price quotation by address
- List of quotable assets

### Assets Information
- Blockchains
- Exchanges
- Centralized exchange pairs
- Asset last trades

### DeFi Data
- DeFi protocol collateral information
- DEX pool liquidity
- Slippage calculation (Uniswap V2 and compatible forks)

### Chart Data
- Exchange chart points
- Asset chart points

### Guest Quotations
- Guest symbols
- Guest quotations

### Real World Assets (RWA)
- Forex
- Commodities
- ETFs

All data is retrieved directly from official DIA REST and GraphQL APIs.

---

## Current Features

### Quotation
- Query token prices by symbol or by asset address
- Manual refresh and automatic refresh
- Clear indication of the last update time
- Designed for quick lookups and continuous monitoring

### Quoted Assets
- Table view with large datasets
- Volume, USD volume, decimals and address information
- Blockchain-based filtering
- Fast text search (symbol, name, address)
- Separate meta lists for blockchains and exchanges

### Meta Information
- Live list of supported blockchains
- Live list of exchanges
- Exchange status indicators (e.g. scraper active)

---

## Technical Overview

- C# / .NET 10
- WPF desktop application
- MVVM architecture (CommunityToolkit.Mvvm)

---

## Status

This project is under active development.
The core architecture is stable, while features and API coverage are continuously expanded.

---

## Contributions

Suggestions, bug reports and pull requests are welcome.
If you work with DIA data and miss a specific feature, feel free to open an issue.

---

## License

To be defined.
