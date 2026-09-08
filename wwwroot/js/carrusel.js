let slideActual = 0;
const slides = document.querySelectorAll('.slide');
const puntos = document.querySelectorAll('.punto');

function mostrarSlide(indice) {
    if (slides.length === 0) return;

    slideActual = (indice + slides.length) % slides.length;

    slides.forEach(function (slide, posicion) {
        slide.classList.toggle('activo', posicion === slideActual);
    });

    puntos.forEach(function (punto, posicion) {
        punto.classList.toggle('activo', posicion === slideActual);
    });
}

function cambiarSlide(direccion) {
    mostrarSlide(slideActual + direccion);
}

function irSlide(indice) {
    mostrarSlide(indice);
}

mostrarSlide(slideActual);

setInterval(function () {
    cambiarSlide(1);
}, 3000);
