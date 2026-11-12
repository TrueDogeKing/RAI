'use strict';

// [1] klasa, [12] prywatne pola i metody, [4] parametry domyslne,
// [5] destrukturyzacja, [7] parametry resztkowe
class Ksiazka {
    static #licznik = 0;

    #id;
    #cena;

    constructor({ autor, tytul, cena = 0, wydawnictwo = 'nieznane' }, ...slowaKluczowe) {
        if (!autor || !tytul) {
            throw new Error('Ksiazka wymaga autora i tytulu');
        }
        this.#id = ++Ksiazka.#licznik;
        this.autor = autor;
        this.tytul = tytul;
        this.wydawnictwo = wydawnictwo;
        this.cena = cena;
        this.slowa_kluczowe = slowaKluczowe.map(Ksiazka.#normalizuj);
    }

    static #normalizuj(slowo) {
        return slowo.trim().toLowerCase();
    }

    get id() {
        return this.#id;
    }

    get cena() {
        return this.#cena;
    }

    set cena(wartosc) {
        if (typeof wartosc !== 'number' || wartosc < 0) {
            throw new RangeError('cena musi byc nieujemna liczba');
        }
        this.#cena = wartosc;
    }

    #formatujCene() {
        // [11] separatory w liczbach
        return this.#cena >= 1_000 ? `${(this.#cena / 1_000).toFixed(1)} tys. zl` : `${this.#cena.toFixed(2)} zl`;
    }

    // [6] interpolacja napisow
    toString() {
        return `"${this.tytul}" - ${this.autor} (${this.wydawnictwo}), ${this.#formatujCene()}`;
    }

    // [10] replaceAll
    get slug() {
        return this.tytul.toLowerCase().replaceAll(' ', '-');
    }

    maSlowo(slowo) {
        return this.slowa_kluczowe.includes(Ksiazka.#normalizuj(slowo));
    }
}

module.exports = Ksiazka;
