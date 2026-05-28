# Render deployment

Use `PillAppBackend/PillApp.Api` as the root directory.

Use the Render blueprint in `render.yaml`.

Build command:

`dotnet build PillApp.Api.csproj`

Start command:

`dotnet PillApp.Api.dll --urls http://0.0.0.0:$PORT`

Health check path:

`/health`

Environment variables:

- `ConnectionStrings__SupabaseDb`
- `Security__JwtIssuer`
- `Security__JwtAudience`
- `Security__JwtSigningKey`
- `Security__AdminUsername`
- `Security__AdminPassword`
- `Security__AdminRole` if you do not want the default `admin`
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... for the frontend origins

React Native note:

- For a native React Native app, browser CORS is not a factor, so you usually do not need to set any allowed origins.
- If you also run Expo Web or another browser-based preview, add its origin to `Cors__AllowedOrigins`.
- During local development, point the app to a reachable backend URL: `http://localhost:5000` for desktop-only testing, `http://10.0.2.2:5000` for the Android emulator, or your LAN IP for a physical device.

Notes:

- The backend now requires a valid JWT for the admin diagnostics endpoint.
- Use `POST /api/auth/login` with the admin username and password to obtain the token.
- The JWT must contain the configured admin role claim.
- Render sits behind a proxy, so forwarded headers and HTTPS redirection are already handled in the app.
- The public `/health` endpoint is meant for Render checks and does not expose sensitive data.