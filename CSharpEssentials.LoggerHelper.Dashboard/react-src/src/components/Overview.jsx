import React from 'react'
import Logs from './Logs'
import MetricsChart from './MetricsChart'

export default function Overview() {
  return (
    <div style={{ padding: '2em' }}>
      <h1>🧠 LoggerHelper Dashboard Overview</h1>
      <Logs />
      <hr />
      <MetricsChart />
    </div>
  )
}