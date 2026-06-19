namespace CSharpEssentials.LoggerHelper.Dashboard;

internal static class DashboardHtml {
    internal static string GetPage(string basePath) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1"/>
<title>LoggerHelper Dashboard</title>
<style>
*{margin:0;padding:0;box-sizing:border-box}
:root{
  --bg:#0d1117;--surface:#161b22;--border:#30363d;
  --text:#e6edf3;--muted:#8b949e;--accent:#58a6ff;
  --green:#3fb950;--red:#f85149;--orange:#d29922;--purple:#bc8cff;
}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif;
  background:var(--bg);color:var(--text);line-height:1.5;min-height:100vh}
.header{background:var(--surface);border-bottom:1px solid var(--border);padding:16px 24px;
  display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:12px}
.header h1{font-size:20px;font-weight:600;display:flex;align-items:center;gap:10px}
.header h1 .dot{width:10px;height:10px;border-radius:50%;display:inline-block}
.header .meta{color:var(--muted);font-size:13px;display:flex;gap:16px;flex-wrap:wrap}
.header .meta span{display:flex;align-items:center;gap:4px}
.container{max-width:1200px;margin:0 auto;padding:24px}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:16px;margin-bottom:24px}
.card{background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:20px}
.card .label{font-size:12px;color:var(--muted);text-transform:uppercase;letter-spacing:0.5px;margin-bottom:4px}
.card .value{font-size:28px;font-weight:700}
.card .value.green{color:var(--green)}.card .value.red{color:var(--red)}
.card .value.orange{color:var(--orange)}.card .value.accent{color:var(--accent)}
.section{background:var(--surface);border:1px solid var(--border);border-radius:8px;margin-bottom:24px;overflow:hidden}
.section-header{padding:16px 20px;border-bottom:1px solid var(--border);display:flex;
  align-items:center;justify-content:space-between}
.section-header h2{font-size:16px;font-weight:600;display:flex;align-items:center;gap:8px}
.section-header .badge{background:var(--border);color:var(--text);font-size:11px;padding:2px 8px;
  border-radius:10px;font-weight:500}
.section-header .badge.red{background:rgba(248,81,73,.15);color:var(--red)}
.section-header .badge.green{background:rgba(63,185,80,.15);color:var(--green)}
table{width:100%;border-collapse:collapse}
th{text-align:left;padding:10px 20px;font-size:12px;color:var(--muted);text-transform:uppercase;
  letter-spacing:0.5px;background:rgba(110,118,129,.05);border-bottom:1px solid var(--border)}
td{padding:12px 20px;border-bottom:1px solid var(--border);font-size:14px;vertical-align:top}
tr:last-child td{border-bottom:none}
.status-dot{width:8px;height:8px;border-radius:50%;display:inline-block;margin-right:6px}
.status-dot.active{background:var(--green)}.status-dot.failed{background:var(--red)}
.tag{display:inline-block;padding:2px 8px;border-radius:4px;font-size:11px;font-weight:500;
  margin:2px;background:rgba(88,166,255,.1);color:var(--accent)}
.tag.verbose{color:var(--muted);background:rgba(139,148,158,.1)}
.tag.debug{color:var(--muted);background:rgba(139,148,158,.1)}
.tag.information{color:var(--green);background:rgba(63,185,80,.1)}
.tag.warning{color:var(--orange);background:rgba(210,153,34,.1)}
.tag.error{color:var(--red);background:rgba(248,81,73,.1)}
.tag.fatal{color:#ff6b6b;background:rgba(255,107,107,.1)}
.error-row{cursor:pointer}.error-row:hover{background:rgba(88,166,255,.04)}
.error-detail{display:none;padding:12px 20px;background:rgba(0,0,0,.2);border-bottom:1px solid var(--border)}
.error-detail.open{display:block}
.error-detail pre{font-size:12px;color:var(--muted);white-space:pre-wrap;word-break:break-all;
  font-family:'SF Mono',SFMono-Regular,Consolas,'Liberation Mono',Menlo,monospace;
  background:rgba(0,0,0,.3);padding:12px;border-radius:4px;margin-top:8px;max-height:300px;overflow:auto}
.error-detail .ctx{color:var(--orange);font-size:13px;margin-top:4px}
.empty{padding:40px;text-align:center;color:var(--muted);font-size:14px}
.empty .icon{font-size:32px;margin-bottom:8px}
.refresh-btn{background:none;border:1px solid var(--border);color:var(--muted);padding:6px 12px;
  border-radius:6px;cursor:pointer;font-size:12px;transition:all .15s}
.refresh-btn:hover{border-color:var(--accent);color:var(--accent)}
.auto-refresh{color:var(--muted);font-size:12px;margin-left:8px}
.footer{text-align:center;padding:24px;color:var(--muted);font-size:12px}
.footer a{color:var(--accent);text-decoration:none}
.footer a:hover{text-decoration:underline}
@media(max-width:640px){
  .container{padding:12px}.grid{grid-template-columns:1fr 1fr}
  .header{padding:12px 16px}.header h1{font-size:16px}
  td,th{padding:8px 12px;font-size:12px}
}
.loading{text-align:center;padding:60px;color:var(--muted)}
.loading .spinner{width:32px;height:32px;border:3px solid var(--border);border-top-color:var(--accent);
  border-radius:50%;animation:spin .8s linear infinite;margin:0 auto 12px}
@keyframes spin { to { transform:rotate(360deg) } }
</style>
</head>
<body>
<div class="header">
  <h1><span class="dot" id="status-dot"></span> LoggerHelper Dashboard</h1>
  <div class="meta">
    <span id="app-name"></span>
    <span id="uptime"></span>
    <span><button class="refresh-btn" onclick="loadData()">Refresh</button>
          <span class="auto-refresh" id="countdown"></span></span>
  </div>
</div>
<div class="container">
  <div id="loading" class="loading"><div class="spinner"></div>Loading diagnostics...</div>
  <div id="content" style="display:none">
    <div class="grid">
      <div class="card"><div class="label">Status</div><div class="value" id="status-val"></div></div>
      <div class="card"><div class="label">Active Sinks</div><div class="value green" id="active-val"></div></div>
      <div class="card"><div class="label">Failed Sinks</div><div class="value" id="failed-val"></div></div>
      <div class="card"><div class="label">Errors</div><div class="value" id="errors-val"></div></div>
    </div>

    <div class="section" id="errors-section">
      <div class="section-header">
        <h2>Sink Errors <span class="badge" id="errors-badge"></span></h2>
      </div>
      <div id="errors-body"></div>
    </div>

    <div class="section">
      <div class="section-header">
        <h2>Sinks <span class="badge" id="sinks-badge"></span></h2>
      </div>
      <div id="sinks-body"></div>
    </div>

    <div class="section">
      <div class="section-header">
        <h2>Routing Configuration <span class="badge" id="routes-badge"></span></h2>
      </div>
      <div id="routes-body"></div>
    </div>
  </div>
</div>
<div class="footer">
  LoggerHelper v5 &mdash;
  <a href="https://www.loggerhelper.com" target="_blank">Documentation</a> &middot;
  <a href="https://github.com/alexbypa/CSharp.Essentials" target="_blank">GitHub</a> &middot;
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper" target="_blank">NuGet</a>
</div>
<script>
const API = '{{basePath}}/api/data';
const INTERVAL = 30;
let timer = INTERVAL;
let intervalId;

function levelClass(l) { return l.toLowerCase(); }

function renderLevels(levels) {
  return levels.map(l => `<span class="tag ${levelClass(l)}">${l}</span>`).join('');
}

function renderSinks(sinks) {
  if (!sinks.length) return '<div class="empty"><div class="icon">&#9898;</div>No sinks configured</div>';
  let html = '<table><thead><tr><th>Status</th><th>Sink</th><th>Plugin</th><th>Levels</th></tr></thead><tbody>';
  sinks.forEach(s => {
    const dot = s.active ? 'active' : 'failed';
    const label = s.active ? 'Active' : 'Failed';
    html += `<tr>
      <td><span class="status-dot ${dot}"></span>${label}</td>
      <td><strong>${esc(s.name)}</strong></td>
      <td style="color:var(--muted);font-size:12px">${esc(s.pluginType)}</td>
      <td>${renderLevels(s.levels)}</td>
    </tr>`;
  });
  html += '</tbody></table>';
  return html;
}

function renderErrors(errors) {
  if (!errors.length) return '<div class="empty"><div class="icon">&#10004;</div>No errors recorded — all sinks started successfully</div>';
  let html = '<table><thead><tr><th>Time</th><th>Sink</th><th>Error</th></tr></thead><tbody>';
  errors.forEach((e, i) => {
    html += `<tr class="error-row" onclick="toggleDetail(${i})">
      <td style="white-space:nowrap;color:var(--muted)">${esc(e.timestamp)}</td>
      <td><strong style="color:var(--red)">${esc(e.sinkName)}</strong></td>
      <td>${esc(e.message)}</td>
    </tr>`;
    const hasExtra = e.stackTrace || e.context;
    html += `<tr><td colspan="3" style="padding:0"><div class="error-detail" id="detail-${i}">`;
    if (e.context) html += `<div class="ctx">Context: ${esc(e.context)}</div>`;
    if (e.stackTrace) html += `<pre>${esc(e.stackTrace)}</pre>`;
    if (!hasExtra) html += `<div style="color:var(--muted);padding:8px 0;font-size:13px">No additional details available</div>`;
    html += '</div></td></tr>';
  });
  html += '</tbody></table>';
  return html;
}

function renderRoutes(routes) {
  if (!routes.length) return '<div class="empty"><div class="icon">&#128268;</div>No routes configured</div>';
  let html = '<table><thead><tr><th>Sink</th><th>Levels</th></tr></thead><tbody>';
  routes.forEach(r => {
    html += `<tr><td><strong>${esc(r.sink)}</strong></td><td>${renderLevels(r.levels)}</td></tr>`;
  });
  html += '</tbody></table>';
  return html;
}

function toggleDetail(i) {
  const el = document.getElementById('detail-' + i);
  if (el) el.classList.toggle('open');
}

function esc(s) {
  if (!s) return '';
  const d = document.createElement('div');
  d.textContent = s;
  return d.innerHTML;
}

function setStatus(data) {
  const dot = document.getElementById('status-dot');
  const val = document.getElementById('status-val');
  val.textContent = data.status;
  if (data.status === 'OK') { dot.style.background = 'var(--green)'; val.className = 'value green'; }
  else if (data.status === 'WARNING') { dot.style.background = 'var(--orange)'; val.className = 'value orange'; }
  else { dot.style.background = 'var(--red)'; val.className = 'value red'; }
}

async function loadData() {
  try {
    const res = await fetch(API);
    const data = await res.json();

    document.getElementById('loading').style.display = 'none';
    document.getElementById('content').style.display = 'block';

    setStatus(data);
    document.getElementById('app-name').textContent = data.applicationName || 'Unknown';
    document.getElementById('uptime').textContent = 'Uptime: ' + (data.uptime || '-');
    document.getElementById('active-val').textContent = data.activeSinks;
    const failedEl = document.getElementById('failed-val');
    failedEl.textContent = data.failedSinks;
    failedEl.className = 'value ' + (data.failedSinks > 0 ? 'red' : 'green');
    const errEl = document.getElementById('errors-val');
    errEl.textContent = data.errorCount;
    errEl.className = 'value ' + (data.errorCount > 0 ? 'red' : 'green');

    const eb = document.getElementById('errors-badge');
    eb.textContent = data.errorCount + ' error' + (data.errorCount !== 1 ? 's' : '');
    eb.className = 'badge ' + (data.errorCount > 0 ? 'red' : 'green');

    document.getElementById('sinks-badge').textContent = data.sinks.length + ' sink' + (data.sinks.length !== 1 ? 's' : '');
    document.getElementById('routes-badge').textContent = data.routes.length + ' route' + (data.routes.length !== 1 ? 's' : '');

    document.getElementById('errors-body').innerHTML = renderErrors(data.errors);
    document.getElementById('sinks-body').innerHTML = renderSinks(data.sinks);
    document.getElementById('routes-body').innerHTML = renderRoutes(data.routes);

    timer = INTERVAL;
  } catch (err) {
    document.getElementById('loading').innerHTML =
      '<div style="color:var(--red)">Failed to load dashboard data.<br>' +
      '<span style="color:var(--muted);font-size:12px">' + esc(err.message) + '</span></div>';
  }
}

function tick() {
  timer--;
  if (timer <= 0) { loadData(); return; }
  const cd = document.getElementById('countdown');
  if (cd) cd.textContent = 'auto-refresh in ' + timer + 's';
}

loadData();
intervalId = setInterval(tick, 1000);
</script>
</body>
</html>
""";
}
