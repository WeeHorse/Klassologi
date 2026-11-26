# Technology

Praktiska teknologier, mjukvaruarkitektur och teknisk metodik.

Status per val: **Beslutat** | **Förslag** (ej beslutat) | **Att verifiera**

## Översikt

| Del | Val | Status |
|-----|-----|--------|
| Backend | C# / .NET (LTS-version), ASP.NET Core Minimal API | Beslutat |
| Arkitektur | Tunn, imperativ, organiserad per funktion (se nedan) | Beslutat |
| Databas | MySQL, aktuell LTS-version (9.7) | Beslutat |
| Lokal databas | MySQL i Docker via Docker Compose (se nedan) | Beslutat |
| Dataåtkomst | Dapper + MySqlConnector, ingen EF Core (se nedan) | Beslutat |
| Migreringar | Versionerade SQL-skript med DbUp (se nedan) | Beslutat |
| Realtid | SignalR | Förslag – behövs från lager 4 (kommunikation) |
| Kontrakt mot klient | OpenAPI-dokument (`Microsoft.AspNetCore.OpenApi`) och Scalar som testgränssnitt, bara under utveckling | Beslutat |
| Klient mot API | Egen, handskriven klient med `fetch` (se nedan) | Beslutat |
| Klient | TypeScript + Vite + lit-html, webb först (se nedan) | Beslutat |
| UI-komponenter | Ionic Core 9 (`@ionic/core`), webbkomponenter med plattformsanpassat utseende (se nedan) | Beslutat |
| Mobilapp | Capacitor-skal kring webbappen, när appen ska ut i appbutikerna | Beslutat |
| Autentisering | ASP.NET Core Identity med egen lagring via Dapper, konton bara genom inbjudan (se nedan) | Beslutat |
| Filer | S3-kompatibel objektlagring | Förslag |
| Video/röst | LiveKit | Förslag – senare |
| Drift | EU-baserad hosting | Förslag |
| Test | xUnit (backend), Newman (API), Vitest och Playwright (frontend) – se nedan | Beslutat |

## Arkitektur

**Beslutat:** En tunn, imperativ arkitektur med minimum av ceremoni. Diagram finns i [Architecture.md](Architecture.md).

Principer:

- **Organisera per funktion**, inte per teknisk typ. Varje funktionsområde (t.ex. `Classes`, `Users`, `Posts`) samlar sina endpoints, logik och datatyper på ett ställe.
- **Endpoints anropar logik direkt.** Minimal API-endpoints anropar vanliga metoder som läser och skriver med explicit SQL via Dapper. Ingen mediator, inga kommandobussar.
- **Inga abstraktioner i förväg.** Inga generiska repositories, inga interfaces med en enda implementation och inga separata lagerprojekt (Domain/Application/Infrastructure) förrän ett konkret behov uppstår.
- **Affärsregler i vanlig kod där de hör hemma.** Regler som rör roller, anonymitet, moderering och klassavgränsning ska vara samlade, läsbara och enhetstestbara.
- **Behörighet explicit i backend.** Behörighet avgörs i vanlig kod mot databasen, samlad per område. Åtkomst till en klass avgörs bara i `Features/Classes/ClassAccess.cs`. ASP.NET Core:s autentisering används för att veta vem användaren är (från steg 3). Behörighetspolicies införs bara om de förenklar koden.

Grundstruktur (etablerad i walking skeleton, 2026-11-26):

```
Klassologi.slnx
global.json          .NET SDK-version och testplattform
compose.yaml         lokal databas
src/
  Klassologi.Api/
    Program.cs
    Data/            anslutning (Db), migreringar (DbUp)
      Migrations/    numrerade SQL-skript, t.ex. 0001_database_defaults.sql
    Auth/            Identity: lagring, inloggning, engångslänkar, kontoregler
    Features/        ett funktionsområde per katalog: Status/, Classes/, Invitations/, PasswordResets/, senare Posts/
tests/
  Klassologi.Api.Tests/
web/                 frontend (Vite, lit-html)
  src/               per funktion, t.ex. status/
  e2e/               Playwright
```

**Migreringar (beslutat 2026-11-26):** DbUp (`dbup-mysql`). Skripten ligger som inbäddade resurser i `Data/Migrations/`, körs i namnordning när API:t startar och loggas i tabellen `schemaversions`. Ett kört skript ändras aldrig, utan rättas med ett nytt skript.

**Konfiguration:** Anslutningssträngen `ConnectionStrings:Klassologi` ligger aldrig i repot. Lokalt sätts den med `dotnet user-secrets`, i drift med miljövariabel eller en hanterad hemlighet.

## Frontend

**Beslutat (2026-11-26):** TypeScript + Vite + lit-html, webb först. Capacitor läggs till när appen ska ut i appbutikerna.

**Principer:**

- **Vanlig imperativ TypeScript** organiserad per funktion, i linje med backendens arkitektur. Inget ramverk för tillstånd eller rendering.
- **lit-html enbart för rendering.** Det är ett mallbibliotek, inte ett ramverk, och det escapar text automatiskt. Användargenererat innehåll (inlägg, kommentarer, chatt) visas därför säkert som text.
- **Inget `innerHTML`.** Förbjuds med en lint-regel, för att undvika XSS.
- **API-anrop via en egen klient** (`web/src/api.ts`, beslutat 2026-11-26): tunna, typade funktioner ovanpå `fetch`. Typerna skrivs för hand efter API-kontraktet i [UseCases.md](UseCases.md#api-steg-3a), med OpenAPI-dokumentet som referens. Eftersom typerna inte genereras kan klienten glida isär från API:t. Det fångas av Playwright-testerna, som kör mot det riktiga API:t.
- **Navigering med en egen, minimal router** över History API:t. Varje vy är en modul som renderar sig själv med lit-html. Ionics egen router används inte, eftersom den förutsätter att varje vy är en egen webbkomponent.
- **Tillgänglighet:** WCAG 2.2 AA ([Q17](Questions.md#q17-krav-på-tillgänglighet)), kontrolleras med axe (`@axe-core/playwright`).
- **Webben ska fungera fullt ut på egen hand.** Capacitor-skalet lägger bara till mobilfunktioner som push-notiser och notisåtgärder via plugins. Webbkoden ska inte behöva skrivas om.

**UI-komponenter (beslutat 2026-11-26):** Ionic Core 9 (`@ionic/core`, MIT), fäst vid en version som har funnits i minst två veckor.

- Komponenterna är webbkomponenter och används direkt i lit-html-mallar med egenskaps- och händelsebindningar (`.value=`, `@ionInput=`). Inget Angular, React eller Vue.
- Utseendet anpassas efter plattform: iOS-stil på iPhone, Material-stil på Android. Det minskar skillnaden mot en native-app i Capacitor-skalet.
- Ionic står för färdiga mobilmönster: flikar, listor, modaler, toasts, dra för att uppdatera och virtuell scroll.
- Egen styling görs med Ionics CSS-variabler och `::part`, inte genom att skriva över intern CSS.

*Konsekvenser:*
- Appen ser ut som en mobilapp även på datorn. Datorlayout (t.ex. `ion-split-pane`) måste utformas medvetet, eftersom webben ska fungera fullt ut.
- Ionic genomsyrar mallarna och är svårt att byta bort senare.
- Komponenterna använder Shadow DOM. Playwright hanterar det, men selektorer bör bygga på roller och etiketter, inte intern struktur.

*Övervägda alternativ:* Bootstrap (stabilt och välkänt, men generiskt webbutseende och JavaScript-komponenter som krockar med lit-html), Pico CSS och egen CSS (lättare, men utan mobilmönster och plattformsanpassning).

**Stoppunkt:** Om vi börjar bygga eget maskineri för tillstånd och omritning, troligen kring realtidschatten i lager 4, utvärderar vi ett lätt ramverk (t.ex. Svelte eller Preact) i stället för att växa ett eget.

**Bakgrund:** Expo (React Native) övervägdes. Det ger mer native-känsla och fullt stöd för mobilfunktioner, men har betydligt mer ceremoni och passar sämre med den tunna, imperativa arkitekturen. Med Capacitor behåller vi en vanlig webbapp och får ändå riktiga appar för iOS och Android.

**Konsekvenser:**

- Gränssnittet i mobilappen är webb i en inbyggd webbvy. Det känns inte helt native vid tunga animationer eller långa listor.
- Bygge för iOS kräver Xcode (Mac) och utvecklarkonton för App Store och Google Play.

## Dataåtkomst: Dapper utan EF Core

**Beslutat (2026-11-26):** Dapper med MySqlConnector som databasdrivrutin. Ingen EF Core.

**Bakgrund:** Pomelo, den mognaste MySQL-providern för EF Core, saknade stöd för EF Core 10 (LTS). Den senaste versionen, 9.0.0, är låst till EF Core 9, vars support har upphört. Oracles provider stöder EF Core 10, men vi valde i stället att avstå från EF Core helt. Det ger tunnare dataåtkomst och tar bort beroendet av en ORM-provider. Paketversioner kontrollerade på NuGet 2026-11-26: Dapper 2.1.x och MySqlConnector 3.0.0 (med `net10.0`).

**Konsekvenser:**

- SQL skrivs explicit och syns i koden. Inget dolt beteende från en ORM.
- Migreringar hanteras med versionerade SQL-skript i stället för EF Core-migreringar.
- Det finns inga globala query-filter. Klassavgränsningen måste därför upprätthållas i koden (se nedan).
- Användarhantering kan inte bygga på ASP.NET Core Identitys färdiga EF Core-lagring. Vi skriver därför egen lagring för Identity med Dapper (se [Autentisering](#autentisering)).

**Klassavgränsning utan query-filter:**

- Varje fråga mot klassbunden data tar klassens id som uttrycklig parameter, och id:t hämtas från den inloggade användarens verifierade medlemskap, aldrig enbart från klientens anrop.
- Medlemskapskontrollen samlas på ett ställe och anropas av varje klassbunden endpoint.
- Automatiska tester verifierar att en användare inte kan läsa eller ändra data i en klass som användaren inte tillhör.

## Test

**Beslutat (2026-11-26):** xUnit för backend, Newman för API-tester, Vitest och Playwright för frontend.

**Ansvarsfördelning:**

| Verktyg | Vad det testar | Typiskt innehåll |
|---------|----------------|------------------|
| xUnit (enhetstester) | Affärsregler isolerat | Roller, behörighetsregler, anonymitet, validering |
| xUnit + `WebApplicationFactory` + Testcontainers | API:t inifrån, mot riktig MySQL | Klassavgränsning, behörighet per endpoint, felfall, databasbeteende |
| Newman | API:t utifrån som svart låda, mot en körande miljö | Use-case-flöden från början till slut, kontraktet mot frontend, kontroller efter driftsättning |
| Vitest | Frontendlogik | |
| Playwright | Hela flöden i webbläsaren | Kritiska flöden i MVP:n |

**Riktlinjer:**

- **Ingen dubblering.** Behörighet och klassavgränsning testas i xUnit-integrationstesterna, där testdata kan styras. Newman testar flöden och kontrakt.
- **Oberoende verifiering.** Newman-samlingar skrivs utifrån use cases och API-kontraktet, inte utifrån implementationen.
- **Newman, inte Postman CLI.** Newman är öppen källkod och kräver inget Postman-konto.
- **Samlingar i repot.** Postman-samlingar exporteras till repot, hålls små och delas upp per use case eller lager.
- **Inga hemligheter i repot.** Tokens och lösenord läses från miljövariabler, aldrig från samlingar eller miljöfiler i repot. Miljöer med hemligheter synkas inte till Postmans moln.
- **Känd utgångspunkt.** Varje Newman-körning använder en återställbar testmiljö eller skapar sin egen testdata, t.ex. en egen klass.
- **Utvärdering.** Om samlingarna blir svåra att granska i git utvärderas textbaserade alternativ (t.ex. Bruno eller `.http`-filer).
- **Körs lokalt tills vidare (beslutat 2026-11-26).** Testerna körs lokalt innan en pull request öppnas eller uppdateras. De flyttas till en CI-pipeline senare.

**Versioner och miljökrav (2026-11-26):**

- **xUnit v3** körs med Microsoft Testing Platform (`dotnet test --solution Klassologi.slnx`), vilket .NET 10 SDK kräver. Inställningen finns i `global.json`.
- **Testcontainers** kräver Docker Engine 25 eller senare (Docker API 1.44).
- **TypeScript 6.0**, inte 7.0, eftersom typescript-eslint ännu inte stöder TypeScript 7.
- **`Microsoft.AspNetCore.Mvc.Testing` 10.0.x**, i takt med .NET 10 LTS.

## Autentisering

**Beslutat (2026-11-26, [Q2](Questions.md#q2-användarnas-ålder-och-inloggning)):** Egna konton med ASP.NET Core Identity. Användarna är vuxna, och konton skapas bara genom inbjudan till en klass.

- **Identity sköter det säkerhetskritiska:** hashning av lösenord, säkerhetsstämplar, utelåsning efter misslyckade försök och tokens. Vi skriver bara lagringen (`IUserStore` med flera) med Dapper mot tabellen `users`. Ingen egen kryptografi.
- **Ingen öppen registrering.** Identitys färdiga endpoints (`MapIdentityApi`) används inte, eftersom de innehåller öppen registrering.
- **SSO** (t.ex. Skolfederation) ingår inte i MVP:n. Det kan läggas till senare som extern inloggning i Identity.
- **Implementation (steg 3a):**
  - `Auth/UserStore.cs` implementerar lagringen för Identity med Dapper.
  - Identity lägger användarens id i claimet `sub`, som `Auth/CurrentUser.cs` läser.
  - Säkerhetsstämpeln kontrolleras vid varje anrop, så att utloggning och nytt lösenord gäller alla sessioner direkt. Det kostar en databasfråga per anrop.
  - Endpoints som kräver inloggning använder `RequireAuthorization`, så 401 kommer före inläsning av bodyn.
- **Anropstakt:** inloggning, aktivering och återställning är begränsade till 20 anrop per minut och IP-adress (`Auth:RateLimitPerMinute`).

**API-dokumentation (beslutat 2026-11-26):** Under utveckling publicerar API:t sitt OpenAPI-dokument på `/openapi/v1.json` och ett testgränssnitt (Scalar) på `/scalar`. Båda finns bara i miljön `Development`, och ett test verifierar att de inte exponeras i drift. Scalar är fäst vid en version som har funnits i minst två veckor (2.21.0).
- **Session (beslutat, [Q16](Questions.md#q16-session-cookie-eller-token)):** cookie med `HttpOnly`, `Secure` och `SameSite=Strict`. Webbappen och API:t har samma ursprung. Inloggning med token läggs till när Capacitor-appen kommer.
- **Länkar för inbjudan och återställning (beslutat, [Q14](Questions.md#q14-hur-når-inbjudan-och-återställning-av-lösenord-användaren)):** engångslänkar som visas för den som bjuder in, och den personen delar dem själv. Ingen e-post i MVP:n.
- **Första administratören (beslutat, [Q15](Questions.md#q15-hur-skapas-den-första-administratören)):** ett kommando i API:t skriver ut en engångslänk för inbjudan av en administratör (`create-admin-invitation`). Länkens adress byggs från `App:PublicBaseUrl`, som är `http://localhost:5173` lokalt.

## Databas: MySQL – säkerhetsåtgärder

MySQL saknar radnivåskydd (RLS) och har några standardbeteenden som påverkar säkerhet och dataintegritet. Följande gäller:

- **Teckenkodning:** `utf8mb4` överallt.
- **Collation (beslutat 2026-11-26, [Q12](Questions.md#q12-collation-för-svensk-text)):** databasens standard är `utf8mb4_sv_0900_ai_ci`, så att å, ä och ö är egna bokstäver och sorteras efter z. Id-kolumner och andra tekniska värden (t.ex. roller) använder `ascii_bin`. Inloggningsuppgifter jämförs på normaliserade fält (t.ex. `NormalizedEmail`) så att skillnader i skiftläge och accenter inte ger dubbletter eller kollisioner.
- **Id:n:** UUID v7 som `CHAR(36)`, så att antal och ordning inte kan utläsas ur id:n i URL:er.
- **Strikt läge:** `STRICT_TRANS_TABLES` påslaget, så att för långa värden ger fel i stället för att avkortas.
- **Tid:** `DATETIME(6)` och alltid UTC.
- **Klassavgränsning:** upprätthålls i koden och verifieras med automatiska tester (se Dataåtkomst ovan).
- **Migreringar:** små migreringar och backup före driftsättning, eftersom MySQL inte kan rulla tillbaka schemaändringar i en transaktion.
- **Krypterad anslutning:** API:t ansluter med TLS (`SslMode=Required` i MySqlConnector). MySQL 9 saknar det äldre lösenordsinsticksprogrammet `mysql_native_password` och använder `caching_sha2_password`, som kräver TLS eller utbyte av publik nyckel.

**Version (beslutat 2026-11-26):** MySQL 9.7 LTS, släppt 2026-04-21 som efterföljare till 8.4 LTS.

### Lokal databas

**Beslutat (2026-11-26):** Under utveckling körs MySQL lokalt i Docker via `compose.yaml` i repots rot. Användning beskrivs i [README](../README.md).

- Samma inställningar som ovan sätts redan lokalt: `utf8mb4`, uttrycklig collation, strikt läge och UTC.
- Porten binds bara till `127.0.0.1`.
- Lösenord läses från `.env`, som inte versionshanteras. Mallen `.env.example` innehåller bara platshållare.
- Datat ligger i en Docker-volym och kan återställas med `docker compose down -v`.
- Den lokala databasen är för utveckling. xUnit-integrationstesterna startar egna databaser med Testcontainers.

## Att verifiera

- **Paketstorlek med Ionic:** första bygget i steg 3b är 607 kB (136 kB gzip) i en enda fil. Det fungerar, men koddelning per vy bör övervägas när fler vyer tillkommer.
- **Säkra cookies i Safari lokalt:** Chrome och Firefox godtar `Secure`-cookies på `http://localhost`, men Safari har historiskt inte gjort det. Om inloggningen inte fungerar i Safari under utveckling behövs HTTPS lokalt.

**Erfarenheter av Ionic i steg 3b:**

- Integrationen med lit-html fungerar. Komponenterna registreras en och en i `web/src/ionic.ts`.
- Datorlayouten löses med en centrerad innehållsbredd (`.container`).
- Ionics inbyggda knapp för att visa lösenord har fast engelsk etikett. Vi använder en egen knapp med svensk etikett (WCAG 3.1.2).
- `ion-content` har redan rollen `main`, så vyerna lägger inte till ett eget `<main>`.
- I Playwright tar Ionics synliga kontrollyta emot klick i stället för `ion-select`s dolda knapp. Testerna använder tangentbordet, vilket samtidigt prövar tangentbordsåtkomst.
- **Notisåtgärder i Capacitor:** att snabbsvar och reaktioner direkt från notisen och låsskärmen går att göra med befintliga plugins på både iOS och Android, eller vad som kräver egen native-kod. Behövs först för lager 8 (notiser).
- **Klientens IP-adress i drift:** bakom en omvänd proxy måste `ForwardedHeaders` konfigureras. Annars delar alla klienter samma gräns för anropstakt. Avgörs när driften designas.
- **SAML-stöd** om inloggning via skolans federation blir aktuellt efter MVP:n ([Q2](Questions.md#q2-användarnas-ålder-och-inloggning)).
