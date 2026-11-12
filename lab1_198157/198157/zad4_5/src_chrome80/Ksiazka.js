'use strict';

// [1] klasa, [12] prywatne pola i metody, [4] parametry domyslne,
// [5] destrukturyzacja, [7] parametry resztkowe
function _classPrivateMethodInitSpec(e, a) { _checkPrivateRedeclaration(e, a), a.add(e); }
function _classPrivateFieldInitSpec(e, t, a) { _checkPrivateRedeclaration(e, t), t.set(e, a); }
function _checkPrivateRedeclaration(e, t) { if (t.has(e)) throw new TypeError("Cannot initialize the same private elements twice on an object"); }
function _classPrivateFieldGet(s, a) { return s.get(_assertClassBrand(s, a)); }
function _classPrivateFieldSet(s, a, r) { return s.set(_assertClassBrand(s, a), r), r; }
function _assertClassBrand(e, t, n) { if ("function" == typeof e ? e === t : e.has(t)) return arguments.length < 3 ? t : n; throw new TypeError("Private element is not present on this object"); }
var _id = /*#__PURE__*/new WeakMap();
var _cena = /*#__PURE__*/new WeakMap();
var _Ksiazka_brand = /*#__PURE__*/new WeakSet();
class Ksiazka {
  constructor({
    autor,
    tytul,
    cena = 0,
    wydawnictwo = 'nieznane'
  }, ...slowaKluczowe) {
    var _Ksiazka$licznik, _Ksiazka$licznik2;
    _classPrivateMethodInitSpec(this, _Ksiazka_brand);
    _classPrivateFieldInitSpec(this, _id, void 0);
    _classPrivateFieldInitSpec(this, _cena, void 0);
    if (!autor || !tytul) {
      throw new Error('Ksiazka wymaga autora i tytulu');
    }
    _classPrivateFieldSet(_id, this, _licznik._ = (_Ksiazka$licznik2 = _licznik._, ++_Ksiazka$licznik2));
    this.autor = autor;
    this.tytul = tytul;
    this.wydawnictwo = wydawnictwo;
    this.cena = cena;
    this.slowa_kluczowe = slowaKluczowe.map(_normalizuj);
  }
  get id() {
    return _classPrivateFieldGet(_id, this);
  }
  get cena() {
    return _classPrivateFieldGet(_cena, this);
  }
  set cena(wartosc) {
    if (typeof wartosc !== 'number' || wartosc < 0) {
      throw new RangeError('cena musi byc nieujemna liczba');
    }
    _classPrivateFieldSet(_cena, this, wartosc);
  }
  // [6] interpolacja napisow
  toString() {
    return `"${this.tytul}" - ${this.autor} (${this.wydawnictwo}), ${_assertClassBrand(_Ksiazka_brand, this, _formatujCene).call(this)}`;
  }

  // [10] replaceAll
  get slug() {
    return this.tytul.toLowerCase().replaceAll(' ', '-');
  }
  maSlowo(slowo) {
    return this.slowa_kluczowe.includes(_normalizuj.call(Ksiazka, slowo));
  }
}
function _normalizuj(slowo) {
  return slowo.trim().toLowerCase();
}
function _formatujCene() {
  // [11] separatory w liczbach
  return _classPrivateFieldGet(_cena, this) >= 1_000 ? `${(_classPrivateFieldGet(_cena, this) / 1_000).toFixed(1)} tys. zl` : `${_classPrivateFieldGet(_cena, this).toFixed(2)} zl`;
}
var _licznik = {
  _: 0
};
module.exports = Ksiazka;