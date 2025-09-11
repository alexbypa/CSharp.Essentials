import React, { useEffect, useState } from "react";

export default function LoggerSinks() {
    const [data, setData] = useState({ currentError: "", sinks: [], errors: [], errorCount: 0, sinkCount: 0 });
    const [loading, setLoading] = useState(false);
    const [autoRefresh, setAutoRefresh] = useState(true);
    const [err, setErr] = useState("");

    const load = async () => {
        try {
            setLoading(true);
            setErr("");
            const res = await fetch("/api/logger-errors");
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const json = await res.json();
            setData(json);
        } catch (e) {
            setErr(e.message || "fetch error");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(); }, []);
    useEffect(() => {
        if (!autoRefresh) return;
        const id = setInterval(load, 5000);
        return () => clearInterval(id);
    }, [autoRefresh]);

    return (
        <div className="container py-4">
            <div className="d-flex align-items-center mb-3">
                <h2 className="me-3 mb-0">Logger Sinks</h2>
                <span className="badge bg-secondary me-2">Sinks: {data.sinkCount}</span>
                <span className={`badge ${data.errorCount > 0 ? "bg-danger" : "bg-success"}`}>
                    Errors: {data.errorCount}
                </span>
                <div className="ms-auto d-flex align-items-center">
                    <div className="form-check form-switch me-3">
                        <input className="form-check-input" type="checkbox" id="autoRefresh"
                            checked={autoRefresh} onChange={e => setAutoRefresh(e.target.checked)} />
                        <label className="form-check-label" htmlFor="autoRefresh">Auto refresh (5s)</label>
                    </div>
                    <button className="btn btn-outline-primary" onClick={load} disabled={loading}>
                        {loading ? "Refreshing..." : "Refresh"}
                    </button>
                </div>
            </div>

            {/* Current error */}
            <div className="card mb-3">
                <div className="card-body">
                    <h5 className="card-title mb-2">Current Error</h5>
                    <p className={`card-text ${data.currentError ? "" : "text-muted"}`}>
                        {data.currentError || "Nessun errore"}
                    </p>
                </div>
            </div>

            {/* Sinks table */}
            <div className="card mb-3">
                <div className="card-body">
                    <h5 className="card-title mb-3">Sinks caricati</h5>
                    <div className="table-responsive">
                        <table className="table align-middle">
                            <thead>
                                <tr>
                                    <th style={{ width: "220px" }}>Sink</th>
                                    <th>Levels</th>
                                </tr>
                            </thead>
                            <tbody>
                                {(data.sinks || []).map((s, i) => (
                                    <tr key={i}>
                                        <td className="fw-semibold">{s.sinkName}</td>
                                        <td>
                                            {(s.levels || []).map((lv, k) =>
                                                <span key={k} className="badge bg-light text-dark me-1">{lv}</span>
                                            )}
                                        </td>
                                    </tr>
                                ))}
                                {(!data.sinks || data.sinks.length === 0) && (
                                    <tr><td colSpan={2} className="text-center text-muted py-4">Nessun sink caricato</td></tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            {/* Errors list */}
            <div className="card">
                <div className="card-body">
                    <h5 className="card-title mb-3">Ultimi errori</h5>
                    {data.errors && data.errors.length > 0 ? (
                        <ul className="list-group">
                            {data.errors.slice(0, 50).map((e, i) => (
                                <li key={i} className="list-group-item">
                                    <div className="small text-muted">{new Date(e.timestamp).toLocaleString()}</div>
                                    <div><strong>{e.sinkName}</strong></div>
                                    <div className="text-wrap">{e.errorMessage}</div>
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <p className="text-muted mb-0">Nessun errore trovato</p>
                    )}
                </div>
            </div>

            {err && <div className="alert alert-danger mt-3">Errore caricamento: {err}</div>}
        </div>
    );
}
