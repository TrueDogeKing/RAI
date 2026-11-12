'use strict';

function _classPrivateMethodInitSpec(e, a) { _checkPrivateRedeclaration(e, a), a.add(e); }
function _defineProperty(e, r, t) { return (r = _toPropertyKey(r)) in e ? Object.defineProperty(e, r, { value: t, enumerable: !0, configurable: !0, writable: !0 }) : e[r] = t, e; }
function _toPropertyKey(t) { var i = _toPrimitive(t, "string"); return "symbol" == typeof i ? i : i + ""; }
function _toPrimitive(t, e) { if ("object" != typeof t || !t) return t; var r; if ("undefined" != typeof Symbol && void 0 !== (r = t[Symbol.toPrimitive])) { var i = r.call(t, e || "default"); if ("object" != typeof i) return i; throw new TypeError("@@toPrimitive must return a primitive value."); } return ("string" === e ? String : Number)(t); }
function _classPrivateFieldInitSpec(e, t, a) { _checkPrivateRedeclaration(e, t), t.set(e, a); }
function _checkPrivateRedeclaration(e, t) { if (t.has(e)) throw new TypeError("Cannot initialize the same private elements twice on an object"); }
function _classPrivateFieldGet(s, a) { return s.get(_assertClassBrand(s, a)); }
function _assertClassBrand(e, t, n) { if ("function" == typeof e ? e === t : e.has(t)) return arguments.length < 3 ? t : n; throw new TypeError("Private element is not present on this object"); }
const Ksiazka = require('./Ksiazka');
var _ksiazki = /*#__PURE__*/new WeakMap();
var _wypozyczone = /*#__PURE__*/new WeakMap();
var _Biblioteka_brand = /*#__PURE__*/new WeakSet();
class Biblioteka {
  constructor(nazwa = 'Biblioteka') {
    _classPrivateMethodInitSpec(this, _Biblioteka_brand);
    _classPrivateFieldInitSpec(this, _ksiazki, new Map());
    _classPrivateFieldInitSpec(this, _wypozyczone, new Map());
    // id ksiazki -> aktualne wypozyczenie
    // historia wszystkich wypozyczen i zwrotow
    _defineProperty(this, "lista_wypozyczen", []);
    this.nazwa = nazwa;
  }
  dodaj(...ksiazki) {
    ksiazki.forEach(k => {
      if (!(k instanceof Ksiazka)) {
        throw new TypeError('mozna dodawac tylko obiekty Ksiazka');
      }
      _classPrivateFieldGet(_ksiazki, this).set(k.id, k);
    });
    return this;
  }
  get ksiazki() {
    return [..._classPrivateFieldGet(_ksiazki, this).values()];
  }
  get dostepne() {
    return this.ksiazki.filter(({
      id
    }) => !_classPrivateFieldGet(_wypozyczone, this).has(id));
  }
  wypozycz(idKsiazki, czytelnik, data = new Date()) {
    const ksiazka = _assertClassBrand(_Biblioteka_brand, this, _pobierz).call(this, idKsiazki);
    if (_classPrivateFieldGet(_wypozyczone, this).has(idKsiazki)) {
      throw new Error(`"${ksiazka.tytul}" jest juz wypozyczona przez ${this.kto_wypozyczyl(idKsiazki)}`);
    }
    const wpis = {
      typ: 'wypozyczenie',
      idKsiazki,
      czytelnik,
      data
    };
    _classPrivateFieldGet(_wypozyczone, this).set(idKsiazki, wpis);
    this.lista_wypozyczen.push(wpis);
    return wpis;
  }
  zwroc(idKsiazki, data = new Date()) {
    _assertClassBrand(_Biblioteka_brand, this, _pobierz).call(this, idKsiazki);
    const wypozyczenie = _classPrivateFieldGet(_wypozyczone, this).get(idKsiazki);
    if (!wypozyczenie) {
      throw new Error(`ksiazka o id ${idKsiazki} nie jest wypozyczona`);
    }
    const {
      czytelnik
    } = wypozyczenie;
    const wpis = {
      typ: 'zwrot',
      idKsiazki,
      czytelnik,
      data
    };
    _classPrivateFieldGet(_wypozyczone, this).delete(idKsiazki);
    this.lista_wypozyczen.push(wpis);
    return wpis;
  }

  // [9] optional chaining + ??
  kto_wypozyczyl(idKsiazki) {
    return _classPrivateFieldGet(_wypozyczone, this).get(idKsiazki)?.czytelnik ?? null;
  }

  // szukanie po slowach kluczowych - ksiazka musi miec wszystkie podane slowa
  szukaj(...slowa) {
    if (slowa.length === 0) {
      return [];
    }
    return this.ksiazki.filter(k => slowa.every(s => k.maSlowo(s)));
  }
  historia(czytelnik) {
    return this.lista_wypozyczen.filter(w => w.czytelnik === czytelnik);
  }

  // [8] zachowanie this w funkcji zagniezdzonej - funkcja strzalkowa vs function
  raportStrzalka() {
    return this.ksiazki.map(k => `${this.nazwa}: ${k.tytul}`);
  }
  raportFunction() {
    return this.ksiazki.map(function (k) {
      // w trybie strict this === undefined w zwyklej funkcji zagniezdzonej
      return `${this?.nazwa}: ${k.tytul}`;
    });
  }
  wartoscKsiegozbioru() {
    return this.ksiazki.reduce((suma, {
      cena
    }) => suma + cena, 0);
  }
}
function _pobierz(idKsiazki) {
  const ksiazka = _classPrivateFieldGet(_ksiazki, this).get(idKsiazki);
  if (!ksiazka) {
    throw new Error(`brak ksiazki o id ${idKsiazki}`);
  }
  return ksiazka;
}
module.exports = Biblioteka;