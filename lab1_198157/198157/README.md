# Lab 1 - JavaScript (198157)

| Zadanie | Katalog | Uruchomienie |
|---|---|---|
| I. Testy jednostkowe | `1-UTNode`, `2-UTNodeInBrowser` | `npm test` |
| II. Struktura projektu | `zad2` | `npm test` |
| III. Prototypy / domkniecia | `zad3` | `npm test` |
| IV + V. Logika biznesowa, konstrukcje jezyka, Babel | `zad4_5` | `npm test`, `npm run build:all` |
| VI. Bundler (webpack) | `zad6` | `npm start` (dev server), `npm run build` |

W kazdym katalogu najpierw `npm install`.

## III. Prototypy i domkniecia (`zad3`)

- `src/PojazdDomkniecie.js` - `id`, `maxPredkosc`, `predkosc` sa zmiennymi lokalnymi konstruktora,
  metody `status/start/stop` maja do nich dostep przez domkniecie. Z zewnatrz `pojazd.predkosc`
  jest `undefined`, a zapis tworzy jedynie nowa, niezwiazana wlasciwosc.
- `src/PojazdPrototyp.js` - pola publiczne, metody w `Pojazd.prototype` (wspoldzielone przez wszystkie obiekty).
- Testy sprawdzaja m.in.: `pojazd.prototype === undefined`, `pojazd.__proto__ === Pojazd.prototype`,
  `Pojazd.prototype.constructor === Pojazd`, dodanie metody do prototypu po utworzeniu obiektu
  (widoczna od razu w istniejacych obiektach) i nadpisanie `Pojazd.prototype` (stare obiekty
  zachowuja stary prototyp i przestaja byc `instanceof Pojazd`, nowe traca `constructor`).

## IV. Biblioteka (`zad4_5`)

`Ksiazka` (autor, tytul, cena, wydawnictwo, slowa kluczowe) oraz `Biblioteka` z `lista_wypozyczen`
(historia wypozyczen i zwrotow) i metodami `wypozycz`, `zwroc`, `kto_wypozyczyl`, `szukaj`.
Kolekcje obslugiwane bez petli (`map`, `filter`, `every`, `reduce`, `Map`).

## V. Konstrukcje jezyka

| # | Konstrukcja | Gdzie | Brak wsparcia np. w |
|---|---|---|---|
| 1 | `class` | `Ksiazka`, `Biblioteka` | IE 11, Node < 6 |
| 2 | funkcje strzalkowe | `Biblioteka.szukaj`, `dodaj` | IE 11 |
| 3 | `let` / `const` (zasieg bloku) | wszedzie, test w `BibliotekaTest.js` | IE 10 (IE 11 czesciowo) |
| 4 | parametry domyslne | `constructor(nazwa = 'Biblioteka')`, `wypozycz(..., data = new Date())` | IE 11, Chrome < 49 |
| 5 | destrukturyzacja | konstruktor `Ksiazka`, `zwroc`, `reduce((suma, { cena }) => ...)` | IE 11, Chrome < 49 |
| 6 | interpolacja napisow | `toString`, komunikaty bledow | IE 11 |
| 7 | parametry resztkowe | `dodaj(...ksiazki)`, `szukaj(...slowa)` | IE 11, Chrome < 47 |
| 8 | `this` w funkcji zagniezdzonej vs `=>` | `raportStrzalka` / `raportFunction` | - |
| 9 | `?.` i `??` | `kto_wypozyczyl` | Chrome < 80, Firefox < 72, Node < 14 |
| 10 | `String.prototype.replaceAll` | `Ksiazka.slug` | Chrome < 85, Firefox < 77, Node < 15 |
| 11 | separatory w liczbach `1_000` | `#formatujCene` | Chrome < 75, Node < 12.5 |
| 12 | prywatne pola/metody/statyczne `#` | `#id`, `#cena`, `#pobierz`, `static #licznik` | Chrome < 84 (metody), Firefox < 90, Safari < 15, Node < 14.6 |
| + | gettery/settery, `static`, spread `[...]`, `Map` | `Ksiazka.cena`, `Biblioteka.ksiazki` | IE 11 (spread) |

### Babel

```
npm install --save-dev @babel/core @babel/cli @babel/preset-env
npm run build          # babel src --out-dir src_env --presets=@babel/env  (domyslne browserslist)
npm run build:ie11     # babel.old.json    - targets: ie 11
npm run build:chrome80 # babel.modern.json - targets: chrome 80
```

Roznice w wygenerowanym kodzie:

- **ie 11** (`src_ie11`) - klasy zamienione na funkcje + `_createClass`/`_classCallCheck`,
  pola prywatne na `WeakMap`/`WeakSet`, destrukturyzacja na `_ref.autor`, parametry domyslne na
  `=== void 0 ? ... : ...`, szablony na konkatenacje, `?.`/`??` na porownania z `null`, `1_000` na `1000`.
  Plik jest najdluzszy (ok. 170 linii zamiast 100).
- **chrome 80** (`src_chrome80`) - zostaja `class`, `=>`, szablony, `?.`, `??` i `1_000`
  (Chrome 80 je obsluguje), transpilowane sa tylko prywatne pola/metody (`#`, od Chrome 84).
- `replaceAll` nie jest skladnia tylko metoda biblioteki standardowej - Babel jej nie przepisuje,
  w starych przegladarkach wymagany jest polyfill (`core-js` + `useBuiltIns: "usage"`).

## VI. Bundler - webpack (`zad6`)

Aplikacja przegladarkowa korzystajaca z modelu `Ksiazka`/`Biblioteka` (wyszukiwanie, wypozycz/zwroc).

1. **Konfiguracja i uruchomienie** - `webpack.config.js`: `entry: ./src/index.js`, `output: dist/`,
   loadery (`babel-loader` dla `.js`, `style-loader` + `css-loader` dla `.css`),
   `HtmlWebpackPlugin` generuje `index.html` z dolaczonym bundlem. `npm run build` -> `dist/`.
2. **Dev server** - `npm start` (`webpack serve`) uruchamia `webpack-dev-server` na
   http://localhost:8080. Bundle trzymany jest w pamieci i przebudowywany przy kazdym zapisie pliku.
3. **Hot Module Replacement** - `devServer.hot: true`; w `src/index.js` `module.hot.accept('./widok', ...)`
   podmienia modul `widok.js` w dzialajacej stronie bez przeladowania - stan (wypozyczenia,
   licznik renderow) zostaje zachowany. Zmiany w `style.css` takze sa wstrzykiwane bez przeladowania
   (`style-loader` obsluguje HMR). Zmiana modulu bez `accept` powoduje pelne przeladowanie strony.
4. **Babel z bundlerem** - regula `{ test: /\.js$/, exclude: /node_modules/, use: 'babel-loader' }`
   z `@babel/preset-env` (`targets: 'ie 11'`); kazdy plik JS jest transpilowany przed dolaczeniem do bundla.
5. **Source mapping** - bundle jest transpilowany i zminifikowany, wiec bledy i breakpointy wskazywalyby
   na nieczytelny kod. Source mapa (`bundle.js.map`, komentarz `//# sourceMappingURL=`) mapuje pozycje
   w wygenerowanym kodzie na oryginalne pliki i linie. Dzieki temu w DevTools (F12 -> Sources -> `webpack://`)
   widac oryginalne `src/model/Biblioteka.js`, mozna stawiac w nim breakpointy, a stack trace wskazuje
   oryginalne linie. Ustawienie `devtool`: `eval-source-map` w trybie dev (szybkie przebudowy),
   `source-map` w produkcji (osobny plik `.map`).
