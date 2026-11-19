// Sends the antiforgery token with every same-origin POST made through jQuery
// (jquery-ajax-unobtrusive anchors, $.post, etc.).
(function ($) {
    $(document).ajaxSend(function (event, xhr, settings) {
        var token = $('meta[name="request-verification-token"]').attr('content');
        var type = (settings.type || '').toUpperCase();
        if (token && type === 'POST' && !settings.crossDomain) {
            xhr.setRequestHeader('RequestVerificationToken', token);
        }
    });
})(jQuery);
