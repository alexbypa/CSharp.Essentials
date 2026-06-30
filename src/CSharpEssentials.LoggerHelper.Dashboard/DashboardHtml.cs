using System.Data.Common;
using System.Drawing;
using System.Reflection.Metadata;
using System.Security.Cryptography.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace CSharpEssentials.LoggerHelper.Dashboard;

internal static class DashboardHtml {
    internal static string Render(string basePath, int refreshInterval) => $$"""
<!DOCTYPE html>
<html lang="en" data-theme="dark">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1"/>
<meta name="dashboard-refresh" content="{refreshInterval}"/>
<title>LoggerHelper Dashboard</title>
<style>
:root {--bg: #0f1117; --surface: #1a1d27; --surface2: #242836;
  --border: #2e3348; --text: #e4e6f0; --text2: #8b8fa8;
  --accent: #6366f1; --accent2: #818cf8;
  --ok: #22c55e; --warn: #f59e0b; --err: #ef4444;
  --radius: 8px; --shadow: 0 2px 8px rgba(0,0,0,.3);
}
* {margin:0; padding:0; box-sizing:border-box; }
body {font - family: 'Segoe UI',system-ui,-apple-system,sans-serif; background:var(--bg); color:var(--text); line-height:1.5; }
.container {max - width:1400px; margin:0 auto; padding:16px; }

/* Header */
header {display:flex; align-items:center; justify-content:space-between; padding:16px 0; border-bottom:1px solid var(--border); margin-bottom:24px; }
header h1 {font - size:1.5rem; font-weight:600; }
header h1 span {color:var(--accent2); }
.badge {padding:4px 12px; border-radius:12px; font-size:.75rem; font-weight:600; text-transform:uppercase; }
.badge-ok {background:rgba(34,197,94,.15); color:var(--ok); }
.badge-warn {background:rgba(245,158,11,.15); color:var(--warn); }
.badge-err {background:rgba(239,68,68,.15); color:var(--err); }
.badge-active {background:rgba(99,102,241,.15); color:var(--accent2); }

/* Cards Grid */
.cards {display:grid; grid-template-columns:repeat(auto-fit,minmax(200px,1fr)); gap:16px; margin-bottom:24px; }
.card {background:var(--surface); border:1px solid var(--border); border-radius:var(--radius); padding:20px; box-shadow:var(--shadow); }
.card-label {font - size:.75rem; color:var(--text2); text-transform:uppercase; letter-spacing:.05em; margin-bottom:4px; }
.card-value {font - size:1.75rem; font-weight:700; }

/* Panels */
.panels {display:grid; grid-template-columns:1fr 1fr; gap:16px; margin-bottom:24px; }
@media(max-width:900px) { .panels { grid-template-columns:1fr; } }
.panel {background:var(--surface); border:1px solid var(--border); border-radius:var(--radius); overflow:hidden; box-shadow:var(--shadow); }
.panel-header {padding:12px 16px; border-bottom:1px solid var(--border); display:flex; align-items:center; justify-content:space-between; background:var(--surface2); }
.panel-header h2 {font - size:.9rem; font-weight:600; }
.panel-body {padding:16px; max-height:400px; overflow-y:auto; }
.panel-full {grid - column:1/-1; }

/* Table */
table {width:100%; border-collapse:collapse; font-size:.85rem; }
th, td {padding:8px 12px; text-align:left; border-bottom:1px solid var(--border); }
th {color:var(--text2); font-weight:500; font-size:.75rem; text-transform:uppercase; }
tr:hover {background:var(--surface2); }

/* Log stream */
.log-entry {font - family:'Cascadia Code','Fira Code',monospace; font-size:.8rem; padding:4px 8px; border-bottom:1px solid rgba(46,51,72,.5); white-space:pre-wrap; word-break:break-all; }
.log-entry:hover {background:var(--surface2); }
.log-time {color:var(--text2); }
.log-debug {color:#a78bfa; }
.log-info {color:#60a5fa; }
.log-warn {color:var(--warn); }
.log-error {color:var(--err); }
.log-fatal {color:#f87171; font-weight:700; }

/* Error row */
.error-row {cursor:pointer; }
.error-detail {display:none; padding:8px 12px; background:var(--surface2); font-family:monospace; font-size:.8rem; white-space:pre-wrap; color:var(--text2); }
.error-row.open + .error-detail {display:table-row; }

/* Controls */
.controls {display:flex; gap:8px; align-items:center; }
input[type="text"],select {background:var(--surface2); border:1px solid var(--border); color:var(--text); padding:6px 10px; border-radius:4px; font-size:.8rem; }
input[type="text"]:focus,select:focus {outline:none; border-color:var(--accent); }
button {background:var(--accent); color:white; border:none; padding:6px 14px; border-radius:4px; cursor:pointer; font-size:.8rem; font-weight:500; }
button:hover {background:var(--accent2); }
button.btn-sm {padding:3px 8px; font-size:.75rem; }
button.btn-danger {background:var(--err); }
button.btn-outline {background:transparent; border:1px solid var(--border); color:var(--text2); }

/* Toggle */
.toggle {position:relative; width:36px; height:20px; display:inline-block; }
.toggle input {opacity:0; width:0; height:0; }
.toggle .slider {position:absolute; inset:0; background:var(--surface2); border-radius:10px; border:1px solid var(--border); cursor:pointer; transition:.2s; }
.toggle .slider::before {content:''; position:absolute; height:14px; width:14px; left:2px; bottom:2px; background:var(--text2); border-radius:50%; transition:.2s; }
.toggle input:checked + .slider {background:var(--accent); border-color:var(--accent); }
.toggle input:checked + .slider::before {transform:translateX(16px); background:white; }

.footer {text - align:center; padding:24px 0; color:var(--text2); font-size:.8rem; border-top:1px solid var(--border); margin-top:24px; }
.footer a {color:var(--accent2); text-decoration:none; }
</style>
</head>
<body>
<div class="container">
  <header>
    <h1>Logger<span>Helper</span> Dashboard</h1>
    <div>
      <span id="health-badge" class="badge badge-ok">Loading...</span>
      <span id="last-refresh" style="margin-left:12px;font-size:.75rem;color:var(--text2)"></span>
    </div>
  </header>

  <div class="cards" id="cards">
    <div class="card"><div class="card-label">Health</div><div class="card-value" id="c-health">&mdash;</div></div>
    <div class="card"><div class="card-label">Active Sinks</div><div class="card-value" id="c-sinks">&mdash;</div></div>
    <div class="card"><div class="card-label">Errors</div><div class="card-value" id="c-errors">&mdash;</div></div>
    <div class="card"><div class="card-label">Buffer</div><div class="card-value" id="c-buffer">&mdash;</div></div>
  </div>

  <div class="panels">
    <div class="panel">
      <div class="panel-header"><h2>Sinks</h2></div>
      <div class="panel-body"><table><thead><tr><th>Sink</th><th>Status</th><th>Levels</th><th>Toggle</th></tr></thead><tbody id="sinks-body"></tbody></table></div>
    </div>
    <div class="panel">
      <div class="panel-header"><h2>Routing Configuration</h2></div>
      <div class="panel-body"><table><thead><tr><th>Sink</th><th>Levels</th></tr></thead><tbody id="routes-body"></tbody></table></div>
    </div>
  </div>

  <div class="panels">
    <div class="panel panel-full">
      <div class="panel-header">
        <h2>Error History</h2>
        <span id="error-count" style="font-size:.8rem;color:var(--text2)"></span>
      </div>
      <div class="panel-body"><table><thead><tr><th>Time</th><th>Sink</th><th>Message</th></tr></thead><tbody id="errors-body"></tbody></table></div>
    </div>
  </div>

  <div class="panels" id="context-panel" style="display:none">
    <div class="panel panel-full">
      <div class="panel-header" style="background:rgba(239,68,68,.08)">
        <h2 style="color:var(--err)">&#9888; Context Before Error</h2>
        <span id="context-flush-time" style="font-size:.8rem;color:var(--text2)"></span>
      </div>
      <div class="panel-body" id="context-body" style="max-height:400px;font-family:monospace;"></div>
    </div>
  </div>

  <div class="panels">
    <div class="panel panel-full">
      <div class="panel-header">
        <h2>Live Log Stream</h2>
        <div class="controls">
          <select id="log-level-filter"><option value="">All Levels</option><option>Debug</option><option>Information</option><option>Warning</option><option>Error</option><option>Fatal</option></select>
          <input type="text" id="log-query" placeholder="Filter logs..." style="width:200px"/>
          <button class="btn-outline btn-sm" onclick="clearLogs()">Clear</button>
          <label class="toggle"><input type="checkbox" id="stream-toggle" checked/><span class="slider"></span></label>
          <span style="font-size:.75rem;color:var(--text2)">Live</span>
        </div>
      </div>
      <div class="panel-body" id="log-stream" style="max-height:500px;font-family:monospace;"></div>
    </div>
  </div>

  <div class="footer">
    <a href="https://www.loggerhelper.com" target="_blank">loggerhelper.com</a> &middot;
    CSharpEssentials.LoggerHelper v5.2.0 &middot;
    Dashboard auto-refreshes every <span id="refresh-seconds">30</span>s
  </div>
</div>

<script>
// Derive config from the page URL and meta tags — no server-side interpolation in JS
const BASE = window.location.pathname.replace(/\/$/, '');
const REFRESH = (parseInt(document.querySelector('meta[name="dashboard-refresh"]')?.content, 10) || 30) * 1000;
let eventSource = null;

// Update footer with actual refresh interval
const rs = document.getElementById('refresh-seconds');
if (rs) rs.textContent = REFRESH / 1000;

console.log('[Dashboard] BASE=' + BASE + ' REFRESH=' + REFRESH + 'ms');

async function refresh() {
  try {
    const url = BASE + '/api/status';
    console.log('[Dashboard] Fetching ' + url);
    const r = await fetch(url);
    if (!r.ok) {
      document.getElementById('health-badge').textContent = 'API Error ' + r.status;
      document.getElementById('health-badge').className = 'badge badge-err';
      document.getElementById('c-health').textContent = 'ERR';
      document.getElementById('c-health').style.color = 'var(--err)';
      console.error('[Dashboard] API returned:', r.status, r.statusText);
      return;
    }
    const text = await r.text();
    let d;
    try {
      d = JSON.parse(text);
    } catch (parseErr) {
      document.getElementById('health-badge').textContent = 'Parse Error';
      document.getElementById('health-badge').className = 'badge badge-err';
      console.error('[Dashboard] Invalid JSON:', text.substring(0, 500), parseErr);
      return;
    }

    // Health badge
    const hb = document.getElementById('health-badge');
    hb.textContent = d.health || 'Unknown';
    hb.className = 'badge badge-' + (d.health === 'OK' ? 'ok' : d.health === 'WARNING' ? 'warn' : 'err');

    // Cards
    document.getElementById('c-health').textContent = d.health || '?';
    document.getElementById('c-health').style.color = d.health === 'OK' ? 'var(--ok)' : d.health === 'WARNING' ? 'var(--warn)' : 'var(--err)';
    const sinksList = Array.isArray(d.sinks) ? d.sinks : [];
    const activeSinks = sinksList.filter(s => s.status === 'ACTIVE').length;
    document.getElementById('c-sinks').textContent = activeSinks + '/' + sinksList.length;
    const errTotal = d.errors?.total ?? 0;
    document.getElementById('c-errors').textContent = errTotal;
    document.getElementById('c-errors').style.color = errTotal > 0 ? 'var(--err)' : 'var(--ok)';
    document.getElementById('c-buffer').textContent = d.contextualLogging ? 'ON' : 'OFF';
    document.getElementById('c-buffer').style.color = d.contextualLogging ? 'var(--ok)' : 'var(--text2)';

    // Sinks table
    const sb = document.getElementById('sinks-body');
    sb.innerHTML = '';
    sinksList.forEach(s => {
      const tr = document.createElement('tr');
      const badge = s.status === 'ACTIVE' ? '<span class="badge badge-ok">ACTIVE</span>' : '<span class="badge badge-err">FAILED</span>';
      tr.innerHTML = '<td><strong>' + s.name + '</strong></td><td>' + badge + '</td><td>' + (s.levels||[]).join(', ') + '</td><td><label class="toggle"><input type="checkbox" ' + (s.status==='ACTIVE'?'checked':'') + ' onchange="toggleSink(\'' + s.name + '\',this.checked)"/><span class="slider"></span></label></td>';
      sb.appendChild(tr);
    });

    // Routes table
    const rb = document.getElementById('routes-body');
    rb.innerHTML = '';
    const routesList = Array.isArray(d.routes) ? d.routes : [];
    routesList.forEach(r => {
      const tr = document.createElement('tr');
      tr.innerHTML = '<td>' + r.sink + '</td><td>' + (r.levels||[]).join(', ') + '</td>';
      rb.appendChild(tr);
    });

    // Errors
    const eb = document.getElementById('errors-body');
    eb.innerHTML = '';
    document.getElementById('error-count').textContent = errTotal + ' total';
    if (d.errors?.recent) {
      d.errors.recent.forEach((e, i) => {
        const tr = document.createElement('tr');
        tr.className = 'error-row';
        tr.onclick = () => tr.classList.toggle('open');
        tr.innerHTML = '<td>' + e.timestamp + '</td><td><span class="badge badge-err">' + e.sink + '</span></td><td>' + e.message + '</td>';
        eb.appendChild(tr);
        if (e.stackTrace) {
          const detail = document.createElement('tr');
          detail.className = 'error-detail';
          detail.innerHTML = '<td colspan="3"><pre>' + e.stackTrace + '</pre></td>';
          eb.appendChild(detail);
        }
      });
    }

    // Context Before Error
    const cp = document.getElementById('context-panel');
    const cb = document.getElementById('context-body');
    const hasFlush = d.lastFlush && (d.lastFlush.triggeringError || (d.lastFlush.entries && d.lastFlush.entries.length > 0));
    if (hasFlush) {
      cp.style.display = '';
      document.getElementById('context-flush-time').textContent = 'Flushed at ' + d.lastFlush.flushedAt;
      cb.innerHTML = '';
      // Context entries (Debug/Info/Warning that preceded the error)
      (d.lastFlush.entries || []).forEach(e => {
        const div = document.createElement('div');
        div.className = 'log-entry';
        const levelClass = 'log-' + (e.level||'info').toLowerCase();
        div.innerHTML = '<span class="log-time">' + e.timestamp + '</span> <span class="' + levelClass + '">[' + (e.level||'?').padEnd(11) + ']</span> <span style="color:var(--text2)">[' + (e.source||'?') + ']</span> ' + (e.message||'');
        cb.appendChild(div);
      });
      // Triggering Error/Fatal — shown last with a visual separator
      if (d.lastFlush.triggeringError) {
        const sep = document.createElement('div');
        sep.style = 'border-top:2px solid var(--err);margin:8px 0 4px;padding-top:4px;font-size:.7rem;color:var(--err);letter-spacing:.05em;text-transform:uppercase;';
        sep.textContent = '▼ Triggering event';
        cb.appendChild(sep);
        const te = d.lastFlush.triggeringError;
        const div = document.createElement('div');
        div.className = 'log-entry';
        const levelClass = 'log-' + (te.level||'error').toLowerCase();
        div.style = 'background:rgba(239,68,68,.07);border-left:3px solid var(--err);';
        div.innerHTML = '<span class="log-time">' + te.timestamp + '</span> <span class="' + levelClass + '"><strong>[' + (te.level||'?').padEnd(11) + ']</strong></span> <span style="color:var(--text2)">[' + (te.source||'?') + ']</span> <strong>' + (te.message||'') + '</strong>';
        cb.appendChild(div);
      }
    } else {
      cp.style.display = 'none';
    }

    document.getElementById('last-refresh').textContent = 'Updated ' + new Date().toLocaleTimeString();
    console.log('[Dashboard] Refresh OK — health=' + d.health + ' sinks=' + sinksList.length);
  } catch(ex) {
    document.getElementById('health-badge').textContent = 'Connection Error';
    document.getElementById('health-badge').className = 'badge badge-err';
    document.getElementById('c-health').textContent = 'ERR';
    document.getElementById('c-health').style.color = 'var(--err)';
    console.error('[Dashboard] Refresh failed:', ex);
  }
}

function startStream() {
  if (eventSource) eventSource.close();
  const toggle = document.getElementById('stream-toggle');
  if (!toggle.checked) return;

  const url = BASE + '/api/stream';
  console.log('[Dashboard] Starting SSE stream:', url);
  eventSource = new EventSource(url);
  eventSource.onmessage = (e) => {
    try {
      const entry = JSON.parse(e.data);
      addLogEntry(entry);
    } catch {}
  };
  eventSource.onerror = () => {
    console.warn('[Dashboard] SSE connection lost, reconnecting in 5s...');
    setTimeout(startStream, 5000);
  };
}

function addLogEntry(entry) {
  const levelFilter = document.getElementById('log-level-filter').value;
  const queryFilter = document.getElementById('log-query').value.toLowerCase();

  if (levelFilter && entry.level !== levelFilter) return;
  if (queryFilter && !(entry.message||'').toLowerCase().includes(queryFilter) && !(entry.source||'').toLowerCase().includes(queryFilter)) return;

  const container = document.getElementById('log-stream');
  const div = document.createElement('div');
  div.className = 'log-entry';
  const levelClass = 'log-' + (entry.level||'info').toLowerCase();
  div.innerHTML = '<span class="log-time">' + entry.timestamp + '</span> <span class="' + levelClass + '">[' + (entry.level||'?').padEnd(11) + ']</span> <span style="color:var(--text2)">[' + (entry.source||'?') + ']</span> ' + (entry.message||'');
  container.appendChild(div);

  while (container.childElementCount > 500) container.removeChild(container.firstChild);
  container.scrollTop = container.scrollHeight;
}

function clearLogs() {document.getElementById('log-stream').innerHTML = '';
}

async function toggleSink(name, enabled) {await refresh();
}

// Init
refresh();
setInterval(refresh, REFRESH);
startStream();
document.getElementById('stream-toggle').addEventListener('change', startStream);
document.getElementById('log-level-filter').addEventListener('change', () => {clearLogs();
});
</script>
</body>
</html>
""";
}