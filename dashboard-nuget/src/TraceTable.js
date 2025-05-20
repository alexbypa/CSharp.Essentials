import { useEffect, useState } from "react";
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts';

function App() {
  const [metrics, setMetrics] = useState([]);
  const [filter, setFilter] = useState("all");

  useEffect(() => {
    const fetchData = async () => {
      try {
        const res = await fetch("http://localhost:5133/api/metrics");
        const data = await res.json();
        setMetrics(prev =>
          JSON.stringify(prev) !== JSON.stringify(data) ? data : prev
        );
      } catch (err) {
        console.error("Fetch failed", err);
      }
    };

    fetchData();
    const interval = setInterval(fetchData, 5000);
    return () => clearInterval(interval);
  }, []);

  const renderTags = (tagsJson) => {
    if (!tagsJson) return "-";
    try {
      const tags = JSON.parse(tagsJson);
      return Object.entries(tags)
        .map(([k, v]) => `${k}=${v}`)
        .join(", ");
    } catch (err) {
      return "[malformed]";
    }
  };

  const getStatusBadge = (metric, value) => {
    if (metric === "memory_used_mb") {
      if (value > 85) return <span className="badge bg-danger">Critico</span>;
      if (value > 75) return <span className="badge bg-warning text-dark">Warning</span>;
      return <span className="badge bg-success">OK</span>;
    }
    return null;
  };

  return (
    <>
      <div className="container mt-5">
        <h1 className="text-primary mb-4">📊 NuGet Metrics Dashboard</h1>

        <div className="mb-3">
          <label className="form-label">Filtro metrica:</label>
          <select className="form-select w-auto"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
          >
            <option value="all">Tutte</option>
            {[...new Set(metrics.map(m => m.metric))].map((name, i) => (
              <option key={i} value={name}>{name}</option>
            ))}
          </select>
        </div>

        <table className="table table-striped table-hover">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>Metric</th>
              <th>TraceId</th>
              <th>Value</th>
              <th>Tags</th>
            </tr>
          </thead>
          <tbody>
            {metrics
              .filter(m => filter === "all" || m.metric === filter)
              .map((m, idx) => (
                <tr key={idx}>
                  <td>{new Date(m.timestamp).toLocaleString()}</td>
                  <td>{m.metric}</td>
                  <td>
                    {m.traceId ? (
                      <a href={`http://localhost:5133/api/traces/${m.traceId}`} target="_blank" rel="noreferrer">
                        {m.traceId}
                      </a>
                    ) : "-"}
                  </td>
                  <td>{m.value} {getStatusBadge(m.metric, parseFloat(m.value))}</td>
                  <td>{renderTags(m.tags)}</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <h2 className="mt-5">📈 Grafico storico</h2>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart
          data={metrics
            .filter(m => filter === "all" || m.metric === filter)
            .map(m => ({
              time: new Date(m.timestamp).toLocaleTimeString(),
              value: parseFloat(m.value)
            }))}
          margin={{ top: 5, right: 20, bottom: 5, left: 0 }}
        >
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="time" />
          <YAxis />
          <Tooltip />
          <Legend />
          <Line type="monotone" dataKey="value" stroke="#007bff" activeDot={{ r: 8 }} />
        </LineChart>
      </ResponsiveContainer>
    </>
  );
}

export default App;
