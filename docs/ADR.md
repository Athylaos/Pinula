# Architecture Decision Records (ADR)

This document records the core architectural decisions made during the design and refactoring of Pinula, including their technical context, chosen solutions, and engineering trade-offs.

---

## ADR-001: Minimal APIs with Thin Endpoints and Service Layer over MVC Controllers or MediatR

* **Status:** Accepted
* **Context:** Early iterations placed Entity Framework Core queries, image processing, and domain validation directly inside Minimal API route handlers under `Pinula.API/Endpoints/` (`RecipeEndpoint.cs`, `IngredientEndpoint.cs`), creating tightly coupled endpoints that could not be unit-tested without mocking `HttpContext`.
* **Decision:** Retain ASP.NET Core Minimal APIs strictly as thin HTTP adapters (`Pinula.API/Endpoints/*Endpoint.cs`) that extract `ClaimsPrincipal`, request `baseUrl`, and locale headers, and delegate all persistence and business logic to constructor-injected, stateless backend services (`Pinula.API/Interface/I*BackendService.cs` and `Pinula.API/Services/*BackendService.cs`).
* **Consequences & Trade-offs:**
    * **Positive:** Backend services have zero dependencies on `HttpRequest`, `HttpContext`, or `IWebHostEnvironment`, enabling direct unit testing and reuse from `NutriDbSeeder.cs`. Route definitions remain explicit and lightweight without MediatR pipeline overhead.
    * **Negative:** Domain-heavy services such as `RecipeBackendService.cs` and `MealPlanBackendService.cs` grow large as workflows expand, requiring strict internal method organization and dedicated projection classes in `Pinula.API/Projections/`.

---

## ADR-002: PostgreSQL `jsonb` Dictionaries over Normalized Translation Tables

* **Status:** Accepted
* **Context:** Pinula supports multiple UI and content languages (including English and Czech, backed by `App.resx` and `App.cs.resx` resource files) across recipes, recipe steps, ingredients, categories, shopping categories, and units. Modeling localized attributes with normalized relational translation tables requires repetitive `LEFT JOIN` chains on every read query and complicates write operations.
* **Decision:** Store localized strings directly on domain entities as `Dictionary<string, string>` properties mapped to PostgreSQL `jsonb` columns (introduced in migration `20260622123110_MigrateNamesToDictionary.cs`), resolving locales at runtime through a deterministic fallback chain (`requestedLang -> "en" -> default`).
* **Consequences & Trade-offs:**
    * **Positive:** Eliminates translation join tables, simplifies entity creation and updates, and enables EF Core with Npgsql to translate dictionary lookups directly into SQL JSON extraction operators (`->>`).
    * **Negative:** Requires custom value converters in `PinulaDbContext.OnModelCreating` when executing tests on SQLite (which lacks native `jsonb`), and diacritic-insensitive search requires combining SQL `ILike` pre-filtering with in-memory diacritic stripping (`RemoveDiacritics`).

---

## ADR-003: Migration from `SixLabors.ImageSharp` to `SkiaSharp` for Server-Side Media Processing

* **Status:** Accepted
* **Context:** Earlier versions of `Pinula.API` relied on `SixLabors.ImageSharp` for server-side image resizing and encoding. Unlicensed 2.1.x versions triggered NuGet security advisories during CI builds, whereas newer 3.2.0+ and 4.x releases introduced a mandatory build-time license key.
* **Decision:** Replace `SixLabors.ImageSharp` with `SkiaSharp` and `SkiaSharp.NativeAssets.Linux.NoDependencies` in `Pinula.API.csproj`, centralizing decoding, aspect-preserving resizing/cropping, WebP encoding (quality 80), and superseded file cleanup inside `Pinula.API/Services/ImageProcessingService.cs` (`IImageProcessingService`).
* **Consequences & Trade-offs:**
    * **Positive:** Eliminates NuGet vulnerability warnings in GitHub Actions CI builds, removes external license key dependencies, and runs inside Linux Docker containers without OS-level graphics packages.
    * **Negative:** `SkiaSharp` depends on native platform binaries (`NativeAssets`) and unmanaged memory handles, requiring explicit inclusion of the Linux native asset package and deterministic disposal of `SKBitmap`, `SKImage`, and `SKData` instances.

---

## ADR-004: Shared Class Library (`Pinula.Shared`) for Contracts, Constants, and Typed HTTP Clients

* **Status:** Accepted
* **Context:** Both the ASP.NET Core backend (`Pinula.API`) and the Blazor WebAssembly frontend (`Pinula.WASM`) consume the same DTOs, response contracts (`GeneralResponse`), domain enums (`DifficultyLevel`, `MealType`, `VerificationCodeType`), and default asset path constants (`ImageConstants`). Duplicating these models across client and server projects introduces contract drift and inconsistent error handling.
* **Decision:** Centralize all DTOs (`Pinula.Shared/DTOs/`), enums (`Pinula.Shared/Enums/`), asset constants (`Pinula.Shared/Constants/ImageConstants.cs`), client interfaces (`Pinula.Shared/Interface/I*ApiClient.cs`), and typed HTTP client implementations (`Pinula.Shared/Services/*ApiClient.cs`) inside `Pinula.Shared`, referenced by both `Pinula.API` and `Pinula.WASM`.
* **Consequences & Trade-offs:**
    * **Positive:** Guarantees compile-time contract synchronization between frontend and backend, standardizes HTTP status and `GeneralResponse` deserialization across all Blazor components, and eliminates hardcoded fallback image filenames.
    * **Negative:** `Pinula.Shared` must remain free of server-specific dependencies (`Microsoft.AspNetCore.Http`, `Microsoft.EntityFrameworkCore`), requiring file upload methods in `*ApiClient` interfaces to accept `Stream` parameters rather than `IFormFile` or `IBrowserFile`.

---

## ADR-005: SQLite In-Memory Relational Testing over EF Core InMemory Provider

* **Status:** Accepted
* **Context:** Testing backend services with `Microsoft.EntityFrameworkCore.InMemory` bypasses relational integrity checks, foreign-key constraints, and cascade delete rules, allowing tests to pass even when entity relationships in `PinulaDbContext` are misconfigured.
* **Decision:** Configure `TestDbContextFactory.cs` in `Pinula.API.Tests` to open a dedicated SQLite in-memory connection (`DataSource=:memory:`) per test instance (`CategoryBackendServiceTests.cs`) and initialize the schema with `Database.EnsureCreated()`.
* **Consequences & Trade-offs:**
    * **Positive:** Enforces real SQL relational constraints, foreign-key validation, and cascade delete behaviors during isolated unit tests without requiring a PostgreSQL service container in CI (`.github/workflows/test.yml`).
    * **Negative:** SQLite does not support PostgreSQL-specific column types (`jsonb`) or case-insensitive operators (`ILike`), requiring provider detection in `PinulaDbContext.OnModelCreating` to fall back to JSON-serialized `TEXT` columns when the active provider is not Npgsql.

---

## ADR-006: Hybrid Object Mapping Strategy (SQL Expression Projections and Mapster)

* **Status:** Accepted
* **Context:** Relying solely on runtime object mappers over loaded EF Core entities causes overfetching of unused columns and navigation properties on list endpoints, whereas pure SQL expression projections cannot execute complex C# graph transformations or custom runtime hooks.
* **Decision:** Combine compile-time `Expression<Func<TEntity, TDto>>` SQL projections (`Pinula.API/Projections/CategoryProjections.cs`, `IngredientProjections.cs`, and inline `.Select(...)` queries) for list, search, and tree-building endpoints with Mapster runtime adaptation (`MappingConfig.cs` and `MappingExtensions.AdaptWithLangAndBaseUrl<TDto>`) for deep entity graphs.
* **Consequences & Trade-offs:**
    * **Positive:** Prevents N+1 queries and unnecessary `.Include()` joins on high-traffic read endpoints while keeping complex detail mappings (`RecipeDetailsDto`, `InventoryItemDisplayDto`, `MealPlanPreviewDto`) maintainable with `lang` and `baseUrl` context injection.
    * **Negative:** Developers must maintain two mapping mechanisms and ensure runtime `MapContext` parameters are properly supplied when invoking `AdaptWithLangAndBaseUrl`.

---

## ADR-007: Unified `GeneralResponse` Contract over Exception-Driven HTTP Control Flow

* **Status:** Accepted
* **Context:** Returning bare HTTP status codes, tuples, or anonymous objects from state-mutating endpoints forced Blazor WebAssembly components to parse heterogeneous error payloads or rely on `HttpRequestException` handling for expected domain validation failures.
* **Decision:** Standardize all create, update, delete, and state-toggle service methods on `GeneralResponse` (`Pinula.Shared/DTOs/Other/GeneralResponse.cs`), or specialized subclasses such as `ToggleFavoriteResponse`, `ToggleBoolResponse`, `CommentPostResponse`, and `LoginResponse` carrying explicit `Successful`, `StatusCode`, `Message`, and `Id` properties. Minimal API endpoints forward the payload and status code via `Results.Json(result, statusCode: result.StatusCode)`.
* **Consequences & Trade-offs:**
    * **Positive:** Provides a uniform, strongly typed response contract across all `*BackendService` and `*ApiClient` classes, allowing Blazor UI components to inspect `result.Successful` and `result.Message` without try/catch boilerplate.
    * **Negative:** Backend services must explicitly map domain failure states into `GeneralResponse` instances with matching HTTP status codes (`400`, `401`, `404`, `500`) rather than throwing domain exceptions.

---

## ADR-008: Stateless JWT Authentication in `localStorage` with Custom `DelegatingHandler`

* **Status:** Accepted
* **Context:** Blazor WebAssembly runs entirely in the browser and communicates with a separate ASP.NET Core Minimal API host (`api.pinula.app` / `stage-api.pinula.app`). Using cookie-based authentication or ASP.NET Core Identity would introduce cross-subdomain cookie/CSRF complexity and heavy server-side identity schema tables.
* **Decision:** Implement stateless JWT Bearer authentication (`UserBackendService` + `JwtSecurityTokenHandler`) paired with a custom client-side `ApiAuthenticationStateProvider` and `AuthHttpMessageHandler` (`DelegatingHandler`) in `Pinula.WASM`. The JWT is persisted in browser `localStorage` and automatically attached to outgoing `HttpClient` requests alongside the active `Accept-Language` header.
* **Consequences & Trade-offs:**
    * **Positive:** Keeps `Pinula.API` completely stateless across container restarts, avoids cross-origin cookie/CSRF issues, and centralizes auth token and locale header injection for every `*ApiClient` call.
    * **Negative:** Tokens stored in `localStorage` are accessible to JavaScript (requiring strict XSS hygiene), and token revocation relies on token expiration or client-side logout rather than server-side session invalidation.

---

## ADR-009: Decoupled Python CLI Pipeline (`tools/data-generator`) for AI Enrichment

* **Status:** Accepted
* **Context:** Populating nutritional macros, multi-language translations, and ingredient images via external AI APIs is slow, rate-limited, and executed only during batch catalog curation rather than in response to user requests. Embedding long-running AI generation pipelines inside `Pinula.API` would bloat API dependencies and tie up web worker resources.
* **Decision:** Isolate offline catalog enrichment and image generation into standalone Python CLI scripts (`tools/data-generator/enrich_ingredients.py`, `generate_images_ingredients.py`) that process batches locally and push validated results into `Pinula.API` via dedicated administrative endpoints (`Pinula.API/Endpoints/ScriptEndpoint.cs`).
* **Consequences & Trade-offs:**
    * **Positive:** Keeps the .NET backend lightweight and focused on low-latency user traffic, allows independent iteration on AI prompts and Python tooling, and routes all final writes through API validation and `SkiaSharp` WebP processing.
    * **Negative:** Requires maintaining Pydantic schemas (`tools/data-generator/schemas.py`) alongside C# DTOs (`DGIngredientUpdateDto`) and protecting `ScriptEndpoint` routes with strict API key/admin authorization.
