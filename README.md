# Construction Services Management System

A web-based Construction Services Management System designed to help construction service providers manage clients, services, bookings, schedules, billing, payments, tools, and reports in one centralized system.

## Overview

The Construction Services Management System is designed for construction service providers such as Alexis, who offers the following services:

- Plumbing
- Electrical
- Masonry
- Carpentry Works
- Other construction services

The system helps replace manual record keeping with a centralized system for managing customer information, service rates, bookings, schedules, billing, and payments.

## Features

### Client Management

- Add new clients
- Edit client information
- View client profiles
- Search clients
- Manage client contact information
- Manage client addresses

### Service Management

- Add construction services
- Edit service information
- Manage service rates
- View available services

### Booking Management

- Create customer bookings
- Select construction services
- Record scheduled visit dates
- Calculate booking amounts
- Manage booking information

### Weekly Schedule

- View weekly customer schedules
- Monitor upcoming appointments
- Organize scheduled construction service visits

### Billing Management

- Automatically generate a billing transaction after a successful booking
- View customer billing information
- View billing statements
- Monitor billing status

### Payment Management

- Record customer payments
- Track payment transactions
- Monitor outstanding balances
- Update payment status

### Reports

- View billing statements per customer
- Review customer billing transactions
- Monitor payment and billing information

### Tool Management

- Add and manage construction tools
- Update tool information
- Monitor available tools

### Dashboard

- View an overview of the system
- Monitor clients
- Monitor bookings
- Monitor services
- Monitor billing and payment activities

### Unit Testing

The project includes a dedicated unit testing project:

ConstructionServicesManagementSystem.Tests`

Unit tests are included for:

- Booking Service
- Client Service
- Dashboard Service
- Hourly Rate Service
- Payment Service
- Schedule Service
- Tool Service

## Technologies Used

- C#
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Razor Views
- Bootstrap
- HTML
- CSS
- JavaScript
- .NET
- xUnit

## Project Structure

ConstructionServicesManagementSystem/
│
├── ConstructionServicesManagementSystem/
│   ├── Controllers/
│   │   ├── BookingController.cs
│   │   ├── ClientController.cs
│   │   ├── PaymentController.cs
│   │   ├── ReportsController.cs
│   │   ├── ScheduleController.cs
│   │   ├── ServiceController.cs
│   │   └── ToolController.cs
│   │
│   ├── Data/
│   │   └── AppDbContext.cs
│   │
│   ├── Enums/
│   │   ├── BillingStatus.cs
│   │   └── ...
│   │
│   ├── Models/
│   │   ├── Booking.cs
│   │   ├── Billing.cs
│   │   ├── Client.cs
│   │   ├── Payment.cs
│   │   ├── Schedule.cs
│   │   ├── Tool.cs
│   │   └── ...
│   │
│   ├── ViewModels/
│   │   ├── Booking/
│   │   ├── Client/
│   │   ├── Payment/
│   │   └── ...
│   │
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IBookingService.cs
│   │   │   ├── IClientService.cs
│   │   │   └── ...
│   │   ├── BookingService.cs
│   │   ├── ClientService.cs
│   │   ├── DashboardService.cs
│   │   ├── HourlyRateService.cs
│   │   ├── PaymentService.cs
│   │   ├── ScheduleService.cs
│   │   └── ToolService.cs
│   │
│   ├── Views/
│   │   ├── Booking/
│   │   ├── Client/
│   │   ├── Payment/
│   │   ├── Reports/
│   │   ├── Schedule/
│   │   ├── Service/
│   │   └── Tool/
│   │
│   ├── wwwroot/
│   ├── Migrations/
│   ├── Properties/
│   ├── appsettings.json
│   ├── Program.cs
│   └── ConstructionServicesManagementSystem.csproj
│
├── ConstructionServicesManagementSystem.Tests/
│   ├── Services/
│   │   ├── BookingServiceTests.cs
│   │   ├── ClientServiceTests.cs
│   │   ├── DashboardServiceTests.cs
│   │   ├── HourlyRateServiceTests.cs
│   │   ├── PaymentServiceTests.cs
│   │   ├── ScheduleServiceTests.cs
│   │   └── ToolServiceTests.cs
|   | 
│   └── ConstructionServicesManagementSystem.Tests.csproj
│
├── ConstructionServicesManagementSystem.sln
├── .gitattributes
├── .gitignore
└── README.md
