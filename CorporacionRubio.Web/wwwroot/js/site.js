(() => {
  setTimeout(() => {
    document.querySelectorAll('.alert-dismissible').forEach(alert => {
      const instance = bootstrap.Alert.getOrCreateInstance(alert);
      instance.close();
    });
  }, 6000);
})();


// Corrección final de modales: evita que el fondo gris quede encima del formulario.
// Se ejecuta en todas las páginas del panel y mueve los modales al <body>, como espera Bootstrap.
document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.modal').forEach(modal => {
    modal.setAttribute('data-bs-backdrop', 'false');
    modal.setAttribute('data-bs-keyboard', 'true');
    modal.classList.add('admin-safe-modal');
    if (modal.parentElement !== document.body) {
      document.body.appendChild(modal);
    }

    modal.addEventListener('shown.bs.modal', () => {
      document.querySelectorAll('.modal-backdrop').forEach(backdrop => backdrop.remove());
      document.body.classList.remove('modal-open');
      document.body.style.removeProperty('overflow');
      document.body.style.removeProperty('padding-right');
      modal.style.zIndex = '30000';
      const content = modal.querySelector('.modal-content');
      if (content) content.style.pointerEvents = 'auto';
    });

    modal.addEventListener('hidden.bs.modal', () => {
      document.querySelectorAll('.modal-backdrop').forEach(backdrop => backdrop.remove());
      document.body.classList.remove('modal-open');
      document.body.style.removeProperty('overflow');
      document.body.style.removeProperty('padding-right');
    });
  });
});
