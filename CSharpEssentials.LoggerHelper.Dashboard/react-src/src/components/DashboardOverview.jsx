import React from 'react'

const DashboardOverview = () => {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-around', marginTop: '2em' }}>
      <div style={{ border: '1px solid #ccc', padding: '1em', borderRadius: '8px', width: '200px' }}>
        <h3>Error Logs</h3>
        <p>🛑 12 recenti</p>
      </div>
      <div style={{ border: '1px solid #ccc', padding: '1em', borderRadius: '8px', width: '200px' }}>
        <h3>HTTP Requests</h3>
        <p>📈 234</p>
      </div>
      <div style={{ border: '1px solid #ccc', padding: '1em', borderRadius: '8px', width: '200px' }}>
        <h3>DB Queries</h3>
        <p>🔍 87</p>
      </div>
    </div>
  )
}

export default DashboardOverview