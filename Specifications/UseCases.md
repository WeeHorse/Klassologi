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
| UC-02 | Bjuda in användare till klass | Lärare i klassen, administratör | Ge elever och lärare tillgång till klassen | UC-01 | Q2 |
| UC-03 | [Tilldela och ändra roll](#uc-03-tilldela-och-ändra-roll) | Lärare i klassen, administratör | Ge användare rätt behörigheter (elev, lärare, elevrepresentant) | UC-01 | – |

### Lager 2 – Användare (MVP)

| ID | Use case | Primär aktör | Mål | Beror på | Frågor |
|----|----------|--------------|-----|----------|--------|
| UC-04 | Registrera konto / aktivera inbjudan | Elev, lärare | Få ett konto kopplat till klassen | UC-02 | Q2 |
| UC-05 | Logga in och logga ut | Alla användare | Få säker åtkomst till applikationen | UC-04 | Q2 |
| UC-06 | Hantera egen profil | Alla användare | Presentera sig för klassen | UC-05 | Q2 |
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
| 2 | `feature/class-membership` | UC-01, UC-03, läsning av klasser och klassavgränsning. Användare finns som tabell men skapas direkt i databasen. | Pågår |
| 3 | `feature/authentication` | UC-04, UC-05: riktig inloggning. Beror på Q2. | – |
| 4 | `feature/notice-board` | UC-08–10. Beror på Q10 och Q13. | – |
| 5 | – | UC-02, UC-06, UC-07, UC-11, UC-12 | – |

Klasser och medlemskap byggs före inloggningen trots att lager 1 och 2 i listan har motsatt ordning. Klassavgränsningen är systemets viktigaste säkerhetsegenskap och kan testas utan riktig inloggning, eftersom testerna använder ett eget autentiseringsschema.

## Detaljerade use cases

Gemensamt för alla endpoints nedan:

- **Ej inloggad:** svar 401.
  *Känd avvikelse (2026-11-26), åtgärdas i steg 3:* Ett anonymt anrop med ogiltig body (t.ex. trasig JSON eller okänd roll) får i dag 400 i stället för 401, eftersom bodyn läses in innan inloggningen kontrolleras.
- **Klassen finns inte:** svar 404.
- **Inloggad men inte medlem i klassen och inte administratör:** svar 403 ([Q11](Questions.md#q11-svar-när-en-icke-medlem-försöker-nå-en-klass)).
- **Behörighet** avgörs från användarens medlemskap och administratörsstatus i databasen, aldrig från uppgifter som klienten skickar.
- **Roller** gäller per klass: `Student` (elev), `Teacher` (lärare) och `StudentRepresentative` (elevrepresentant). Administratör är en egenskap hos användaren, inte en roll i klassen ([Q5](Questions.md#q5-roller-och-behörigheter)).

### UC-01 Skapa klass

**Status:** Beslutat (2026-11-26)

| | |
|---|---|
| **Aktör** | Administratör |
| **Mål** | Upprätta en klass med en första lärare |
| **Förutsättningar** | Aktören är inloggad och administratör. Den utsedda läraren finns som användare. |

**Huvudflöde:**

1. Administratören anger klassens namn och vem som ska vara första lärare.
2. Systemet kontrollerar uppgifterna.
3. Systemet skapar klassen och gör den utsedda användaren till medlem med rollen lärare.
4. Systemet svarar med klassens id (201).

**Alternativa flöden:**

- **A1. Aktören är inte administratör:** 403. Ingen klass skapas.
- **A2. Ogiltigt namn:** tomt, bara blanksteg eller längre än 100 tecken efter att blanksteg i början och slutet tagits bort. Svar 400 med beskrivning av felet.
- **A3. Den utsedda läraren finns inte:** 400 med beskrivning av felet.

**Efterhand:** Klassen finns och har exakt en medlem, läraren. Det framgår vem som skapade klassen och när.

**Regler:**

- Klassnamn behöver inte vara unika, eftersom till exempel "9A" kan återkomma år efter år.
- Klassnamn sparas med svensk collation ([Q12](Questions.md#q12-collation-för-svensk-text)).

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
