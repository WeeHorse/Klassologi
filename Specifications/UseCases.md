# Use Cases

Use cases uttryckta, detaljerade och prioriterade utifrån [Functionality](Functionality.md) och [Application description](Application-description.md).

## Bruttolista

**Status:** Lager 1–3 är beslutade som MVP ([Q8](Questions.md#q8-prioritering-och-omfattning-av-första-versionen)). Lager 4 och senare är ordnade efter beroende och värde men inte inbördes beslutade.

### Prioriteringsprincip

Listan är ordnad i lager efter beroenden, enligt principen för en minsta användbara kärnapplikation (MVP): varje lager förutsätter de föregående och ska ge ett fungerande helt system när det är klart.

1. **Plattform** – det måste finnas en klass att vara medlem i.
2. **Användare** – någon måste kunna logga in och höra till klassen.
3. **Poster** – det måste finnas något att dela: information på anslagstavlan.

Lager 1–3 utgör den föreslagna MVP:n: en klass där användare loggar in och där information publiceras och läses på ett ställe. Det är den minsta helhet som uppfyller applikationens kärnsyfte, att samla klassens information och förkorta kommunikationsvägarna. Senare lager bygger vidare på samma grund.

Roller gäller per klass enligt [Q5](Questions.md#q5-roller-och-behörigheter). Elevrepresentantens extra behörigheter är inte avgjorda ([Q13](Questions.md#q13-elevrepresentantens-behörigheter)), så aktörer som rör den rollen är preliminära.

Kolumnen *Frågor* anger öppna frågor som måste besvaras innan use caset kan detaljeras.

### Lager 1 – Plattform (MVP)

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-01 | [Skapa klass](#uc-01-skapa-klass) | Administratör | Upprätta en klass som användare kan höra till | – | – |
| UC-02 | Bjuda in användare till klass | Lärare i klassen, administratör | Ge elever och lärare tillgång till klassen | UC-01 | Q14 |
| UC-03 | [Tilldela och ändra roll](#uc-03-tilldela-och-ändra-roll) | Lärare i klassen, administratör | Ge användare rätt behörigheter (elev, lärare, elevrepresentant) | UC-01 | – |

### Lager 2 – Användare (MVP)

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-04 | Aktivera konto via inbjudan | Elev, lärare | Få ett konto kopplat till klassen | UC-02 | – |
| UC-05 | Logga in och logga ut | Alla användare | Få säker åtkomst till applikationen | UC-04 | – |
| UC-06 | Hantera egen profil | Alla användare | Presentera sig för klassen | UC-05 | – |
| UC-07 | Visa klasslista och profiler | Alla användare | Se vilka som ingår i klassen | UC-06 | – |

### Lager 3 – Poster / anslagstavla (MVP)

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-08 | Publicera inlägg på anslagstavlan | Lärare (ev. elevrepresentant) | Nå hela klassen med information | UC-05 | Q10, Q13 |
| UC-09 | Läsa anslagstavlan | Alla användare | Hitta aktuell information på ett ställe | UC-08 | – |
| UC-10 | Redigera och ta bort eget inlägg | Inläggets författare | Hålla informationen korrekt | UC-08 | – |
| UC-11 | Kommentera och reagera på inlägg | Alla användare | Ställa följdfrågor och visa att man tagit del | UC-09 | Q10 |
| UC-12 | Moderera inlägg och kommentarer | Lärare/administratör | Upprätthålla god ton och gemensamma regler | UC-11 | Q13 |

---

*Gräns för MVP:n. Lagren nedan är ordnade efter beroende och värde men inte inbördes beslutade.*

---

### Lager 4 – Kommunikation

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-13 | Skriva i gemensam textkanal | Alla användare | Diskutera med klassen | Lager 2 | – |
| UC-14 | Föra privat chatt | Alla användare | Kommunicera en-till-en | Lager 2 | – |
| UC-15 | Skapa och dela egen kanal | Alla användare | Samla en diskussion kring ett ämne | UC-13 | Q5 |
| UC-16 | Skicka meddelande till lärare | Elev | Nå läraren direkt | UC-14 | – |
| UC-17 | Kommunicera med elevrepresentanter | Elev | Lämna frågor och synpunkter som kan föras vidare | UC-13 | Q5 |
| UC-18 | Ställa anonym fråga | Elev | Fråga utan att profilen visas | UC-13 | Q1 |

### Lager 5 – Grupper och samarbete

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-19 | Skapa studie- eller projektgrupp | Elev | Samarbeta i en mindre grupp | Lager 2 | – |
| UC-20 | Bjuda in elever och lärare till grupp | Gruppmedlem | Sätta ihop gruppen | UC-19 | – |
| UC-21 | Kommunicera i grupp | Gruppmedlem | Diskutera inom gruppen | UC-19, UC-13 | – |

### Lager 6 – Material och fildelning

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-22 | Dela dokument, kursmaterial och länkar | Lärare, elev | Samla material på ett ställe | Lager 2 | Q5 |
| UC-23 | Kategorisera material efter ämne | Lärare | Göra materialet lätt att hitta | UC-22 | – |
| UC-24 | Söka och hämta material | Alla användare | Hitta rätt material snabbt | UC-22 | – |

### Lager 7 – Kalender och planering

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-25 | Visa och hantera klasskalender | Lärare (skriva), alla (läsa) | Ha gemensam överblick över händelser | Lager 2 | Q5 |
| UC-26 | Visa och hantera gruppkalender | Gruppmedlem | Planera gruppens arbete | UC-19, UC-25 | – |
| UC-27 | Planera gemensam aktivitet | Alla användare | Samordna en aktivitet | UC-25 | – |

### Lager 8 – Notiser

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-28 | Få notis om nytt innehåll och ändringar | Alla användare | Inte missa viktig information | Lager 3–7 | Q7 |
| UC-29 | Anpassa notiser | Alla användare | Styra vad man blir störd av | UC-28 | – |
| UC-30 | Utföra snabbåtgärd från notis | Alla användare | Svara eller reagera utan att öppna appen | UC-28 | Q7 |

### Senare (ej ordnade)

| ID | Use case | Primär aktör | Frågor |
|----|----------|--------------|--------|
| UC-31 | Visa status och tillgänglighet (inkl. lärares) | Alla användare | – |
| UC-32 | Röstkanaler, röst- och videosamtal | Alla användare | Q7 |
| UC-33 | Skärmdelning | Alla användare | Q7 |
| UC-34 | Lämna in arbete i grupp | Gruppmedlem | – |
| UC-35 | Dokumentera gruppens arbete | Gruppmedlem | – |
| UC-36 | Synkronisera grupp med GitHub | Gruppmedlem | – |
| UC-37 | Boka rum för individuellt arbete eller grupparbete | Elev | – |
| UC-38 | Checka in närvaro | Elev | Q4 |
| UC-39 | Använda personlig AI-assistent | Alla användare | – |
| UC-40 | Använda gemensam AI-assistent i grupp | Gruppmedlem | – |
| UC-41 | Samverka med företag och lärosäten | Lärare, elev, extern part | Q6 |
| UC-42 | Kategorisera elever | Lärare | Q9 |
| UC-43 | Lägga till tillägg och anpassa funktionalitet | Alla användare | – |

## Byggordning för MVP

**Beslutat (2026-11-26).** Lagren beskriver beroenden mellan funktioner. Byggordningen tar dessutom hänsyn till risk: det som bär säkerheten byggs och testas först. Varje steg är en egen feature-branch.

| Steg | Branch | Innehåll | Status |
|------|--------|----------|--------|
| 1 | `feature/walking-skeleton` | Hela kedjan utan affärsfunktioner: API, migreringar, webbapp och alla testverktyg | Klart |
| 2 | `feature/class-membership` | UC-01, UC-03, läsning av klasser och klassavgränsning. Användare finns som tabell men skapas direkt i databasen. | Klart |
| 3a | `feature/authentication` | Backend för UC-02 (enkel variant), UC-04, UC-05, återställning av lösenord, medlemslista och kommandot för administratörer ([Q2](Questions.md#q2-användarnas-ålder-och-inloggning)) | Klart (PR) |
| 3b | `feature/authentication-ui` | Gränssnitt för steg 3: Logga in, Aktivera konto, Välj nytt lösenord, Mina klasser, Klass | Pågår |
| 4 | `feature/notice-board` | UC-08–10. Beror på Q10 och Q13. | – |
| 5 | – | UC-02 (resten), UC-06, UC-07, UC-11, UC-12 | – |

Klasser och medlemskap byggs före inloggningen trots att lager 1 och 2 i listan har motsatt ordning. Klassavgränsningen är systemets viktigaste säkerhetsegenskap och kan testas utan riktig inloggning, eftersom testerna använder ett eget autentiseringsschema.

## Detaljerade use cases

Gemensamt för alla endpoints nedan:

- **Ej inloggad:** svar 401, även när bodyn är ogiltig. Inloggningen kontrolleras innan bodyn läses in (åtgärdat i steg 3a).
- **Klassen finns inte:** svar 404.
- **Inloggad men inte medlem i klassen och inte administratör:** svar 403 ([Q11](Questions.md#q11-svar-när-en-icke-medlem-försöker-nå-en-klass)).
- **Behörighet** avgörs från användarens medlemskap och administratörsstatus i databasen, aldrig från uppgifter som klienten skickar.
- **Roller** gäller per klass: `Student` (elev), `Teacher` (lärare) och `StudentRepresentative` (elevrepresentant). Administratör är en egenskap hos användaren, inte en roll i klassen ([Q5](Questions.md#q5-roller-och-behörigheter)).

### UC-01 Skapa klass

**Status:** Beslutat (2026-11-26), ändrad 2026-11-26 för inbjudningar (se [Konsekvenser för tidigare beslut](#konsekvenser-för-tidigare-beslut)).

| | |
|---|---|
| **Aktör** | Administratör |
| **Mål** | Upprätta en klass och bjuda in dess första lärare |
| **Förutsättningar** | Aktören är inloggad och administratör |

**Huvudflöde:**

1. Administratören anger klassens namn.
2. Systemet kontrollerar namnet.
3. Systemet skapar klassen utan medlemmar och skapar en inbjudan med rollen lärare (som i UC-02).
4. Systemet svarar med klassens id och inbjudningslänken (201).

**Alternativa flöden:**

- **A1. Aktören är inte administratör:** 403. Ingen klass skapas.
- **A2. Ogiltigt namn:** tomt, bara blanksteg eller längre än 100 tecken efter att blanksteg i början och slutet tagits bort. Svar 400 med beskrivning av felet.

**Efterhand:** Klassen finns utan medlemmar, och en oanvänd lärarinbjudan finns. Det framgår vem som skapade klassen och när.

**Regler:**

- Klassnamn behöver inte vara unika, eftersom till exempel "9A" kan återkomma år efter år.
- Klassnamn sparas med svensk collation ([Q12](Questions.md#q12-collation-för-svensk-text)).
- En klass kan stå utan lärare tills den första inbjudan har använts. Regeln om minst en lärare (UC-03) gäller när rollen för en befintlig lärare ändras.

### UC-03 Tilldela och ändra roll

**Status:** Beslutat (2026-11-26)

| | |
|---|---|
| **Aktör** | Lärare i klassen, administratör |
| **Mål** | Ge en användare rätt roll i klassen |
| **Förutsättningar** | Klassen och användaren finns. |

Tills inbjudningar finns (UC-02) lägger samma åtgärd också till användaren i klassen om användaren inte redan är medlem.

**Huvudflöde:**

1. Aktören anger användare och roll.
2. Systemet kontrollerar att aktören är lärare i klassen eller administratör.
3. Systemet sätter rollen och lägger till användaren som medlem om det behövs (204).

**Alternativa flöden:**

- **A1. Aktören är medlem men inte lärare**, t.ex. elev eller elevrepresentant: 403.
- **A2. Aktören är lärare i en annan klass men inte i den här:** 403.
- **A3. Ogiltig roll:** 400. Rollnamn tolkas utan hänsyn till skiftläge (`student` blir `Student`), men andra värden, även siffror, är ogiltiga.
- **A4. Användaren finns inte:** 404 med beskrivning av felet.
- **A5. Ändringen skulle lämna klassen utan lärare:** 409. Rollen ändras inte.

**Efterhand:** Användaren är medlem i klassen med den angivna rollen.

**Regler:**

- **En klass har alltid minst en lärare.** Kontrollen och ändringen görs i samma transaktion, så att två samtidiga ändringar inte kan lämna klassen utan lärare.
- Att ta bort medlemmar ingår inte här.

### Läsa klasser

Stöd för UC-03 och senare use cases, och den grund som klassavgränsningen testas mot.

- **Mina klasser:** en inloggad användare ser de klasser användaren är medlem i, med sin roll, sorterade på namn i svensk ordning.
- **En klass:** medlemmar och administratörer ser klassens id och namn och sin egen roll. Administratören har ingen roll om den inte också är medlem.

### Steg 3: inbjudan, konton och inloggning

**Status:** Beslutat (2026-11-26). Bygger på besluten i [Q2](Questions.md#q2-användarnas-ålder-och-inloggning), [Q14](Questions.md#q14-hur-når-inbjudan-och-återställning-av-lösenord-användaren), [Q15](Questions.md#q15-hur-skapas-den-första-administratören) och [Q16](Questions.md#q16-session-cookie-eller-token).

#### Gemensamma regler

- **Engångslänkar** (inbjudan och återställning av lösenord):
  - Innehåller en slumpmässig token på minst 256 bitar. Databasen lagrar bara en hash av token, så en läckt databas ger inga användbara länkar.
  - Token ligger i länkens fragment (`#…`), som webbläsaren inte skickar till servern. Den hamnar därför inte i serverloggar eller i `Referer`-headers.
  - Kan bara användas en gång.
- **Inloggningsuppgifter:** e-postadress och lösenord. E-postadressen jämförs normaliserad (skiftläge spelar ingen roll) och är unik. Den används bara för inloggning, eftersom ingen e-post skickas i MVP:n.
- **Lösenord:**
  - 12–128 tecken.
  - Inga krav på teckentyper, i linje med NIST SP 800-63B.
  - Hashas av Identity.
- **Spärr efter misslyckade inloggningar:** efter 5 misslyckade försök i följd spärras kontot i 15 minuter.
- **Begränsning av anropstakt:** inloggning, aktivering och återställning begränsas till 20 anrop per minut och IP-adress, med ASP.NET Cores inbyggda rate limiter. Överskridande ger 429.
- **Session:** cookie med `HttpOnly`, `Secure` och `SameSite=Strict`. Den gäller i 14 dagar och förlängs vid användning. Vid utloggning och nytt lösenord blir alla sessioner för kontot ogiltiga (Identitys säkerhetsstämpel).
- **401 före allt annat:** endpoints som kräver inloggning svarar 401 innan bodyn läses in, och API:t omdirigerar aldrig till en inloggningssida. Det åtgärdar den kända avvikelsen från steg 2.
- **Gränssnitt:** formulären följer [användbarhetsheuristikerna](Functionality.md#användbarhet). De visar vad som händer medan något pågår, beskriver fel på klarspråk med förslag på åtgärd, behåller inmatade värden (utom lösenord) när något går fel och har en knapp för att visa lösenordet i stället för ett extra fält för att upprepa det.

#### UC-02 Bjuda in användare till klass (enkel variant)

| | |
|---|---|
| **Aktör** | Lärare i klassen, administratör |
| **Mål** | Ge en ny eller befintlig användare tillgång till klassen med en viss roll |
| **Förutsättningar** | Aktören är inloggad och lärare i klassen eller administratör |

**Huvudflöde:**

1. Aktören väljer roll (elev, elevrepresentant eller lärare) i klassens vy.
2. Systemet skapar en inbjudan och visar en länk som är giltig i 7 dagar.
3. Aktören kopierar länken med en knapp och delar den själv.

**Alternativa flöden:** Samma 401/403/404 som för klasser. Medlemmar som inte är lärare får 403.

**Efterhand:** En oanvänd inbjudan finns, kopplad till klass och roll, med uppgift om vem som skapade den och när.

**Utanför steg 3:** lista och återkalla inbjudningar.

#### Första administratören (Q15)

Kommandot `dotnet run --project src/Klassologi.Api -- create-admin-invitation` skapar en inbjudan utan klass som ger administratörsbehörighet, och skriver ut länken. Den gäller i 24 timmar.

- **Fler administratörer** skapas bara med samma kommando. Det finns ingen möjlighet i webben att göra någon till administratör.
- **En befintlig användare** som är inloggad och öppnar en administratörsinbjudan får administratörsbehörighet på sitt befintliga konto.

#### UC-04 Aktivera konto via inbjudan

| | |
|---|---|
| **Aktör** | Den som har fått en inbjudningslänk |
| **Mål** | Få ett konto, eller använda ett befintligt, och bli medlem i klassen |
| **Förutsättningar** | Länken är giltig: den finns, har inte använts och har inte gått ut |

**Huvudflöde (nytt konto):**

1. Användaren öppnar länken. Systemet visar vilken klass och roll inbjudan gäller.
2. Användaren anger visningsnamn (1–100 tecken), e-postadress och lösenord.
3. Systemet skapar kontot, lägger till användaren i klassen med rollen, förbrukar inbjudan och loggar in användaren.
4. Användaren kommer till klassens vy.

**Alternativa flöden:**

- **A1. Länken är ogiltig, förbrukad eller har gått ut:** samma meddelande i alla tre fallen: "Länken gäller inte längre. Be den som bjöd in dig om en ny." Inget konto skapas.
- **A2. Användaren är redan inloggad:** systemet visar "Gå med i klassen" i stället för formuläret. Användaren blir medlem med rollen och inbjudan förbrukas. Om användaren redan är medlem ändras inte rollen, men inbjudan förbrukas ändå.
- **A3. E-postadressen har redan ett konto:** "Det finns redan ett konto med den e-postadressen. Logga in för att gå med i klassen." Efter inloggning fortsätter flödet som A2.
- **A4. Ogiltiga uppgifter**, t.ex. för kort lösenord: fältvisa fel. Inget konto skapas och inbjudan förbrukas inte.
- **A5. Administratörsinbjudan:** som huvudflödet, men kontot får administratörsbehörighet i stället för ett medlemskap. Användaren kommer till startsidan. Är användaren redan inloggad får det befintliga kontot administratörsbehörighet (som A2).

**Efterhand:** Kontot finns, användaren är medlem i klassen (eller administratör), inbjudan är förbrukad och användaren är inloggad.

**Regel:** Två samtidiga försök att använda samma inbjudan får inte båda lyckas.

#### UC-05 Logga in och logga ut

| | |
|---|---|
| **Aktör** | Användare med konto |
| **Mål** | Få säker åtkomst till sina klasser |

**Huvudflöde:**

1. Användaren anger e-postadress och lösenord.
2. Systemet loggar in användaren.
3. Användaren kommer till startsidan, som listar användarens klasser ("Mina klasser").

**Alternativa flöden:**

- **A1. Fel e-post eller lösenord:** "Fel e-postadress eller lösenord." Meddelandet avslöjar inte vilket av dem som var fel.
- **A2. Kontot är spärrat:** "För många misslyckade försök. Försök igen om 15 minuter."
- **A3. Logga ut:** sessionen avslutas och användaren kommer till inloggningssidan.

#### Återställa lösenord

| | |
|---|---|
| **Aktör** | Lärare i klassen (för klassens medlemmar som inte är lärare), administratör (för alla) |
| **Mål** | Hjälpa en användare som har glömt sitt lösenord |

**Huvudflöde:**

1. Aktören väljer "Återställ lösenord" för en medlem.
2. Systemet visar en engångslänk som gäller i 24 timmar.
3. Användaren öppnar länken och anger ett nytt lösenord.
4. Alla användarens sessioner blir ogiltiga, och användaren loggas in med det nya lösenordet.

**Säkerhet:** Den som skapar länken skulle kunna använda den själv och ta över kontot. Därför sparas vem som skapade länken och när. Lärare kan inte återställa lösenord för andra lärare eller administratörer.

#### Gränssnitt

| Vy | Innehåll |
|----|----------|
| Logga in | E-post, lösenord (med "visa"), knapp. Felmeddelanden enligt UC-05. |
| Aktivera konto | Klass och roll, formulär enligt UC-04, eller "Gå med i klassen" om användaren är inloggad. |
| Startsida | Mina klasser, Logga ut. Administratörer har dessutom "Skapa klass". |
| Klass | Klassens namn och min roll. För lärare och administratörer: "Bjud in" med rollval och en länk att kopiera, samt medlemslistan med rollval och "Återställ lösenord". |
| Välj nytt lösenord | Nytt lösenord (med "visa"), knapp. Öppnas via länken för återställning. |

**Adresser:** `/logga-in`, `/aktivera#<token>`, `/aterstall#<token>`, `/` (Mina klasser), `/klasser/<id>` och `/status`.

**Utanför steg 3:** e-post, att själv byta lösenord (UC-06), tvåfaktorsautentisering, medlemslista och profiler (UC-07), att ta bort medlemmar.

#### API (steg 3a)

Token i engångslänkar skickas alltid i bodyn, aldrig i URL:en. Länkar returneras som relativa sökvägar till webbappen: `/aktivera#<token>` och `/aterstall#<token>`.

| Anrop | Inloggning | Beskrivning | Svar |
|-------|------------|-------------|------|
| `POST /api/auth/login` | Nej | `{email, password}` | 204 och sessionscookie. 401 vid fel uppgifter, med `code`: `invalid_credentials` eller `locked_out`. |
| `POST /api/auth/logout` | Ja | Avslutar alla sessioner för kontot | 204 |
| `GET /api/auth/me` | Ja | `{id, displayName, email, isAdministrator}` | 200 |
| `POST /api/classes` | Ja | `{name}`. UC-01. | 201 `{id, name, teacherInvitation: {url, expiresAt}}` |
| `POST /api/classes/{id}/invitations` | Ja | `{role}`. UC-02. | 201 `{url, expiresAt}` |
| `GET /api/classes/{id}/members` | Ja | Lärare och administratörer. Sorterad på namn. | 200 `[{userId, displayName, role}]` |
| `POST /api/classes/{id}/members/{userId}/password-reset` | Ja | Lärare (inte för lärare eller administratörer) och administratörer | 201 `{url, expiresAt}` |
| `POST /api/invitations/inspect` | Nej | `{token}` | 200 `{className, role, isAdministrator}`. 404 om länken inte gäller. |
| `POST /api/invitations/accept` | Valfri | `{token}` om inloggad, annars `{token, displayName, email, password}`. UC-04. | 201 (nytt konto, inloggad) eller 200 (befintligt konto). 404 om länken inte gäller, 409 med `code`: `email_taken`, 400 vid ogiltiga fält. |
| `POST /api/password-resets/complete` | Nej | `{token, newPassword}` | 204 och inloggad. 404 om länken inte gäller, 400 vid ogiltigt lösenord. |

#### Konsekvenser för tidigare beslut

**1. Ändring av UC-01 Skapa klass (beslutat 2026-11-26).** UC-01 krävde att den första läraren redan fanns som användare. När konton bara skapas genom inbjudan finns läraren oftast inte än. Därför:

- Administratören anger bara klassens namn.
- Klassen skapas utan medlemmar, och systemet visar direkt en inbjudningslänk med rollen lärare.
- Regeln om minst en lärare gäller fortfarande vid ändringar av roller. En klass kan alltså stå utan lärare tills den första inbjudan har använts, men en befintlig lärare kan aldrig tas bort så att klassen blir utan.

**2. Medlemslista för lärare och administratörer (beslutat 2026-11-26).** Återställning av lösenord och ändring av roller kräver att man kan välja en medlem, men medlemslistan (UC-07) ligger i steg 5. Därför: klassens vy visar en enkel lista med namn och roll för lärare och administratörer, med åtgärderna "Ändra roll" och "Återställ lösenord". UC-07 i steg 5 ger sedan alla medlemmar listan med profiler.
