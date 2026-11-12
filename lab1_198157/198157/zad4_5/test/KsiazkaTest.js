'use strict';

const { expect } = require('chai');
const Ksiazka = require('../src/Ksiazka');

describe('Ksiazka', () => {
    const nowa = (dane = {}, ...slowa) =>
        new Ksiazka({ autor: 'Stanislaw Lem', tytul: 'Solaris', cena: 39.9, wydawnictwo: 'Wydawnictwo Literackie', ...dane }, ...slowa);

    it('przechowuje atrybuty', () => {
        const k = nowa({}, 'Sci-Fi', ' kosmos ');
        expect(k).to.include({ autor: 'Stanislaw Lem', tytul: 'Solaris', cena: 39.9, wydawnictwo: 'Wydawnictwo Literackie' });
        expect(k.slowa_kluczowe).to.eql(['sci-fi', 'kosmos']);
    });

    it('stosuje wartosci domyslne', () => {
        const k = new Ksiazka({ autor: 'A', tytul: 'B' });
        expect(k.cena).to.equal(0);
        expect(k.wydawnictwo).to.equal('nieznane');
        expect(k.slowa_kluczowe).to.eql([]);
    });

    it('wymaga autora i tytulu', () => {
        expect(() => new Ksiazka({ autor: 'A' })).to.throw('autora i tytulu');
    });

    it('nadaje unikalne id tylko do odczytu', () => {
        const a = nowa(), b = nowa();
        expect(a.id).to.not.equal(b.id);
        expect(() => { a.id = 100; }).to.throw(TypeError);
    });

    it('waliduje cene przez setter', () => {
        const k = nowa();
        expect(() => { k.cena = -1; }).to.throw(RangeError);
        k.cena = 10;
        expect(k.cena).to.equal(10);
    });

    it('ukrywa pola prywatne', () => {
        expect(Object.keys(nowa())).to.not.include('#cena');
        expect(nowa().formatujCene).to.be.undefined;
    });

    it('formatuje opis', () => {
        expect(nowa().toString()).to.equal('"Solaris" - Stanislaw Lem (Wydawnictwo Literackie), 39.90 zl');
        expect(nowa({ cena: 2_500 }).toString()).to.contain('2.5 tys. zl');
    });

    it('tworzy slug z tytulu', () => {
        expect(nowa({ tytul: 'Cyberiada i inne opowiadania' }).slug).to.equal('cyberiada-i-inne-opowiadania');
    });

    it('sprawdza slowa kluczowe bez wzgledu na wielkosc liter', () => {
        const k = nowa({}, 'Kosmos');
        expect(k.maSlowo('KOSMOS')).to.equal(true);
        expect(k.maSlowo('fantasy')).to.equal(false);
    });
});
