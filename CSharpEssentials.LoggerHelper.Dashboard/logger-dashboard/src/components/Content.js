import React from 'react';
import './Content.css';

function Content() {
  return (
    <div className="content">
      <p>Benvenuto nella CSharpEssentials Logger Dashboard!</p>
      {/* Esempio di accesso a un asset */}
      {/* <img src={process.env.PUBLIC_URL + '/Assets/your-logo.png'} alt="Logo" /> */}
    </div>
  );
}

export default Content;