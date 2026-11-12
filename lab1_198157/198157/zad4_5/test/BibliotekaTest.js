'use strict';

const { expect } = require('chai');
const Ksiazka = require('../src/Ksiazka');
const Biblioteka = require('../src/Biblioteka');

describe('Biblioteka', () => {
    let biblioteka, solaris, wiedzmin, diuna;

    beforeEach(() => {
        solaris = new Ksiazka({ autor: 'Stanislaw Lem', tytul: 'Solaris', cena: 40 }, 'sci-fi', 'kosmos', 'klasyka');
        wiedzmin = new Ksiazka({ autor: 'Andrzej Sapkowski', tytul: 'Ostatnie zyczenie', cena: 35 }, 'fantasy', 'klasyka');
        diuna = new Ksiazka({ autor: 'Frank Herbert', tytul: 'Diuna', cena: 55 }, 'sci-fi', 'pustynia');
        biblioteka = new Biblioteka('Miejska').dodaj(solaris, wiedzmin, diuna);
    });

    describe('dodawanie', () => {
        it('dodaje wiele ksiazek naraz', () => {
            expect(biblioteka.ksiazki).to.have.lengthOf(3);
        });

        it('odrzuca obiekty niebedace ksiazka', () => {
            expect(() => biblioteka.dodaj({ tytul: 'X' })).to.throw(TypeError);
        });

        it('ma domyslna nazwe', () => {
            expect(new Biblioteka().nazwa).to.equal('Biblioteka');
        });
    });

    describe('wypozycz / zwroc / kto_wypozyczyl', () => {
        it('wypozycza ksiazke i zapisuje to na liscie', () => {
            const data = new Date(2026, 9, 1);
            biblioteka.wypozycz(solaris.id, 'Jan', data);
            expect(biblioteka.kto_wypozyczyl(solaris.id)).to.equal('Jan');
            expect(biblioteka.lista_wypozyczen).to.eql([{ typ: 'wypozyczenie', idKsiazki: solaris.id, czytelnik: 'Jan', data }]);
            expect(biblioteka.dostepne).to.have.members([wiedzmin, diuna]);
        });

        it('nie pozwala wypozyczyc tej samej ksiazki dwa razy', () => {
            biblioteka.wypozycz(solaris.id, 'Jan');
            expect(() => biblioteka.wypozycz(solaris.id, 'Anna')).to.throw('wypozyczona przez Jan');
        });

        it('zwraca ksiazke i zapisuje zwrot', () => {
            biblioteka.wypozycz(solaris.id, 'Jan');
            const wpis = biblioteka.zwroc(solaris.id);
            expect(wpis).to.include({ typ: 'zwrot', czytelnik: 'Jan' });
            expect(biblioteka.kto_wypozyczyl(solaris.id)).to.be.null;
            expect(biblioteka.lista_wypozyczen.map(w => w.typ)).to.eql(['wypozyczenie', 'zwrot']);
            expect(biblioteka.dostepne).to.have.lengthOf(3);
        });

        it('nie pozwala zwrocic niewypozyczonej ksiazki', () => {
            expect(() => biblioteka.zwroc(diuna.id)).to.throw('nie jest wypozyczona');
        });

        it('zglasza blad dla nieznanej ksiazki', () => {
            expect(() => biblioteka.wypozycz(-1, 'Jan')).to.throw('brak ksiazki');
            expect(() => biblioteka.zwroc(-1)).to.throw('brak ksiazki');
        });

        it('kto_wypozyczyl zwraca null dla dostepnej lub nieznanej ksiazki', () => {
            expect(biblioteka.kto_wypozyczyl(diuna.id)).to.be.null;
            expect(biblioteka.kto_wypozyczyl(12345)).to.be.null;
        });

        it('zwraca historie czytelnika', () => {
            biblioteka.wypozycz(solaris.id, 'Jan');
            biblioteka.wypozycz(diuna.id, 'Anna');
            biblioteka.zwroc(solaris.id);
            expect(biblioteka.historia('Jan')).to.have.lengthOf(2);
            expect(biblioteka.historia('Anna')).to.have.lengthOf(1);
        });
    });

    describe('szukaj', () => {
        it('znajduje po jednym slowie', () => {
            expect(biblioteka.szukaj('sci-fi')).to.have.members([solaris, diuna]);
        });

        it('wymaga wszystkich podanych slow', () => {
            expect(biblioteka.szukaj('sci-fi', 'klasyka')).to.eql([solaris]);
        });

        it('ignoruje wielkosc liter', () => {
            expect(biblioteka.szukaj('FANTASY')).to.eql([wiedzmin]);
        });

        it('zwraca pusta liste gdy brak wynikow lub slow', () => {
            expect(biblioteka.szukaj('romans')).to.eql([]);
            expect(biblioteka.szukaj()).to.eql([]);
        });
    });

    describe('pozostale konstrukcje jezyka', () => {
        it('funkcja strzalkowa zachowuje this, zwykla funkcja nie', () => {
            expect(biblioteka.raportStrzalka()[0]).to.equal('Miejska: Solaris');
            expect(biblioteka.raportFunction()[0]).to.equal('undefined: Solaris');
        });

        it('liczy wartosc ksiegozbioru', () => {
            expect(biblioteka.wartoscKsiegozbioru()).to.equal(130);
        });

        it('zmienne let/const maja zasieg bloku', () => {
            const wyniki = [];
            for (let i = 0; i < 3; i++) {
                wyniki.push(() => i);
            }
            expect(wyniki.map(f => f())).to.eql([0, 1, 2]);

            const wynikiVar = [];
            for (var j = 0; j < 3; j++) {
                wynikiVar.push(() => j);
            }
            expect(wynikiVar.map(f => f())).to.eql([3, 3, 3]);
        });
    });
});
