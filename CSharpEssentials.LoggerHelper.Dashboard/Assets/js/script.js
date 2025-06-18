// CSharpEssentials.LoggerHelper.Dashboard/Assets/js/script.js

document.addEventListener('DOMContentLoaded', () => {
    const body = document.querySelector('body');
    if (body) {
        const messageDiv = document.createElement('div');
        messageDiv.textContent = 'Questo messaggio è stato aggiunto dal JavaScript embedded!';
        messageDiv.style.marginTop = '20px';
        messageDiv.style.color = 'green';
        messageDiv.style.fontWeight = 'bold';
        body.appendChild(messageDiv);
        console.log('Script.js caricato e ha aggiunto un messaggio al body.');
    }
});