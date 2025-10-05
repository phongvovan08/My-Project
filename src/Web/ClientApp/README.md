# MyProject.Web

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 15.2.8.

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via a platform of your choice. To use this command, you need to first add a package that implements end-to-end testing capabilities.

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.io/cli) page.

# Phân tích Project

Dựa trên các tài liệu được cung cấp, đây là một dự án **ASP.NET Core** với kiến trúc **Clean Architecture** sử dụng **.NET 9.0** và **Aspire**. Dưới đây là phân tích chi tiết:

## 1. **Kiến trúc tổng quan**

Dự án tuân theo **Clean Architecture** với các layer được phân tách rõ ràng:

```
├── Domain (Core business logic)
├── Application (Use cases & business rules)
├── Infrastructure (External concerns)
├── Web (Presentation layer)
├── AppHost (Aspire orchestration)
└── ServiceDefaults (Shared configurations)
```

## 2. **Domain Layer** 
**Mục đích**: Chứa business logic cốt lõi, entities, value objects, domain events

**Thành phần chính**:
- **Entities**: `TodoList`, `TodoItem` với base classes `BaseEntity`, `BaseAuditableEntity`
- **Value Objects**: `Colour` (immutable, equality comparison)
- **Enums**: `PriorityLevel` (None, Low, Medium, High)
- **Domain Events**: `TodoItemCreatedEvent`, `TodoItemCompletedEvent`, `TodoItemDeletedEvent`
- **Constants**: `Roles.Administrator`, `Policies.CanPurge`

**Đặc điểm nổi bật**:
- Không phụ thuộc vào layer khác
- Sử dụng Domain Events pattern
- Value Objects với validation

## 3. **Application Layer**
**Mục đích**: Orchestrate domain logic, implement use cases

**Patterns được sử dụng**:
- **CQRS** (Command Query Responsibility Segregation) với **MediatR**
- **Pipeline Behaviors**:
  - `LoggingBehaviour`: Log requests
  - `ValidationBehaviour`: FluentValidation
  - `AuthorizationBehaviour`: Role/Policy-based authorization
  - `PerformanceBehaviour`: Track long-running requests (>500ms)
  - `UnhandledExceptionBehaviour`: Global exception handling

**Use Cases**:

### Commands (Modifications):
- `CreateTodoList`, `UpdateTodoList`, `DeleteTodoList`, `PurgeTodoLists`
- `CreateTodoItem`, `UpdateTodoItem`, `UpdateTodoItemDetail`, `DeleteTodoItem`

### Queries (Read operations):
- `GetTodos`: Lấy tất cả TodoLists với Items
- `GetTodoItemsWithPagination`: Phân trang TodoItems
- `GetWeatherForecasts`: Demo endpoint

**Validators**:
- FluentValidation cho tất cả commands
- Custom async validators (VD: `BeUniqueTitle`)

**DTOs & Mapping**:
- AutoMapper cho mapping
- `PaginatedList<T>` cho pagination
- `LookupDto`, `TodoItemBriefDto`, `TodoListDto`

## 4. **Infrastructure Layer**
**Mục đích**: Implement interfaces từ Application, xử lý external concerns

**Thành phần**:

### Data Access:
- **EF Core** với SQL Server
- `ApplicationDbContext` extends `IdentityDbContext`
- **Interceptors**:
  - `AuditableEntityInterceptor`: Tự động set Created/Modified fields
  - `DispatchDomainEventsInterceptor`: Publish domain events khi SaveChanges

### Identity:
- ASP.NET Core Identity
- `ApplicationUser` extends `IdentityUser`
- `IdentityService`: Implements `IIdentityService`
- Role-based và Policy-based authorization

### Database Initialization:
- `ApplicationDbContextInitialiser`: Seed data
- Default admin user: `administrator@localhost` / `Administrator1!`
- Sample TodoList với 4 items

### Configuration:
- Entity configurations (Fluent API)
- Owned types cho Value Objects (`Colour`)

## 5. **Web Layer (Presentation)**
**Mục đích**: API endpoints, SPA hosting, HTTP concerns

**Stack**:
- **ASP.NET Core Minimal APIs**
- **Angular 18** SPA (ClientApp folder)
- **NSwag** cho OpenAPI/Swagger
- **Razor Pages** cho Identity UI

**API Endpoints** (via `EndpointGroupBase`):
```
/api/TodoLists
  GET    - GetTodoLists
  POST   - CreateTodoList
  PUT    /{id} - UpdateTodoList
  DELETE /{id} - DeleteTodoList

/api/TodoItems
  GET    - GetTodoItemsWithPagination
  POST   - CreateTodoItem
  PUT    /{id} - UpdateTodoItem
  PUT    /UpdateDetail/{id} - UpdateTodoItemDetail
  DELETE /{id} - DeleteTodoItem

/api/WeatherForecasts
  GET    - GetWeatherForecasts
```

**Đặc điểm**:
- Typed Results (`Results<T>`, `Ok<T>`, `Created<T>`)
- Global exception handling với `CustomExceptionHandler`
- Authentication required cho tất cả endpoints
- SPA proxy cho development

## 6. **AppHost (Aspire Orchestration)**
**Mục đích**: Orchestrate microservices/components

**Cấu hình**:
```csharp
var sql = builder.AddSqlServer("sql");
var database = sql.AddDatabase("MyProjectDb");
builder.AddProject<Projects.Web>("web")
    .WithReference(database)
    .WaitFor(database);
```

**Dashboard endpoints**:
- HTTPS: `https://localhost:17078`
- HTTP: `http://localhost:15010`
- OTLP: `https://localhost:21118`

## 7. **ServiceDefaults**
**Mục đích**: Shared configurations cho Aspire services

**Includes**:
- OpenTelemetry (Metrics, Tracing, Logging)
- Service Discovery
- Resilience (HTTP retry policies)
- Health checks (`/health`, `/alive`)

## 8. **Technology Stack**

| Category | Technology |
|----------|-----------|
| **Framework** | .NET 9.0 |
| **Orchestration** | .NET Aspire |
| **Database** | SQL Server |
| **ORM** | Entity Framework Core |
| **Identity** | ASP.NET Core Identity |
| **API** | Minimal APIs |
| **Frontend** | Angular 18 |
| **Documentation** | NSwag (OpenAPI) |
| **Validation** | FluentValidation |
| **Mapping** | AutoMapper |
| **Mediator** | MediatR |
| **Guards** | Ardalis.GuardClauses |
| **Testing** | Jasmine, Karma |

## 9. **Design Patterns & Principles**

1. **Clean Architecture**: Separation of concerns, dependency inversion
2. **CQRS**: Command/Query separation
3. **Mediator Pattern**: Decoupled request handling
4. **Repository Pattern**: Abstracted via `IApplicationDbContext`
5. **Unit of Work**: Built into EF Core's `SaveChangesAsync`
6. **Domain Events**: Decouple domain logic
7. **Pipeline Pattern**: MediatR behaviors
8. **Value Objects**: Immutable, equality-based
9. **Specification Pattern**: (implicit in queries)
10. **Dependency Injection**: Constructor injection throughout

## 10. **Security Features**

- **Authentication**: Cookie-based (ASP.NET Core Identity)
- **Authorization**: 
  - Role-based (`[Authorize(Roles = "Administrator")]`)
  - Policy-based (`[Authorize(Policy = "CanPurge")]`)
  - Custom `AuthorizeAttribute` for commands
- **Audit Trail**: Automatic tracking of Created/Modified by/at
- **Validation**: Input validation via FluentValidation
- **HTTPS**: Enforced in production

## 11. **Development Workflow**

### Local Development:
```bash
# Start Aspire AppHost
dotnet run --project src/AppHost

# Angular dev server (proxied)
cd src/Web/ClientApp
npm start
```

### Database:
- LocalDB: `(localdb)\mssqllocaldb`
- Auto-initialized on startup (Development only)
- **⚠️ Note**: Uses `EnsureDeleted()` + `EnsureCreated()` - không dùng cho production

### API Testing:
- Swagger UI: `/api`
- HTTP file: `src/Web/Web.http` (Visual Studio)

## 12. **Strengths (Điểm mạnh)**

✅ Clean, maintainable architecture  
✅ Strong separation of concerns  
✅ Comprehensive validation & error handling  
✅ Audit trail built-in  
✅ Type-safe DTOs  
✅ Domain events for extensibility  
✅ Modern .NET 9 + Aspire  
✅ OpenTelemetry observability  

## 13. **Potential Improvements (Cải tiến)**

⚠️ **Database Migration**: Đổi từ `EnsureCreated()` sang EF Migrations  
⚠️ **Unit Tests**: Chưa thấy test projects  
⚠️ **Logging**: Có thể thêm structured logging (Serilog)  
⚠️ **Caching**: Chưa có caching layer  
⚠️ **Rate Limiting**: Cân nhắc thêm cho production  
⚠️ **API Versioning**: Cho backward compatibility  
⚠️ **Docker**: Chưa thấy Dockerfile  

## 14. **Use Case Example Flow**

**Ví dụ: Create TodoItem**

```
1. HTTP POST /api/TodoItems
   ↓
2. TodoItems endpoint receives CreateTodoItemCommand
   ↓
3. MediatR Pipeline:
   - LoggingBehaviour logs request
   - ValidationBehaviour validates (title ≤ 200 chars)
   - AuthorizationBehaviour checks if authenticated
   ↓
4. CreateTodoItemCommandHandler:
   - Creates TodoItem entity
   - Adds TodoItemCreatedEvent
   - Saves to DB
   ↓
5. DispatchDomainEventsInterceptor:
   - Publishes TodoItemCreatedEvent
   ↓
6. TodoItemCreatedEventHandler:
   - Logs event
   ↓
7. Response: 201 Created with item ID
```

---

**Tóm lại**: Đây là một dự án **enterprise-grade** với kiến trúc chuyên nghiệp, phù hợp cho các ứng dụng quy mô vừa đến lớn, dễ maintain và extend.