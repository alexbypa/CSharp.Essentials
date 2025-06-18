document.addEventListener('DOMContentLoaded', () => {
    const heading = document.querySelector('h1');
    if (heading) {
        heading.textContent = "Dashboard di Test: JS Caricato Correttamente!";
        heading.style.color = '#27ae60'; // Verde brillante
        heading.style.textShadow = '1px 1px 2px rgba(0,0,0,0.2)';
    }
    console.log(">>> Script JavaScript della Dashboard caricato e in esecuzione.");
});