'use strict';

require('./style.css');
const Ksiazka = require('./model/Ksiazka');
const Biblioteka = require('./model/Biblioteka');
let { renderuj } = require('./widok');

// stan aplikacji przezywa podmiane modulu widok.js przez HMR
const biblioteka = new Biblioteka('Miejska').dodaj(
    new Ksiazka({ autor: 'Stanislaw Lem', tytul: 'Solaris', cena: 40 }, 'sci-fi', 'kosmos', 'klasyka'),
    new Ksiazka({ autor: 'Andrzej Sapkowski', tytul: 'Ostatnie zyczenie', cena: 35 }, 'fantasy', 'klasyka'),
    new Ksiazka({ autor: 'Frank Herbert', tytul: 'Diuna', cena: 55 }, 'sci-fi', 'pustynia')
);

const app = document.getElementById('app');
const pole = document.getElementById('slowa');
let licznikRenderow = 0;

function odswiez() {
    const slowa = pole.value.split(/\s+/).filter(Boolean);
    const ksiazki = slowa.length ? biblioteka.szukaj(...slowa) : biblioteka.ksiazki;
    app.innerHTML = renderuj(biblioteka, ksiazki);
    document.getElementById('licznik').textContent =
        `Wypozyczen i zwrotow: ${biblioteka.lista_wypozyczen.length}, renderow: ${++licznikRenderow}`;
}

document.getElementById('szukaj').addEventListener('submit', e => {
    e.preventDefault();
    odswiez();
});

app.addEventListener('click', e => {
    const id = Number(e.target.dataset?.id);
    if (!id) {
        return;
    }
    if (biblioteka.kto_wypozyczyl(id)) {
        biblioteka.zwroc(id);
    } else {
        biblioteka.wypozycz(id, 'Czytelnik');
    }
    odswiez();
});

odswiez();

if (module.hot) {
    module.hot.accept('./widok', () => {
        ({ renderuj } = require('./widok'));
        odswiez();
    });
}
