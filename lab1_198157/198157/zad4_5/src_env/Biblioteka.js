'use strict';

const Ksiazka = require('./Ksiazka');
class Biblioteka {
  #ksiazki = new Map();
  #wypozyczone = new Map(); // id ksiazki -> aktualne wypozyczenie

  // historia wszystkich wypozyczen i zwrotow
  lista_wypozyczen = [];
  constructor(nazwa = 'Biblioteka') {
    this.nazwa = nazwa;
  }
  dodaj(...ksiazki) {
    ksiazki.forEach(k => {
      if (!(k instanceof Ksiazka)) {
        throw new TypeError('mozna dodawac tylko obiekty Ksiazka');
      }
      this.#ksiazki.set(k.id, k);
    });
    return this;
  }
  get ksiazki() {
    return [...this.#ksiazki.values()];
  }
  get dostepne() {
    return this.ksiazki.filter(({
      id
    }) => !this.#wypozyczone.has(id));
  }
  #pobierz(idKsiazki) {
    const ksiazka = this.#ksiazki.get(idKsiazki);
    if (!ksiazka) {
      throw new Error(`brak ksiazki o id ${idKsiazki}`);
    }
    return ksiazka;
  }
  wypozycz(idKsiazki, czytelnik, data = new Date()) {
    const ksiazka = this.#pobierz(idKsiazki);
    if (this.#wypozyczone.has(idKsiazki)) {
      throw new Error(`"${ksiazka.tytul}" jest juz wypozyczona przez ${this.kto_wypozyczyl(idKsiazki)}`);
    }
    const wpis = {
      typ: 'wypozyczenie',
      idKsiazki,
      czytelnik,
      data
    };
    this.#wypozyczone.set(idKsiazki, wpis);
    this.lista_wypozyczen.push(wpis);
    return wpis;
  }
  zwroc(idKsiazki, data = new Date()) {
    this.#pobierz(idKsiazki);
    const wypozyczenie = this.#wypozyczone.get(idKsiazki);
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
    this.#wypozyczone.delete(idKsiazki);
    this.lista_wypozyczen.push(wpis);
    return wpis;
  }

  // [9] optional chaining + ??
  kto_wypozyczyl(idKsiazki) {
    return this.#wypozyczone.get(idKsiazki)?.czytelnik ?? null;
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
module.exports = Biblioteka;