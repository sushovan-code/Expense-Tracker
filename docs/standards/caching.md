# Caching

- Use `HybridCache` (Microsoft.Extensions.Caching.Hybrid): in-memory L1 plus Redis L2, with stampede protection and tag-based invalidation.
- Redis: a container started by Aspire locally (`AddRedis`), Upstash Redis in the cloud. Connection string `ConnectionStrings:Redis`.
- Output caching for read-heavy public GET endpoints, with the Redis output-cache store.
- Keys: `<product>:<entity>:<id>:v<version>`. Bump the version when the cached shape changes.
- Every entry has an expiry. Invalidate on writes by tag (`RemoveByTagAsync`) or by event handlers.
- Never cache secrets, tokens or unmasked PII. Cache per user or tenant when data is user-specific (include the ID in the key).
- Redis is never the system of record. The app must work (more slowly) if Redis is down.
- Typical uses: reference data, dashboard aggregates, LLM and embedding results keyed by a content hash, idempotency keys, rate-limit counters.
