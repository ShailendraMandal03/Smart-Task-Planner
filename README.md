# SmartTaskPlanner — Full-Stack Task Management System

> A full-stack web application for intelligent task management with dependency-aware execution planning, built with **ASP.NET Core 8** and **Angular 18**.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Setup Instructions](#setup-instructions)
3. [Technology Choices](#technology-choices)
4. [Project Structure](#project-structure)
5. [Assumptions Made](#assumptions-made)
6. [How the Execution Planning Logic Works](#how-the-execution-planning-logic-works)
7. [How Keyset Pagination Works](#how-keyset-pagination-works)
8. [Design Decisions and Trade-offs](#design-decisions-and-trade-offs)
9. [Limitations and Future Improvements](#limitations-and-future-improvements)

---

## Project Overview

SmartTaskPlanner is a full-stack application that allows users to create, manage, and schedule tasks with inter-task dependencies. Its core feature is a **smart execution planner** that uses topological sorting to generate an optimal, dependency-respecting order in which tasks should be executed — prioritising high-priority tasks and minimising wasted effort.

**Key capabilities:**
- Full CRUD for tasks (Create, Read, Update, Delete)
- Keyset-based pagination (efficient, cursor-driven paging on the main task view)
- Dependency tracking (task A must complete before task B)
- Real-time cycle detection (prevents invalid dependency graphs)
- Priority-aware topological execution plan generation
- Structured error handling with RFC 7807 Problem Details
- Structured logging via Serilog
- In-memory persistence with pre-loaded seed data

---

## Setup Instructions

### Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18.x / 20.x LTS | https://nodejs.org |
| Angular CLI | 18.x | `npm install -g @angular/cli` |

---

### Backend Setup (ASP.NET Core API)

```bash
# 1. Navigate to the backend solution folder
cd SmartTaskPlanner

# 2. Restore NuGet packages
dotnet restore

# 3. Build the solution
dotnet build

# 4. Run the API (from the API project directory)
cd SmartTaskPlanner.API
dotnet run
```

The API will start at:
- **HTTP:** `http://localhost:5000`
- **Swagger UI:** `http://localhost:5000/swagger`

> **Note:** No database setup is required. The application uses an in-memory store pre-loaded with seed data.

---

### Frontend Setup (Angular)

```bash
# 1. Navigate to the frontend directory
cd SmartTaskPlanner_Frontend

# 2. Install npm dependencies
npm install

# 3. Start the development server
npm start
# or
ng serve
```

The Angular app will be available at: **`http://localhost:4200`**

> The API base URL is configured inside the Angular services. Ensure the backend is running before starting the frontend.

---

### Running Unit Tests

```bash
# Navigate to the test project
cd SmartTaskPlanner/SmartTaskPlanner.Tests

# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

---

## Technology Choices

### Backend

| Technology | Version | Rationale |
|------------|---------|-----------|
| **ASP.NET Core** | 8.0 | Mature, high-performance web framework; ideal for REST APIs with built-in DI |
| **C#** | 12 | Strongly-typed, modern language features (records, pattern matching, LINQ) |
| **FluentValidation** | 11.x | Declarative, testable validation rules decoupled from domain logic |
| **Serilog** | 10.x | Structured logging with multiple sinks (console + rolling file) |
| **Swashbuckle (Swagger)** | 6.4 | Automatic OpenAPI documentation and interactive API testing UI |
| **xUnit** | 2.x | Industry-standard .NET test framework with excellent async support |
| **Moq** | 4.x | Mocking library for isolating dependencies in unit tests |

### Frontend

| Technology | Version | Rationale |
|------------|---------|-----------|
| **Angular** | 18.2 | Component-based, opinionated framework for large-scale SPAs |
| **TypeScript** | 5.5 | Type safety across the entire frontend |
| **Bootstrap** | 5.3 | Responsive UI grid system and prebuilt components |
| **RxJS** | 7.8 | Reactive streams for handling async HTTP calls and state changes |

### Architecture

- **Clean Architecture** (also known as Onion Architecture) is employed to separate concerns and enforce dependency rules.
- **In-memory persistence** (`ConcurrentDictionary`) is used as per assignment requirements, avoiding any need for external database setup.

---

## Project Structure

```
Nemetschek_Assignment/
│
├── README.md                              ← This file
│
├── SmartTaskPlanner/                      ← Backend solution
│   ├── SmartTaskPlanner.API/              ← Presentation layer (HTTP endpoints)
│   │   ├── Controllers/
│   │   │   └── TasksController.cs         ← REST API endpoints
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs  ← Global error handler
│   │   ├── Program.cs                     ← App startup & DI wiring
│   │   └── appsettings.json               ← Serilog & logging configuration
│   │
│   ├── SmartTaskPlanner.Application/      ← Use-case / business logic layer
│   │   ├── DTOs/
│   │   │   └── TaskDtos.cs                ← Request/Response data contracts
│   │   ├── Interfaces/
│   │   │   ├── IGraphService.cs           ← Graph operations contract
│   │   │   ├── ITaskRepository.cs         ← Data access contract
│   │   │   └── ITaskService.cs            ← Business logic contract
│   │   ├── Services/
│   │   │   ├── GraphService.cs            ← Cycle detection + Topological sort
│   │   │   └── TaskService.cs             ← Orchestrates task CRUD + planning
│   │   ├── Validators/
│   │   │   └── TaskDtoValidators.cs       ← FluentValidation rules
│   │   └── DependencyInjection.cs         ← Service registration helper
│   │
│   ├── SmartTaskPlanner.Domain/           ← Core domain layer (no dependencies)
│   │   ├── Entities/
│   │   │   └── TaskItem.cs                ← Core aggregate entity
│   │   ├── Enums/
│   │   │   ├── Priority.cs                ← Low / Medium / High
│   │   │   ├── TaskStatus.cs              ← ToDo / InProgress / Done
│   │   │   └── TaskType.cs                ← General / Development / Testing / Bug
│   │   ├── Exceptions/
│   │   │   └── DomainExceptions.cs        ← Typed domain exceptions
│   │   └── Factories/
│   │       └── TaskFactory.cs             ← Business-rule creation logic
│   │
│   ├── SmartTaskPlanner.Infrastructure/   ← Data access / external services layer
│   │   ├── Repositories/
│   │   │   └── InMemoryTaskRepository.cs  ← Thread-safe in-memory data store
│   │   └── DependencyInjection.cs         ← Repository registration helper
│   │
│   └── SmartTaskPlanner.Tests/            ← Unit test project
│       ├── GraphServiceTests.cs           ← Tests for topological sort & cycle detection
│       ├── TaskServiceTests.cs            ← Tests for CRUD business logic
│       ├── TaskFactoryTests.cs            ← Tests for factory creation rules
│       └── InMemoryRepositoryTests.cs     ← Tests for repository operations
│
└── SmartTaskPlanner_Frontend/                   ← Angular frontend
    └── src/
        └── app/
            ├── core/
            │   ├── models/                ← TypeScript interfaces
            │   └── services/              ← HTTP service layer (API calls)
            └── features/
                ├── task-list/             ← Task list view
                ├── task-form/             ← Create/Edit form
                ├── task-detail/           ← Single task detail view
                └── execution-plan/        ← Execution plan view
```

---

## Assumptions Made

1. **No Authentication/Authorization**: The assignment focuses on task management logic. All API endpoints are open with no user sessions or JWT tokens.

2. **Single-user, in-memory storage**: Data is not persisted between server restarts. The `ConcurrentDictionary` store is appropriate for a single-process, single-deployment scenario.

3. **ID Generation is server-side**: The backend auto-assigns IDs using a type-prefixed format (e.g., `D-101` for Development, `T-201` for Testing, `B-301` for Bug, `G-401` for General). Clients never provide IDs.

4. **"Estimated Effort" is a dimensionless integer**: It represents relative story points or hours — the unit is intentionally left flexible. The planning algorithm treats it as a numeric weight only.

5. **Dependencies reference tasks that already exist**: A task may only depend on tasks currently in the system. Referencing a non-existent ID is treated as a validation error.

6. **Circular dependency check is eager**: Cycle validation runs on every create and update operation to keep the task graph acyclic at all times.

7. **Deletion is protected**: A task cannot be deleted if another active task depends on it. The dependents must be modified or deleted first.

8. **Bug tasks are always High priority**: This is a deliberate business rule encoded in the `TaskFactory`. Even if a user submits a Bug task with Low priority, it is overridden to High.

9. **Testing tasks default to "Quality Assurance" category**: If a Testing-type task is created with no category, the factory assigns it automatically.

10. **CORS is open (`AllowAll`)**: This is acceptable for a local development/demo assignment. In production, origins would be restricted.

---

## How the Execution Planning Logic Works

The execution planning is the core algorithmic feature of this application. It lives in [`GraphService.cs`](SmartTaskPlanner/SmartTaskPlanner.Application/Services/GraphService.cs).

### The Problem

Given a set of tasks where some tasks depend on others, determine a valid execution order such that:
- No task is executed before its dependencies.
- Higher-priority tasks are executed as early as possible.
- Among tasks of equal priority, tasks requiring less effort come first ("quick wins").

### Algorithm: Kahn's Algorithm (BFS-based Topological Sort) with a Priority Queue

**Step 1 — Build the Dependency Graph**

The tasks and their dependencies form a **Directed Acyclic Graph (DAG)**. For every task that declares a dependency `[B depends on A]`, an edge is added from `A → B` in an adjacency list.

Each task also receives an **in-degree** count — the number of tasks that must complete before it.

```
Example:
  G-401 (Setup Project) → D-101 (Create Login API)
  G-401 (Setup Project) → D-102 (Create Dashboard UI)
  G-401 (Setup Project) → T-201 (Write Unit Tests)

  In-degree:
    G-401 = 0  (no dependencies → can start immediately)
    D-101 = 1  (waits for G-401)
    D-102 = 1  (waits for G-401)
    T-201 = 1  (waits for G-401)
```

**Step 2 — Seed the Priority Queue**

All tasks with `in-degree == 0` (no unfulfilled dependencies) are added to a **min-heap priority queue** using a custom comparer (`TaskExecutionComparer`).

The comparer enforces the following tie-breaking rules (in order):

| Rule | Direction | Rationale |
|------|-----------|-----------|
| 1. Priority (High → Low) | Descending | Critical work first |
| 2. EstimatedEffort (low → high) | Ascending | Quick wins first |
| 3. Task ID (alphabetical) | Ascending | Deterministic tie-breaking |

**Step 3 — Process the Queue**

1. Dequeue the task with the highest priority according to the comparer.
2. Add it to the **execution plan** list.
3. For each task that depended on this task, decrement its in-degree by 1.
4. If any task's in-degree reaches 0, it is now "unlocked" and added to the priority queue.
5. Repeat until the queue is empty.

**Step 4 — Cycle Guard**

After the algorithm completes, if the execution plan contains fewer tasks than the total task count, some tasks were never processed — meaning they are part of a cycle. A `CircularDependencyException` is thrown.

### Cycle Detection (Pre-validation)

Before any task is created or updated, `EnsureNoCyclesOrInvalidDependencies` runs a **DFS-based cycle detection** using a recursion stack:

1. **Self-dependency check**: A task cannot list itself as a dependency.
2. **Missing dependency check**: All referenced dependency IDs must exist.
3. **DFS cycle check**: Walks the dependency graph; if a node is encountered that is already on the current recursion path, a cycle exists.

### Visualised Example

```
Tasks (with dependencies and priority):
  G-401: Setup Project     [High, Effort: 2, no deps]
  D-101: Create Login API  [High, Effort: 5, deps: G-401]
  D-102: Create Dashboard  [Medium, Effort: 3, deps: G-401]
  T-201: Write Unit Tests  [High, Effort: 2, deps: G-401]

Generated Execution Plan:
  1. G-401 — Setup Project     (Priority: High, Effort: 2) — unlocked first, no deps
  2. T-201 — Write Unit Tests  (Priority: High, Effort: 2) — same priority as D-101, lower effort wins
  3. D-101 — Create Login API  (Priority: High, Effort: 5) — same priority as T-201, higher effort
  4. D-102 — Create Dashboard  (Priority: Medium, Effort: 3) — lower priority, goes last
```

---

## How Keyset Pagination Works

Keyset pagination (also known as cursor-based pagination) is implemented on the core task retrieval API to facilitate fast, drift-free, and highly scalable retrieval of tasks.

### Why Keyset Pagination?

Traditional offset-based pagination (`LIMIT 5 OFFSET 10`) suffers from two major issues:
1. **Performance degradation**: The database/memory store must scan and discard all previous offsets, causing $O(N)$ lookup times for deeper pages.
2. **Data drift (skipped/duplicate items)**: If tasks are added or deleted while a user is scrolling, items shift positions, resulting in duplicate or skipped records on the UI.

Keyset pagination solves this by retrieving rows immediately after a specific **"anchor key"** (cursor) using a deterministic sort order ($O(1)$ lookup time and 100% drift-free scrolling).

### The Implementation Details

1. **Deterministic Sorting Key**:
   Tasks are sorted by `CreatedAt` ascending, and tie-broken by `Id` ascending. This ensures an absolute unique order for every item.

2. **The Cursor**:
   The cursor is a Base64-encoded string containing the sorting keys of the last element on the current page:
   `Base64(LastItem.CreatedAt.ToString("O") + "|" + LastItem.Id)`
   
   *Example decoded cursor:* `2026-07-02T19:50:38.1234567Z|D-101`

3. **Page Retrieval**:
   - **First Page**: Request without a cursor parameter: `GET /api/tasks?pageSize=5`.
   - **Next Page**: The API returns a `nextCursor` value in the response envelope. The client passes this value as the query parameter for the subsequent request: `GET /api/tasks?pageSize=5&cursor=MjAyNi0wNy0wMlQxOTo1MDozOC4xMjM0NTY3WnxELTEwMQ==`.
   - **Filtering Logic**: The query filters values where `CreatedAt > cursorCreatedAt` or (`CreatedAt == cursorCreatedAt` and `Id > cursorId`).

4. **UI Integration**:
   - The UI loads page 1 of the list initially.
   - A **"Load More Tasks"** button is displayed at the bottom of the table if `hasNext` is true.
   - Clicking the button loads the next page using the `nextCursor` and appends new tasks to the view.
   - Searching or filtering switches to searching across the local full-task cache to offer instant, global results.

---

## Design Decisions and Trade-offs

### 1. Clean Architecture

**Decision**: The solution is split into four layers — `Domain`, `Application`, `Infrastructure`, `API`.

**Rationale**: Dependency rules flow strictly inward. The domain has zero external dependencies, making it independently testable. The application layer depends only on domain interfaces, not on infrastructure implementations.

**Trade-off**: More boilerplate and project files than a simple MVC approach. Justified by testability and maintainability at scale.

---

### 2. In-Memory Repository as a Singleton

**Decision**: `InMemoryTaskRepository` is registered as a `Singleton` (not `Scoped`) in DI.

**Rationale**: A scoped service would create a new instance per HTTP request, wiping the in-memory data. A singleton ensures the `ConcurrentDictionary` persists for the application's lifetime. `ConcurrentDictionary` handles thread safety for concurrent requests.

**Trade-off**: In-memory data is lost on application restart. Acceptable for the assignment scope; a real system would use a database.

---

### 3. Factory Pattern for Task Creation

**Decision**: A `TaskFactory` class encodes type-specific business rules for task creation (e.g., Bugs are always High priority).

**Rationale**: Centralises creation logic. Prevents business rules from being scattered across controllers, DTOs, or services. Easy to extend with new `TaskType` rules without touching other code.

**Trade-off**: Adds a layer of indirection. Worth the trade-off for enforceability and discoverability of business rules.

---

### 4. Priority Queue for Topological Sort

**Decision**: Used .NET's built-in `PriorityQueue<TElement, TPriority>` instead of a simple FIFO queue.

**Rationale**: A plain BFS topological sort produces a valid order but doesn't respect task priorities. The priority queue ensures that whenever multiple tasks become available simultaneously, the most important one is selected first.

**Trade-off**: Slightly more complex than plain BFS. The custom `IComparer<TaskItem>` must be carefully maintained when new sorting criteria are added.

---

### 5. Global Exception Handling Middleware

**Decision**: All domain exceptions are caught in `ExceptionHandlingMiddleware` and translated to RFC 7807 `ProblemDetails` responses.

**Rationale**: Controllers remain clean — they contain no try/catch logic. All error translations are centralised in one place. Consistent error format for all API consumers.

**Trade-off**: Developers must look in two places (the service and the middleware) to understand what HTTP status code a given exception produces.

---

### 6. DFS Pre-validation + Kahn's Post-validation (Dual Cycle Guard)

**Decision**: Cycle detection is performed twice — once eagerly via DFS before saving a task, and once passively via Kahn's algorithm during plan generation.

**Rationale**: The DFS check prevents an invalid graph from ever being persisted. The Kahn's check is a safety net in case of any logic gaps in the pre-validation. Defence-in-depth.

**Trade-off**: Slight computational redundancy. Acceptable given typical task graph sizes.

---

## Limitations and Future Improvements

### Current Limitations

| Area | Limitation |
|------|------------|
| **Persistence** | Data is lost on application restart (in-memory only) |
| **Authentication** | No user accounts, roles, or access control |
| **Multi-tenancy** | No concept of projects, workspaces, or teams |
| **Concurrency** | Last-write-wins on concurrent updates to the same task |
| **Pagination** | Key-based pagination is implemented for task list view; plan generation still processes the full graph |
| **Filtering/Sorting** | No server-side filtering or sorting for task lists (performed on client side) |
| **Execution plan** | Does not account for resource availability or parallelism (assumes sequential execution) |
| **Dependency depth** | No visual dependency graph; only a flat ordered list is returned |

### Future Improvements

1. **Database persistence**: Swap `InMemoryTaskRepository` for an EF Core repository targeting PostgreSQL or SQL Server. The clean architecture means only the Infrastructure layer changes.

2. **Authentication & Authorization**: Add ASP.NET Identity + JWT bearer tokens. Task visibility could be scoped per user or team.

3. **Parallel Execution Plan**: Extend the execution plan to return execution "waves" or "stages" — groups of tasks that can run concurrently because none of them depend on each other within the wave.

4. **SignalR Real-time Updates**: Broadcast task state changes to all connected clients in real time — useful for team collaboration.

5. **Advanced Server-side Filtering**: Add `OData` or custom query parameters for filtering by status, priority, type, or category, dynamically integrated with the keyset pagination cursor.

6. **Event Sourcing / Audit Log**: Record every state change as an immutable event, enabling full history and rollback capability.

7. **Dependency Graph Visualisation**: Integrate a graph rendering library (e.g., D3.js or Cytoscape) on the frontend to visualise task dependencies as a DAG.

8. **Deadline and Scheduling**: Add a `DueDate` field to tasks, and enhance the planner to factor in deadlines when ordering tasks.

9. **Dockerisation**: Add a `Dockerfile` and `docker-compose.yml` to containerise both the API and the Angular app for one-command startup.

10. **CI/CD Pipeline**: Add GitHub Actions workflows for automated build, test, lint, and deployment on every pull request.

---

*Built as part of the Nemetschek / Spacewell technical assessment.*
