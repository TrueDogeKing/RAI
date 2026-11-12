'use strict';

function _typeof(o) { "@babel/helpers - typeof"; return _typeof = "function" == typeof Symbol && "symbol" == typeof Symbol.iterator ? function (o) { return typeof o; } : function (o) { return o && "function" == typeof Symbol && o.constructor === Symbol && o !== Symbol.prototype ? "symbol" : typeof o; }, _typeof(o); }
function _toConsumableArray(r) { return _arrayWithoutHoles(r) || _iterableToArray(r) || _unsupportedIterableToArray(r) || _nonIterableSpread(); }
function _nonIterableSpread() { throw new TypeError("Invalid attempt to spread non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method."); }
function _unsupportedIterableToArray(r, a) { if (r) { if ("string" == typeof r) return _arrayLikeToArray(r, a); var t = {}.toString.call(r).slice(8, -1); return "Object" === t && r.constructor && (t = r.constructor.name), "Map" === t || "Set" === t ? Array.from(r) : "Arguments" === t || /^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(t) ? _arrayLikeToArray(r, a) : void 0; } }
function _iterableToArray(r) { var e = "undefined" != typeof Symbol && r[Symbol.iterator] || r["@@iterator"]; if (Array.isArray(r) && (null == e || e === ("undefined" != typeof Symbol && [][Symbol.iterator]) || e === []["@@iterator"])) return r; if (null != e) { for (var t, o = [], a = e.call(r); !(t = a.next()).done;) o.push(t.value); return o; } }
function _arrayWithoutHoles(r) { if (Array.isArray(r)) return _arrayLikeToArray(r); }
function _arrayLikeToArray(r, a) { (null == a || a > r.length) && (a = r.length); for (var e = 0, n = Array(a); e < a; e++) n[e] = r[e]; return n; }
function _classCallCheck(a, n) { if (!(a instanceof n)) throw new TypeError("Cannot call a class as a function"); }
function _defineProperties(e, r) { for (var t = 0; t < r.length; t++) { var o = r[t]; o.enumerable = o.enumerable || !1, o.configurable = !0, "value" in o && (o.writable = !0), Object.defineProperty(e, _toPropertyKey(o.key), o); } }
function _createClass(e, r, t) { return r && _defineProperties(e.prototype, r), t && _defineProperties(e, t), Object.defineProperty(e, "prototype", { writable: !1 }), e; }
function _classPrivateMethodInitSpec(e, a) { _checkPrivateRedeclaration(e, a), a.add(e); }
function _defineProperty(e, r, t) { return (r = _toPropertyKey(r)) in e ? Object.defineProperty(e, r, { value: t, enumerable: !0, configurable: !0, writable: !0 }) : e[r] = t, e; }
function _toPropertyKey(t) { var i = _toPrimitive(t, "string"); return "symbol" == _typeof(i) ? i : i + ""; }
function _toPrimitive(t, e) { if ("object" != _typeof(t) || !t) return t; var r; if ("undefined" != typeof Symbol && void 0 !== (r = t[Symbol.toPrimitive])) { var i = r.call(t, e || "default"); if ("object" != _typeof(i)) return i; throw new TypeError("@@toPrimitive must return a primitive value."); } return ("string" === e ? String : Number)(t); }
function _classPrivateFieldInitSpec(e, t, a) { _checkPrivateRedeclaration(e, t), t.set(e, a); }
function _checkPrivateRedeclaration(e, t) { if (t.has(e)) throw new TypeError("Cannot initialize the same private elements twice on an object"); }
function _classPrivateFieldGet(s, a) { return s.get(_assertClassBrand(s, a)); }
function _assertClassBrand(e, t, n) { if ("function" == typeof e ? e === t : e.has(t)) return arguments.length < 3 ? t : n; throw new TypeError("Private element is not present on this object"); }
var Ksiazka = require('./Ksiazka');
var _ksiazki = /*#__PURE__*/new WeakMap();
var _wypozyczone = /*#__PURE__*/new WeakMap();
var _Biblioteka_brand = /*#__PURE__*/new WeakSet();
var Biblioteka = /*#__PURE__*/function () {
  function Biblioteka() {
    var nazwa = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : 'Biblioteka';
    _classCallCheck(this, Biblioteka);
    _classPrivateMethodInitSpec(this, _Biblioteka_brand);
    _classPrivateFieldInitSpec(this, _ksiazki, new Map());
    _classPrivateFieldInitSpec(this, _wypozyczone, new Map());
    // id ksiazki -> aktualne wypozyczenie
    // historia wszystkich wypozyczen i zwrotow
    _defineProperty(this, "lista_wypozyczen", []);
    this.nazwa = nazwa;
  }
  return _createClass(Biblioteka, [{
    key: "dodaj",
    value: function dodaj() {
      var _this = this;
      for (var _len = arguments.length, ksiazki = new Array(_len), _key = 0; _key < _len; _key++) {
        ksiazki[_key] = arguments[_key];
      }
      ksiazki.forEach(function (k) {
        if (!(k instanceof Ksiazka)) {
          throw new TypeError('mozna dodawac tylko obiekty Ksiazka');
        }
        _classPrivateFieldGet(_ksiazki, _this).set(k.id, k);
      });
      return this;
    }
  }, {
    key: "ksiazki",
    get: function get() {
      return _toConsumableArray(_classPrivateFieldGet(_ksiazki, this).values());
    }
  }, {
    key: "dostepne",
    get: function get() {
      var _this2 = this;
      return this.ksiazki.filter(function (_ref) {
        var id = _ref.id;
        return !_classPrivateFieldGet(_wypozyczone, _this2).has(id);
      });
    }
  }, {
    key: "wypozycz",
    value: function wypozycz(idKsiazki, czytelnik) {
      var data = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : new Date();
      var ksiazka = _assertClassBrand(_Biblioteka_brand, this, _pobierz).call(this, idKsiazki);
      if (_classPrivateFieldGet(_wypozyczone, this).has(idKsiazki)) {
        throw new Error("\"".concat(ksiazka.tytul, "\" jest juz wypozyczona przez ").concat(this.kto_wypozyczyl(idKsiazki)));
      }
      var wpis = {
        typ: 'wypozyczenie',
        idKsiazki: idKsiazki,
        czytelnik: czytelnik,
        data: data
      };
      _classPrivateFieldGet(_wypozyczone, this).set(idKsiazki, wpis);
      this.lista_wypozyczen.push(wpis);
      return wpis;
    }
  }, {
    key: "zwroc",
    value: function zwroc(idKsiazki) {
      var data = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : new Date();
      _assertClassBrand(_Biblioteka_brand, this, _pobierz).call(this, idKsiazki);
      var wypozyczenie = _classPrivateFieldGet(_wypozyczone, this).get(idKsiazki);
      if (!wypozyczenie) {
        throw new Error("ksiazka o id ".concat(idKsiazki, " nie jest wypozyczona"));
      }
      var czytelnik = wypozyczenie.czytelnik;
      var wpis = {
        typ: 'zwrot',
        idKsiazki: idKsiazki,
        czytelnik: czytelnik,
        data: data
      };
      _classPrivateFieldGet(_wypozyczone, this).delete(idKsiazki);
      this.lista_wypozyczen.push(wpis);
      return wpis;
    }

    // [9] optional chaining + ??
  }, {
    key: "kto_wypozyczyl",
    value: function kto_wypozyczyl(idKsiazki) {
      var _classPrivateFieldGet2, _classPrivateFieldGet3;
      return (_classPrivateFieldGet2 = (_classPrivateFieldGet3 = _classPrivateFieldGet(_wypozyczone, this).get(idKsiazki)) === null || _classPrivateFieldGet3 === void 0 ? void 0 : _classPrivateFieldGet3.czytelnik) !== null && _classPrivateFieldGet2 !== void 0 ? _classPrivateFieldGet2 : null;
    }

    // szukanie po slowach kluczowych - ksiazka musi miec wszystkie podane slowa
  }, {
    key: "szukaj",
    value: function szukaj() {
      for (var _len2 = arguments.length, slowa = new Array(_len2), _key2 = 0; _key2 < _len2; _key2++) {
        slowa[_key2] = arguments[_key2];
      }
      if (slowa.length === 0) {
        return [];
      }
      return this.ksiazki.filter(function (k) {
        return slowa.every(function (s) {
          return k.maSlowo(s);
        });
      });
    }
  }, {
    key: "historia",
    value: function historia(czytelnik) {
      return this.lista_wypozyczen.filter(function (w) {
        return w.czytelnik === czytelnik;
      });
    }

    // [8] zachowanie this w funkcji zagniezdzonej - funkcja strzalkowa vs function
  }, {
    key: "raportStrzalka",
    value: function raportStrzalka() {
      var _this3 = this;
      return this.ksiazki.map(function (k) {
        return "".concat(_this3.nazwa, ": ").concat(k.tytul);
      });
    }
  }, {
    key: "raportFunction",
    value: function raportFunction() {
      return this.ksiazki.map(function (k) {
        // w trybie strict this === undefined w zwyklej funkcji zagniezdzonej
        return "".concat(this === null || this === void 0 ? void 0 : this.nazwa, ": ").concat(k.tytul);
      });
    }
  }, {
    key: "wartoscKsiegozbioru",
    value: function wartoscKsiegozbioru() {
      return this.ksiazki.reduce(function (suma, _ref2) {
        var cena = _ref2.cena;
        return suma + cena;
      }, 0);
    }
  }]);
}();
function _pobierz(idKsiazki) {
  var ksiazka = _classPrivateFieldGet(_ksiazki, this).get(idKsiazki);
  if (!ksiazka) {
    throw new Error("brak ksiazki o id ".concat(idKsiazki));
  }
  return ksiazka;
}
module.exports = Biblioteka;