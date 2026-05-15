// ── Copy to clipboard (code blocks) ─────────────────────────────
document.addEventListener('click', e => {
  const btn = e.target.closest('.code-header .copy-btn');
  if (!btn) return;
  const selector = btn.dataset.copy;
  let text;
  if (selector && selector.startsWith('#')) {
    const el = document.querySelector(selector);
    text = el?.textContent;
  } else if (selector) {
    text = selector; // plain string
  } else {
    const block = btn.closest('.code-block');
    text = block?.querySelector('pre')?.textContent;
  }
  if (!text) return;
  navigator.clipboard.writeText(text.trim()).then(() => {
    const orig = btn.textContent;
    btn.textContent = 'Copied!';
    btn.style.color = 'var(--accent-green)';
    btn.style.borderColor = 'var(--accent-green)';
    setTimeout(() => {
      btn.textContent = orig;
      btn.style.color = '';
      btn.style.borderColor = '';
    }, 2000);
  });
});

// ── Install box copy ────────────────────────────────────────────
document.addEventListener('click', e => {
  const box = e.target.closest('.install-box');
  if (!box || e.target.closest('.code-header')) return;
  const code = box.querySelector('code');
  if (!code) return;
  navigator.clipboard.writeText(code.textContent.trim()).then(() => {
    const btn = box.querySelector('.copy-btn');
    if (btn) {
      btn.textContent = 'Copied!';
      setTimeout(() => btn.innerHTML = '&#128203;', 2000);
    }
  });
});

// ── Tab switching ───────────────────────────────────────────────
document.querySelectorAll('.tab-bar').forEach(bar => {
  bar.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const group = bar.dataset.tabGroup;
      const target = btn.dataset.tab;

      bar.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      document.querySelectorAll(`.tab-panel[data-tab-group="${group}"]`).forEach(p => {
        p.classList.toggle('active', p.dataset.tab === target);
      });
    });
  });
});

// ── Sink card switching ─────────────────────────────────────────
document.querySelectorAll('.sink-card[data-sink]').forEach(card => {
  card.addEventListener('click', () => {
    const sink = card.dataset.sink;
    document.querySelectorAll('.sink-card[data-sink]').forEach(c => c.classList.remove('active'));
    document.querySelectorAll('.sink-snippet[data-sink]').forEach(s => s.classList.remove('active'));
    card.classList.add('active');
    const snippet = document.querySelector(`.sink-snippet[data-sink="${sink}"]`);
    if (snippet) snippet.classList.add('active');
  });
});

// ── Playground ──────────────────────────────────────────────────
const playgroundRun = document.getElementById('playground-run');
const playgroundEditor = document.getElementById('playground-editor');
const playgroundOutput = document.getElementById('playground-output');

function simulatePlayground() {
  if (!playgroundEditor || !playgroundOutput) return;
  let config;
  try {
    config = JSON.parse(playgroundEditor.value);
  } catch {
    playgroundOutput.innerHTML = '<span style="color:var(--accent-red)">Invalid JSON — fix the configuration and try again.</span>';
    return;
  }

  const routes = config.Routes || [];
  const appName = config.ApplicationName || 'MyApp';
  const levels = [
    { name: 'Verbose',     color: 'var(--text-muted)' },
    { name: 'Debug',       color: 'var(--text-muted)' },
    { name: 'Information', color: 'var(--accent-green)' },
    { name: 'Warning',     color: 'var(--accent-orange)' },
    { name: 'Error',       color: 'var(--accent-red)' },
    { name: 'Fatal',       color: '#ff6b6b' }
  ];

  const now = new Date().toISOString().slice(0, 19);
  let output = `<span style="color:var(--text-muted)">[${now}] ${appName} — LoggerHelper v5 initialized</span>\n`;
  output += `<span style="color:var(--text-muted)">Routes configured: ${routes.length}</span>\n\n`;

  levels.forEach(level => {
    const matchingSinks = routes
      .filter(r => r.Levels && r.Levels.includes(level.name))
      .map(r => r.Sink);

    if (matchingSinks.length > 0) {
      output += `<span style="color:${level.color}">[${level.name.padEnd(11)}]</span> "Sample ${level.name.toLowerCase()} message" `;
      output += `<span style="color:var(--accent)">→ ${matchingSinks.join(', ')}</span>\n`;
    } else {
      output += `<span style="color:var(--text-muted)">[${level.name.padEnd(11)}] (no route — message dropped)</span>\n`;
    }
  });

  output += `\n<span style="color:var(--accent-green)">✓ Routing simulation complete</span>`;
  playgroundOutput.innerHTML = output;
}

if (playgroundRun) {
  playgroundRun.addEventListener('click', simulatePlayground);
}

if (playgroundEditor) {
  playgroundEditor.addEventListener('keydown', e => {
    if (e.ctrlKey && e.key === 'Enter') {
      e.preventDefault();
      simulatePlayground();
    }
  });
  // Run on load
  simulatePlayground();
}

// ── Smooth scroll for nav links ─────────────────────────────────
document.querySelectorAll('.nav a[href^="#"]').forEach(a => {
  a.addEventListener('click', e => {
    e.preventDefault();
    const target = document.querySelector(a.getAttribute('href'));
    if (target) target.scrollIntoView({ behavior: 'smooth' });
    // Close mobile menu
    document.querySelector('.nav-links')?.classList.remove('open');
  });
});

// ── Active nav link on scroll ───────────────────────────────────
const sections = document.querySelectorAll('section[id]');
const navLinks = document.querySelectorAll('.nav-links a[href^="#"]');

window.addEventListener('scroll', () => {
  let current = '';
  sections.forEach(section => {
    if (window.scrollY >= section.offsetTop - 120) current = section.id;
  });
  navLinks.forEach(link => {
    link.style.color = link.getAttribute('href') === `#${current}` ? 'var(--accent)' : '';
  });
}, { passive: true });

// ── Hamburger menu ──────────────────────────────────────────────
document.querySelector('.hamburger')?.addEventListener('click', () => {
  document.querySelector('.nav-links')?.classList.toggle('open');
});
