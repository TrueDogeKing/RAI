'use strict';

// [1] klasa, [12] prywatne pola i metody, [4] parametry domyslne,
// [5] destrukturyzacja, [7] parametry resztkowe
function _typeof(o) { "@babel/helpers - typeof"; return _typeof = "function" == typeof Symbol && "symbol" == typeof Symbol.iterator ? function (o) { return typeof o; } : function (o) { return o && "function" == typeof Symbol && o.constructor === Symbol && o !== Symbol.prototype ? "symbol" : typeof o; }, _typeof(o); }
function _classCallCheck(a, n) { if (!(a instanceof n)) throw new TypeError("Cannot call a class as a function"); }
function _defineProperties(e, r) { for (var t = 0; t < r.length; t++) { var o = r[t]; o.enumerable = o.enumerable || !1, o.configurable = !0, "value" in o && (o.writable = !0), Object.defineProperty(e, _toPropertyKey(o.key), o); } }
function _createClass(e, r, t) { return r && _defineProperties(e.prototype, r), t && _defineProperties(e, t), Object.defineProperty(e, "prototype", { writable: !1 }), e; }
function _toPropertyKey(t) { var i = _toPrimitive(t, "string"); return "symbol" == _typeof(i) ? i : i + ""; }
function _toPrimitive(t, e) { if ("object" != _typeof(t) || !t) return t; var r; if ("undefined" != typeof Symbol && void 0 !== (r = t[Symbol.toPrimitive])) { var i = r.call(t, e || "default"); if ("object" != _typeof(i)) return i; throw new TypeError("@@toPrimitive must return a primitive value."); } return ("string" === e ? String : Number)(t); }
function _classPrivateMethodInitSpec(e, a) { _checkPrivateRedeclaration(e, a), a.add(e); }
function _classPrivateFieldInitSpec(e, t, a) { _checkPrivateRedeclaration(e, t), t.set(e, a); }
function _checkPrivateRedeclaration(e, t) { if (t.has(e)) throw new TypeError("Cannot initialize the same private elements twice on an object"); }
function _classPrivateFieldGet(s, a) { return s.get(_assertClassBrand(s, a)); }
function _classPrivateFieldSet(s, a, r) { return s.set(_assertClassBrand(s, a), r), r; }
function _assertClassBrand(e, t, n) { if ("function" == typeof e ? e === t : e.has(t)) return arguments.length < 3 ? t : n; throw new TypeError("Private element is not present on this object"); }
var _id = /*#__PURE__*/new WeakMap();
var _cena = /*#__PURE__*/new WeakMap();
var _Ksiazka_brand = /*#__PURE__*/new WeakSet();
var Ksiazka = /*#__PURE__*/function () {
  function Ksiazka(_ref) {
    var _Ksiazka$licznik, _Ksiazka$licznik2;
    var autor = _ref.autor,
      tytul = _ref.tytul,
      _ref$cena = _ref.cena,
      cena = _ref$cena === void 0 ? 0 : _ref$cena,
      _ref$wydawnictwo = _ref.wydawnictwo,
      wydawnictwo = _ref$wydawnictwo === void 0 ? 'nieznane' : _ref$wydawnictwo;
    _classCallCheck(this, Ksiazka);
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
    for (var _len = arguments.length, slowaKluczowe = new Array(_len > 1 ? _len - 1 : 0), _key = 1; _key < _len; _key++) {
      slowaKluczowe[_key - 1] = arguments[_key];
    }
    this.slowa_kluczowe = slowaKluczowe.map(_normalizuj);
  }
  return _createClass(Ksiazka, [{
    key: "id",
    get: function get() {
      return _classPrivateFieldGet(_id, this);
    }
  }, {
    key: "cena",
    get: function get() {
      return _classPrivateFieldGet(_cena, this);
    },
    set: function set(wartosc) {
      if (typeof wartosc !== 'number' || wartosc < 0) {
        throw new RangeError('cena musi byc nieujemna liczba');
      }
      _classPrivateFieldSet(_cena, this, wartosc);
    }
  }, {
    key: "toString",
    value:
    // [6] interpolacja napisow
    function toString() {
      return "\"".concat(this.tytul, "\" - ").concat(this.autor, " (").concat(this.wydawnictwo, "), ").concat(_assertClassBrand(_Ksiazka_brand, this, _formatujCene).call(this));
    }

    // [10] replaceAll
  }, {
    key: "slug",
    get: function get() {
      return this.tytul.toLowerCase().replaceAll(' ', '-');
    }
  }, {
    key: "maSlowo",
    value: function maSlowo(slowo) {
      return this.slowa_kluczowe.includes(_normalizuj.call(Ksiazka, slowo));
    }
  }]);
}();
function _normalizuj(slowo) {
  return slowo.trim().toLowerCase();
}
function _formatujCene() {
  // [11] separatory w liczbach
  return _classPrivateFieldGet(_cena, this) >= 1000 ? "".concat((_classPrivateFieldGet(_cena, this) / 1000).toFixed(1), " tys. zl") : "".concat(_classPrivateFieldGet(_cena, this).toFixed(2), " zl");
}
var _licznik = {
  _: 0
};
module.exports = Ksiazka;