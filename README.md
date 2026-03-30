# V9 TECH - Recruitment Management System (Job Portal)

![.NET](https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular_17-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![TailwindCSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=for-the-badge&logo=tailwind-css&logoColor=white)
![Gemini AI](https://img.shields.io/badge/Gemini_AI-8E75B2?style=for-the-badge&logo=googlebard&logoColor=white)

V9 TECH is a comprehensive, full-stack recruitment management portal designed to automate and streamline the hiring lifecycle for HR professionals and provide a seamless job-seeking experience for candidates.

## Key Features

### 👨‍💼 For Candidates (Client Side)
* **Smart Job Search:** Advanced filtering by location, salary, and job type.
* **Easy Application:** Drag-and-drop CV upload with real-time PDF preview.
* **Application Tracking:** Monitor interview status and receive offer letters.
* **V9 Assistant (AI Chatbot):** Integrated with Google Gemini 2.5 API to answer FAQs and match jobs in real-time, featuring a robust rule-based fallback mechanism.

### 🏢 For HR & Admin (Management Dashboard)
* **Identity Management:** Role-Based Access Control (RBAC) utilizing JWT Authentication.
* **Job & CV Management:** Create job postings, manage incoming applications, and track candidate pipelines.
* **Automated Offers:** Generate standard Offer Letters and preview emails before sending.
* **Analytics Dashboard:** Visual representation of recruitment funnels and HR performance metrics using `ng2-charts`.

## 🛠️ Tech Stack
* **Backend:** ASP.NET Core 8.0 Web API, Entity Framework Core, Clean Architecture.
* **Frontend:** Angular 17, TypeScript, Tailwind CSS.
* **Database:** Microsoft SQL Server.
* **Integrations:** Google OAuth 2.0 (Login), Google Gemini API (Chatbot).

## 📂 Project Structure
* `/Client`: Frontend source code (Angular 17).
* `/UTC_DATN`: Backend source code (.NET 8 Web API).
* `DB.sql`: Database schema and seed data.

## Getting Started

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js](https://nodejs.org/) (v18 or higher) & Angular CLI
* SQL Server

### 1. Setup Backend
```bash
cd UTC_DATN
# Update your Connection String in appsettings.json
dotnet ef database update
dotnet run
