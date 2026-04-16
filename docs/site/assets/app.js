/* CSharpEssentials.LoggerHelper — Site interactivity */

document.addEventListener('DOMContentLoaded', () => {
    initNav();
    initCopyButtons();
    initTabs();
    initPlayground();
    initSinkSelector();
});

/* ── Sticky nav shadow ───────────────────────────── */
function initNav() {
    const nav = document.querySelector('.nav');
    const hamburger = document.querySelector('.hamburger');
    const links = document.querySelector('.nav-links');

    window.addEventListener('scroll', () => {
        nav.classList.toggle('scrolled', window.scrollY > 20);
    });

    if (hamburger) {
        hamburger.addEventListener('click', () => links.classList.toggle('open'));
        document.querySelectorAll('.nav-links a').forEach(a =>
            a.addEventListener('click', () => links.classList.remove('open'))
        );
    }
}

/* ── Copy to clipboard ───────────────────────────── */
function initCopyButtons() {
    document.querySelectorAll('[data-copy]').forEach(btn => {
        btn.addEventListener('click', () => {
            const target = btn.getAttribute('data-copy');
            let text;
            if (target.startsWith('#')) {
                const el = document.querySelector(target);
                text = el?.textContent || el?.value || '';
            } else {
                text = target;
            }
            navigator.clipboard.writeText(text.trim()).then(() => {
                btn.classList.add('copied');
                const prev = btn.innerHTML;
                btn.innerHTML = '&#10003; Copied';
                setTimeout(() => { btn.innerHTML = prev; btn.classList.remove('copied'); }, 2000);
            });
        });
    });
}

/* ── Tab switching ───────────────────────────────── */
function initTabs() {
    document.querySelectorAll('.tab-bar').forEach(bar => {
        const group = bar.getAttribute('data-tab-group');
        bar.querySelectorAll('.tab-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const tabId = btn.getAttribute('data-tab');
                bar.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                document.querySelectorAll(`.tab-panel[data-tab-group="${group}"]`).forEach(p => {
                    p.classList.toggle('active', p.getAttribute('data-tab') === tabId);
                });
            });
        });
    });
}

/* ── Sink selector ───────────────────────────────── */
function initSinkSelector() {
    document.querySelectorAll('.sink-card[data-sink]').forEach(card => {
        card.addEventListener('click', () => {
            const sink = card.getAttribute('data-sink');
            document.querySelectorAll('.sink-card[data-sink]').forEach(c => c.classList.remove('active'));
            card.classList.add('active');
            document.querySelectorAll('.sink-snippet').forEach(s => {
                s.classList.toggle('active', s.getAttribute('data-sink') === sink);
            });
        });
    });
}

/* ── Interactive Playground ──────────────────────── */
function initPlayground() {
    const editor = document.getElementById('playground-editor');
    const output = document.getElementById('playground-output');
    const runBtn = document.getElementById('playground-run');
    if (!editor || !output || !runBtn) return;

    runBtn.addEventListener('click', () => runPlayground(editor, output));
    // Also run on Ctrl+Enter
    editor.addEventListener('keydown', e => {
        if (e.ctrlKey && e.key === 'Enter') {
            e.preventDefault();
            runPlayground(editor, output);
        }
    });

    // Run once on load
    runPlayground(editor, output);
}

function runPlayground(editor, output) {
    let config;
    try {
        config = JSON.parse(editor.value);
    } catch (e) {
        output.innerHTML = `<span class="log-error">[ERR] Invalid JSON: ${escapeHtml(e.message)}</span>`;
        return;
    }

    const appName = config.ApplicationName || 'MyApp';
    const routes = config.Routes || [];
    const lines = [];
    const ts = () => new Date().toISOString().replace('T', ' ').slice(0, 23);

    // Simulate log messages at different levels
    const sampleLogs = [
        { level: 'Debug',       msg: 'Loading configuration from appsettings.json' },
        { level: 'Information', msg: `Application ${appName} started successfully` },
        { level: 'Information', msg: 'Processing request GET /api/orders' },
        { level: 'Warning',     msg: 'Response time exceeded threshold: 1200ms' },
        { level: 'Error',       msg: 'Failed to connect to payment gateway: timeout after 30s' },
        { level: 'Information', msg: 'Order #12345 created for customer Acme Corp' },
        { level: 'Fatal',       msg: 'Unhandled exception in background worker' },
        { level: 'Debug',       msg: 'Cache miss for key user:session:abc123' },
        { level: 'Information', msg: 'Health check completed: all services healthy' },
    ];

    if (routes.length === 0) {
        lines.push(`<span class="log-warn">[WRN] No routes configured — logs won't be routed to any sink</span>`);
    }

    for (const log of sampleLogs) {
        const matchedSinks = [];
        for (const route of routes) {
            const levels = route.Levels || [];
            if (levels.includes(log.level)) {
                matchedSinks.push(route.Sink || '?');
            }
        }

        const levelClass = `log-${log.level.toLowerCase().replace('information', 'info')}`;
        const levelTag = log.level.substring(0, 3).toUpperCase();
        const sinkTag = matchedSinks.length > 0
            ? `<span style="color:#64748b"> → [${matchedSinks.join(', ')}]</span>`
            : `<span style="color:#334155"> → (no matching sink)</span>`;

        lines.push(
            `<span class="${levelClass}">[${ts()}] [${levelTag}]</span> ${escapeHtml(log.msg)}${sinkTag}`
        );
    }

    const routeSummary = routes.map(r =>
        `  ${r.Sink}: ${(r.Levels || []).join(', ')}`
    ).join('\n');

    const header = `<span style="color:#7c3aed">── LoggerHelper v5 Simulation ──</span>
<span style="color:#64748b">App: ${escapeHtml(appName)} | Routes: ${routes.length}</span>
${routeSummary ? `<span style="color:#64748b">${escapeHtml(routeSummary)}</span>` : ''}
<span style="color:#2d2d44">────────────────────────────────</span>
`;

    output.innerHTML = header + lines.join('\n');
}

function escapeHtml(str) {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}
