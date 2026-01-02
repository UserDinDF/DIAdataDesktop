# DIAdata Desktop

DIAdata Desktop is a Windows desktop application built with **.NET (WPF)** that provides
a fast and intuitive way to explore **DIA price feeds and market data**.

🔗 **Official DIA**  
https://github.com/diadata-org

---

## 📸 Screenshots

<p align="center">
  <img src="https://private-user-images.githubusercontent.com/146924936/531466259-977380d1-dc5c-4313-9c3c-71ae508a8aec.png?jwt=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3NjczNTY2MjAsIm5iZiI6MTc2NzM1NjMyMCwicGF0aCI6Ii8xNDY5MjQ5MzYvNTMxNDY2MjU5LTk3NzM4MGQxLWRjNWMtNDMxMy05YzNjLTcxYWU1MDhhOGFlYy5wbmc_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjYwMTAyJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI2MDEwMlQxMjE4NDBaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT00M2EzMzUzNGU5ZTU2ZGMwZTUyZjU5YWNjNmVlODVkNWI5M2IwOTgwMmMyMjliZmM0ZDdhM2ZhYjZiYTZkYjI1JlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCJ9.jejidSVH1Q3KImTCwsp0yIb5CiB_o3zGX_DMIn8jlCc" width="48%" />
  </p>
  <p align="center">
  <img src="https://private-user-images.githubusercontent.com/146924936/531466247-5c55327a-49a5-47ec-a206-d6cfb8635e50.png?jwt=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3NjczNTY2MjAsIm5iZiI6MTc2NzM1NjMyMCwicGF0aCI6Ii8xNDY5MjQ5MzYvNTMxNDY2MjQ3LTVjNTUzMjdhLTQ5YTUtNDdlYy1hMjA2LWQ2Y2ZiODYzNWU1MC5wbmc_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjYwMTAyJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI2MDEwMlQxMjE4NDBaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT0wMjg2NzdkMGI4ZTQyNmEyNmQyZDlhZDJjYzE3YzlkODc5OTMxYzQ4Yzc2MmJlYjdjMmU2NmVlYmU3ZTE1ZjM0JlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCJ9.lIi4UQ-vN2VCPFgqzprqR7fdjFqHcjux7FYM7Edj01s" width="48%" />
  </p>
  <p align="center">
    <img src="https://private-user-images.githubusercontent.com/146924936/531466238-730cbc67-e49d-4117-89d0-7c6e7e334982.png?jwt=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3NjczNTY2MjAsIm5iZiI6MTc2NzM1NjMyMCwicGF0aCI6Ii8xNDY5MjQ5MzYvNTMxNDY2MjM4LTczMGNiYzY3LWU0OWQtNDExNy04OWQwLTdjNmU3ZTMzNDk4Mi5wbmc_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjYwMTAyJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI2MDEwMlQxMjE4NDBaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT1mMDg0OWRiYmUwMTEwNjBhMDExYzg3ZTkxOTY0ZDU4YTFmZmM3NzVhZjNmNDY3MGJiMzIxN2ZjZDNiMzQzYWZmJlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCJ9.jDqdx5LdBICVLw-HJrum_VxOC7S5kkryFOVt_JvNzs8" width="48%" />
</p>
<p align="center">
  <img src="https://private-user-images.githubusercontent.com/146924936/531466256-606f4133-7fa9-446e-b9ee-7937f9b7d48b.png?jwt=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3NjczNTY2MjAsIm5iZiI6MTc2NzM1NjMyMCwicGF0aCI6Ii8xNDY5MjQ5MzYvNTMxNDY2MjU2LTYwNmY0MTMzLTdmYTktNDQ2ZS1iOWVlLTc5MzdmOWI3ZDQ4Yi5wbmc_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjYwMTAyJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI2MDEwMlQxMjE4NDBaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT0zY2EzNjJkMzk4NjM0ZDcyZGNkMDBjMjQyNGRjZGJkNmUyMTE4YmI1MDJiYzZmMmM0MzU1ZWYzYTdiZWYwZjkwJlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCJ9.inS8OGXBenAi-tcxyrQooGHYYQf0J54_erm6DAtdfgc" width="48%" />
  </p>
  <p align="center">
  <img src="https://private-user-images.githubusercontent.com/146924936/531466252-40d566eb-8028-4d3b-a0bc-9b9d5e803a43.png?jwt=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3NjczNTY2MjAsIm5iZiI6MTc2NzM1NjMyMCwicGF0aCI6Ii8xNDY5MjQ5MzYvNTMxNDY2MjUyLTQwZDU2NmViLTgwMjgtNGQzYi1hMGJjLTliOWQ1ZTgwM2E0My5wbmc_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjYwMTAyJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI2MDEwMlQxMjE4NDBaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT01YmU5MjQwM2QwMmNkYTk0MDZlZjlkMzI1ODY4ZTIwNWEzNTNkY2MxNDIzMDY4NjFhNWE0NGYzNWY0YmMwNjM1JlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCJ9.pfCGfM-OUMJsiB4djanqbhMdBfapTUin6kG76gEpPm0" width="48%" />
</p>

---

## ✨ Features
- Digital asset price feeds (tokens, volume, sources)
- Exchange overview and statistics
- Real World Assets (commodities, forex, ETFs, equities)
- Favorites system (assets, exchanges, RWAs)
- Auto-refresh with configurable intervals
- Local persistence using SQLite

---

## 🖥️ Platform
- Windows (x64)
- Self-contained build (no .NET installation required)

---

## ⚠️ Status
This project is an **early public preview**.  
The application is under active development.

---

## 📦 Installation
1. Download the latest release from  
   👉 https://github.com/UserDinDF/DIAdataDesktop/releases
2. Extract the ZIP
3. Run `DIAdataDesktop.exe`

## 🧑‍💻 Run in Visual Studio 2026 (Development)

To run the application locally in **Visual Studio 2026**, follow these steps:

### Requirements
- Windows 10 or newer
- Visual Studio 2026
- .NET SDK (as required by the solution)
- Workload: **.NET Desktop Development**

### Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/UserDinDF/DIAdataDesktop.git
