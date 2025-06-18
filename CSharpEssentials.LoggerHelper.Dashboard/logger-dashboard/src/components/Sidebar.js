import React from 'react';
import './Sidebar.css';

function Sidebar() {
  return (
    <div className="sidebar">
      <h2>Logger Dashboard</h2>
      <nav>
        <ul>
          <li>Errors</li>
          <li>Metrics</li>
          <li>Traces</li>
          <li>Logs</li>
        </ul>
      </nav>
    </div>
  );
}

export default Sidebar;