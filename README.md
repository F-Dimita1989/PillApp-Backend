PillApp-Backend
================

Descrizione
-----------
Backend dell'app PillApp, in ASP.NET Core (.NET 10). Espone un'API REST **di sola lettura** per la ricerca e il lookup dei farmaci di classe A. I dati vengono caricati e aggiornati direttamente su Supabase: l'API non ha endpoint di scrittura e non richiede autenticazione.

Funzionalità principali
-----------------------
- Ricerca paginata dei farmaci per principio attivo, denominazione della confezione e descrizione del gruppo
- Lookup di un singolo farmaco per codice AIC
- Cache in memoria dei risultati più header `Cache-Control`, per non interrogare Supabase a ogni carattere digitato
- Compressione Brotli/Gzip delle risposte
- Healthcheck e keepalive per impedire a Supabase di sospendere il progetto
- Accesso ai dati con Entity Framework Core e provider Npgsql
- CORS configurabile, rate limiting per IP, intestazioni di sicurezza HTTP
- Gestione centralizzata degli errori con risposte `ProblemDetails`

Architettura e file chiave
-------------------------
- `PillApp.slnx` — solution che raggruppa i due progetti
- `Directory.Packages.props` — versioni dei pacchetti NuGet, centralizzate
- `PillAppBackend/PillApp.Api/`
  - `Program.cs` — configurazione dell'applicazione, middleware, endpoint minimali
  - `Controllers/FarmaciController.cs` — validazione dell'input e rotte pubbliche
  - `Services/FarmaciReadService.cs` — query di lettura e cache
  - `Helpers/FarmacoSearchQuery.cs` — costruzione della ricerca testuale
  - `Helpers/FarmacoDtoMapper.cs` — proiezione entità → DTO
  - `Infrastructure/GlobalExceptionHandler.cs` — errori non gestiti
  - `Data/AppDbContext.cs` — DbContext EF Core
  - `Models/FarmacoClasseA.cs` — entità mappata sulla tabella `farmaci_classe_a`
- `PillAppBackend/PillApp.Api.Tests/` — test unitari e di integrazione (xUnit)
- `scripts/create-search-indexes.sql` — indici richiesti dal database
- `Dockerfile`, `render.yaml` — deploy containerizzato su Render

Endpoint
--------
| Metodo | Rotta | Descrizione |
|--------|-------|-------------|
| GET | `/` | Elenco degli endpoint pubblici |
| GET | `/health` | Stato dell'applicazione. Non interroga il database, così un problema di rete verso Supabase non provoca il riavvio continuo del servizio su Render |
| GET | `/keepalive-db` | Verifica la raggiungibilità del database. Richiede l'header `X-KEEPALIVE` |
| GET | `/api/farmaci/search?q=&limit=&offset=` | Ricerca paginata. `q` richiede almeno 3 caratteri, `limit` va da 1 a 100 (default 20) |
| GET | `/api/farmaci/{aic}` | Lookup per codice AIC |

Configurazione
--------------
Variabili d'ambiente (da impostare come secrets sull'hosting):

| Variabile | Obbligatoria | Descrizione |
|-----------|--------------|-------------|
| `ConnectionStrings__SupabaseDb` | sì | Connection string PostgreSQL |
| `Security__KeepaliveSecret` | sì fuori da Development | Segreto atteso nell'header `X-KEEPALIVE` |
| `Cors__AllowedOrigins__0` | in produzione | Origine consentita. Aggiungerne altre incrementando l'indice |
| `Cache__TtlMinutes` | no | Durata della cache in minuti (default 360) |
| `RateLimiting__PermitPerMinute` | no | Richieste al minuto per IP (default 300) |

L'applicazione non parte se manca la connection string o, fuori da Development, il segreto di keepalive: meglio un errore immediato che un servizio online e non funzionante.

Indici del database
-------------------
Prima di andare in produzione eseguire `scripts/create-search-indexes.sql` nel SQL Editor di Supabase. Senza quegli indici il lookup per AIC e la ricerca testuale eseguono scansioni complete della tabella.

Keepalive: evitare che Supabase si sospenda
-------------------------------------------
Il piano gratuito di Supabase mette in pausa il progetto dopo 7 giorni di inattività. Il workflow `.github/workflows/keepalive.yml` chiama periodicamente `/keepalive-db`, che apre una connessione leggera con `CanConnectAsync()`. Servono due secrets su GitHub: `BACKEND_BASE_URL` e `KEEPALIVE_SECRET`.

Da tenere presente: GitHub disattiva i workflow schedulati nei repository senza commit da 60 giorni, e può ritardare le esecuzioni cron nei momenti di carico.

Esecuzione locale
-----------------
Prerequisiti: .NET SDK 10 e una connection string PostgreSQL valida.

    dotnet build PillApp.slnx
    dotnet test PillApp.slnx
    dotnet run --project PillAppBackend/PillApp.Api/PillApp.Api.csproj

In Development sono attivi Swagger su `/swagger` e CORS aperto a qualsiasi origine.

Per non tenere le credenziali in `appsettings.Development.json`, usare i user secrets:

    dotnet user-secrets init --project PillAppBackend/PillApp.Api
    dotnet user-secrets set "ConnectionStrings:SupabaseDb" "Host=...;Database=...;Username=...;Password=..." --project PillAppBackend/PillApp.Api

Il file `PillAppBackend/PillApp.Api/PillApp.Api.http` contiene richieste pronte da eseguire.

Deploy
------
- **Render**: usare `render.yaml` come blueprint e impostare le tre variabili d'ambiente nella dashboard. Health check su `/health`. Vedi `RENDER.md`.
- **Docker**: `docker build -f Dockerfile -t pillapp-backend .`. L'immagine ascolta sulla porta indicata da `PORT` (default 8080) e gira con un utente non-root.

CI
--
`.github/workflows/build-test.yml` esegue restore, scansione delle vulnerabilità NuGet, build e test a ogni push e pull request su `main` e `develop`, e costruisce l'immagine Docker sui push in `main`.

Licenza
-------
Specificare qui la licenza del progetto se applicabile.
