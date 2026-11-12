'use strict';

var expect = require('chai').expect;
var Pojazd = require('../src/PojazdPrototyp');

describe('Pojazd (prototyp)', function () {
    var pojazd;

    beforeEach(function () {
        pojazd = new Pojazd('GD-777', 200);
    });

    describe('interfejs', function () {
        it('nowy pojazd stoi', function () {
            expect(pojazd.status()).to.eql({ id: 'GD-777', maxPredkosc: 200, predkosc: 0, wRuchu: false });
        });

        it('start i stop zmieniaja predkosc', function () {
            pojazd.start(150);
            expect(pojazd.status().predkosc).to.equal(150);
            pojazd.stop();
            expect(pojazd.status().predkosc).to.equal(0);
        });

        it('nie pozwala przekroczyc predkosci maksymalnej', function () {
            expect(function () { pojazd.start(201); }).to.throw(RangeError);
        });
    });

    describe('pola publiczne', function () {
        it('pola sa dostepne do odczytu', function () {
            expect(pojazd.id).to.equal('GD-777');
            expect(pojazd.max_predkosc).to.equal(200);
            expect(pojazd.predkosc).to.equal(0);
        });

        it('pola sa dostepne do zapisu i zmieniaja stan obiektu', function () {
            pojazd.predkosc = 999;
            pojazd.max_predkosc = 1000;
            expect(pojazd.status().predkosc).to.equal(999);
            // ominiecie walidacji - brak enkapsulacji
            expect(function () { pojazd.start(1000); }).to.not.throw();
        });
    });

    describe('prototype / constructor / __proto__', function () {
        it('metody leza w prototypie, a nie w obiekcie', function () {
            expect(pojazd).to.not.have.own.property('start');
            expect(Pojazd.prototype).to.have.own.property('start');
            expect(pojazd.start).to.equal(new Pojazd('X', 1).start);
        });

        it('obiekt nie ma wlasciwosci prototype, ma ja konstruktor', function () {
            expect(pojazd.prototype).to.be.undefined;
            expect(Pojazd.prototype).to.be.an('object');
        });

        it('constructor wskazuje na funkcje Pojazd', function () {
            expect(Pojazd.prototype.constructor).to.equal(Pojazd);
            expect(pojazd.constructor).to.equal(Pojazd);
            expect(pojazd).to.not.have.own.property('constructor');
        });

        it('__proto__ obiektu to prototyp konstruktora', function () {
            expect(pojazd.__proto__).to.equal(Pojazd.prototype);
            expect(Object.getPrototypeOf(pojazd)).to.equal(Pojazd.prototype);
            expect(Object.getPrototypeOf(Pojazd.prototype)).to.equal(Object.prototype);
            expect(pojazd instanceof Pojazd).to.equal(true);
        });
    });

    describe('modyfikacja prototypu po utworzeniu obiektu', function () {
        var oryginalnyPrototyp = Pojazd.prototype;

        afterEach(function () {
            delete oryginalnyPrototyp.trabka;
            Pojazd.prototype = oryginalnyPrototyp;
        });

        it('funkcja dodana do prototypu jest widoczna w istniejacych obiektach', function () {
            expect(pojazd.trabka).to.be.undefined;
            Pojazd.prototype.trabka = function () { return this.id + ': bip bip'; };
            expect(pojazd.trabka()).to.equal('GD-777: bip bip');
        });

        it('nadpisanie prototype nie wplywa na istniejace obiekty', function () {
            Pojazd.prototype = {
                status: function () { return 'nowy prototyp'; }
            };
            var nowy = new Pojazd('N', 10);

            expect(pojazd.status().id).to.equal('GD-777');
            expect(pojazd instanceof Pojazd).to.equal(false);

            expect(nowy.status()).to.equal('nowy prototyp');
            expect(nowy.start).to.be.undefined;
            expect(nowy instanceof Pojazd).to.equal(true);
            // nowy prototyp nie ma wlasnego constructor - dziedziczony z Object.prototype
            expect(nowy.constructor).to.equal(Object);
        });

        it('wlasciwosc obiektu przeslania metode z prototypu', function () {
            pojazd.status = function () { return 'przeslonieta'; };
            expect(pojazd.status()).to.equal('przeslonieta');
            expect(new Pojazd('Y', 5).status().id).to.equal('Y');
        });
    });
});
