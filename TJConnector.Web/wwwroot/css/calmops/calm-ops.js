/* ============================================================================
   CALM OPS — calm-ops.js
   Shared client-side primitives for the Changer suite (Changer, Reprocessor2,
   TJConnector). Keep this file IDENTICAL across the three apps' wwwroot copies:
     - Reprocessor2.WebServer/wwwroot/calmops/calm-ops.js
     - TJConnector.Web/wwwroot/css/calmops/calm-ops.js   (lives next to the css)
     - Changer/wwwroot/js/calm-ops.js
   Canonical source of truth: H:/CO/calm-ops/calm-ops.js

   PRIMITIVE: window.coFilters — persisted filter state per page+user.
   - localStorage variant  (load / save / remove)      → for interactive Blazor
     apps where a flash on first render is a non-issue (Reprocessor2).
   - cookie variant         (loadCookie / saveCookie …) → a small per-page scoped
     cookie for the prerendered / cookie-auth apps (Changer, TJ).

   Caveats (by design, accepted in WORKORDER A2):
     * Per-browser, not per-account (the user key only namespaces the storage).
     * Never store sensitive values here — filter selections only.
     * Deep-link shareability is lost (the URL no longer carries filter state).
   ============================================================================ */
(function () {
    "use strict";

    function safeKey(page, user) {
        // Namespace by page + user so two operators on one browser don't collide,
        // and two pages don't stomp each other.
        return "cofilters:" + (page || "page") + ":" + (user || "anon");
    }

    var coFilters = {
        // ----- localStorage variant (Reprocessor2) -----------------------------
        // Returns the stored JSON string, or null if absent / unavailable.
        load: function (page, user) {
            try {
                return window.localStorage.getItem(safeKey(page, user));
            } catch (e) {
                return null;
            }
        },
        save: function (page, user, json) {
            try {
                if (json === null || json === undefined || json === "") {
                    window.localStorage.removeItem(safeKey(page, user));
                } else {
                    window.localStorage.setItem(safeKey(page, user), json);
                }
            } catch (e) { /* storage blocked / full — persistence is best-effort */ }
        },
        remove: function (page, user) {
            try { window.localStorage.removeItem(safeKey(page, user)); }
            catch (e) { /* ignore */ }
        },

        // ----- cookie variant (Changer, TJ) ------------------------------------
        // Small, per-page scoped cookie. SameSite=Lax, path scoped to the current
        // origin root, ~180 day lifetime. Value is URI-encoded JSON.
        loadCookie: function (page, user) {
            try {
                var name = safeKey(page, user) + "=";
                var parts = (document.cookie || "").split(";");
                for (var i = 0; i < parts.length; i++) {
                    var c = parts[i].trim();
                    if (c.indexOf(name) === 0) {
                        return decodeURIComponent(c.substring(name.length));
                    }
                }
                return null;
            } catch (e) {
                return null;
            }
        },
        saveCookie: function (page, user, json) {
            try {
                var key = safeKey(page, user);
                if (json === null || json === undefined || json === "") {
                    document.cookie = key + "=; Max-Age=0; Path=/; SameSite=Lax";
                    return;
                }
                var maxAge = 60 * 60 * 24 * 180; // 180 days
                document.cookie = key + "=" + encodeURIComponent(json) +
                    "; Max-Age=" + maxAge + "; Path=/; SameSite=Lax";
            } catch (e) { /* ignore */ }
        },
        removeCookie: function (page, user) {
            try {
                document.cookie = safeKey(page, user) + "=; Max-Age=0; Path=/; SameSite=Lax";
            } catch (e) { /* ignore */ }
        }
    };

    window.coFilters = coFilters;
})();

/* ============================================================================
   PRIMITIVE: window.coCopy — right-click → Copy on any cell/value (WORKORDER A6)

   Usage (markup only, no per-cell interop): give the element class `co-copy-cell`
   and a `data-co-copy` attribute holding the exact text to copy. Example:
       <span class="co-copy-cell" data-co-copy="@gtin">@gtin</span>
   A single delegated `contextmenu` listener (added ONCE on window) catches the
   right-click, suppresses the native menu, and shows the CalmOps `.co-ctx-menu`
   with a single "Copy" item that writes the value to the clipboard.

   Why delegation: works identically for SSR (Changer) and interactive Blazor
   (Reprocessor2 / TJ) — no JSInterop round-trip per cell, and dynamically
   rendered / expanded rows are covered automatically (no re-wiring on render).

   RULE (matches the React reference CopyCell): only put `.co-copy-cell` on a
   STATIC cell or a cell inside an expanded detail panel — never on a cell whose
   row is itself clickable/expandable (the row's click would compete). When the
   value may be null/empty, omit the class+attr so there's nothing to copy.
   ============================================================================ */
(function () {
    "use strict";
    if (window.coCopy && window.coCopy.__installed) return;

    var current = null; // { backdrop, menu } when a menu is open

    function close() {
        if (!current) return;
        if (current.backdrop && current.backdrop.parentNode) current.backdrop.parentNode.removeChild(current.backdrop);
        if (current.menu && current.menu.parentNode) current.menu.parentNode.removeChild(current.menu);
        current = null;
    }

    function flash(msg) {
        // Best-effort, dependency-free confirmation. Uses the CalmOps toast look
        // if a host exists; otherwise a transient bare .co-toast.
        try {
            var host = document.querySelector(".co-toast-host, .toast-container");
            var t = document.createElement("div");
            t.className = "co-toast co-toast-success";
            t.textContent = msg;
            if (host) { host.appendChild(t); } else { document.body.appendChild(t); }
            setTimeout(function () { if (t.parentNode) t.parentNode.removeChild(t); }, 1400);
        } catch (e) { /* ignore */ }
    }

    function copyText(text) {
        try {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                return navigator.clipboard.writeText(text);
            }
        } catch (e) { /* fall through to legacy */ }
        // Legacy fallback (older / non-secure contexts).
        try {
            var ta = document.createElement("textarea");
            ta.value = text;
            ta.style.position = "fixed";
            ta.style.opacity = "0";
            document.body.appendChild(ta);
            ta.select();
            document.execCommand("copy");
            document.body.removeChild(ta);
        } catch (e) { /* ignore */ }
        return Promise.resolve();
    }

    function openMenu(x, y, value) {
        close();
        var backdrop = document.createElement("div");
        backdrop.className = "co-ctx-backdrop";
        backdrop.addEventListener("click", close);
        backdrop.addEventListener("contextmenu", function (e) { e.preventDefault(); close(); });

        var menu = document.createElement("div");
        menu.className = "co-ctx-menu";

        var btn = document.createElement("button");
        btn.type = "button";
        btn.className = "co-ctx-item";
        btn.innerHTML = '<span aria-hidden="true">⧉</span> Copy';
        btn.addEventListener("click", function () {
            var v = String(value == null ? "" : value);
            Promise.resolve(copyText(v)).then(function () { flash("Copied"); });
            close();
        });
        menu.appendChild(btn);

        document.body.appendChild(backdrop);
        document.body.appendChild(menu);

        // Position, clamping to the viewport so the menu never overflows.
        var w = menu.offsetWidth || 140, h = menu.offsetHeight || 40;
        var left = Math.min(x, window.innerWidth - w - 6);
        var top = Math.min(y, window.innerHeight - h - 6);
        menu.style.left = Math.max(4, left) + "px";
        menu.style.top = Math.max(4, top) + "px";

        current = { backdrop: backdrop, menu: menu };
    }

    document.addEventListener("contextmenu", function (e) {
        var cell = e.target.closest ? e.target.closest(".co-copy-cell") : null;
        if (!cell) return;
        e.preventDefault();
        var value = cell.getAttribute("data-co-copy");
        if (value === null) value = cell.textContent;
        openMenu(e.clientX, e.clientY, value);
    });

    // Dismiss on Escape / scroll / resize for robustness.
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") close(); });
    window.addEventListener("scroll", close, true);
    window.addEventListener("resize", close);

    window.coCopy = { __installed: true, copy: copyText, close: close };
})();
