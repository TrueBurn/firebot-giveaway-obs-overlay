// Silent self-healing for the OBS overlay page.
// Blazor raises "components-reconnect-state-changed" on #components-reconnect-modal.
(function () {
    const el = document.getElementById('components-reconnect-modal');
    if (!el || el.dataset.bound) return;
    el.dataset.bound = '1';

    let polling = false;

    // Reload only once the server answers, otherwise OBS would be left showing a browser error page.
    async function reloadWhenServerIsUp() {
        if (polling) return;
        polling = true;
        for (;;) {
            try {
                const res = await fetch(location.href, { method: 'HEAD', cache: 'no-store' });
                if (res.ok) {
                    location.reload();
                    return;
                }
            } catch {
                // server still down
            }
            await new Promise(r => setTimeout(r, 3000));
        }
    }

    el.addEventListener('components-reconnect-state-changed', (e) => {
        const state = e.detail && e.detail.state;
        if (state === 'rejected' || state === 'failed') {
            // rejected: server restarted and no longer knows this circuit
            // failed: retries exhausted
            reloadWhenServerIsUp();
        }
    });

    // Unhandled circuit error: Blazor reveals #blazor-error-ui (style display:block). Recover by reloading.
    const errorUi = document.getElementById('blazor-error-ui');
    if (errorUi) {
        new MutationObserver(() => {
            if (errorUi.style.display && errorUi.style.display !== 'none') {
                setTimeout(reloadWhenServerIsUp, 2000);
            }
        }).observe(errorUi, { attributes: true, attributeFilter: ['style'] });
    }
})();
