PillApp-Backend
================

Descrizione
-----------
Questo repository contiene il backend dell'app PillApp, implementato in ASP.NET Core (.NET 10). Fornisce un'API REST per la ricerca e il lookup di farmaci (classe A) e funzionalità di autenticazione per l'area admin.

Funzionalità principali
-----------------------
- Endpoint API per lookup farmaci (classe A)
- Autenticazione JWT per le operazioni amministrative
- Endpoint di healthcheck e keepalive per mantenere attivo il database (es. Supabase)
- Accesso ai dati tramite Entity Framework Core con provider Npgsql (Postgres/Supabase)
- CORS configurabile per ambienti di produzione/dev
- Rate limiting globale per mitigare abusi (limite di default: 120 richieste/minuto per IP)
- Intestazioni di sicurezza HTTP impostate in uscita (HSTS in produzione, X-Frame-Options, Referrer-Policy, ecc.)

Architettura e file chiave
-------------------------
- PillAppBackend/PillApp.Api/
  - Program.cs: configurazione dell'applicazione, middleware, routing e definition degli endpoint (health, keepalive-db, controllers)
  - Controllers/: contiene `AuthController.cs` e `FarmaciController.cs` per le rotte principali
  - Data/AppDbContext.cs: DbContext EF Core e mapping delle entità
  - Models/: modelle EF (es. FarmacoClasseA)
  - Dtos/: DTOs usati per request/response
  - Dockerfile: immagine per il deploy containerizzato

Endpoint importanti
-------------------
- GET /health
  - Restituisce lo stato dell'applicazione
- GET /keepalive-db
  - Esegue `db.Database.CanConnectAsync()` per verificare la raggiungibilità del database. Pensato per essere chiamato da uno scheduler (es. GitHub Actions) per mantenere attivo il DB.
- Controller Admin/Autenticazione
  - Route per login admin (fornisce JWT) e operazioni protette da ruolo `admin`.
- Router Farmaci
  - Endpoint per ricerca/lookup farmaci (AIC, nome, ecc.)

Sicurezza e configurazione
--------------------------
Variabili di ambiente / impostazioni (consigliate come secrets su hosting):
- ConnectionStrings: `SupabaseDb` (connection string PostgreSQL)
- Security__JwtIssuer
- Security__JwtAudience
- Security__JwtSigningKey
- Security__AdminUsername
- Security__AdminPassword
- Security__AdminRole (opzionale, default: "admin")
- CORS: `Cors:AllowedOrigins` (array di origini per produzione)

Note di sicurezza:
- L'endpoint `/keepalive-db` è protetto con un header segreto e, fuori da Development, il secret è obbligatorio all'avvio. Si usa un header custom controllato tramite variabile d'ambiente (es. `KEEPALIVE_SECRET`).
- Non includere chiavi o password nei file di configurazione del repo. Usa GitHub Secrets / Render environment variables.

Keepalive: evitare che Supabase si sospenda
-----------------------------------------
Strategia adottata nel repository:
- Workflow GitHub Actions: `.github/workflows/keepalive.yml` esegue una richiesta periodica (cron) all'endpoint `/keepalive-db` per tenere attiva la connessione al DB.

Perché funziona
- `CanConnectAsync()` apre una connessione leggera al DB. Un ping periodico impedisce che il provider (es. Supabase) classifichi il database come inattivo.

Raccomandazioni:
- Proteggi la route con un header segreto e passa il segreto come GitHub Secret. Esempio curl nel workflow:

  curl --fail --silent --show-error -H "X-KEEPALIVE: ${{ secrets.KEEPALIVE_SECRET }}" "$BACKEND_BASE_URL/keepalive-db"

- Riduci la frequenza della cron se vuoi risparmiare minuti Actions (es. ogni 15-30 minuti). 10 minuti è accettabile ma non sempre necessario.

Esecuzione locale
-----------------
Prerequisiti:
- .NET SDK 8/10 installato
- Postgres/Supabase accessibile o connection string valida

Esempi di comandi:

  dotnet build PillAppBackend/PillApp.Api/PillApp.Api.csproj
  dotnet run --project PillAppBackend/PillApp.Api/PillApp.Api.csproj

Impostare le variabili d'ambiente in PowerShell (esempio):

  $Env:ASPNETCORE_ENVIRONMENT = "Development"
  $Env:ConnectionStrings__SupabaseDb = "Host=...;Database=...;Username=...;Password=..."
  $Env:Security__JwtIssuer = "your-issuer"
  $Env:Security__JwtAudience = "your-audience"
  $Env:Security__JwtSigningKey = "long-secret-key"
  $Env:Security__AdminUsername = "admin"
  $Env:Security__AdminPassword = "strong-password"

Deploy
------
Opzioni comuni:
- Render: usare il `render.yaml` e impostare le environment variables in Render dashboard. Il repository include materiale (`render.yaml`, `RENDER.md`) per agevolare il deploy.
- Docker: esiste un `Dockerfile` per costruire l'immagine del backend; puoi deployare su qualsiasi container registry o PaaS che supporti Docker.

CI / Quality
------------
- Si consiglia di aggiungere una GitHub Action che esegue `dotnet build` e i test su ogni push e PR. Questo aiuta a prevenire commit che rompono la build.

Pulizia e manutenzione
----------------------
- `.gitignore` contiene le regole per escludere `bin/`, `obj/`, file IDE e artifact di compilazione.
- Alcuni script locali per pulizia/storico (es. git-filter-repo) possono esistere fuori dal repo; non aggiungere strumenti eseguibili non necessari al repository.

Contatti
--------
Per domande sul deploy o su come impostare i secrets su Render / GitHub Actions, contattare il maintainer del progetto.

Licenza
-------
Specificare qui la licenza del progetto se applicabile.
