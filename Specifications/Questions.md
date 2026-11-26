# Öppna frågor

Frågor som måste besvaras innan vi kan specificera eller designa de berörda delarna. När en fråga är besvarad dokumenteras beslutet i relevant specifikation och frågan markeras som besvarad här.

Status: **Öppen** | **Besvarad**

## Q1. Anonyma frågor – anonyma för vem?

**Status:** Öppen
**Berör:** Kommunikation, Integritet och behörighet, Social miljö

Ska anonyma frågor vara anonyma för alla, inklusive lärare och administratörer, eller enbart för andra elever?

**Varför det spelar roll:** Påverkar om avsändaren lagras alls, och därmed möjligheten att moderera och hantera kränkningar. Fullständig anonymitet kan stå i konflikt med kravet på god ton.

## Q2. Användarnas ålder och inloggning

**Status:** Öppen
**Berör:** Användare, profiler och roller, Integritet

Kan användarna vara minderåriga? Ska inloggning ske via skolans befintliga identitetslösning (SSO) eller via egna konton?

**Varför det spelar roll:** Påverkar GDPR-hantering, samtycke, vilka personuppgifter vi får lagra och val av autentiseringslösning.

## Q3. Omfattning – en klass, flera klasser eller flera skolor?

**Status:** Öppen
**Berör:** Datamodell, Användare och roller

Ska en installation hantera en klass, flera klasser eller flera skolor?

**Varför det spelar roll:** Avgör om datamodellen behöver stödja flera separata organisationer (multi-tenancy) och hur behörigheter avgränsas.

## Q4. Närvarokontroll – hur verifieras platsen?

**Status:** Öppen
**Berör:** Närvaro, Integritet

Hur ska vi kontrollera att eleven befinner sig på skolan vid incheckning (t.ex. GPS, skolans nätverk, QR-kod)?

**Varför det spelar roll:** Platsdata är integritetskänslig. Valet påverkar både teknisk lösning och vilka personuppgifter som behandlas.

## Q5. Roller och behörigheter

**Status:** Öppen
**Berör:** Användare, profiler och roller

Vilka behörigheter har en elevrepresentant utöver en vanlig elev? Vem skapar klasser och tilldelar roller? Finns en administratörsroll?

**Varför det spelar roll:** Grunden för behörighetsmodellen.

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

**Status:** Öppen
**Berör:** Samtliga funktionsområden

Vilka funktioner ska ingå i första versionen?

**Förslag (ej beslutat):** Lager 1–3 i [bruttolistan](UseCases.md#bruttolista): plattform, användare och poster (anslagstavla). Kommunikation, grupper, material och kalender följer därefter.

**Varför det spelar roll:** Krävs för att prioritera och detaljera use cases.

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

**Status:** Öppen
**Berör:** Integritet och behörighet, [Architecture](Architecture.md#klassbunden-förfrågan)

Ska API:t svara 403 (åtkomst nekad) eller 404 (finns inte) när en användare försöker nå data i en klass som användaren inte tillhör?

**Varför det spelar roll:** 404 avslöjar inte att klassen finns, men gör felsökning svårare. Krävs för att detaljera UC-09 och övriga klassbundna use cases.

## Q12. Collation för svensk text

**Status:** Öppen
**Berör:** Datamodell, [Technology](Technology.md#databas-mysql--säkerhetsåtgärder)

Servern använder MySQL:s standard `utf8mb4_0900_ai_ci`, som behandlar å och ä som a och ö som o vid jämförelse och sortering. Ska kolumner med namn och annan svensk text i stället använda `utf8mb4_sv_0900_ai_ci`?

**Varför det spelar roll:** Påverkar sökning, sortering av klasslistor och unikhet. Med standardcollation räknas till exempel "Åsa" och "Asa" som samma värde. Krävs när de första tabellerna skapas.
