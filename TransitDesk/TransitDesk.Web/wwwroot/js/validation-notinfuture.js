// Kliencka połowa atrybutu [NotInFuture]: serwer renderuje data-val-notinfuture="komunikat"
// oraz data-val-notinfuture-skew="5"; tutaj uczymy jQuery Validate, jak tę regułę sprawdzić.
(function ($) {
    $.validator.addMethod("notinfuture", function (value, element, skewMinutes) {
        if (this.optional(element)) {
            return true;
        }
        // <input type="datetime-local"> zwraca "yyyy-MM-ddTHH:mm" – interpretowane jako czas lokalny.
        var date = new Date(value);
        if (isNaN(date.getTime())) {
            return false;
        }
        return date.getTime() <= Date.now() + skewMinutes * 60 * 1000;
    });

    $.validator.unobtrusive.adapters.add("notinfuture", ["skew"], function (options) {
        options.rules.notinfuture = parseInt(options.params.skew, 10) || 0;
        options.messages.notinfuture = options.message;
    });
})(jQuery);
