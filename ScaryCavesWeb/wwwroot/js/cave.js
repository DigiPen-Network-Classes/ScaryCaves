// The cave page's only script. htmx does the swapping; this keeps the message list
// short and gets the player somewhere sensible when the live stream goes away.
(function () {
    "use strict";

    var MAX_MESSAGES = 10;
    var closed = false;

    function trim() {
        var messages = document.getElementById("messages");
        while (messages && messages.children.length > MAX_MESSAGES) {
            messages.removeChild(messages.lastElementChild);
        }
    }

    document.body.addEventListener("htmx:sseMessage", trim);
    document.body.addEventListener("htmx:afterSwap", trim);

    // "superseded": the cave is open in a newer tab, which now owns the session.
    document.body.addEventListener("htmx:sseClose", function (e) {
        if (e.detail.type !== "message") {
            return;
        }
        closed = true;
        var panel = document.getElementById("room-panel");
        if (panel) {
            panel.innerHTML = "<p>The Scary Cave is open in another window.</p>";
        }
    });

    // Lost the stream (server restart, or the login expired): reload, and if the login
    // has gone the server sends us to the login page.
    document.body.addEventListener("htmx:sseError", function () {
        if (closed) {
            return;
        }
        window.setTimeout(function () {
            if (!closed) {
                location.reload();
            }
        }, 2000);
    });
})();
