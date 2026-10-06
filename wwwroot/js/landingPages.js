// ── Carrusel de imágenes ──────────────────────────────────────
let slideActual = 0;
const slides = document.querySelectorAll('.slide');
const puntos = document.querySelectorAll('.punto');

function mostrarSlide(indice) {
    if (slides.length === 0) return;
    slideActual = (indice + slides.length) % slides.length;
    slides.forEach((slide, i) => slide.classList.toggle('activo', i === slideActual));
    puntos.forEach((punto, i) => punto.classList.toggle('activo', i === slideActual));
}

function cambiarSlide(direccion) { mostrarSlide(slideActual + direccion); }
function irSlide(indice) { mostrarSlide(indice); }

mostrarSlide(slideActual);
setInterval(() => cambiarSlide(1), 5000);

// ── Visibilidad de contraseña ─────────────────────────────────
function togglePasswordVisibility(inputId, btn) {
    const input = document.getElementById(inputId);
    const icon = btn.querySelector('i');
    const isPassword = input.type === 'password';
    input.type = isPassword ? 'text' : 'password';
    icon.classList.toggle('bi-eye', !isPassword);
    icon.classList.toggle('bi-eye-slash', isPassword);
}

// ── Scroll suave para enlaces ancla ──────────────────────────
document.addEventListener('DOMContentLoaded', () => {

    // Scroll suave con easing personalizado
    document.querySelectorAll('a[href^="#"]').forEach(enlace => {
        enlace.addEventListener('click', e => {
            const id = enlace.getAttribute('href');
            if (!id || id === '#') return;
            const destino = document.querySelector(id);
            if (!destino) return;
            e.preventDefault();

            const navbarH = document.getElementById('pubNavbar')?.offsetHeight ?? 72;
            const destinoY = destino.getBoundingClientRect().top + window.scrollY - navbarH - 12;
            const inicio = window.scrollY;
            const distancia = destinoY - inicio;
            const duracion = Math.min(900, Math.max(400, Math.abs(distancia) * 0.5));
            let tiempoInicio = null;

            function easeInOutCubic(t) {
                return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
            }

            function animar(ts) {
                if (!tiempoInicio) tiempoInicio = ts;
                const progreso = Math.min((ts - tiempoInicio) / duracion, 1);
                window.scrollTo(0, inicio + distancia * easeInOutCubic(progreso));
                if (progreso < 1) requestAnimationFrame(animar);
            }
            requestAnimationFrame(animar);
        });
    });

    // ── Animaciones de aparición al hacer scroll ─────────────
    // Solo animar elementos que están FUERA del viewport inicial
    const selectores = [
        '.servicios .tarjeta',
        '.consejos .consejo',
        '.informacion-contenido .beneficio',
        '.pub-prod-card',
        '.informacion-texto',
        '.contacto-inicio'
    ];

    // Altura del viewport para detectar qué está "above the fold"
    const alturaViewport = window.innerHeight;

    selectores.forEach(sel => {
        document.querySelectorAll(sel).forEach((el, i) => {
            const rect = el.getBoundingClientRect();
            // Solo aplicar reveal si el elemento está debajo del viewport al cargar
            if (rect.top > alturaViewport * 0.95) {
                el.classList.add('reveal');
                el.style.setProperty('--reveal-delay', `${(i % 4) * 70}ms`);
            }
        });
    });

    // IntersectionObserver para activar la animación
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('reveal-visible');
                observer.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.08,
        rootMargin: '0px 0px -20px 0px'
    });

    document.querySelectorAll('.reveal').forEach(el => observer.observe(el));

    // ── Formularios de Login y Registro AJAX en Modales ─────
    document.querySelectorAll('.modal-login-form').forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const submitBtn = form.querySelector('button[type="submit"]');
            const origHtml = submitBtn ? submitBtn.innerHTML : '';
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span> Procesando...';
            }

            try {
                const res = await (window.postFormAjax ? window.postFormAjax(form.action, new FormData(form)) : fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form),
                    headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
                }).then(r => r.json()));

                if (res.success) {
                    if (window.showAppToast) window.showAppToast(res.message || 'Inicio de sesión exitoso. Redirigiendo...', 'success');
                    setTimeout(() => {
                        window.location.href = res.redirectUrl || '/';
                    }, 400);
                } else {
                    if (window.showAppToast) {
                        window.showAppToast(res.message || 'Credenciales inválidas.', 'error');
                    } else {
                        alert(res.message || 'Error al procesar la solicitud.');
                    }
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = origHtml;
                    }
                }
            } catch (err) {
                console.error(err);
                if (window.showAppToast) window.showAppToast('Error de conexión con el servidor.', 'error');
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = origHtml;
                }
            }
        });
    });
});

