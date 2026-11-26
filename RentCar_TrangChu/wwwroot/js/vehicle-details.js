(() => {
    const mainImage = document.getElementById('vehicle-main-image');
    const thumbnails = document.querySelectorAll('[data-gallery-image]');
    for (const thumbnail of thumbnails) {
        thumbnail.addEventListener('click', () => {
            mainImage.src = thumbnail.dataset.galleryImage;
            for (const button of thumbnails)
                button.setAttribute('aria-pressed', String(button === thumbnail));
        });
    }
})();

