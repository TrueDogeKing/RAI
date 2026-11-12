'use strict';

// Pojazd z publicznymi polami - metody sa wspoldzielone przez prototyp konstruktora.
function Pojazd(id, maxPredkosc) {
    this.id = id;
    this.max_predkosc = maxPredkosc;
    this.predkosc = 0;
}

Pojazd.prototype.status = function () {
    return {
        id: this.id,
        maxPredkosc: this.max_predkosc,
        predkosc: this.predkosc,
        wRuchu: this.predkosc > 0
    };
};

Pojazd.prototype.start = function (nowaPredkosc) {
    if (typeof nowaPredkosc !== 'number' || nowaPredkosc <= 0) {
        throw new RangeError('predkosc musi byc liczba dodatnia');
    }
    if (nowaPredkosc > this.max_predkosc) {
        throw new RangeError('przekroczono predkosc maksymalna ' + this.max_predkosc);
    }
    this.predkosc = nowaPredkosc;
    return this;
};

Pojazd.prototype.stop = function () {
    this.predkosc = 0;
    return this;
};

module.exports = Pojazd;
