// reCAPTCHA v3 for the login and register forms: fetch a token, put it in the hidden
// field, then submit the form for real. form.submit() doesn't fire "submit" again.
(function () {
    "use strict";

    var form = document.getElementById("captcha-form");
    if (!form) {
        return;
    }
    form.addEventListener("submit", function (e) {
        e.preventDefault();
        if (typeof grecaptcha === "undefined") {
            alert("Recaptcha not ready");
            return;
        }
        grecaptcha.ready(function () {
            grecaptcha.execute(form.dataset.siteKey, { action: form.dataset.action }).then(function (token) {
                document.getElementById("captcha-token").value = token;
                form.submit();
            });
        });
    });
})();
