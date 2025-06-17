import React, { useEffect, useState } from 'react'

export default function Logs() {
  const [logs, setLogs] = useState([])

  useEffect(() => {
    fetch('/loggerdashboard/api/logs')
      .then(res => res.json())
      .then(setLogs)
  }, [])

  return (
    <div>
      <h2>📜 Logs</h2>
      <table border="1" cellPadding="6">
        <thead>
          <tr>
            <th>Timestamp</th>
            <th>Message</th>
          </tr>
        </thead>
        <tbody>
          {logs.map((l, i) => (
            <tr key={i}>
              <td>{l.timestamp}</td>
              <td>{l.message}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}