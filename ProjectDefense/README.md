# ProjectDefense – system rezerwacji terminów obron projektów

**Przedmiot:** Rozwój aplikacji internetowych (RAI) – laboratorium 2, Razor Pages
**Autor:** Filip Pudlak, nr albumu 198157
**Technologie:** C# 12, .NET 8, ASP.NET Core Razor Pages, ASP.NET Core Identity, Entity Framework Core (SQLite), Minimal API, Swagger, xUnit

## 1. Cel i zakres

Celem projektu jest aplikacja internetowa do rezerwacji terminów obron projektów laboratoryjnych. Prowadzący definiuje sale oraz okna dostępności, z których system generuje atomowe terminy (sloty), a studenci rezerwują wolne terminy z zachowaniem reguł biznesowych: jednej aktywnej rezerwacji na studenta, blokady terminów z przeszłości oraz braku kolizji sal. Funkcjonalność udostępniono przez interfejs Razor Pages, REST-owe Minimal API z dokumentacją OpenAPI oraz klienta konsolowego.

## 2. Architektura rozwiązania

Solucja `ProjectDefense.sln` składa się z czterech projektów:

| Projekt | Typ | Odpowiedzialność |
|---|---|---|
| `ProjectDefense.Core` | biblioteka klas | encje domenowe, DTO, typy wspólne (`Roles`, `OperationResult`, `SlotStatus`) |
| `ProjectDefense.Web` | ASP.NET Core Razor Pages | `DbContext`, serwisy domenowe, strony, Identity, API, lokalizacja, eksport |
| `ProjectDefense.ConsoleClient` | aplikacja konsolowa | klient HTTP korzystający z Minimal API |
| `ProjectDefense.Tests` | xUnit | testy jednostkowe i integracyjne |

Zależności: `Web → Core`, `ConsoleClient → Core`, `Tests → Core`, `Tests → Web`. Projekt `Core` nie zależy od EF Core – korzysta jedynie z `FrameworkReference` do `Microsoft.AspNetCore.App`, potrzebnego do dziedziczenia po `IdentityUser`. Dzięki temu klient konsolowy współdzieli z serwerem typy DTO bez zależności od warstwy dostępu do danych.

Logikę biznesową wydzielono do serwisów w `Web/Services` (`AvailabilityService`, `ReservationService`, `BanService`, `ExportService`, `SlotGenerator`, `SlotLabelFormatter`). Strony Razor i endpointy API są cienkimi adapterami wywołującymi te same serwisy, więc reguły domenowe egzekwowane są w jednym miejscu, niezależnie od kanału (UI, API, spreparowany POST). Serwisy zwracają `OperationResult` z kategorią błędu (`Validation`, `NotFound`, `Conflict`, `Forbidden`), którą warstwa API mapuje na kody HTTP 400/404/409/403. Czas bieżący pobierany jest z abstrakcji `TimeProvider` (.NET 8), co umożliwia deterministyczne testowanie reguł zależnych od czasu.

## 3. Model domenowy

| Encja | Najważniejsze pola |
|---|---|
| `ApplicationUser : IdentityUser` | `FullName`, `StudentIndexNumber?`, `IsBanned` |
| `Room` | `Id`, `Name`, `RoomNumber` (unikalny), `WorkstationsCount` |
| `LecturerAvailability` | `LecturerId`, `RoomId`, `StartDate`, `EndDate` (`DateOnly`), `DailyStartTime`, `DailyEndTime` (`TimeOnly`), `SlotDurationMinutes` |
| `ReservationSlot` | `LecturerAvailabilityId`, `StartTime`, `EndTime`, `IsBlockedByLecturer`, `StudentId?` (`null` – wolny) |
| `StudentBan` | `StudentId`, `BannedByLecturerId`, `Reason`, `BannedAt`, `BannedUntil?` (`null` – blokada stała), `LiftedAt?` |

Role: `Lecturer` (etykieta „Prowadzący”) i `Student`. Pola `StudentId` i `IsBlockedByLecturer` oznaczono atrybutem `[ConcurrencyCheck]`. EF Core dołącza wtedy ich pierwotne wartości do klauzuli `WHERE` polecenia `UPDATE`, więc dwie równoczesne próby rezerwacji tego samego slotu nie mogą obie zakończyć się sukcesem – druga otrzymuje `DbUpdateConcurrencyException`, zamieniany na błąd `Conflict`.

## 4. Realizacja wymagań

### 4.1. Moduł prowadzącego (cz. 1)

- **CRUD sal** – `Pages/Lecturer/Rooms/{Index,Create,Edit,Delete,Details}`, z kontrolą unikalności numeru sali i zakazem usunięcia sali, dla której istnieją okna dostępności.
- **Generowanie okien** – `Pages/Lecturer/Availability/Create`: lista sal (`SelectList`), zakres dat, dzienny przedział godzin i długość slotu. `SlotGenerator` dla każdego dnia zakresu tworzy kolejne sloty `[t, t+Δ)` tak długo, jak `t + Δ ≤ DailyEndTime`; niepełny slot na końcu dnia nie jest tworzony.
- **Tabela rezerwacji** – `Pages/Lecturer/Reservations/Index` z filtrowaniem po sali i statusie (`Wolny`, `Zarezerwowany`, `Zablokowany`, `Przeszły`); kolumny: kto, sala, termin.
- **Anulowanie i przepisanie** – anulowanie zwalnia slot; `Reassign` przenosi studenta na inny wolny termin w jednym wywołaniu `SaveChanges`.
- **Blokada przedziału** – `Pages/Lecturer/Block`: wszystkie przyszłe sloty sali przecinające się z przedziałem otrzymują `IsBlockedByLecturer = true`, a przypisani studenci są zwalniani. Odblokowanie pojedynczego slotu z tabeli rezerwacji.
- **Banowanie** – `Pages/Lecturer/Students`: ustawia `IsBanned` i tworzy rekord `StudentBan` (opcjonalnie z datą końca), zwalnia przyszłe rezerwacje studenta. Blokada czasowa wygasa automatycznie.

### 4.2. Moduł studenta (cz. 2)

`Pages/Student/Index` prezentuje aktywną rezerwację oraz listę terminów spełniających warunki `StudentId == null`, `!IsBlockedByLecturer`, `StartTime > now`. Limit jednej aktywnej (przyszłej) rezerwacji sprawdzany jest w `ReservationService.BookAsync`, a nie w widoku – spreparowany POST lub wywołanie API kończy się tym samym komunikatem w `TempData`/ProblemDetails. „Zmień termin” (`RescheduleAsync`) zwalnia stary i przypisuje nowy slot w jednej operacji `SaveChanges`, wykonywanej przez EF Core w pojedynczej transakcji bazy danych; w razie błędu walidacji żadna zmiana nie jest zapisywana.

### 4.3. Walidacja i logika domenowa (cz. 3)

- `SlotDurationMinutes ∈ [5, 180]` (`[Range]` + `AvailabilityValidator`), `EndDate ≥ StartDate`, `DailyEndTime − DailyStartTime ≥ SlotDurationMinutes`, data rozpoczęcia nie w przeszłości. Walidacja wykonywana jest zarówno w formularzu (`ModelState`), jak i w serwisie.
- **Kolizje sal** – dwa okna tej samej sali kolidują, gdy nakładają się zarówno zakresy dat, jak i przedziały godzin; dla przedziałów półotwartych warunek nakładania ma postać `StartA < EndB ∧ EndA > StartB`. Okna stykające się (np. 8:00–10:00 i 10:00–12:00) nie kolidują.
- **Past Slots Lock** – każda operacja modyfikująca (`Book`, `Cancel`, `Reschedule`, `Reassign`, `Block`, `Unblock`, `UpdateSlot`) odrzuca sloty z `StartTime ≤ now`; w UI dla takich slotów nie są renderowane przyciski akcji.
- **Etykiety slotów** – `SlotLabelFormatter`, np. „Sala B214 (Laboratorium Sieciowe) – Pon. 16.11.2026, godz. 10:15–10:30”, zależne od kultury.

### 4.4. Identity, role i e-mail (cz. 4)

`ApplicationDbContext : IdentityDbContext<ApplicationUser>`, `RequireConfirmedAccount = true`. `DataSeeder` przy starcie tworzy schemat bazy, role, konta testowe, przykładowe sale oraz okno dostępności na najbliższe dni. Nowo zarejestrowany użytkownik automatycznie otrzymuje rolę `Student` (`ApplicationUserManager.CreateAsync`).

Wybrano **lokalny serwer SMTP (Mailpit)** jako domyślną implementację `IEmailSender` (`SmtpEmailSender`), ponieważ nie wymaga konta ani klucza API i pozwala podejrzeć wiadomości w przeglądarce. W środowisku Development domyślnie używany jest `FileEmailSender`, który zapisuje wiadomości jako pliki HTML w katalogu `mails/` aplikacji i wypisuje link potwierdzający w logu konsoli, co pozwala uruchomić projekt bez serwera pocztowego. Implementacja `SendGridEmailSender` jest dostępna po ustawieniu `Email:Provider = SendGrid` i klucza `Email:SendGrid:ApiKey`. Wiadomości wysyłane są jako HTML (`IsBodyHtml = true`), dlatego encje `&amp;` w atrybucie `href` linku potwierdzającego są poprawnie dekodowane przez klienta poczty; wersja tekstowa (SendGrid) powstaje przez zdekodowanie encji HTML.

Autoryzacja: konwencje `AuthorizeFolder("/")` (wszystkie strony wymagają zalogowania), `AuthorizeFolder("/Lecturer", "Lecturer")`, `AuthorizeFolder("/Student", "Student")`. Bez logowania dostępne są wyłącznie strony obszaru Identity (logowanie, rejestracja, potwierdzenie e-mail) oraz strona błędu.

### 4.5. Internacjonalizacja (cz. 5)

Obsługiwane kultury `pl-PL` (domyślna) i `en-US` przez `UseRequestLocalization` z dostawcami: parametr `?culture=`, ciasteczko `.AspNetCore.Culture`, nagłówek `Accept-Language`. Przełącznik w `_Layout.cshtml` wywołuje endpoint `/culture/{culture}`, który zapisuje ciasteczko i przekierowuje (wyłącznie lokalnie) na bieżącą stronę. Teksty interfejsu, etykiety `[Display]` i komunikaty walidacji pochodzą z plików `Resources/SharedResource.resx` (angielski) i `SharedResource.pl.resx` (polski); daty i godziny formatowane są zgodnie z bieżącą kulturą.

### 4.6. Minimal API i Swagger (cz. 6)

| Metoda i ścieżka | Odpowiedź |
|---|---|
| `GET /api/rooms` | `200` + `RoomDto[]` |
| `GET /api/slots/available?roomId=` | `200` + `AvailableSlotDto[]` |
| `POST /api/slots/{id:int}/book` | `200` + `BookingResultDto`; `400`/`403`/`404`/`409` + ProblemDetails |

Ciało żądania rezerwacji: `BookSlotRequestDto { "student": "<id lub e-mail>" }`. Endpointy zwracają wyłącznie DTO (rekordy z `Core.Dtos`), nigdy encje EF Core. Rezerwacja przez API korzysta z `ReservationService.BookAsync`, więc obowiązują wszystkie reguły z części 2 i 3. Dokumentacja: `/swagger`. Endpointy API pozostawiono bez uwierzytelniania, aby umożliwić pracę klienta konsolowego zgodnie z treścią zadania; w środowisku produkcyjnym należałoby je zabezpieczyć (np. tokenem JWT).

### 4.7. Klient konsolowy (cz. 7)

`ProjectDefense.ConsoleClient` korzysta z `HttpClient`, `GetFromJsonAsync` i `PostAsJsonAsync`. Menu umożliwia wyświetlenie tabeli sal, wolnych terminów oraz rezerwację po podaniu identyfikatora lub e-maila studenta i numeru slotu. Odpowiedź `200` wyświetlana jest jako sukces, a błędy biznesowe `400/404/409` – wraz z treścią pola `detail` z ProblemDetails.

### 4.8. Eksport (cz. 8, bonus)

`Pages/Lecturer/Export` – formularz z wyborem sali, zakresu dat, opcją „tylko zarezerwowane” i formatem. `ExportService` generuje plik w całości w pamięci operacyjnej (`MemoryStream`/`byte[]`) i zwraca go przez `File(...)`:

- `.txt` – UTF-8 z BOM, tabela wyrównana do szerokości kolumn, `text/plain; charset=utf-8`;
- `.xlsx` – ClosedXML: pogrubiony i wypełniony nagłówek, zamrożony pierwszy wiersz, autofiltr, `AdjustToContents()`;
- `.pdf` – QuestPDF (`LicenseType.Community`): A4 poziomo, nagłówek z datą wygenerowania, tabela z powtarzanym nagłówkiem, numeracja stron.

## 5. Testy automatyczne

| Klasa | Weryfikowane zachowanie |
|---|---|
| `SlotGenerationTests` | okno 08:00–10:00, slot 15 min, 2 dni → 16 niepokrywających się slotów; sloty w granicach godzin; brak niepełnego slotu; walidacja `[5, 180]` |
| `RoomConflictDetectionTests` (EF Core InMemory) | 09:00–11:00 przy istniejącym 08:00–10:00 → odrzucone; inna sala, okna stykające się, rozłączne daty → zaakceptowane |
| `StudentBookingRulesTests` | druga rezerwacja → `Conflict`; operacje na przeszłych slotach → błąd; atomowa zmiana terminu; blokada przedziału; ban stały i czasowy |
| `MinimalApiIntegrationTests` (`WebApplicationFactory`) | `GET /api/rooms` i `GET /api/slots/available` → `200` i struktura JSON DTO; limit rezerwacji przez API → `409`; nieznany student → `404` |

W testach jednostkowych czas zastępowany jest klasą `FakeTimeProvider`, co pozwala przesuwać „teraz” i sprawdzać blokadę przeszłych terminów oraz wygasanie banów. W testach integracyjnych rejestracja `DbContextOptions<ApplicationDbContext>` jest podmieniana na bazę InMemory.

## 6. Uruchomienie

Wymagane: .NET SDK 8.0 lub nowszy.

```
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ProjectDefense.Web
```

Skróty (wymagany [Bun](https://bun.sh) lub npm – `npm run <skrypt>`):

| Polecenie | Działanie |
|---|---|
| `bun dev` | aplikacja Web w trybie `dotnet watch` (przeładowanie po zmianach) |
| `bun run test` | wszystkie testy |
| `bun run console` | klient konsolowy |
| `bun run db:reset` | usunięcie bazy SQLite (zostanie utworzona ponownie) |
| `bun run mailpit` | lokalny serwer SMTP Mailpit w Dockerze |

Aplikacja nasłuchuje pod `http://localhost:5080` (profil `https`: `https://localhost:7080`). Baza SQLite `projectdefense.db` tworzona jest automatycznie przy pierwszym uruchomieniu; po zmianie modelu wystarczy ją usunąć. Alternatywnie można przejść na migracje: `dotnet ef migrations add Initial -p src/ProjectDefense.Web` i zastąpić `EnsureCreatedAsync()` wywołaniem `MigrateAsync()`.

Konta testowe (hasło `Haslo123!`, konfigurowalne w `Seed:DefaultPassword`):

| Rola | E-mail |
|---|---|
| Prowadzący | `prowadzacy@projectdefense.local` |
| Student | `student1@projectdefense.local` |
| Student | `student2@projectdefense.local` |

Poczta (rejestracja nowych kont): w trybie Development wiadomości trafiają do `src/ProjectDefense.Web/mails/*.html`, a link potwierdzający jest wypisywany w konsoli. Aby korzystać z SMTP, należy ustawić `Email:Provider = Smtp` i uruchomić Mailpit, np. `docker run -p 8025:8025 -p 1025:1025 axllent/mailpit`, i otworzyć `http://localhost:8025`. Klient konsolowy:

```
dotnet run --project src/ProjectDefense.ConsoleClient -- http://localhost:5080/
```

## 7. Wnioski

1. Umieszczenie reguł biznesowych w serwisach, a nie w stronach, sprawia, że UI, API i testy korzystają z jednej implementacji, a reguł nie da się obejść spreparowanym żądaniem.
2. Abstrakcja czasu (`TimeProvider`) jest warunkiem testowalności logiki zależnej od „teraz”, takiej jak blokada przeszłych terminów i wygasanie banów.
3. Warunek `StartA < EndB ∧ EndA > StartB` jest uniwersalnym testem nakładania przedziałów półotwartych; w przypadku okien dostępności należy go zastosować osobno do dat i do godzin.
4. Spójność danych przy równoczesnym dostępie wymaga mechanizmu po stronie bazy (tokeny współbieżności), ponieważ samo sprawdzenie warunku przed zapisem nie wyklucza wyścigu.
5. Rozdzielenie encji i DTO chroni przed ujawnieniem pól wewnętrznych (np. identyfikatorów studentów) oraz przed cyklicznymi referencjami podczas serializacji.
