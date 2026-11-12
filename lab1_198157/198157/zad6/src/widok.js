'use strict';

// Renderowanie tabeli - ten modul jest podmieniany przez HMR bez przeladowania strony.
function renderuj(biblioteka, ksiazki) {
    const wiersze = ksiazki.map(k => {
        const kto = biblioteka.kto_wypozyczyl(k.id);
        return `<tr class="${kto ? 'wypozyczona' : ''}">
            <td>${k.tytul}</td>
            <td>${k.autor}</td>
            <td>${k.slowa_kluczowe.join(', ')}</td>
            <td>${kto ? `wypozyczyl: ${kto}` : 'dostepna'}</td>
            <td><button data-id="${k.id}">${kto ? 'Zwroc' : 'Wypozycz'}</button></td>
        </tr>`;
    });
    return `<table>
        <thead><tr><th>Tytul</th><th>Autor</th><th>Slowa kluczowe</th><th>Status</th><th></th></tr></thead>
        <tbody>${wiersze.join('')}</tbody>
    </table>`;
}

module.exports = { renderuj };
