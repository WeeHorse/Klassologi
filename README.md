# Klassologi

En social applikation för skolklasser. Krav, beslut och öppna frågor finns i [Specifications](Specifications/).

## Lokal utveckling

Kräver:
- Docker Engine 25 eller senare med Compose (Testcontainers kräver Docker API 1.44)
- .NET SDK 10
- Node.js 22 eller senare

### 1. Databas (MySQL i Docker)

1. Skapa `.env` från mallen och byt ut lösenorden:

   ```sh
   cp .env.example .env
   ```

   `.env` ignoreras av git. Lägg aldrig riktiga lösenord i `.env.example`.

2. Starta databasen:

   ```sh
   docker compose up -d
   ```

   Databasen är redo när `docker compose ps` visar `healthy`. Den nås bara från den egna datorn, på `127.0.0.1:3306` (porten kan ändras med `MYSQL_PORT` i `.env`).

3. Anslut med en klient vid behov:

   ```sh
   docker compose exec mysql sh -c 'mysql -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" "$MYSQL_DATABASE"'
   ```

Stoppa med `docker compose down`. Datat ligger kvar i en Docker-volym. `docker compose down -v` raderar datat och ger en tom databas vid nästa start.

### 2. API (.NET)

Sätt anslutningssträngen som user secret, så att lösenordet hålls utanför repot (värdena från `.env`):

```sh
dotnet user-secrets set "ConnectionStrings:Klassologi" \
  "Server=127.0.0.1;Port=3306;Database=klassologi;User ID=klassologi;Password=<MYSQL_PASSWORD>;SslMode=Required" \
  --project src/Klassologi.Api
```

Starta API:t på `http://localhost:5080`:

```sh
dotnet run --project src/Klassologi.Api
```

Migreringarna i `src/Klassologi.Api/Data/Migrations/` körs automatiskt när API:t startar. `GET /api/status` visar att API:t når databasen.

Under utveckling finns API-dokumentation och ett testgränssnitt på http://localhost:5080/scalar. Logga in med `POST /api/auth/login`, så skickas sessionscookien med i följande anrop.

De flesta endpoints kräver inloggning. API:t finns i [UseCases.md](Specifications/UseCases.md#api-steg-3a).

#### Första administratören

Konton skapas bara genom inbjudan. Den första administratören bjuds in med ett kommando på servern:

```sh
dotnet run --project src/Klassologi.Api -- create-admin-invitation
```

Kommandot skriver ut en engångslänk som gäller i 24 timmar. Länkens adress byggs från `App:PublicBaseUrl` (lokalt `http://localhost:5173`). Öppna länken i webbläsaren för att skapa administratörskontot.

### 3. Webbapp

```sh
cd web
npm install
npm run dev
```

Öppna `http://localhost:5173`. Vite skickar vidare anrop till `/api` till API:t.

## Tester

Alla tester nedan körs automatiskt vid varje pull request mot `main` (GitHub Actions, `.github/workflows/tests.yml`).


| Vad | Kommando | Kräver |
|-----|----------|--------|
| Backend (xUnit + Testcontainers) | `dotnet test --solution Klassologi.slnx` | Docker. Startar en egen databas och rör inte utvecklingsdatabasen. |
| Frontendlogik (Vitest) | `npm test` i `web/`, eller `npm run test:ui` för ett gränssnitt i webbläsaren som kör om testerna vid ändringar | – |
| Lint (förbjuder bl.a. `innerHTML`) | `npm run lint` i `web/` | – |
| Typkontroll och bygge | `npm run build` i `web/` | – |
| Hela kedjan och tillgänglighet (Playwright + axe) | `npm run e2e` i `web/` | Databasen igång och anslutningssträngen satt. Startar egna instanser av API (port 5081) och webbapp (port 5174), skapar en administratör med kommandot och skapar testkonton i utvecklingsdatabasen. Första gången: `npx playwright install chromium`. |
