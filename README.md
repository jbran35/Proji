# Proji
A High-Performance, Secure, and Scalable Project/Task Management System - Designed to Demonstrate Enterprise .NET Patterns. 

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-Server-5C2D91?style=for-the-badge&logo=blazor&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![MediatR](https://img.shields.io/badge/MediatR-004880?style=for-the-badge)

## Introduction
Proji utilizes a Clean Architecture layout, adopts the CQRS pattern via MediatR, and makes a good-faith effort to enforce Domain-Driven-Design principles. 

Although fully functional as it stands, Proji is likely best suited as an extensible base that provides the core functionality and architectural foundation to construct a more robust or customized Project Management solution. 

## Using Proji For Project Management

![Proji Usage Demo](https://github.com/user-attachments/assets/85fa29d0-920d-41b4-8cad-82dcd5fca7a8)


## Getting Started (Local Set Up Guide)

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* [Docker Desktop (Running)](https://www.docker.com/products/docker-desktop/)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) / [VS Code](https://code.visualstudio.com/) / [JetBrains Rider](https://www.jetbrains.com/rider/download/)
* [Git](https://github.com/git-guides/install-git)


### Clone the Repository To Your Machine
```
git clone https://github.com/jbran35/Proji.git
cd Proji
```

### Spin Up SQL Server & Redis
Ensure Docker Desktop is running. Open a terminal in the root directory (where the docker-compose.yml file is located) and run the following command to spin up the Microsoft SQL Server and Redis containers in the background:

```
docker-compose up -d
```

### Run the Application
You can run the application through an IDE (Visual Studio / VS Code / Rider) or the .NET CLI. 

#### Using an IDE [[Video]](https://youtu.be/pPaJ3wLPymQ)
   * Open Proji.sln.
   * Right-click the Solution in Solution Explorer and select Configure Startup Projects.
   * Select Multiple startup projects and set both TaskManager.API and TaskManager.Presentation to Start, ensuring the API Project is set to start first.
   * Press F5 or click the green Start button.
    
#### Using .NET CLI [[Video]](https://youtu.be/6qtNGp1O4_s)
   * Open two separate terminal windows from the root directory:
  
   * **In Terminal 1:**
      
     ```
     dotnet run --project TaskManager.API --launch-profile https
     ```
  
   * **In Terminal 2:**
  
     ```
     dotnet run --project TaskManager.Presentation --launch-profile https
     ```

  * There is no need to manually run Entity Framework update-database commands. On the very first API startup, the DbInitializer.cs service will automatically detect the fresh SQL Docker container, apply all EF Core migrations, and seed the database with a default user and sample projects.

   * **In Browser:**
     ```
     https://localhost:7146
     ```

### Configuration & Default Credentials
By default, the application is configured to run out-of-the-box using the `appsettings.Development.json` profiles. 

If you wish to connect a database management tool (like SSMS or Azure Data Studio) to the running Docker container, the default local credentials are:

#### SQL Server
* **Server:** `127.0.0.1,1433`
* **User:** `sa`
* **Password:** `SuperSecret123!`

#### Redis
* **Host:** `127.0.0.1`
* **Port:** `6379`
## The Tech Stack

* **Backend:** .NET 10, ASP.NET Core Web API
* **Frontend:** Blazor Server, Blazor Bootstrap 3.5
* **Data & State:** Entity Framework Core, SQL Server
* **Caching:** Redis (Distributed Cache), IMemoryCache
* **Architecture/Patterns:** Clean Architecture, CQRS, MediatR, Result Pattern, FluentValidation

## Viewing API Documentation (Swagger)

## Running Test Suite

## Architectural Highlights

* **BFF Authentication:** The API is constructed to strictly issue a JWT upon successful login. The Blazor Server client then manages the JWT and stores it in a cookie. In turn, any additional clients (e.g., a native mobile app) can leverage the existing authentication system without any changes to the API needed.
* **CQRS & MediatR:** The API controllers are intentionally thin and simply route Commands/Queries to isolated handlers.
* **RESTful API Endpoints:** API endpoints are resource-based and utilize standard HTTP methods and standardized error handling.
* **Domain-Driven Design:** Utilizes rich entities, Value Objects, Domain Events, and the Result<T> pattern for elegant control flow rather than relying on exceptions.
* **Performance Optimizations:** Utilizes a Dual-Layer (L1/L2) Cache-Aside Architecture: IMemoryCache and scoped StateServices in the Presentation Layer reduce redundant API calls, while an IDistributedCache (Redis) in the Application Layer minimizes redundant database reads.
* **Real-Time Communication (SignalR):** Leverages a SignalR Hub over WebSockets to push live, event-driven UI updates to connected clients when shared resources (like Tasks or Projects) are modified. This highly extensible foundation can easily be scaled in the future to support robust inter-user chat or a centralized notification center.

## Structure

| Project | Responsibility & Contents |
| :--- | :--- |
| **`TaskManager.Domain`** | Business logic, Entities, Enums, Value Objects, Domain Events, Result Implementation, Repository Interfaces. |
| **`TaskManager.Application`** | CQRS, Events/Event Handlers, Mappers, DTOs, Validators/Validation Behavior, ICacheInvalidator, ITokenService, IUpdateNotificationService. Dependency Injection. |
| **`TaskManager.Infrastructure`** | EF Core DbContext, Seeding Script, Migrations, Repositories, Redis Implementation, TokenService, UnitOfWork, JWTSettings. |
| **`TaskManager.API`** | RESTful Endpoints in controllers, Login/Register Models, Hubs, TodoItemNotificationService implementation. |
| **`TaskManager.Presentation`** | Blazor Server UI Components, ViewModels, StateServices/Presentation Caches, API Client, UI Enums. |

## Current Limitations/Future Enhancements
* Pagination options for the My Projects page to maintain performance when loading large datasets.
* Expanded SignalR hub/notification system for inter-user messaging and a centralized notification center.
* Expanding authentication to adopt OAuth 2.0 standard.
* A more robust testing suite.
* Thorough WCAG Audit.
* Allowing explicitly ordered lists and order manipulation methods.

## Contact - Joshua Brander
* **GitHub:** https://github.com/jbran35
* **Email:** jbrander35@gmail.com

## Project Link
* [HERE](https://github.com/jbran35/Proji)
