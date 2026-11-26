# Klassologi – krav och funktioner

## 1. Användare, profiler och roller

- Registrering och inloggning.
- Individuell profil för varje elev.
- Klasslista.
- Kategorisering av elever.
- Visa användares status och tillgänglighet.
- Olika användarroller med olika behörigheter, exempelvis:
  - Elev.
  - Lärare.
  - Elevrepresentant.

## 2. Information och kursmaterial

- Samlad anslagstavla med information till klassen.
- Kategorier för ämnen och läromedel.
- Gemensamt bibliotek för:
  - Dokument.
  - Kursmaterial.
  - Länkar.
- Fildelning.

## 3. Kommunikation

- Gemensamma textkanaler.
- Gemensamma röstkanaler.
- Privat chatt.
- Videosamtal.
- Skärmdelning.
- Möjlighet att skapa egna kommunikationskanaler och dela dem med andra.
- Möjlighet att ställa frågor anonymt, utan att avsändarens profil visas.

### Kommunikation med lärare

- Skicka meddelanden till lärare.
- Röst- och videosamtal med lärare.
- Visa lärares tillgänglighet.

### Kommunikation med elevrepresentanter

- Separat chatt med elevrepresentanterna.
- Möjlighet att skicka frågor och synpunkter till representanterna, som sedan kan föra dem vidare till klassen.

### Extern kommunikation och samverkan

- Samarbetskanaler eller kategorier för kontakt med:
  - Företag och näringsliv.
  - Lärosäten.

## 4. Grupper och samarbete

- Skapa mindre grupper inom klassen, exempelvis:
  - Studiegrupper.
  - Projektgrupper.
- Bjuda in andra elever till grupper.
- Bjuda in lärare till grupper.
- Gemensamma resurser och funktioner inom varje grupp:
  - Kommunikationskanaler.
  - Gruppkalender.
  - Gemensam AI-assistent.
  - Inlämningar.
  - Dokumentation.
  - Synkronisering med GitHub.

## 5. Planering och kalender

- Gemensam kalender för klassen.
- Gruppspecifika kalendrar.
- Planeringsverktyg.
- Hantering av gemensamma aktiviteter.

## 6. Studieplatser och rumsbokning

- Pluggrum.
- Möjlighet att boka rum för individuellt arbete.
- Möjlighet att boka rum för grupparbete.

## 7. Närvaro

- Registrera sin egen närvaro genom incheckning.
- Möjlighet att kontrollera att eleven befinner sig på skolan vid incheckning.

## 8. AI-assistans

- Egen AI-assistent i applikationen.
- Gemensamma AI-assistenter inom studie- och projektgrupper.

## 9. Notiser

- Anpassningsbara notiser.
- Notiser vid kalenderuppdateringar.
- Notiser när nytt material publiceras.
- Notiser till mobilen.
- Snabbåtgärder direkt från notiser, exempelvis:
  - Svara på meddelanden.
  - Reagera på innehåll.
- Möjlighet att genomföra snabbåtgärder direkt från låsskärmen.

## 10. Individuell anpassning

- Möjlighet att lägga till tillägg (extensions).
- Möjlighet att anpassa applikationens funktionalitet efter individuella behov.

---

# Övergripande krav

## Tillgänglighet och plattformar

- Applikationen ska vara tillgänglig både som webbapplikation och mobilapp.
- Vanliga aktiviteter ska kunna genomföras med få interaktionssteg.
- Applikationen ska minska friktionen vid kommunikation och informationsinhämtning.

## Integritet och behörighet

- Användares behörigheter ska kunna skilja sig åt beroende på användarroll.
- Vid anonym frågeställning ska avsändarens profil inte visas.

## Social miljö

- Gemensamma regler för kommunikation.
- En god ton mellan elever och lärare.

## Användbarhet

Gränssnittet ska följa Jakob Nielsens tio användbarhetsheuristiker:

1. **Synlig systemstatus.** Användaren ska alltid se vad som händer, t.ex. att ett inlägg publiceras, att ett meddelande har skickats eller att anslutningen har tappats.
2. **Överensstämmelse mellan system och verklighet.** Språk och begrepp ska vara elevers och lärares, inte systemets, t.ex. "klass", "anslagstavla" och "grupp".
3. **Användarkontroll och frihet.** Det ska gå att ångra och avbryta, t.ex. redigera eller ta bort ett eget inlägg och lämna ett påbörjat flöde utan konsekvenser.
4. **Konsekvens och standarder.** Samma sak ska se ut och fungera likadant i hela applikationen och följa etablerade konventioner för webb och mobil.
5. **Förebygga fel.** Gränssnittet ska förhindra misstag, t.ex. bekräftelse innan något tas bort eller publiceras till hela klassen, och tydligt visa om en fråga ställs anonymt eller inte.
6. **Känna igen hellre än minnas.** Val och information ska vara synliga när de behövs, så att användaren inte behöver komma ihåg dem mellan vyer.
7. **Flexibilitet och effektivitet.** Vanliga aktiviteter ska gå snabbt för vana användare, utan att nya användare får det svårare.
8. **Estetisk och minimalistisk design.** Varje vy ska bara visa det som är relevant. Onödig information konkurrerar med den viktiga.
9. **Hjälpa användaren att känna igen, förstå och åtgärda fel.** Felmeddelanden ska vara på klarspråk, beskriva problemet och föreslå en lösning.
10. **Hjälp och dokumentation.** Applikationen ska helst gå att använda utan hjälp, men hjälp ska finnas där den behövs och vara kopplad till uppgiften.

## Tillgänglighet

Gränssnittet ska uppfylla **WCAG 2.2 nivå AA** ([Q17](Questions.md#q17-krav-på-tillgänglighet)), bland annat:

- tillräcklig kontrast mellan text och bakgrund
- att allt går att göra med tangentbordet, med synlig fokusmarkering
- att formulärfält har etiketter och att fel kopplas till rätt fält och läses upp av skärmläsare
- att texten går att förstora utan att innehåll försvinner.

**Verifiering:** automatisk kontroll med axe i Playwright för varje vy. Det som inte kan kontrolleras automatiskt, till exempel tangentbordsflöden och skärmläsare, prövas manuellt vid den heuristiska utvärderingen.

**Verifiering av heuristikerna:** Heuristikerna är kvalitativa krav och kan inte verifieras fullt ut med automatiska tester. De verifieras genom heuristisk utvärdering av gränssnittet, åtminstone innan varje lager i [use case-listan](UseCases.md#bruttolista) räknas som klart.