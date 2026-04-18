# Gradely Backend (.NET C#)

Backend API for managing assignments and submissions, built with **ASP.NET Core Web API** using **Clean Architecture** and **Unit of Work + Generic Repository**.

## Stack

- ASP.NET Core Web API (C#)
- SQL Server + EF Core
- ASP.NET Identity + JWT
- Swagger / Swashbuckle

## Architecture

- `Gradely.Domain` – Entities, enums, core interfaces  
- `Gradely.Application` – DTOs, services, business logic  
- `Gradely.Infrastructure` – DbContext, repositories, Unit of Work, Identity  
- `Gradely.Api` – Controllers, Program.cs, configuration

## Auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me` (JWT required)

Roles: Student, Teacher, Admin.

## Core Features

- List assignments: `GET /api/assignments`, `GET /api/assignments/{id}`
- Submit PDF: `POST /api/submissions` (`IFormFile`, PDF only, max 10MB)
- View submissions: `GET /api/submissions`, `GET /api/submissions/{id}`, `GET /api/submissions/{id}/report`
