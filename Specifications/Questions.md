# Öppna frågor

Frågor som måste besvaras innan vi kan specificera eller designa de berörda delarna. När en fråga är besvarad dokumenteras beslutet i relevant specifikation och frågan markeras som besvarad här.

Status: **Öppen** | **Besvarad**

## Q1. Anonyma frågor – anonyma för vem?

**Status:** Öppen
**Berör:** Kommunikation, Integritet och behörighet, Social miljö

Ska anonyma frågor vara anonyma för alla, inklusive lärare och administratörer, eller enbart för andra elever?

**Varför det spelar roll:** Påverkar om avsändaren lagras alls, och därmed möjligheten att moderera och hantera kränkningar. Fullständig anonymitet kan stå i konflikt med kravet på god ton.

## Q2. Användarnas ålder och inloggning

**Status:** Besvarad (2026-11-26)
**Berör:** Användare, profiler och roller, Integritet

Kan användarna vara minderåriga? Ska inloggning ske via skolans befintliga identitetslösning (SSO) eller via egna konton?

**Varför det spelar roll:** Påverkar GDPR-hantering, samtycke, vilka personuppgifter vi får lagra och val av autentiseringslösning.

**Beslut:**

- **Ålder:** Användarna är vuxna (t.ex. yrkeshögskola, vuxenutbildning eller högskola). GDPR gäller ändå, så vi lagrar bara de personuppgifter som behövs.
- **Inloggning:** Egna konton med ASP.NET Core Identity och egen lagring via Dapper. Ingen SSO i MVP:n.
- **Konton:** Skapas bara genom inbjudan till en klass. Det finns ingen öppen registrering. En enkel variant av UC-02 flyttas därför till steg 3.

Se [Technology.md](Technology.md#autentisering) och [UseCases.md](UseCases.md#byggordning-för-mvp).

## Q3. Omfattning – en klass, flera klasser eller flera skolor?

**Status:** Besvarad (2026-11-26)
**Berör:** Datamodell, Användare och roller

Ska en installation hantera en klass, flera klasser eller flera skolor?

**Varför det spelar roll:** Avgör om datamodellen behöver stödja flera separata organisationer (multi-tenancy) och hur behörigheter avgränsas.

**Beslut:** Flera klasser i samma installation, utan skolnivå. En användare kan vara medlem i flera klasser. En skolnivå kan läggas till senare genom att klasser kopplas till en skola. Se [UseCases.md](UseCases.md#detaljerade-use-cases).

## Q4. Närvarokontroll – hur verifieras platsen?

**Status:** Öppen
**Berör:** Närvaro, Integritet

Hur ska vi kontrollera att eleven befinner sig på skolan vid incheckning (t.ex. GPS, skolans nätverk, QR-kod)?

**Varför det spelar roll:** Platsdata är integritetskänslig. Valet påverkar både teknisk lösning och vilka personuppgifter som behandlas.

## Q5. Roller och behörigheter

**Status:** Besvarad (2026-11-26)
**Berör:** Användare, profiler och roller

Vilka behörigheter har en elevrepresentant utöver en vanlig elev? Vem skapar klasser och tilldelar roller? Finns en administratörsroll?

**Varför det spelar roll:** Grunden för behörighetsmodellen.

**Beslut:** Roller gäller per klass: elev, lärare och elevrepresentant. Administratör är en egenskap hos användaren. Administratören skapar klasser och utser den första läraren, och lärare tilldelar roller i sin egen klass. En klass har alltid minst en lärare. Elevrepresentantens extra behörigheter är inte avgjorda, se [Q13](#q13-elevrepresentantens-behörigheter).

## Q6. Externa parter

**Status:** Öppen
**Berör:** Extern kommunikation och samverkan, Integritet och behörighet

Ska företag och lärosäten ha egna konton i applikationen? Vad ska de i så fall få se och göra?

**Varför det spelar roll:** Externa användare innebär en separat behörighetsgräns och påverkar integritetsskyddet för elever.

## Q7. Webb och mobil

**Status:** Besvarad (2026-11-26)
**Berör:** Tillgänglighet och plattformar, Teknik

Räcker en responsiv webbapplikation eller PWA initialt, eller krävs en mobilapp från start? Mobilnotiser och snabbåtgärder från låsskärmen kan kräva en mobilapp.

**Varför det spelar roll:** Påverkar teknikval och arkitektur tidigt.

**Beslut:** Webb först. Mobilappar för iOS och Android tas fram genom att webbappen läggs i ett Capacitor-skal när appen ska ut i appbutikerna. Se [Technology.md](Technology.md#frontend).

## Q8. Prioritering och omfattning av första versionen

**Status:** Besvarad (2026-11-26)
**Berör:** Samtliga funktionsområden

Vilka funktioner ska ingå i första versionen?

**Varför det spelar roll:** Krävs för att prioritera och detaljera use cases.

**Beslut:** Lager 1–3 i [bruttolistan](UseCases.md#bruttolista) utgör MVP:n: plattform, användare och poster (anslagstavla). Byggordningen följer risk och beroenden, se [Byggordning för MVP](UseCases.md#byggordning-för-mvp).

## Q9. Kategorisering av elever – vad avses?

**Status:** Öppen
**Berör:** Användare, profiler och roller

Funktionslistan nämner "kategorisering av elever". Vad ska kategoriseras (t.ex. inriktning, grupp, behov), vem kategoriserar och vem ser kategorierna?

**Varför det spelar roll:** Beroende på svaret kan det röra känsliga personuppgifter och påverka behörighetsmodellen.

## Q10. Vem får publicera och kommentera på anslagstavlan?

**Status:** Öppen
**Berör:** Information och kursmaterial, Användare och roller

Får bara lärare publicera på anslagstavlan, eller även elevrepresentanter och elever? Får alla kommentera och reagera?

**Varför det spelar roll:** Avgör anslagstavlans karaktär (envägsinformation eller diskussion) och behovet av moderering. Krävs för att detaljera UC-08 och UC-11 i MVP.

## Q11. Svar när en icke-medlem försöker nå en klass

**Status:** Besvarad (2026-11-26)
**Berör:** Integritet och behörighet, [Architecture](Architecture.md#klassbunden-förfrågan)

Ska API:t svara 403 (åtkomst nekad) eller 404 (finns inte) när en användare försöker nå data i en klass som användaren inte tillhör?

**Varför det spelar roll:** 404 avslöjar inte att klassen finns, men gör felsökning svårare. Krävs för att detaljera UC-09 och övriga klassbundna use cases.

**Beslut:** 403 när användaren inte är medlem i klassen (och inte är administratör), 404 bara när klassen inte finns. Inom klassen ger saknad behörighet 403. Se [UseCases.md](UseCases.md#detaljerade-use-cases).

## Q12. Collation för svensk text

**Status:** Besvarad (2026-11-26)
**Berör:** Datamodell, [Technology](Technology.md#databas-mysql--säkerhetsåtgärder)

Servern använder MySQL:s standard `utf8mb4_0900_ai_ci`, som behandlar å och ä som a och ö som o vid jämförelse och sortering. Ska kolumner med namn och annan svensk text i stället använda `utf8mb4_sv_0900_ai_ci`?

**Varför det spelar roll:** Påverkar sökning, sortering av klasslistor och unikhet. Med standardcollation räknas till exempel "Åsa" och "Asa" som samma värde. Krävs när de första tabellerna skapas.

**Beslut:** Svensk collation, `utf8mb4_sv_0900_ai_ci`, som databasens standard. Id-kolumner och andra tekniska värden använder `ascii_bin`. Se [Technology.md](Technology.md#databas-mysql--säkerhetsåtgärder).

## Q13. Elevrepresentantens behörigheter

**Status:** Öppen
**Berör:** Användare, profiler och roller, Kommunikation

Vad får en elevrepresentant göra utöver en elev? Till exempel publicera på anslagstavlan ([Q10](#q10-vem-får-publicera-och-kommentera-på-anslagstavlan)), moderera eller ta emot frågor från klassen (UC-17).

**Varför det spelar roll:** Rollen finns i datamodellen men har ännu inga egna rättigheter. Krävs för att detaljera UC-08 och UC-17.

## Q14. Hur når inbjudan och återställning av lösenord användaren?

**Status:** Besvarad (2026-11-26)
**Berör:** UC-02, UC-04, Drift

Konton skapas bara genom inbjudan ([Q2](#q2-användarnas-ålder-och-inloggning)). Ska inbjudningslänken skickas med e-post, eller visas för den som bjuder in, så att den personen delar den själv? Samma fråga gäller återställning av lösenord.

**Varför det spelar roll:** E-post kräver en tjänst för utskick och hantering av leveransfel. En länk som delas manuellt är enklare men kräver att läraren gör ett steg till.

**Beslut:** I MVP:n visas en engångslänk för den som bjuder in, och den personen delar den själv. Ingen e-posttjänst. Återställning av lösenord görs på samma sätt av en lärare i klassen eller en administratör. E-post kan läggas till senare.

## Q15. Hur skapas den första administratören?

**Status:** Besvarad (2026-11-26)
**Berör:** UC-01, Drift, Säkerhet

Konton skapas bara genom inbjudan, och klasser skapas bara av administratörer. Vem bjuder då in den första administratören?

**Varför det spelar roll:** Den första administratören är systemets högsta behörighet. Vägen dit får inte gå att använda av vem som helst.

**Beslut:** Ett kommando i API:t skriver ut en engångslänk för inbjudan av en administratör. Kommandot kräver åtkomst till servern och databasen, inte till webben.

## Q16. Session: cookie eller token?

**Status:** Besvarad (2026-11-26)
**Berör:** UC-05, Säkerhet, Mobilapp

Hur håller webbappen reda på att användaren är inloggad: med en cookie som webbläsaren skickar automatiskt, eller med en token som appen själv skickar i varje anrop?

**Varför det spelar roll:** En `HttpOnly`-cookie kan inte läsas av JavaScript och skyddar därför bättre mot stöld via XSS. Den fungerar enklast när webbappen och API:t har samma ursprung. Capacitor-appen har senare ett annat ursprung och kan behöva token.

**Beslut:** `HttpOnly`-, `Secure`- och `SameSite=Strict`-cookie från Identity för webbappen, som har samma ursprung som API:t. Inloggning med token läggs till som ett andra schema när Capacitor-appen kommer.

## Q17. Krav på tillgänglighet

**Status:** Besvarad (2026-11-26)
**Berör:** Användbarhet, Gränssnitt, Test

Ska gränssnittet uppfylla WCAG 2.2 på nivå AA (kontrast, tangentbordsnavigering, skärmläsare, textstorlek)?

**Varför det spelar roll:** Användbarhetsheuristikerna täcker inte tillgänglighet. Kravet påverkar styling, komponentval och tester, till exempel automatisk kontroll med axe i Playwright. Det kan dessutom vara ett krav från skolor som köper tjänsten. Behövs när vyerna i steg 3b byggs.

**Beslut:** Ja. Gränssnittet ska uppfylla WCAG 2.2 nivå AA. Det kontrolleras automatiskt med axe i Playwright för varje vy, och manuellt (tangentbord och skärmläsare) vid den heuristiska utvärderingen. Se [Functionality.md](Functionality.md#tillgänglighet).
