import React from 'react'
import DashboardOverview from './components/DashboardOverview'

function App() {
  return (
    <div style={{ padding: '2em', fontFamily: 'Arial' }}>
      <h1 style={{ textAlign: 'center' }}>📊 LoggerHelper Dashboard</h1>
      <DashboardOverview />
    </div>
  )
}

export default App