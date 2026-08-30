# Deploy su Render

## Configurazione del servizio

Il deploy avviene tramite immagine Docker: usare il blueprint `render.yaml` nella root del repository, che punta al `Dockerfile`. Non servono build command né start command, perché sono già definiti nel Dockerfile.

- Health check path: `/health`
- Porta: l'immagine legge la variabile `PORT` che Render imposta automaticamente

## Variabili d'ambiente

| Variabile | Obbligatoria | Note |
|-----------|--------------|------|
| `ConnectionStrings__SupabaseDb` | sì | Connection string PostgreSQL di Supabase |
| `Security__KeepaliveSecret` | sì | Segreto atteso nell'header `X-KEEPALIVE`, deve coincidere con il secret su GitHub |
| `Cors__AllowedOrigins__0` | solo per client browser | Aggiungere `__1`, `__2` e così via per più origini |
| `Cache__TtlMinutes` | no | Default 360 |
| `RateLimiting__PermitPerMinute` | no | Default 300 |

Il servizio non parte se manca la connection string o il segreto di keepalive: è voluto, così un errore di configurazione emerge subito invece di produrre un servizio online ma rotto.

## Nota su React Native

In un'app React Native nativa il CORS del browser non entra in gioco, quindi di norma non serve impostare nessuna origine consentita. Va configurato solo se si usa Expo Web o un'altra anteprima nel browser.

In sviluppo locale puntare l'app a un URL raggiungibile: `http://localhost:5227` per il solo desktop, `http://10.0.2.2:5227` per l'emulatore Android, oppure l'IP della LAN per un dispositivo fisico.

## Keepalive di Supabase

Il piano gratuito di Supabase sospende il progetto dopo 7 giorni di inattività. Invece del Render Cron (a pagamento) si usa GitHub Actions:

1. Impostare `Security__KeepaliveSecret` su Render
2. Impostare su GitHub i secrets `BACKEND_BASE_URL` (l'URL pubblico Render) e `KEEPALIVE_SECRET` (lo stesso valore)
3. Il workflow `.github/workflows/keepalive.yml` chiama `https://tuo-servizio.onrender.com/keepalive-db` a intervalli regolari

La chiamata tiene sveglio sia il database che il servizio web, evitando anche il cold start del piano gratuito di Render.

## Altre note

- L'API è di sola lettura e pubblica: non c'è nessun token da configurare né endpoint amministrativi da proteggere
- Render sta dietro un proxy: l'app è già configurata per leggere gli header inoltrati e ricavare l'IP reale del client, necessario al rate limiting
- `/health` non interroga il database, per evitare che un problema di rete verso Supabase provochi il riavvio a ciclo continuo del servizio
- Prima del primo deploy eseguire `scripts/create-search-indexes.sql` su Supabase
