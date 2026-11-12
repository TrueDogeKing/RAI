'use strict';

var expect = require('chai').expect;
var Pojazd = require('../src/PojazdDomkniecie');

describe('Pojazd (domkniecie)', function () {
    var pojazd;

    beforeEach(function () {
        pojazd = new Pojazd('WX-1234', 120);
    });

    describe('interfejs', function () {
        it('nowy pojazd stoi', function () {
            expect(pojazd.status()).to.eql({ id: 'WX-1234', maxPredkosc: 120, predkosc: 0, wRuchu: false });
        });

        it('start ustawia predkosc', function () {
            pojazd.start(80);
            expect(pojazd.status().predkosc).to.equal(80);
            expect(pojazd.status().wRuchu).to.equal(true);
        });

        it('stop zatrzymuje pojazd', function () {
            pojazd.start(80).stop();
            expect(pojazd.status().predkosc).to.equal(0);
            expect(pojazd.status().wRuchu).to.equal(false);
        });

        it('nie pozwala przekroczyc predkosci maksymalnej', function () {
            expect(function () { pojazd.start(121); }).to.throw(RangeError);
            expect(pojazd.status().predkosc).to.equal(0);
        });

        it('odrzuca niepoprawna predkosc', function () {
            expect(function () { pojazd.start(-5); }).to.throw(RangeError);
            expect(function () { pojazd.start('szybko'); }).to.throw(RangeError);
        });

        it('wymaga wywolania przez new', function () {
            expect(function () { Pojazd('A', 10); }).to.throw(TypeError);
        });

        it('kazdy obiekt ma wlasne kopie metod', function () {
            var inny = new Pojazd('B', 50);
            expect(pojazd.start).to.not.equal(inny.start);
            expect(pojazd).to.have.own.property('start');
        });
    });

    describe('pola prywatne', function () {
        it('nie da sie odczytac pol spoza obiektu', function () {
            expect(pojazd.id).to.be.undefined;
            expect(pojazd.predkosc).to.be.undefined;
            expect(pojazd.maxPredkosc).to.be.undefined;
            expect(Object.keys(pojazd)).to.have.members(['status', 'start', 'stop']);
        });

        it('zapis "pola" z zewnatrz nie zmienia stanu wewnetrznego', function () {
            pojazd.predkosc = 999;
            pojazd.id = 'HACK';
            expect(pojazd.status().predkosc).to.equal(0);
            expect(pojazd.status().id).to.equal('WX-1234');
            // powstala tylko nowa, niezwiazana wlasciwosc publiczna
            expect(pojazd.predkosc).to.equal(999);
        });

        it('zmiana obiektu zwroconego przez status nie zmienia stanu', function () {
            var s = pojazd.status();
            s.predkosc = 500;
            expect(pojazd.status().predkosc).to.equal(0);
        });

        it('stan jest niezalezny dla roznych obiektow', function () {
            var inny = new Pojazd('B', 50);
            pojazd.start(100);
            expect(inny.status().predkosc).to.equal(0);
        });
    });
});
