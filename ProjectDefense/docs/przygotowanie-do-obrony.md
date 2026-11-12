# Przygotowanie do obrony – co trzeba rozumieć

## 1. Przepływ żądania w Razor Pages

Żądanie `GET /Lecturer/Rooms/Edit/3` trafia do pliku `Pages/Lecturer/Rooms/Edit.cshtml` (szablon `@page "{id:int}"`). Framework tworzy `EditModel` (zależności z kontenera DI przez konstruktor), wywołuje `OnGetAsync(int id)`, a następnie renderuje widok z `Model`. Formularz `method="post"` wraca pod ten sam adres i wywołuje `OnPostAsync`. Nazwane handlery (`asp-page-handler="Cancel"`) mapowane są na metody `OnPostCancelAsync`.

Do wyjaśnienia na obronie:

- `[BindProperty]` – wiązanie pól formularza z właściwością modelu strony; `SupportsGet = true` – także przy GET (filtry w adresie);
- `ModelState.IsValid` – wynik walidacji atrybutów (`[Required]`, `[Range]`); błędy własne dodaje się przez `ModelState.AddModelError`;
- wzorzec **Post/Redirect/Get** – po udanym POST wykonujemy `RedirectToPage`, a komunikat przekazujemy przez `TempData` (przetrwa jedno przekierowanie);
- tag helpery: `asp-for`, `asp-items`, `asp-validation-for`, `asp-page`, `asp-route-id`;
- token antyforgery – dodawany automatycznie do formularzy przez tag helper `<form>` i sprawdzany przy POST.

## 2. Dependency Injection i cykl życia

W `Program.cs` serwisy są rejestrowane jako `Scoped` (jedna instancja na żądanie HTTP – tak jak `DbContext`) lub `Singleton` (`SlotGenerator`, `TimeProvider`). Pytanie kontrolne: dlaczego `ReservationService` nie może być singletonem? Ponieważ zależy od `ApplicationDbContext`, który jest scoped i nie jest bezpieczny wielowątkowo.

## 3. Entity Framework Core

- `ApplicationDbContext` dziedziczy po `IdentityDbContext<ApplicationUser>`, więc zawiera tabele Identity (`AspNetUsers`, `AspNetRoles`, ...) oraz własne `DbSet`-y;
- konfiguracja relacji w `OnModelCreating` (`HasOne/WithMany/HasForeignKey`, `OnDelete`); dlaczego sala ma `Restrict`, a slot `SetNull` dla studenta;
- `Include/ThenInclude` (eager loading), `AsNoTracking` (odczyt bez śledzenia zmian), projekcja `Select` do DTO – zapytanie SQL pobiera tylko potrzebne kolumny;
- **Change Tracker** – zmiana właściwości śledzonej encji i jedno `SaveChangesAsync()` dają jedną transakcję; na tym opiera się atomowa „Zmiana terminu”;
- `[ConcurrencyCheck]` i `DbUpdateConcurrencyException` – ochrona przed podwójną rezerwacją przy równoczesnych żądaniach;
- `EnsureCreated` a migracje – kiedy które stosować.

## 4. Reguły domenowe – gdzie są sprawdzane

| Reguła | Miejsce |
|---|---|
| jedna aktywna rezerwacja | `ReservationService.BookAsync` → `HasActiveReservationAsync` |
| blokada przeszłych slotów | `ReservationService.IsPast` w każdej operacji modyfikującej |
| kolizja sal | `AvailabilityService.HasConflictAsync` |
| długość slotu, kolejność dat i godzin | `AvailabilityValidator.Validate` + atrybuty formularza |
| ban | `BanService.IsBannedAsync` wywoływane przed rezerwacją |

Najważniejsza teza: walidacja w UI jest dla wygody użytkownika, walidacja w serwisie dla bezpieczeństwa. Spreparowany POST (np. z Postmana albo zmieniony w F12) omija przeglądarkę, ale nie omija serwisu.

Warunek nakładania przedziałów `StartA < EndB && EndA > StartB` – warto umieć narysować wszystkie przypadki (rozłączne, stykające się, częściowo nachodzące, zawierające się) i pokazać, że stykające się nie kolidują.

## 5. Identity

- `AddDefaultIdentity` dostarcza gotowe strony logowania i rejestracji (Razor Class Library, obszar `Identity`);
- `RequireConfirmedAccount = true` – logowanie dopiero po kliknięciu linku z e-maila, stąd potrzeba `IEmailSender`;
- role i polityki: `AddRoles<IdentityRole>()`, `AddPolicy("Lecturer", p => p.RequireRole("Lecturer"))`, konwencje `AuthorizeFolder`;
- problem `&amp;`: link w treści HTML jest kodowany przez `HtmlEncoder`, więc `&` w adresie staje się `&amp;`. W wiadomości HTML przeglądarka/klient poczty dekoduje to poprawnie; błąd pojawia się, gdy taką treść wyśle się jako zwykły tekst.

## 6. Lokalizacja

`AddLocalization` + `UseRequestLocalization` ustala `CultureInfo.CurrentCulture` dla każdego żądania na podstawie (kolejno) parametru `culture`, ciasteczka i nagłówka `Accept-Language`. `IStringLocalizer<SharedResource>` wyszukuje klucz w `SharedResource.pl.resx`, a gdy go nie ma – w pliku neutralnym. Komunikaty `[Required(ErrorMessage = "Required")]` są kluczami zasobów dzięki `AddDataAnnotationsLocalization`.

## 7. Minimal API

`MapGet/MapPost` z parametrami wiązanymi automatycznie (z trasy, zapytania, ciała JSON, DI). `TypedResults` + `Results<Ok<T>, ProblemHttpResult>` pozwalają Swaggerowi poznać możliwe odpowiedzi. Zwracamy DTO, nie encje: encje mają nawigacje (cykle przy serializacji), pola wewnętrzne i wiążą kontrakt API ze schematem bazy.

## 8. Testy

- test jednostkowy generatora – czysta funkcja, bez bazy;
- testy serwisów na EF Core InMemory i `FakeTimeProvider` – szybkie, izolowane (osobna baza na test);
- test integracyjny `WebApplicationFactory<Program>` – uruchamia cały potok ASP.NET Core w pamięci; wymaga `public partial class Program` i podmiany rejestracji `DbContext`.

## 9. Pytania, które mogą paść

1. Co się stanie, gdy dwóch studentów kliknie „Zarezerwuj” w tej samej milisekundzie?
2. Dlaczego „Zmień termin” nie wymaga jawnej transakcji?
3. Jak zapobiec rezerwacji przez spreparowany POST przy aktywnej rezerwacji?
4. Czym różni się `Scoped` od `Transient` i `Singleton`?
5. Dlaczego API zwraca DTO, a nie encję `ReservationSlot`?
6. Jak działa przełączanie języka i gdzie jest zapamiętywany wybór?
7. Jak zmienić dostawcę e-mail z Mailpit na SendGrid bez zmiany kodu?
8. Jak przetestować regułę zależną od aktualnego czasu?
