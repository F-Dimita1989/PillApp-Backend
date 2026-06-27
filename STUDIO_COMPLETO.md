# 📚 Guida di Studio Completa - PillApp Backend

Una guida dettagliata e approfondita per comprendere il progetto PillApp Backend da zero. Leggi questa guida per capire **cosa fa**, **come funziona**, **com'è strutturato** e **come sviluppare**.

---

## 📋 Indice

1. [Introduzione al Progetto](#introduzione-al-progetto)
2. [Cos'è il Backend](#cosè-il-backend)
3. [Tecnologie Utilizzate](#tecnologie-utilizzate)
4. [Architettura Generale](#architettura-generale)
5. [Struttura delle Cartelle](#struttura-delle-cartelle)
6. [Componenti Principali](#componenti-principali)
7. [Come Funziona l'Applicazione](#come-funziona-lapplicazione)
8. [Flusso di Richieste (Request Flow)](#flusso-di-richieste)
9. [Autenticazione e Sicurezza](#autenticazione-e-sicurezza)
10. [Database e Modelli](#database-e-modelli)
11. [API Endpoints](#api-endpoints)
12. [Testing](#testing)
13. [Configurazione e Environment](#configurazione-e-environment)
14. [Deployment su Render](#deployment-su-render)
15. [CI/CD Pipeline](#cicd-pipeline)
16. [Come Sviluppare Nuove Feature](#come-sviluppare-nuove-feature)

---

## Introduzione al Progetto

### Cosa fa PillApp Backend?

PillApp Backend è un **server API REST** che gestisce:
- **Ricerca di farmaci**: Lookup farmaci per codice AIC (codice identificativo farmaceutico italiano)
- **Autenticazione admin**: Login sicuro con token JWT per area amministrativa
- **Monitoraggio del database**: Endpoint per verificare la salute del database

**Caso d'uso reale:**
Un'app mobile chiama l'API `/api/farmaci/{aic}` per cercare un farmaco, il backend interroga il database PostgreSQL e ritorna i dettagli (nome, prezzo, principio attivo, ecc.).

### Chi usa questo Backend?

- **App mobile** (React Native): fa richieste per cercare farmaci
- **Amministratori**: usano endpoint protetti da autenticazione JWT per diagnostica e gestione
- **Servizi di monitoraggio**: controllano `/health` e `/keepalive-db` per verificare che il servizio sia online

---

## Cos'è il Backend

### Definizione semplice
Il backend è il **"cervello"** dell'applicazione. Se l'app mobile è il volto che l'utente vede, il backend è tutto quello che succede dietro le quinte:
- Riceve richieste
- Elabora dati
- Interroga il database
- Ritorna risposte

### Perché separare Backend e Frontend?

1. **Scalabilità**: tanti client (app, web, altri servizi) possono interrogare lo stesso backend
2. **Sicurezza**: credenziali di database non vengono mai mandate al client
3. **Manutenzione**: modifiche al backend non richiedono aggiornamento dell'app su 1 milione di dispositivi
4. **Performance**: backend può ottimizzare query, caching, ecc.

---

## Tecnologie Utilizzate

### Stack Principale

| Tecnologia | Versione | Ruolo | Perché? |
|-----------|----------|-------|--------|
| **.NET** | 10.0 | Framework principale | Linguaggio moderno, performante, sicuro |
| **ASP.NET Core** | 10.0 | Web framework | Crea le API REST, gestisce routing HTTP |
| **C#** | (incluso in .NET 10) | Linguaggio di programmazione | Compilato, type-safe, performante |
| **PostgreSQL** | (via Supabase) | Database | Open-source, affidabile, buona performance |
| **Entity Framework Core** | 10.0 | ORM (Object-Relational Mapper) | Scrive meno SQL, più codice C# pulito |
| **JWT Bearer** | 10.0 | Autenticazione | Token sicuri per proteggere endpoint |
| **xUnit** | 2.4+ | Testing | Scrive e esegue test automatici |

### Cos'è un ORM?

Un ORM (Object-Relational Mapper) traduce oggetti C# in query SQL:

```csharp
// Quello che scrivi in C#:
var farmaci = await _db.FarmaciClasseA
    .Where(f => f.Aic == "023076010")
    .ToListAsync();

// Diventa automaticamente questa query SQL:
// SELECT * FROM farmaci_classe_a WHERE aic = '023076010';
```

### Perché questi tool?

- **.NET 10**: È il framework più moderno di Microsoft, veloce e sicuro
- **PostgreSQL**: Gratuito, open-source, usato da aziende grandi (Spotify, Instagram)
- **JWT**: Standard internazionale per autenticazione API, leggero e stateless
- **xUnit**: Framework di test più popolare in .NET, facile da usare

---

## Architettura Generale

### Modello Client-Server

```
┌─────────────────┐
│   App Mobile    │  (React Native)
│  (Frontend)     │
└────────┬────────┘
         │ HTTP/HTTPS (Request)
         │
         ▼
┌─────────────────────────────────┐
│      PillApp Backend API        │  (ASP.NET Core)
│  - Routing                      │
│  - Autenticazione               │
│  - Business Logic               │
│  - Validazione                  │
└────────┬────────────────────────┘
         │ Query SQL
         ▼
┌─────────────────┐
│   PostgreSQL    │  (Database su Supabase)
│   (Database)    │
└─────────────────┘
```

### Layer (Strati) dell'Applicazione

Il backend è organizzato in layer:

```
┌──────────────────────────────────────┐
│  Layer 1: ROUTING (Program.cs)       │ ← Riceve richieste HTTP
│  Dove: Definisce URL, middleware     │
├──────────────────────────────────────┤
│  Layer 2: CONTROLLERS                │ ← Processa logica
│  Dove: AuthController,               │   Valida input
│        FarmaciController             │   Chiama Database
├──────────────────────────────────────┤
│  Layer 3: DATA ACCESS (DbContext)    │ ← Interroga DB
│  Dove: AppDbContext                  │   Ritorna dati
├──────────────────────────────────────┤
│  Layer 4: DATABASE                   │ ← Salva dati
│  Dove: PostgreSQL (Supabase)         │
└──────────────────────────────────────┘
```

Questo design si chiama **"Separation of Concerns"** (separazione delle responsabilità).

---

## Struttura delle Cartelle

```
PillApp_BackEnd/                           (Root del progetto)
│
├── PillAppBackend/                         (Cartella soluzione)
│   ├── PillApp.Api/                        ⭐ APPLICAZIONE PRINCIPALE
│   │   ├── Program.cs                      ← Configurazione app (entry point)
│   │   ├── PillApp.Api.csproj             ← File di progetto (dipendenze)
│   │   ├── appsettings.json               ← Configurazione (produzione)
│   │   ├── appsettings.Development.json   ← Configurazione (sviluppo)
│   │   │
│   │   ├── Controllers/                    ← Ricevono richieste HTTP
│   │   │   ├── AuthController.cs          (Login admin)
│   │   │   └── FarmaciController.cs       (Ricerca farmaci)
│   │   │
│   │   ├── Data/                           ← Accesso al database
│   │   │   └── AppDbContext.cs            (Configurazione EF Core)
│   │   │
│   │   ├── Models/                         ← Modelli dati
│   │   │   └── FarmacoClasseA.cs          (Farmaco - Classe A)
│   │   │
│   │   ├── Dtos/                           ← Data Transfer Objects (request/response)
│   │   │   ├── AdminLoginRequestDto.cs    (Username + Password)
│   │   │   ├── AdminLoginResponseDto.cs   (Token JWT)
│   │   │   └── FarmacoLookupDto.cs        (Dettagli farmaco)
│   │   │
│   │   ├── Security/                       ← (Cartella per sicurezza futura)
│   │   │
│   │   └── Properties/
│   │       └── launchSettings.json        (Configurazione avvio locale)
│   │
│   └── PillApp.Api.Tests/                  ⭐ TEST DEL PROGETTO
│       ├── PillApp.Api.Tests.csproj       (Configurazione test)
│       ├── AuthControllerTests.cs         (Test autenticazione)
│       └── FarmaciControllerTests.cs      (Test ricerca farmaci)
│
├── Dockerfile                              ← Immagine Docker (produzione)
├── .github/workflows/
│   ├── keepalive.yml                       ← GitHub Actions - Keepalive DB
│   └── build-test.yml                      ← GitHub Actions - Build e Test
├── render.yaml                             ← Configurazione Render (deploy)
├── README.md                               ← Documentazione principale
├── RENDER.md                               ← Guida Render
└── STUDIO_COMPLETO.md                      ← Questa guida!
```

### Cosa sono i DTO?

**DTO = Data Transfer Object**

Sono classi che rappresentano i dati scambiati tra client e server. Non sono direttamente i modelli del database; li usiamo per:

1. **Sicurezza**: Non esponiamo direttamente il modello del database
2. **Validazione**: Possiamo aggiungere regole di validazione
3. **Semplicità**: Il client riceve solo i dati che gli servono

Esempio:

```csharp
// Model (Interno, non lo mandiamo al client)
public class FarmacoClasseA
{
    public long Id { get; set; }              // Interno
    public string Aic { get; set; }
    public string DenominazioneConfezione { get; set; }
    public DateTimeOffset CreatedAt { get; set; }  // Interno
    // ... altri 30 campi del database ...
}

// DTO (Solo quello che il client vede)
public class FarmacoLookupDto
{
    public string Aic { get; set; }
    public string DenominazioneConfezione { get; set; }
    public decimal? PrezzoPubblico { get; set; }
    // Solo 9 campi utili
}
```

---

## Componenti Principali

### 1. Program.cs - L'Entry Point

**Cosa fa:**
È il primo file che .NET esegue quando il backend parte. Configura tutto: middleware, autenticazione, database, rate limiting.

**Cosa contiene:**

```csharp
// 1. Legge la configurazione dalle variabili d'ambiente
var jwtIssuer = builder.Configuration["Security:JwtIssuer"];

// 2. Registra i servizi (dice a .NET quali classi usare)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Aggiunge middleware (funzioni che processano ogni richiesta)
app.UseAuthentication();      // Controlla il token JWT
app.UseRateLimiter();          // Limita richieste (max 120/min per IP)
app.UseHttpsRedirection();     // Reindirizza a HTTPS

// 4. Mappa i controller
app.MapControllers();

// 5. Avvia il server
app.Run();
```

**Analogia:**
Program.cs è come il manuale di istruzioni di una fabbrica:
- Quali macchine usare
- Quanti operai
- Quale ordine di operazioni
- A che ora iniziare/finire

---

### 2. Controllers - Gli Handler di Richieste

**Cosa sono:**
Ricevono le richieste HTTP dai client e ritornano risposte.

#### AuthController - Autenticazione

**Endpoint:**
```
POST /api/auth/login
```

**Input (quello che il client manda):**
```json
{
  "username": "admin",
  "password": "mypassword123"
}
```

**Output (quello che ritorna il backend):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-06-27T20:00:00Z",
  "tokenType": "Bearer"
}
```

**Come funziona il login:**
1. Client manda username e password
2. AuthController confronta con credenziali salvate (in variabili d'ambiente)
3. Se corrette, genera un JWT token valido per 8 ore
4. Client riceve il token e lo conserva
5. Successivamente, il client manda il token in ogni richiesta protetta

#### FarmaciController - Ricerca Farmaci

**Endpoint 1: Lookup per AIC**
```
GET /api/farmaci/023076010
```

Ritorna il farmaco con quel codice AIC.

**Endpoint 2: Ricerca full-text**
```
GET /api/farmaci/search?q=paracetamol
```

Cerca i farmaci il cui nome contiene "paracetamol".

**Endpoint 3: Test connessione (protetto)**
```
GET /api/farmaci/test-connessione
```

Richiede il token JWT. Verifica che il database sia raggiungibile.

---

### 3. Data/AppDbContext - Collegamento al Database

**Cosa fa:**
È il "ponte" tra il codice C# e il database PostgreSQL.

```csharp
public class AppDbContext : DbContext
{
    public DbSet<FarmacoClasseA> FarmaciClasseA => Set<FarmacoClasseA>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configura come i dati sono salvati nel database
        modelBuilder.Entity<FarmacoClasseA>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Aic).IsUnique();  // AIC deve essere unico
        });
    }
}
```

**Cosa significa DbSet?**

```csharp
DbSet<FarmacoClasseA> FarmaciClasseA
```

È una "tabella virtuale" che rappresenta la tabella `farmaci_classe_a` del database. Puoi fare:

```csharp
// Leggere
var farmaco = await FarmaciClasseA.Where(f => f.Aic == "123").FirstAsync();

// Creare
var nuovoFarmaco = new FarmacoClasseA { Aic = "456", ... };
await FarmaciClasseA.AddAsync(nuovoFarmaco);
await SaveChangesAsync();

// Aggiornare / Cancellare (simili)
```

---

### 4. Models - Rappresentazione dei Dati

**FarmacoClasseA.cs**

Rappresenta un farmaco di Classe A italiano (incluso nel SSN).

```csharp
[Table("farmaci_classe_a")]  // Nome tabella nel database
public class FarmacoClasseA
{
    [Key]
    [Column("id")]
    public long Id { get; set; }  // Identificativo unico (Primary Key)

    [Required]  // Obbligatorio
    [Column("aic")]
    public string Aic { get; set; }  // Codice identificativo (es: "023076010")

    [Column("principio_attivo")]
    public string? PrincipioAttivo { get; set; }  // Principio attivo (es: "Paracetamolo")

    [Required]
    [Column("denominazione_confezione")]
    public string DenominazioneConfezione { get; set; }  // Nome commerciale

    [Column("prezzo_pubblico")]
    public decimal? PrezzoPubblico { get; set; }  // Prezzo (es: 5.50)

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }  // Quando è stato aggiunto

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }  // Ultimo aggiornamento
}
```

**Cosa significa `?` dopo il tipo?**

```csharp
public string? PrincipioAttivo { get; set; }
```

Il `?` significa "nullable" = può essere `null` (vuoto). Senza `?`, il campo è obbligatorio.

---

## Come Funziona l'Applicazione

### Il Ciclo Completo (End-to-End)

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Client fa richiesta                                      │
│    GET /api/farmaci/023076010                               │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 2. Networking Layer (ASP.NET Core Kestrel)                  │
│    - Riceve pacchetto HTTP                                  │
│    - Fa parsing: URL, headers, body                         │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 3. Middleware Pipeline                                      │
│    - CORS check: è un'origine autorizzata?                  │
│    - Rate Limiter: è entro i 120 req/min?                   │
│    - Security Headers: aggiungi header HTTP di sicurezza    │
│    - Authentication: se protetto, valida il JWT             │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 4. Routing (Program.cs MapGet/MapPost)                      │
│    - Capisce che va a FarmaciController.GetByAic            │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 5. Controller (FarmaciController.GetByAic)                  │
│    - Riceve il parametro: aic = "023076010"                 │
│    - Valida: aic non è vuoto?                               │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 6. Business Logic                                           │
│    var farmaco = await _db.FarmaciClasseA                   │
│        .Where(f => f.Aic == "023076010")                    │
│        .FirstOrDefaultAsync();                              │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 7. Database Query (Entity Framework → SQL)                  │
│    SELECT * FROM farmaci_classe_a                           │
│    WHERE aic = '023076010'                                  │
│    LIMIT 1;                                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 8. PostgreSQL/Supabase                                      │
│    - Esegue query                                           │
│    - Ritorna riga (o null se non esiste)                    │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 9. Entity Framework Mapping                                 │
│    - Converte risultato SQL in oggetto FarmacoClasseA       │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 10. Controller Response Mapping                             │
│     - Se farmaco exists: ritorna 200 OK + FarmacoLookupDto  │
│     - Se null: ritorna 404 Not Found                        │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 11. Serialization (C# Object → JSON)                        │
│     {                                                       │
│       "aic": "023076010",                                   │
│       "denominazioneConfezione": "Paracetamol 500mg",       │
│       "prezzo": 5.50                                        │
│     }                                                       │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 12. Networking Layer (Kestrel)                              │
│    - Converte JSON in HTTP response                         │
│    - Headers: Content-Type: application/json                │
│    - Status code: 200                                       │
└────────────────────┬────────────────────────────────────────┘
                     │
└────────────────────▼────────────────────────────────────────┐
│ 13. Client riceve risposta                                  │
└────────────────────────────────────────────────────────────┘
```

---

## Flusso di Richieste

### Scenario 1: Ricerca Farmaco Pubblico (Senza Autenticazione)

```
Client                    Backend                   Database
  │                         │                          │
  │──GET /api/farmaci/123──→│                          │
  │                         │                          │
  │                         │──SELECT * WHERE aic=123──│
  │                         │                          │
  │                         │←───Row data──────────────│
  │                         │                          │
  │←──200 + JSON────────────│                          │
```

### Scenario 2: Login (Genereazione Token)

```
Client                    Backend                   Database
  │                         │                          │
  │─POST /api/auth/login────│                          │
  │ {username, password}    │                          │
  │                         │                          │
  │                         │ Controlla variabili env  │
  │                         │ (Admin Username/Pwd)     │
  │                         │                          │
  │                         │ Password match? Sì       │
  │                         │ Genera JWT token         │
  │                         │                          │
  │←──200 + {token, exp}────│                          │
```

### Scenario 3: Richiesta Protetta (Con Token)

```
Client                    Backend                   Database
  │                         │                          │
  │─GET /api/farmaci/test──→│                          │
  │ Headers: {Auth: Bearer jwt...}                     │
  │                         │                          │
  │                         │ Verifica token:          │
  │                         │ - Firma valida?          │
  │                         │ - Non scaduto?           │
  │                         │ - Role corretto?         │
  │                         │ Tutto ok? Sì             │
  │                         │                          │
  │                         │──SELECT COUNT(*)─────────│
  │                         │                          │
  │                         │←─────Count────────────────│
  │                         │                          │
  │←──200 + {count}─────────│                          │
```

---

## Autenticazione e Sicurezza

### Come Funziona JWT (JSON Web Token)

JWT è uno **standard internazionale** per token di autenticazione.

**Struttura di un JWT:**

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJhZG1pbiIsImV4cCI6MTY4OTAwMDAwMH0.
SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c

^ Header              ^ Payload              ^ Signature
```

**Header:** Dice che è JWT e qual è l'algoritmo (HS256)
**Payload:** Dati utente (chi è, che ruoli ha, scadenza)
**Signature:** Firma digitale (prova che il token è autentico)

### Flusso Autenticazione JWT

```
┌──────────────┐
│ 1. CLIENT    │
│ manda login  │
└───────┬──────┘
        │
        ▼
┌──────────────────────────────────┐
│ 2. BACKEND riceve username/pwd   │
└───────┬──────────────────────────┘
        │
        ▼
┌──────────────────────────────────┐
│ 3. Controlla credenziali         │
│    Corrette? Sì                  │
└───────┬──────────────────────────┘
        │
        ▼
┌──────────────────────────────────┐
│ 4. Genera JWT con:               │
│    - Username                    │
│    - Role ("admin")              │
│    - Exp (8 ore da ora)          │
│    - Firma con secret key        │
└───────┬──────────────────────────┘
        │
        ▼
┌──────────────────────────────────┐
│ 5. Manda JWT al client           │
│    in HTTP response              │
└───────┬──────────────────────────┘
        │
        ▼
┌──────────────────────────────────┐
│ 6. CLIENT salva token            │
│    (browser localStorage /       │
│     app memory)                  │
└──────────────────────────────────┘


Successivamente, per richiedeste protette:

┌──────────────────────────────────┐
│ CLIENT                           │
│ GET /api/farmaci/test            │
│ Headers: Authorization: Bearer {JWT}
└───────┬──────────────────────────┘
        │
        ▼
┌──────────────────────────────────┐
│ BACKEND verifica JWT:            │
│ - Firma valida?                  │
│ - Non scaduto?                   │
│ - Role autorizzato?              │
│ Tutto ok? Sì → Procedi           │
└──────────────────────────────────┘
```

### Principi di Sicurezza Implementati

#### 1. Hashing delle Credenziali (Quando Login)

```csharp
// NON SI FA COSÌ (SBAGLIATO):
if (request.Password == configPassword)  // Password in chiaro! Pericoloso!

// SI FA COSÌ (BACKEND ATTUALE):
var passwordMatches = FixedTimeEquals(
    configPassword,      // Quella salvata (in env var)
    request.Password     // Quella mandata dal client
);
```

Usare `FixedTimeEquals` protegge da attacchi timing (cercare di indovinare la password contando il tempo di risposta).

#### 2. Rate Limiting (Protezione da Brute Force)

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString();
        
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => 
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,                    // MAX 120 richieste
                Window = TimeSpan.FromMinutes(1),     // per 1 minuto
            });
    });
});
```

Se un utente fa più di 120 richieste in 1 minuto, riceve 429 (Too Many Requests).

#### 3. Header di Sicurezza HTTP

```csharp
// Impedisce cliccando su link da eseguire JavaScript
context.Response.Headers["X-Content-Type-Options"] = "nosniff";

// Non permette di mettere il sito in un iframe
context.Response.Headers["X-Frame-Options"] = "DENY";

// Non manda il referrer (non dice da dove vieni)
context.Response.Headers["Referrer-Policy"] = "no-referrer";

// Disabilita telecamera, microfono, geolocalizzazione
context.Response.Headers["Permissions-Policy"] = 
    "camera=(), microphone=(), geolocation=()";
```

#### 4. HTTPS in Produzione (HSTS)

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();                   // Force HTTPS sempre
    app.UseHttpsRedirection();       // Reindirizza HTTP → HTTPS
}
```

#### 5. Secret del Keepalive Obbligatorio (Protezione da Attacchi)

Se qualcuno sa che esiste `/keepalive-db`, potrebbe farla chiamare da bot per consumare risorse.

Soluzione: Obbligare un secret header:

```csharp
if (!string.IsNullOrWhiteSpace(keepaliveSecret))
{
    if (!request.Headers.TryGetValue("X-KEEPALIVE", out var incomingSecret) 
        || incomingSecret != keepaliveSecret)
    {
        return Results.Unauthorized();
    }
}
```

---

## Database e Modelli

### Com'è Strutturato il Database

PostgreSQL è un database **relazionale**, cioè i dati sono organizzati in **tabelle**.

#### Tabella: farmaci_classe_a

```sql
CREATE TABLE farmaci_classe_a (
    id BIGSERIAL PRIMARY KEY,                    -- ID unico auto-incrementato
    aic VARCHAR(10) UNIQUE NOT NULL,             -- Codice AIC (unico)
    denominazione_confezione VARCHAR(255) NOT NULL,
    principio_attivo VARCHAR(255),
    prezzo_pubblico DECIMAL(10, 2),              -- Prezzo con 2 decimali
    titolare_aic VARCHAR(255),
    codice_gruppo_equivalenza VARCHAR(50),
    in_lista_trasparenza_aifa BOOLEAN DEFAULT FALSE,
    solo_lista_regione VARCHAR(50),
    metri_cubi_ossigeno DECIMAL(10, 2),
    created_at TIMESTAMP DEFAULT NOW(),          -- Quando inserito
    updated_at TIMESTAMP DEFAULT NOW()           -- Ultimo aggiornamento
);

-- Crea indice su AIC per ricerche veloci
CREATE UNIQUE INDEX idx_aic ON farmaci_classe_a(aic);
```

### Cosa sono gli Indici?

Un indice è come l'**indice di un libro**:
- Senza indice: Devi leggere tutte le 1000 pagine per trovare una parola
- Con indice: Vai alla pagina dell'indice e trovi subito

```sql
CREATE UNIQUE INDEX idx_aic ON farmaci_classe_a(aic);
```

Questo rende le ricerche per AIC **velocissime**, anche con milioni di farmaci.

### Relazione tra C# Model e Tabella

```csharp
[Table("farmaci_classe_a")]           // Nome tabella nel DB
public class FarmacoClasseA
{
    [Key]                              // Primary Key
    [Column("id")]                     // Nome colonna nel DB
    public long Id { get; set; }

    [Required]                         // NOT NULL nel DB
    [Column("aic")]
    public string Aic { get; set; }    // VARCHAR nel DB

    [Column("prezzo_pubblico")]
    public decimal? PrezzoPubblico { get; set; }  // DECIMAL nel DB, nullable
}
```

---

## API Endpoints

### Documentazione Completa degli Endpoint

#### 1. **GET /health** (Pubblico)

**Scopo:** Verifica che il backend sia online (usato da Render)

**Request:**
```http
GET /health HTTP/1.1
Host: api.pillapp.com
```

**Response (200 OK):**
```json
{
  "status": "ok",
  "service": "PillApp.Api"
}
```

**Quando usare:** Health check di monitoraggio

---

#### 2. **GET /keepalive-db** (Protetto con Secret)

**Scopo:** Mantiene PostgreSQL attivo (gratuito su Supabase che non ha timeout con keepalive)

**Request:**
```http
GET /keepalive-db HTTP/1.1
Host: api.pillapp.com
X-KEEPALIVE: your-secret-key
```

**Response (200 OK):**
```json
{
  "status": "ok",
  "database": "reachable"
}
```

**Cosa fa:**
```csharp
var canConnect = await db.Database.CanConnectAsync();
// Apre una connessione leggera al DB per far "sapere" che è usato
```

**Quando usare:** GitHub Actions scheduler (ogni 10 minuti)

---

#### 3. **POST /api/auth/login** (Pubblico)

**Scopo:** Login admin e generazione JWT

**Request:**
```http
POST /api/auth/login HTTP/1.1
Host: api.pillapp.com
Content-Type: application/json

{
  "username": "admin",
  "password": "mypassword"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-06-28T08:30:45.1234567+00:00",
  "tokenType": "Bearer"
}
```

**Response (401 Unauthorized):**
```
(Nessun body se credenziali sbagliate)
```

**Response (503 Service Unavailable):**
```json
{
  "error": "Admin configuration is missing."
}
```

---

#### 4. **GET /api/farmaci/{aic}** (Pubblico)

**Scopo:** Lookup farmaco per codice AIC

**Request:**
```http
GET /api/farmaci/023076010 HTTP/1.1
Host: api.pillapp.com
```

**Response (200 OK):**
```json
{
  "aic": "023076010",
  "denominazioneConfezione": "Paracetamol 500mg",
  "principioAttivo": "Paracetamolo",
  "prezzoPubblico": 5.50,
  "titolareAic": "GlaxoSmithKline",
  "codiceGruppoEquivalenza": "001",
  "inListaTrasparenzaAifa": true,
  "soloListaRegione": null,
  "metriCubiOssigeno": null
}
```

**Response (404 Not Found):**
```json
{
  "error": "No drug found for AIC 023076010."
}
```

**Response (400 Bad Request):**
```json
{
  "error": "Invalid AIC code."
}
```

---

#### 5. **GET /api/farmaci/search?q=paracetamol** (Pubblico)

**Scopo:** Ricerca full-text di farmaci

**Request:**
```http
GET /api/farmaci/search?q=paracetamol HTTP/1.1
Host: api.pillapp.com
```

**Response (200 OK):**
```json
[
  {
    "aic": "023076010",
    "denominazioneConfezione": "Paracetamol 500mg",
    "principioAttivo": "Paracetamolo",
    "prezzoPubblico": 5.50,
    ...
  },
  {
    "aic": "023076020",
    "denominazioneConfezione": "Paracetamol 1000mg",
    "principioAttivo": "Paracetamolo",
    "prezzoPubblico": 8.90,
    ...
  }
]
```

**Note:** Ritorna MAX 20 risultati (evita di mandare troppi dati)

---

#### 6. **GET /api/farmaci/test-connessione** (Protetto)

**Scopo:** Test connessione database (richiede admin token)

**Request:**
```http
GET /api/farmaci/test-connessione HTTP/1.1
Host: api.pillapp.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "message": "Database connection successful.",
  "totalRecords": 15234
}
```

**Response (401 Unauthorized):**
Se non mandi il token o il token è scaduto.

---

## Testing

### Perché i Test?

I test sono script automatici che:
1. Simulano richieste al backend
2. Verificano che le risposte siano corrette
3. Catturano bug prima di mettere in produzione

**Analogia:**
Senza test, è come guidare una macchina nuova senza test drive. Con i test, fai test drive su 1000 km di strada.

### Struttura Test xUnit

```csharp
[Fact]  // "Questo è un test"
public void Login_WithValidCredentials_ReturnsToken()
{
    // ARRANGE: Prepara i dati
    var controller = new AuthController(_configuration);
    var request = new AdminLoginRequestDto 
    { 
        Username = "admin", 
        Password = "password123" 
    };

    // ACT: Esegui l'azione che testi
    var result = controller.Login(request);

    // ASSERT: Verifica che il risultato sia corretto
    var okResult = result.Result as OkObjectResult;
    Assert.NotNull(okResult);
    Assert.Equal(200, okResult.StatusCode);
}
```

**Metodo AAA (Arrange-Act-Assert):**

1. **Arrange:** Prepara lo scenario (come in uno studio fotografico)
2. **Act:** Esegui l'azione (premi il bottone della fotocamera)
3. **Assert:** Verifica il risultato (controlla la foto)

### Test nel Progetto

**File:** `PillAppBackend/PillApp.Api.Tests/`

**Test di AuthController:**
- ✅ Login con credenziali valide ritorna token
- ✅ Login con credenziali invalide ritorna 401
- ✅ Login con credenziali vuote ritorna 400

**Test di FarmaciController:**
- ✅ Lookup con AIC valido ritorna 200 + farmaco
- ✅ Lookup con AIC non esistente ritorna 404
- ✅ Lookup con AIC vuoto ritorna 400
- ✅ Search con query vuota ritorna 400
- ✅ Test-connessione con admin ritorna 200

**Eseguire i test:**
```bash
dotnet test PillAppBackend/PillApp.Api.Tests/PillApp.Api.Tests.csproj
```

---

## Configurazione e Environment

### Cos'è la Configurazione?

Sono i **parametri** che cambiano tra ambienti:
- **Development** (macchina locale): debug attivo, database di test
- **Production** (server online): debug spento, database reale, HTTPS obbligatorio

### File di Configurazione

#### appsettings.json (Produzione)

```json
{
  "ConnectionStrings": {
    "SupabaseDb": ""  // Manda da variabili d'ambiente!
  },
  "Cors": {
    "AllowedOrigins": []  // Origini consentite
  },
  "Security": {
    "JwtIssuer": "",
    "JwtAudience": "",
    "JwtSigningKey": "",
    "AdminUsername": "",
    "AdminPassword": "",
    "AdminRole": "admin"
  }
}
```

#### appsettings.Development.json (Sviluppo Locale)

```json
{
  "ConnectionStrings": {
    "SupabaseDb": ""  // Ancora da env var locale
  },
  "Cors": {
    "AllowedOrigins": []
  }
}
```

### Variabili d'Ambiente (Environment Variables)

Non mettere **mai** password/chiavi in file nel repository!

Invece, usiamo variabili d'ambiente:

```bash
# Forma generale:
export ConnectionStrings__SupabaseDb="postgres://user:pwd@host/db"
export Security__JwtIssuer="my-issuer"
export Security__JwtAudience="my-audience"
export Security__JwtSigningKey="very-long-secret-key-at-least-32-chars"
export Security__AdminUsername="admin"
export Security__AdminPassword="strongpassword123"
export Security__AdminRole="admin"
```

**.NET converte doppi underscore in `:`**

```
ConnectionStrings__SupabaseDb  → ConnectionStrings:SupabaseDb
Security__JwtSigningKey         → Security:JwtSigningKey
```

### Accesso alla Configurazione nel Codice

```csharp
// In qualsiasi classe con iniezione di dipendenze:
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void DoSomething()
    {
        var issuer = _configuration["Security:JwtIssuer"];
        // Prende il valore dalla variabile d'ambiente!
    }
}
```

---

## Deployment su Render

### Cos'è Render?

Render è un **Platform-as-a-Service (PaaS)** che:
- Ospita il backend nel cloud
- Gestisce server, certificati SSL, scalabilità
- Integra con GitHub per auto-deploy

### Processo di Deploy su Render

```
┌─────────────────────────────────┐
│ 1. Pushare il codice su GitHub  │
│    git push origin main         │
└────────────────┬────────────────┘
                 │
┌────────────────▼────────────────┐
│ 2. Render vede il push          │
│    Legge render.yaml            │
└────────────────┬────────────────┘
                 │
┌────────────────▼────────────────┐
│ 3. Clona il repository          │
└────────────────┬────────────────┘
                 │
┌────────────────▼────────────────┐
│ 4. Esegue build command         │
│    dotnet build PillApp.Api.cs..│
└────────────────┬────────────────┘
                 │
┌────────────────▼────────────────┐
│ 5. Esegue start command         │
│    dotnet PillApp.Api.dll       │
└────────────────┬────────────────┘
                 │
┌────────────────▼────────────────┐
│ 6. Legge environment variables  │
│    (da Render dashboard)        │
└────────────────┬────────────────┘
                 │
┌────────────────▼────────────────┐
│ 7. API online! 🎉              │
│    https://your-app.onrender.com│
└─────────────────────────────────┘
```

### render.yaml - Configurazione Deploy

```yaml
services:
  - type: web
    name: pillapp-api
    env: docker                    # Usi Dockerfile
    dockerfilePath: Dockerfile     # Percorso Dockerfile
    healthCheckPath: /health       # Render controlla /health ogni 30s
    
    envVars:                       # Variabili d'ambiente da impostare in Render
      - key: ConnectionStrings__SupabaseDb
        sync: false                # sync: false = non autogenera, devi impostare
      - key: Security__JwtIssuer
        sync: false
      # ... altre variabili
```

### Dockerfile - Containerizzazione

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Fase di build
COPY PillAppBackend/PillApp.Api/PillApp.Api.csproj PillAppBackend/PillApp.Api/
RUN dotnet restore PillAppBackend/PillApp.Api/PillApp.Api.csproj
COPY . .
WORKDIR /src/PillAppBackend/PillApp.Api
RUN dotnet publish PillApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Fase di runtime (immagine finale più piccola)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .

# Comando di avvio
ENTRYPOINT ["sh", "-c", "dotnet PillApp.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
```

**Multi-stage build:**
1. **Fase 1 (build):** Scarica dipendenze, compila tutto (immagine grande ~1GB)
2. **Fase 2 (runtime):** Copia solo il compiled (immagine piccola ~300MB)

Questo riduce la dimensione finale dell'immagine del 70%!

---

## CI/CD Pipeline

### Cosa significa CI/CD?

- **CI (Continuous Integration):** Automatizza build e test ad ogni push
- **CD (Continuous Deployment):** Automatizza deploy in produzione

### GitHub Actions Workflow

**File:** `.github/workflows/build-test.yml`

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]   # Triggera su push a main/develop
    paths:
      - 'PillAppBackend/**'        # Solo se cambi il backend
  pull_request:
    branches: [ main, develop ]   # Triggera anche su PR

jobs:
  build-test:
    runs-on: ubuntu-latest         # Esegui su server Linux
    
    steps:
      - uses: actions/checkout@v4   # Scarica il codice
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0'
      
      - name: Build API
        run: dotnet build PillAppBackend/PillApp.Api/PillApp.Api.csproj
      
      - name: Run Tests
        run: dotnet test PillAppBackend/PillApp.Api.Tests/...
      
      - name: Build Docker Image
        run: docker build -f Dockerfile -t pillapp-backend:latest .
        if: github.event_name == 'push' && github.ref == 'refs/heads/main'
```

**Timeline di Esecuzione:**

```
1. Developer fa push
   ↓
2. GitHub Actions vede il push
   ↓
3. Accende un server Ubuntu
   ↓
4. Scarica il codice
   ↓
5. Installa .NET 10
   ↓
6. Esegue: dotnet build
   ↓
   ├─ Se fallisce → ❌ Build Failed
   └─ Se ok → Continua
   ↓
7. Esegue: dotnet test
   ↓
   ├─ Se test fallisce → ❌ Tests Failed
   └─ Se ok → Continua
   ↓
8. Se solo main branch: Build Docker image
   ↓
9. ✅ Build Successful!
   ↓
10. Notifica via email / GitHub interface
```

### Workflow del Keepalive

**File:** `.github/workflows/keepalive.yml`

```yaml
name: Keep Supabase Warm

on:
  schedule:
    - cron: '*/10 * * * *'  # Ogni 10 minuti (24*6 = 144 volte al giorno)

jobs:
  ping-backend:
    runs-on: ubuntu-latest
    steps:
      - name: Ping keepalive endpoint
        env:
          BACKEND_BASE_URL: ${{ secrets.BACKEND_BASE_URL }}
          KEEPALIVE_SECRET: ${{ secrets.KEEPALIVE_SECRET }}
        run: |
          curl --fail --silent --show-error \
            -H "X-KEEPALIVE: $KEEPALIVE_SECRET" \
            "$BACKEND_BASE_URL/keepalive-db"
```

**Cosa fa ogni 10 minuti:**
1. GitHub Actions esegue il job
2. Manda richiesta GET a `/keepalive-db` con il secret
3. Backend apre una connessione al database PostgreSQL
4. Questo "segnala" a Supabase che il DB è usato
5. Supabase non lo sospende

---

## Come Sviluppare Nuove Feature

### Processo di Sviluppo

#### 1. Creare un Branch per la Feature

```bash
git checkout -b feature/new-endpoint

# Ora sei su un branch isolato, il codice di main rimane intatto
```

#### 2. Fare i Cambiamenti

Esempio: Aggiungere un nuovo endpoint che ritorna statistiche farmaci

**Step 1: Creare il DTO**

```csharp
// PillApp.Api/Dtos/StatisticheFarmaciDto.cs
namespace PillApp.Api.Dtos;

public class StatisticheFarmaciDto
{
    public int TotaleFarmaci { get; set; }
    public int FarmaciGratuiti { get; set; }
    public decimal PrezzoMedio { get; set; }
}
```

**Step 2: Aggiungere il Metodo al Controller**

```csharp
// PillApp.Api/Controllers/FarmaciController.cs

[HttpGet("statistiche")]
[Authorize(Policy = "AdminOnly")]  // Solo admin
public async Task<ActionResult<StatisticheFarmaciDto>> GetStatistiche()
{
    var stats = new StatisticheFarmaciDto
    {
        TotaleFarmaci = await _db.FarmaciClasseA.CountAsync(),
        FarmaciGratuiti = await _db.FarmaciClasseA
            .Where(f => f.PrezzoPubblico == 0)
            .CountAsync(),
        PrezzoMedio = await _db.FarmaciClasseA
            .Where(f => f.PrezzoPubblico.HasValue)
            .AverageAsync(f => f.PrezzoPubblico ?? 0)
    };

    return Ok(stats);
}
```

**Step 3: Aggiungere un Test**

```csharp
// PillApp.Api.Tests/FarmaciControllerTests.cs

[Fact]
public async Task GetStatistiche_ReturnsStatistics()
{
    // Arrange
    using var context = GetInMemoryDbContext();
    var controller = new FarmaciController(context);

    // Act
    var result = await controller.GetStatistiche();
    var okResult = result.Result as OkObjectResult;

    // Assert
    Assert.NotNull(okResult);
    Assert.Equal(200, okResult.StatusCode);
    
    var stats = okResult.Value as StatisticheFarmaciDto;
    Assert.NotNull(stats);
    Assert.True(stats.TotaleFarmaci > 0);
}
```

#### 3. Compilare e Testare Localmente

```bash
# Build
dotnet build PillAppBackend/PillApp.Api/PillApp.Api.csproj

# Test
dotnet test PillAppBackend/PillApp.Api.Tests/

# Eseguire localmente
dotnet run --project PillAppBackend/PillApp.Api/
```

#### 4. Fare Commit

```bash
git add .
git commit -m "feat: aggiunto endpoint /api/farmaci/statistiche per admin"
```

#### 5. Push e Pull Request

```bash
git push origin feature/new-endpoint
```

Poi crei una Pull Request su GitHub per ottenere approvazione.

#### 6. GitHub Actions Verifica Tutto

- Build il progetto
- Esegue i test
- Se ok: ✅
- Se errori: ❌ (non puoi mergiare)

#### 7. Merge su Main

Una volta approvato, fai merge:

```bash
git checkout main
git pull origin main
git merge feature/new-endpoint
git push origin main
```

GitHub Actions fa il deploy automatico a Render!

---

### Checklist Sviluppo Feature

```
□ Creo DTO (data transfer object)
□ Creo metodo nel Controller
□ Aggiungo validazione input
□ Creo test per il nuovo metodo
□ Testo localmente (dotnet run)
□ Tutti i test passano? (dotnet test)
□ Build senza errori? (dotnet build)
□ Messaggi di errore chiari?
□ Ho aggiunto commenti se logica complessa?
□ Ho considerato casi edge (AIC vuoto, null, ecc)?
□ Ho controllato la sicurezza (autenticazione se protetto)?
□ Commit message descrittivo
□ Push su branch feature
□ Pull request su GitHub
□ GitHub Actions all green (✅)
□ Approval dai reviewer
□ Merge su main
□ Deploy automatico a Render
□ Test su produzione (curl https://...)
```

---

## Troubleshooting Comune

### Build Fallisce
```
Errore: "Impossibile trovare PackageReference 'xyz'"
Soluzione: 
  dotnet restore PillAppBackend/PillApp.Api/PillApp.Api.csproj
```

### Test Falliscono
```
Errore: "Test non passa"
Soluzione:
  1. Leggi il messaggio di errore
  2. Guarda lo stacktrace
  3. Debugga il test con breakpoint
```

### Variabili d'Ambiente Non Trovate
```
Errore: "Missing JWT configuration"
Soluzione:
  export Security__JwtSigningKey="your-key"
  export Security__JwtIssuer="your-issuer"
  etc.
```

### Docker Build Fallisce
```
Errore: "Failed to build image"
Soluzione:
  docker build -f Dockerfile -t test:latest . --progress=plain
  (per vedere i log dettagliati)
```

---

## Riassunto Veloce

### Il Progetto in 30 Secondi

```
PillApp Backend = API REST che:
  ├─ Ricerca farmaci in PostgreSQL
  ├─ Autentica admin con JWT
  ├─ Ha 6 endpoint (health, keepalive, login, lookup, search, test)
  ├─ È protetto da rate limiting, HTTPS, validazione
  ├─ Ha test xUnit
  ├─ Si deploya automatico su Render da GitHub
  └─ Usa CI/CD pipeline GitHub Actions
```

### Stack Tecnico

```
Frontend (Client)
    ↓ HTTP/HTTPS
ASP.NET Core 10 (Backend)
    ↓ SQL
PostgreSQL (Supabase)
```

### Componenti Chiave

1. **Program.cs** - Configurazione e middleware
2. **Controllers** - Ricevono e processano richieste
3. **Models** - Rappresentano i dati del DB
4. **DTOs** - Dati per request/response API
5. **DbContext** - Interroga il DB
6. **Tests** - Verificano che tutto funzioni

### Sicurezza

```
✅ JWT per autenticazione
✅ Rate limiting (120 req/min)
✅ HTTPS in produzione
✅ Header HTTP di sicurezza
✅ Validazione input
✅ Secret keeper per keepalive
✅ Variabili d'ambiente per credenziali
```

---

## Prossimi Passi

1. **Leggi il README.md** per panoramica tecnica
2. **Esegui localmente**: `dotnet run --project PillAppBackend/PillApp.Api/`
3. **Fai una richiesta**: `curl http://localhost:5227/health`
4. **Studia il codice**: Inizia da `Program.cs`, poi `Controllers`
5. **Esegui i test**: `dotnet test`
6. **Aggiungi una feature**: Seguendo la checklist sopra

---

## Glossario

| Termine | Significato |
|---------|------------|
| **API** | Application Programming Interface - Interfaccia per comunicare con il backend |
| **REST** | Representational State Transfer - Stile architetturale per API |
| **JWT** | JSON Web Token - Token di autenticazione |
| **ORM** | Object-Relational Mapper - Traduce oggetti in query SQL |
| **DTO** | Data Transfer Object - Classe per trasferire dati |
| **Middleware** | Funzione che processa ogni richiesta |
| **Endpoint** | Un URL specifico dell'API |
| **Rate Limiting** | Limitare numero di richieste per IP/utente |
| **CORS** | Cross-Origin Resource Sharing - Permessi tra domini |
| **HTTPS** | HTTP Secure - Comunicazione crittografata |
| **HSTS** | HTTP Strict Transport Security - Forza HTTPS |
| **CI/CD** | Continuous Integration/Deployment - Automazione deploy |
| **Container** | Immagine Docker con app + dipendenze |
| **PaaS** | Platform-as-a-Service - Hosting gestito (Render) |

---

## Risorse Utili

- **Documentazione .NET:** https://learn.microsoft.com/en-us/dotnet/
- **Entity Framework Core:** https://learn.microsoft.com/en-us/ef/
- **JWT:** https://jwt.io/
- **PostgreSQL:** https://www.postgresql.org/docs/
- **Supabase:** https://supabase.com/docs
- **Render:** https://render.com/docs
- **GitHub Actions:** https://docs.github.com/en/actions

---

**Questo documento è una guida completa. Stampa, salva e riferisciti quando studi il progetto!**

**Ultimo aggiornamento:** 2026-06-27
**Versione:** 1.0
