import React, { useEffect, useState } from 'react'

export default function MetricsChart() {
  const [metrics, setMetrics] = useState([])

  useEffect(() => {
    fetch('/loggerdashboard/api/metrics')
      .then(res => res.json())
      .then(setMetrics)
  }, [])

  return (
    <div>
      <h2>📈 Metrics (last 100)</h2>
      <ul>
        {metrics.map((m, i) => (
          <li key={i}>
            [{m.timestamp}] <strong>{m.name}</strong>: {m.value}
          </li>
        ))}
      </ul>
    </div>
  )
}