# Architecture and loose coupling

Goal: any infrastructure layer can be swapped (database, cache, broker, AI provider, vector store, identity provider, UI API client) by adding an adapter and changing configuration, without touching domain or application code.

## Layers and the dependency rule
- **Domain**: entities, value objects, domain events. References nothing.
- **Application**: use cases (commands and queries), validation, and **ports**: interfaces the use cases need. References Domain only.
- **Infrastructure**: **adapters** implementing the ports (EF Core, Redis, Service Bus, SignalR, Blob, AI). References Application.
- **Api**: endpoints, hubs, authentication, and the composition root that wires adapters to ports. References Application and Infrastructure.
- Dependencies point inward only. Domain and Application never reference EF Core, Azure SDKs, HTTP or SignalR types.

## Ports to define (use these abstractions)
| Concern | Port (in Application or a framework abstraction) | Default adapter |
| --- | --- | --- |
| Persistence | Repository interfaces per aggregate, or a narrow `I<Product>DbContext` | EF Core |
| Caching | `HybridCache` (Microsoft.Extensions.Caching.Hybrid) | Redis L2 (Upstash in cloud, container locally) |
| Messaging | `IMessageBus` (Wolverine) or `IEventPublisher` | Service Bus / SQS / RabbitMQ locally |
| Notifications | `INotificationPublisher` | SignalR hub / Azure SignalR Service |
| File storage | `IFileStorage` | Azure Blob / S3 / R2 |
| AI chat | `IChatClient` (Microsoft.Extensions.AI) | Microsoft Foundry / OpenAI / Claude |
| Embeddings | `IEmbeddingGenerator<string, Embedding<float>>` | Foundry or OpenAI |
| Vector store | `VectorStore` (Microsoft.Extensions.VectorData) | pgvector / Azure AI Search |
| Email | `IEmailSender` | Resend |
| Current user | `ICurrentUser` | Claims from the auth provider |
| Time | `TimeProvider` | System clock (fake in tests) |

## Rules
- Register each layer with one extension method (`AddApplication()`, `AddInfrastructure(configuration)`); choose adapters by configuration (`Cache:Provider`, `Ai:Provider`, `Storage:Provider`).
- Bind settings with the Options pattern and validate on start (`ValidateOnStart`).
- API returns DTOs, never domain entities or EF types; map explicitly.
- Cross-service communication only through `Contracts` messages or versioned HTTP APIs; no shared databases.
- Frontend calls the API through a typed client generated from OpenAPI, wrapped in a service (Angular `InjectionToken`) or hook (React), so components never call `fetch` directly.
- Record significant decisions as ADRs in `docs/adr/NNNN-title.md`.
