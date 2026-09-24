\# FieldOps



FieldOps is a full-stack field service management platform built with .NET microservices and React. It manages work orders, technicians, assignments, authentication, and event-driven email notifications.



\## Key Features



\- Create and monitor field work orders

\- Register and manage technicians

\- Assign technicians to work orders

\- JWT authentication with ASP.NET Core Identity

\- Role-based authorization for Dispatcher, Technician, and Administrator

\- Transactional outbox for reliable event delivery

\- RabbitMQ messaging with dead-letter handling

\- Idempotent notification processing

\- Email testing through Mailpit

\- API Gateway using YARP

\- PostgreSQL persistence with EF Core migrations

\- React and TypeScript operations dashboard

\- Health-check endpoints and automated startup

\- Integration tests for authentication and authorization

\- GitHub Actions continuous integration



\## Architecture



```mermaid

flowchart TD

&#x20;   UI\["React Dashboard"] --> GW\["YARP API Gateway"]



&#x20;   GW --> ID\["Identity API"]

&#x20;   GW --> WO\["Work Orders API"]

&#x20;   GW --> TECH\["Technicians API"]



&#x20;   ID --> PG\["PostgreSQL"]

&#x20;   WO --> PG

&#x20;   TECH --> PG



&#x20;   WO --> OUTBOX\["Transactional Outbox"]

&#x20;   OUTBOX --> MQ\["RabbitMQ"]

&#x20;   MQ --> WORKER\["Notification Worker"]

&#x20;   WORKER --> MAIL\["Mailpit"]

```



\## Services



| Component | Responsibility | Local URL |

|---|---|---|

| React Dashboard | Operations user interface | `http://localhost:5174` |

| API Gateway | Routes frontend requests | `http://localhost:5080` |

| Identity API | Registration, login, JWT and roles | `http://localhost:5090` |

| Work Orders API | Work-order lifecycle and assignment | `http://localhost:5062` |

| Technicians API | Technician records and availability | `http://localhost:5072` |

| Notification Worker | Processes assignment events | Background service |

| RabbitMQ | Message broker | `http://localhost:15672` |

| Mailpit | Local email inbox | `http://localhost:8025` |

| pgAdmin | PostgreSQL administration | `http://localhost:5050` |



\## Technology Stack



\### Backend



\- .NET 10

\- ASP.NET Core Web API

\- Entity Framework Core

\- ASP.NET Core Identity

\- JWT Bearer authentication

\- YARP reverse proxy

\- PostgreSQL

\- RabbitMQ

\- xUnit integration testing



\### Frontend



\- React

\- TypeScript

\- Vite

\- CSS



\### Infrastructure



\- Docker Compose

\- Mailpit

\- pgAdmin

\- GitHub Actions



\## Authentication and Authorization



FieldOps uses JWT bearer tokens issued by the Identity API.



Roles:



\- \*\*Administrator\*\* — full administrative access

\- \*\*Dispatcher\*\* — creates work orders and manages assignments

\- \*\*Technician\*\* — read-only operational access in the current version



Authorization is enforced by the APIs. The React interface also hides actions unavailable to the current role.



\## Reliable Messaging



When a technician is assigned:



1\. The Work Orders API updates the work order.

2\. An assignment event is saved to the outbox in the same database transaction.

3\. The outbox processor publishes the event to RabbitMQ.

4\. The Notification Worker consumes the event.

5\. The worker records the event ID to prevent duplicate processing.

6\. An assignment email is sent to Mailpit.

7\. Failed messages are routed to a dead-letter queue.



\## Running Locally



\### Prerequisites



\- .NET 10 SDK

\- Node.js

\- Docker Desktop

\- PowerShell



\### Start FieldOps



From the repository root:



```powershell

.\\Start-FieldOps.cmd

```



The startup script:



\- Starts Docker Desktop when necessary

\- Starts PostgreSQL, RabbitMQ, Mailpit and pgAdmin

\- Starts each API in dependency order

\- Waits for API health checks

\- Starts the notification worker

\- Starts the React dashboard



\### First-time frontend setup



```powershell

Set-Location src\\fieldops-web

npm install

```



\### Run tests



```powershell

dotnet test tests\\FieldOps.WorkOrders.Api.Tests

```



The integration tests use an isolated in-memory database and test authentication handler, so PostgreSQL and RabbitMQ are not required.



\## Health Checks



```text

http://localhost:5062/health

http://localhost:5072/health

http://localhost:5080/health

http://localhost:5090/health

```



\## Continuous Integration



GitHub Actions automatically:



\- Restores and builds the .NET solution

\- Runs integration tests

\- Installs frontend dependencies

\- Produces the React production build



\## Development Credentials



Credentials included in local configuration are demonstration-only values. They must not be used in production. Production deployments should supply database passwords and JWT signing keys through environment variables or a secrets manager.



\## Future Improvements



\- Link authenticated technician accounts to technician records

\- Technician-specific work queues

\- Work-order status workflow and history

\- Refresh tokens and token revocation

\- Distributed tracing with OpenTelemetry

\- Containerize every application service

\- Expand integration and end-to-end test coverage



