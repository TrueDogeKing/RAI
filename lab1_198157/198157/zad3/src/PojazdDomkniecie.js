'use strict';

// Pojazd z "prywatnymi" polami - id, maxPredkosc i predkosc sa zmiennymi
// lokalnymi konstruktora, dostepnymi tylko przez domkniecie w metodach.
function Pojazd(id, maxPredkosc) {
    if (!(this instanceof Pojazd)) {
        throw new TypeError('Pojazd musi byc wywolany przez new');
    }
    if (typeof maxPredkosc !== 'number' || maxPredkosc <= 0) {
        throw new RangeError('maxPredkosc musi byc liczba dodatnia');
    }

    var predkosc = 0;

    this.status = function () {
        return {
            id: id,
            maxPredkosc: maxPredkosc,
            predkosc: predkosc,
            wRuchu: predkosc > 0
        };
    };

    this.start = function (nowaPredkosc) {
        if (typeof nowaPredkosc !== 'number' || nowaPredkosc <= 0) {
            throw new RangeError('predkosc musi byc liczba dodatnia');
        }
        if (nowaPredkosc > maxPredkosc) {
            throw new RangeError('przekroczono predkosc maksymalna ' + maxPredkosc);
        }
        predkosc = nowaPredkosc;
        return this;
    };

    this.stop = function () {
        predkosc = 0;
        return this;
    };
}

module.exports = Pojazd;
