import React from 'react';
import './App.css';
import Sidebar from './components/Sidebar';
import Header from './components/Header';
import Content from './components/Content';

function App() {
  return (
    <div className="app-container">
      <Sidebar />
      <div className="main-area">
        <Header />
        <Content />
      </div>
    </div>
  );
}

export default App;