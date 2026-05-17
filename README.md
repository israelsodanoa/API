# API.Stories

Minimal API built in **.NET 9** to return the top `k` Hacker News best stories, using **Redis cache**, background refresh, resiliency policies, and a structure inspired by **Hexagonal Architecture (Ports & Adapters)**.

> Documentation note: this project documentation was generated with support from AI code-generation tools.

## 1) Architecture Rationale

### Why Redis Cache

The endpoint `/api/v1/topk/{topk}` is read-heavy and depends on external Hacker News APIs. Redis is used to:

- reduce latency for frequent reads
- avoid hitting Hacker News on every client request
- improve resilience when upstream latency fluctuates

Cache keys are created per story (for example: `story-<id>`).

### Why a Hosted Service for Cache Refresh

`CachedStoryHostedService` is responsible for periodically refreshing cache data in the background (every 10 seconds). This design keeps the request path fast:

- request flow reads from cache
- refresh flow runs independently from client traffic
- API remains responsive even under burst traffic

### Why `Parallel.ForEachAsync`

Parallel processing is used in two hotspots:

- cache warm/update (`CachedStoryHostedService`) to fetch many stories faster
- top-k selection (`HackerNewsStoryService`) to process cached stories concurrently

`MaxDegreeOfParallelism = 50` is configured to balance throughput and resource usage.

### Hexagonal + Minimal API Standards

The project separates concerns into layers:

- **Domain**: core primitives (`Result`, `HeapMinContainer`)
- **Ports**: contracts/interfaces
- **Adapters**: HTTP client, cache provider, handlers, hosted service
- **Entry point**: Minimal API route mapping + API versioning + OpenAPI

This keeps business flow independent from infrastructure details while preserving Minimal API simplicity.

## 2) Unit Tests Structure and Achievements

Unit tests are in:

- `API.Stories.UnitTests/Domain`
- `API.Stories.UnitTests/Application/Ports/Boundaries`
- `API.Stories.UnitTests/Application/Adapters`
- `API.Stories.UnitTests/Application/Adapters/Handlers`
- `API.Stories.UnitTests/Application/Adapters/Services`
- `API.Stories.UnitTests/Application/Adapters/HttpClients`

Current suite includes tests for all main components, covering:

- success/failure semantics of `Result` and `Result<T>`
- heap behavior used for top-k calculation
- handler HTTP results (`200` and `400` paths)
- service behavior for cache-hit/miss and upstream errors
- hosted-service cache refresh flow
- Redis serialization/deserialization behavior
- Hacker News HTTP client parsing and error mapping
- resiliency pipeline and policy behavior
- configuration/DI helper extensions

Run unit tests:

```bash
dotnet test API.Stories.UnitTests/API.Stories.UnitTests.csproj
```

## 3) Setup and Launch with Docker Compose

### Prerequisites

- Docker + Docker Compose

### Start services

```bash
docker compose up -d --build
```

### Stop services

```bash
docker compose down
```

### Main endpoint

```http
GET /api/v1/topk/{topk}
Header: x-api-version: 1.0
```

Example:

```bash
curl -H "x-api-version: 1.0" http://localhost:5038/api/v1/topk/10
```

## 4) Load Tests Structure and How to Launch

Load tests live in `API.Stories.LoadTests` and use **Artillery**.

### Structure

- `artillery/topk-load-test.yml`: scenarios, phases, SLO checks
- `artillery/processor.cjs`: runtime validators and custom metrics
- `package.json`: scripts for smoke, baseline, spike, soak, and report outputs
- `reports/`: generated JSON result files

### Install and run

```bash
cd API.Stories.LoadTests
npm install
npm run test:smoke
npm run test:baseline
npm run test:spike
npm run test:soak
```

Generate report JSON files:

```bash
npm run test:baseline:report
npm run test:spike:report
```

## 5) Load Test Reports (`API.Stories.LoadTests/reports`)

Generated files:

- `API.Stories.LoadTests/reports/baseline-report.json`
- `API.Stories.LoadTests/reports/spike-report.json`

Summary of inner results:

| Report               | Approx. Duration (s) | Requests | HTTP 200 | VU Failures | Mean Latency (ms) | p95 (ms) | p99 (ms) | Max (ms) | Avg Stories/Request | Total Stories Returned |
| -------------------- | -------------------: | -------: | -------: | ----------: | ----------------: | -------: | -------: | -------: | ------------------: | ---------------------: |
| baseline-report.json |              120.734 |     1984 |     1984 |           0 |             305.7 |    407.5 |   1085.9 |     2019 |                22.8 |                  45156 |
| spike-report.json    |               90.669 |     3068 |     3068 |           0 |             245.6 |    327.1 |   1002.4 |     1814 |                22.5 |                  69172 |

---

## Repository Map

- `API.Stories/` - main API project
- `API.Stories.UnitTests/` - unit test project
- `API.Stories.LoadTests/` - load test project (Artillery)
- `docker-compose.yml` - local services orchestration
- `API.sln` - solution file
