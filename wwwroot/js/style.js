// ============================================
// INDEX PAGE SPECIFIC JAVASCRIPT
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    // --- Reveal Animations ---
    const reveals = document.querySelectorAll('.reveal');

    const revealObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('active');
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -40px 0px'
    });

    reveals.forEach(el => revealObserver.observe(el));

    // --- Solution Pills ---
    const pills = document.querySelectorAll('.solution-pill');
    pills.forEach(pill => {
        pill.addEventListener('click', function () {
            pills.forEach(p => p.classList.remove('active'));
            this.classList.add('active');
            showToast('🔍 Filtered: ' + this.textContent.trim());
        });
    });

    // --- Contact Form ---
    const contactForm = document.getElementById('luxuryContactForm');
    if (contactForm) {
        contactForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const name = document.getElementById('name')?.value || 'Guest';
            showToast('✅ Thank you ' + name + '! Your message has been received.');
            this.reset();
        });
    }

    // --- Newsletter ---
    const subscribeBtn = document.getElementById('subscribeNewsBtn');
    const newsEmail = document.getElementById('newsEmail');
    if (subscribeBtn && newsEmail) {
        subscribeBtn.addEventListener('click', function () {
            const email = newsEmail.value.trim();
            if (email && email.includes('@') && email.includes('.')) {
                showToast('✅ Subscribed! ' + email + ' will receive updates.');
                newsEmail.value = '';
            } else {
                showToast('⚠️ Please enter a valid email address.', true);
            }
        });
    }

    // --- Toast System ---
    function showToast(message, isError = false) {
        let toast = document.getElementById('toastMsg');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'toastMsg';
            toast.className = 'toast-msg';
            document.body.appendChild(toast);

            const icon = document.createElement('i');
            icon.className = 'fas fa-check-circle';
            toast.appendChild(icon);

            const textSpan = document.createElement('span');
            textSpan.id = 'toastText';
            toast.appendChild(textSpan);
        }

        const textSpan = document.getElementById('toastText');
        const icon = toast.querySelector('i');

        if (textSpan) textSpan.innerText = message;

        if (isError) {
            if (icon) icon.className = 'fas fa-exclamation-circle';
            toast.style.borderLeftColor = '#dc3545';
            toast.style.backgroundColor = '#c0392b';
        } else {
            if (icon) icon.className = 'fas fa-check-circle';
            toast.style.borderLeftColor = '#00D68F';
            toast.style.backgroundColor = '#0B2B26';
        }

        toast.style.display = 'flex';
        setTimeout(() => {
            if (toast) toast.style.display = 'none';
        }, 3000);
    }

    // Make showToast global for other scripts
    window.showToast = showToast;
});