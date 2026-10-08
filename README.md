# 🎮 DotaPicker

**REST API for Dota 2 Meta Analysis & Hero Pick Recommendation**

---

## 📖 What is Dota 2?

Dota 2 is a complex multiplayer online battle arena (MOBA) game where two teams of 5 players select heroes to compete against each other. Each hero has unique abilities and playstyles. **DotaPicker** analyzes the current game meta (the most effective heroes and strategies) and provides intelligent hero recommendations based on live statistics.

---

## 🎯 Project Overview

DotaPicker is a backend system that:

- **Fetches live hero statistics** from two data sources:
  - **Stratz API** (GraphQL) — hero stats, synergies, matchups
  - **stats.spectral.gg API** — hero role position percentages
- **Maintains an in-memory cache** (Singleton) for sub-millisecond access
- **Analyzes hero picks** based on synergies with allies and advantages against enemies
- **Recommends optimal heroes** with detailed breakdowns

**Tech Stack:**
- C# / ASP.NET Core 10.0
- Entity Framework Core (SQLite / PostgreSQL)
- MediatR (CQRS)
- Docker & Docker Compose
- Swagger/OpenAPI

---

## 🏗️ Architecture

### **Data Flow**

```
┌─────────────────────────────────────────────────────────┐
│  Stratz GraphQL API      stats.spectral.gg API          │
│  (Heroes, Stats, Matchups) (Hero Roles & Positions)     │
└───────────────┬──────────────────────────────┬──────────┘
                │                              │
                └──────────────┬───────────────┘
                               ▼
                    ┌──────────────────────┐
                    │ MetaDataFetcher      │
                    │ (Fetch & Parse Data) │
                    └──────────┬───────────┘
                               ▼
                    ┌──────────────────────┐
                    │ BackgroundDataFetcher│
                    │ (Auto Sync - 7 days) │
                    └──────────┬───────────┘
                               ▼
                    ┌──────────────────────┐
                    │ AppDbContext         │
                    │ (Snapshot Storage)   │
                    └──────────┬───────────┘
                               ▼
                    ┌──────────────────────┐
                    │ InMemoryDotaMetaCache│
                    │ (Singleton - Cache)  │
                    └──────────┬───────────┘
                               ▼
            ┌──────────────────┬─────────────────┐
            ▼                  ▼                  ▼
       ┌─────────┐      ┌──────────┐      ┌──────────────┐
       │HeroAPI  │      │PickerAPI │      │ AnalyticsAPI │
       │(Heroes) │      │(Analysis)│      │(Stats)       │
       └────┬────┘      └────┬─────┘      └────┬─────────┘
            └──────────┬─────────────────────────┘
                       ▼
              ┌─────────────────────┐
              │ HTTP Responses      │
              │ (JSON)              │
              └─────────────────────┘
```

### **System Layers**

```
┌─────────────────────────────────────────────────┐
│ API LAYER (Controllers)                         │
│ HeroController - HTTP endpoints                 │
└────────────────┬────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│ BUSINESS LOGIC LAYER (Services)                 │
│ PickerAnalyzer - Scoring & recommendations     │
└────────────────┬────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│ CACHE LAYER (Singleton)                         │
│ InMemoryDotaMetaCache - O(1) hero lookups      │
└────────────────┬────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│ DATA FETCHING LAYER                             │
│ BackgroundDataFetcher - Auto sync every 7 days │
│ MetaDataFetcher - GraphQL & REST requests      │
│ AppDbContext - EF Core persistence             │
└────────────────┬────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│ EXTERNAL APIs                                   │
│ Stratz GraphQL / stats.spectral.gg             │
└─────────────────────────────────────────────────┘
```

---

## 🔧 Core Services & Interfaces

### **1. IMetaDataFetcher (Transient)**

**File:** `Interfaces/IMetaDataFetcher.cs`

```csharp
public interface IMetaDataFetcher
{
    Task<StratzRootResponse?> FetchMetaDataAsync(CancellationToken cancellationToken);
    Task<CurrentPositionData> FetchMetaDataOfHeroRolesAsync(CancellationToken cancellationToken);
}
```

**Purpose:** Fetches and parses hero data from two external sources

| Method | Source | Returns | Description |
|---|---|---|---|
| `FetchMetaDataAsync()` | Stratz GraphQL | `StratzRootResponse` | Hero stats, synergies, matchups |
| `FetchMetaDataOfHeroRolesAsync()` | stats.spectral.gg | `CurrentPositionData` | Hero role position percentages |

**Implementation:** `Services/MetaDataFetcher.cs`

Sends GraphQL query:
```graphql
query GetHeroInformation {
  constants {
    heroes { id displayName roles { roleId level} }
  }
  heroStats {
    winWeek { heroId winCount matchCount }
    matchUp(take: 150, bracketBasicIds: [DIVINE_IMMORTAL]) {
      heroId
      with { heroId2 synergy winsAverage }
      vs { heroId2 winsAverage }
    }
  }
}
```

---

### **2. IMetaCache (Singleton)**

**File:** `Interfaces/IMetaCache.cs`

```csharp
public interface IMetaCache
{
    DateTime? LastUpdateAt { get; set; }
    bool IsReady { get; set; }
    
    Dictionary<int, string> HeroNames { get; set; }
    Dictionary<int, (int winCount, int mathCount)> HeroWinRates { get; set; }
    Dictionary<(int HeroId, int AllyId), (double Synergy, double WinsAverage)> HeroTeamMatchUps { get; set; }
    Dictionary<(int HeroId, int EnemyId), double> HeroVsMatchUps { get; set; }
    Dictionary<int, HeroFullProfile> HeroRoles { get; set; }
    
    Task UpdateCacheAsync(StratzMetaResponse data);
    Task UpdateRolesCacheAsync(CurrentPositionOfHeroesMetaResponse data);
}
```

**Purpose:** Ultra-fast in-memory storage of all hero metadata

**Cache Contents:**

| Dictionary | Key | Value | Example |
|---|---|---|---|
| `HeroNames` | Hero ID | Hero Name | `{1: "Anti-Mage"}` |
| `HeroWinRates` | Hero ID | (wins, matches) | `{1: (5000, 10000)}` |
| `HeroTeamMatchUps` | (HeroId, AllyId) | (synergy, avg wins) | `{(1, 2): (1.2, 0.55)}` |
| `HeroVsMatchUps` | (HeroId, EnemyId) | win rate | `{(1, 3): 0.58}` |
| `HeroRoles` | Hero ID | Role percentages | `{1: {Carry: 95%, Mid: 2%}}` |

**Implementation:** `Services/InMemoryDotaMetaCache.cs`

**Key Features:**
- ✅ Singleton (one instance forever)
- ✅ O(1) dictionary lookups (no DB queries)
- ✅ Thread-safe operations
- ✅ Updated by BackgroundDataFetcher

---

### **3. IPickerAnalyzer (Scoped)**

**File:** `Interfaces/IPickerAnalsizer.cs`

```csharp
public interface IPickerAnalyzer
{
    Task<AnalyzePickResponse> Handle(AnalyzePickRequest request);
}
```

**Purpose:** Analyze hero picks and generate recommendations

**Input (AnalyzePickRequest):**
```csharp
public record AnalyzePickRequest(
    int SelectedRoleNumber,           // 1-5: Carry, Mid, Offlane, SoftSupport, HardSupport
    List<int> AllyHeroes,             // Selected team heroes (0-5)
    List<int> EnemyHeroes,            // Enemy team heroes (0-5)
    List<int> bannedHeroes,           // Banned heroes (optional)
    int? SelectedHeroId               // Filter for specific hero (optional)
) : IRequest<AnalyzePickResponse>;
```

**Implementation:** `Services/PickerAnalyzer.cs`

**Scoring Algorithm:**

```csharp
private const decimal W1_ALLIES = 0.7m;              // Ally synergy weight
private const decimal W2_ENEMIES = 1.5m;             // Enemy advantage weight
private const decimal W3_BASE_WINRATE = 0.3m;        // Base win rate weight
private const decimal W4_ROLE_CORRESPONDENCE = 0.5m; // Role correspondence weight
private const decimal K_SMOOTHING = 800_000m;        // Confidence smoothing factor

FinalScore = (W1_ALLIES × AllySynergy)
           + (W2_ENEMIES × EnemyAdvantage)
           + (W3_BASE_WINRATE × BaseWinRateDelta)
           + (W4_ROLE_CORRESPONDENCE × RoleDelta)
```

**Output (AnalyzePickResponse):**

```json
{
  "heroStats": [
    {
      "heroId": 10,
      "name": "Morphling",
      "winCount": 5000,
      "matchCount": 8000,
      "winRate": 62.5,
      "allySynergy": 1.15,
      "lineMatchPercentage": 85.5,
      "enemyAdvantage": 2.3,
      "finalScore": 15.42,
      "alliesBreakdown": [
        {
          "heroId": 1,
          "heroName": "Anti-Mage",
          "synergy": 1.2
        }
      ],
      "enemiesBreakdown": [
        {
          "heroId": 4,
          "heroName": "Bristleback",
          "winRateVsEnemy": 58.5,
          "advantage": 8.5
        }
      ]
    }
  ]
}
```

**Filtering:** Only heroes with role match percentage ≥ 20% are recommended

---

### **4. BackgroundDataFetcher (IHostedService)**

**File:** `Services/BackgroundDataFetcher.cs`

**Purpose:** Automatic background synchronization of hero metadata

**Lifecycle:**

```
Application Startup
        ↓
BackgroundDataFetcher.StartAsync()
        ↓
Load latest snapshots from Database
        ↓
Update InMemoryDotaMetaCache from snapshots
        ↓
Check: Has 7 days passed since last update?
        ├─ NO  → Wait remaining time, then repeat
        ├─ YES → Fetch new data from APIs
        │         └─ Call MetaDataFetcher
        │         └─ Save to Database
        │         └─ Update Cache
        │         └─ Set new 7-day timer
        └─ Repeat...
```

**Database Models:**

| Model | Purpose |
|---|---|
| `DotaMetaSnapshot` | Stores `StratzMetaResponse` with timestamp |
| `DotaHeroRolesStatistic` | Stores `CurrentPositionData` with timestamp |

**Error Handling:**

| Exception | Retry Wait |
|---|---|
| `HttpRequestException` | 2 minutes |
| `Generic Exception` | 5 minutes |

**Key Features:**
- ✅ Automatic startup (no manual invocation)
- ✅ Non-blocking (doesn't delay API requests)
- ✅ Persistent storage (DB snapshots)
- ✅ Fast in-memory cache

---

## 📡 API Endpoints

### **HeroController**

**GET** `/Hero/get-all-hero-names`
- Returns all hero names and IDs
- Response: `Dictionary<int, string>`
- Example: `{"1": "Anti-Mage", "2": "Axe", ...}`

**GET** `/Hero/get-hero-winrates`
- Returns hero statistics with win rates
- Response: List of `{ HeroId, DisplayName, WinRate }`
- Example:
  ```json
  [
    { "heroId": 1, "displayName": "Anti-Mage", "winRate": 62.5 },
    { "heroId": 2, "displayName": "Axe", "winRate": 58.3 }
  ]
  ```

**POST** `/Hero/get-analytics`
- Analyze picks and get recommendations
- Request: `AnalyzePickRequest`
- Response: `AnalyzePickResponse`

---

## 🚀 Data Flow in Practice

### **Initial Bootstrap**

```
App Startup
  ↓
BackgroundDataFetcher runs
  ↓
Load latest DotaMetaSnapshot from DB
  ↓
Load latest DotaHeroRolesStatistic from DB
  ↓
Call InMemoryDotaMetaCache.UpdateCacheAsync()
Call InMemoryDotaMetaCache.UpdateRolesCacheAsync()
  ↓
Cache ready for live analysis ✓
```

### **API Request Flow**

```
Client: POST /Hero/get-analytics
  ↓
HeroController receives request
  ↓
Dispatch AnalyzePickRequest via MediatR
  ↓
PickerAnalyzer.Handle() processes
  ↓
Access InMemoryDotaMetaCache (instant O(1))
  ↓
Calculate scores for each hero
  ↓
Return AnalyzePickResponse (JSON)
  ↓
Client receives recommendations
```

### **Background Sync (Every 7 Days)**

```
Timer triggers (7 days passed)
  ↓
MetaDataFetcher.FetchMetaDataAsync() 
  → GraphQL to Stratz API
  ↓
MetaDataFetcher.FetchMetaDataOfHeroRolesAsync()
  → REST to stats.spectral.gg
  ↓
Save both payloads to Database
  ↓
Update InMemoryDotaMetaCache
  ↓
Next request uses fresh data ✓
```

---

## 🛠️ Project Structure

```
DotaPicker/
├── Controllers/
│   └── HeroController.cs               ← API endpoints
├── Commands/
│   └── AnalyzePickRequest.cs           ← CQRS request/response
├── Services/
│   ├── BackgroundDataFetcher.cs        ← IHostedService (auto-sync)
│   ├── MetaDataFetcher.cs              ← Fetch from APIs
│   ├── PickerAnalyzer.cs               ← Scoring logic
│   └── InMemoryDotaMetaCache.cs        ← Singleton cache
├── Interfaces/
│   ├── IMetaCache.cs
│   ├── IMetaDataFetcher.cs
│   └── IPickerAnalyzer.cs
├── DTO/
│   ├── StratzRootResponse.cs
│   ├── CurrentPositionOfHeroesMetaResponse.cs
│   └── DotaHeroRolesStatistic.cs
├── Models/
│   └── DotaMetaSnapshot.cs             ← DB models
├── DB/
│   └── AppDbContext.cs                 ← EF Core context
├── Migrations/
│   └── *.cs                            ← EF migrations
├── Program.cs                          ← DI setup
├── appsettings.json                    ← Config
├── Dockerfile                          ← Container build
├── compose.yaml                        ← Docker Compose
└── DotaPicker.csproj
```

---

## 📊 Dependency Injection (Program.cs)

```csharp
// Singleton — one instance for entire application lifetime
builder.Services.AddSingleton<IMetaCache, InMemoryDotaMetaCache>();

// Transient — new instance per request
builder.Services.AddTransient<IMetaDataFetcher, MetaDataFetcher>();

// Scoped — one instance per HTTP request
builder.Services.AddScoped<IPickerAnalyzer, PickerAnalyzer>();

// Hosted Service — runs in background
builder.Services.AddHostedService<BackgroundDataFetcher>();

// MediatR CQRS
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// HttpClient for Stratz API
builder.Services.AddHttpClient("StratzClient", client =>
{
    client.BaseAddress = new Uri("https://api.stratz.com/graphql");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {stratzToken}");
});
```

---

## 🚀 Running the Project

### **Prerequisites**
- .NET SDK 10.0+
- Docker & Docker Compose (optional)
- Stratz API Key: https://stratz.com

### **Local Setup**

**1. Clone repository**
```bash
git clone https://github.com/giudiceqq/DotaPicker.git
cd DotaPicker
```

**2. Set API Key (User Secrets)**
```bash
dotnet user-secrets set "Stratz:Token" "your-api-key-here"
```

**3. Run locally**
```bash
dotnet restore
dotnet run
```

Swagger UI: `http://localhost:5000/swagger`

### **Docker Setup**

1. Create a `.env` file in the root directory:
```env
STRATZ_TOKEN=your_token_here

```bash
docker compose up
```

App runs on: `http://localhost:8080`

---

## 🔐 Security & Best Practices

| Practice | Benefit |
|---|---|
| **API Key in User Secrets** | Never committed to git |
| **Async/Await** | Non-blocking I/O, high concurrency |
| **Dependency Injection** | Loose coupling, easy testing |
| **CQRS Pattern (MediatR)** | Separation of concerns |
| **Thread-Safe Cache** | Atomic dictionary operations |
| **Docker** | Production-ready containerization |
| **Swagger** | Auto-generated API documentation |

---

## 📈 Performance Optimizations

| Component | Optimization | Benefit |
|---|---|---|
| **Hero Lookups** | Dictionary in memory (O(1)) | No database queries |
| **Background Sync** | Runs every 7 days | Doesn't block API requests |
| **Async/Await** | Non-blocking I/O | Handles thousands of concurrent users |
| **Connection Pooling** | EF Core managed | Efficient database connections |
| **Singleton Cache** | One instance forever | Minimal memory overhead |

---

## 📝 Configuration

**appsettings.json**
```json
{
  "Stratz:Token": "set-via-user-secrets",
  "ConnectionStrings": {
    "Sqlite": "Data Source=dota_meta.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## 📄 License

MIT License

---

## 👤 Author

**Andrii Aksenenko**
- GitHub: [@giudiceqq](https://github.com/giudiceqq)
- Email: andreyaksenenko2807@gmail.com
