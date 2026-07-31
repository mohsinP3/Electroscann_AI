/* ============================================
   COMPLETE SITE.JS - ElectroScan AI
   ============================================ */

/* ============================================
   GLOBAL UTILITIES
   ============================================ */

// Toast notification
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
        if (icon) {
            icon.className = 'fas fa-exclamation-circle';
        }
        toast.style.borderLeftColor = '#dc3545';
        toast.style.backgroundColor = '#c0392b';
    } else {
        if (icon) {
            icon.className = 'fas fa-check-circle';
        }
        toast.style.borderLeftColor = '#00D68F';
        toast.style.backgroundColor = '#0B2B26';
    }

    toast.style.display = 'flex';
    setTimeout(() => {
        if (toast) toast.style.display = 'none';
    }, 3000);
}

// Reveal animations on scroll
function initRevealAnimations() {
    const reveals = document.querySelectorAll('.reveal');
    if (reveals.length === 0) return;

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('active');
            }
        });
    }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });

    reveals.forEach(el => observer.observe(el));
}

// Mobile menu handler
function initMobileMenu() {
    const hamburger = document.getElementById('hamburgerBtn');
    const mobileMenu = document.getElementById('mobileMenu');
    if (!hamburger || !mobileMenu) return;

    hamburger.addEventListener('click', () => {
        mobileMenu.classList.toggle('open');
        const icon = hamburger.querySelector('i');
        if (icon) {
            icon.classList.toggle('fa-bars');
            icon.classList.toggle('fa-times');
        }
    });

    mobileMenu.querySelectorAll('a').forEach(link => {
        link.addEventListener('click', () => {
            mobileMenu.classList.remove('open');
            const icon = hamburger.querySelector('i');
            if (icon) {
                icon.classList.add('fa-bars');
                icon.classList.remove('fa-times');
            }
        });
    });
}

// Scroll to top button
function initScrollToTop() {
    let scrollBtn = document.getElementById('scrollToTop');
    if (!scrollBtn) {
        scrollBtn = document.createElement('button');
        scrollBtn.id = 'scrollToTop';
        scrollBtn.innerHTML = '<i class="fas fa-arrow-up"></i>';
        scrollBtn.style.position = 'fixed';
        scrollBtn.style.bottom = '30px';
        scrollBtn.style.right = '30px';
        scrollBtn.style.width = '50px';
        scrollBtn.style.height = '50px';
        scrollBtn.style.borderRadius = '50%';
        scrollBtn.style.backgroundColor = '#1F8A6B';
        scrollBtn.style.color = 'white';
        scrollBtn.style.border = 'none';
        scrollBtn.style.cursor = 'pointer';
        scrollBtn.style.display = 'none';
        scrollBtn.style.zIndex = '999';
        scrollBtn.style.transition = 'all 0.3s ease';
        document.body.appendChild(scrollBtn);

        scrollBtn.addEventListener('click', () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    window.addEventListener('scroll', () => {
        scrollBtn.style.display = window.scrollY > 300 ? 'block' : 'none';
    });
}

/* ============================================
   STATS BAR ANIMATION
   ============================================ */

function animateStats() {
    const statsSection = document.querySelector('.stats-bar');
    if (!statsSection) return;

    const statValues = statsSection.querySelectorAll('.stat-value');
    if (statValues.length === 0) return;

    const targets = [50000, 99.9, 24, 1247];
    const formats = ['k', 'percent', '247', 'plus'];

    statValues.forEach((stat, index) => {
        if (formats[index] === 'k') stat.innerText = '0+';
        else if (formats[index] === 'percent') stat.innerText = '0%';
        else if (formats[index] === '247') stat.innerText = '0/7';
        else if (formats[index] === 'plus') stat.innerText = '0+';
        else stat.innerText = '0';
    });

    statValues.forEach((stat, index) => {
        const target = targets[index];
        const format = formats[index];
        let current = 0;
        const duration = 2000;
        const step = target / (duration / 16);

        const timer = setInterval(() => {
            current += step;
            if (current >= target) {
                if (format === 'k') stat.innerText = '50K+';
                else if (format === 'percent') stat.innerText = '99.9%';
                else if (format === '247') stat.innerText = '24/7';
                else if (format === 'plus') stat.innerText = '1,247+';
                else stat.innerText = Math.floor(target).toLocaleString();
                clearInterval(timer);
            } else {
                if (format === 'k') {
                    let val = Math.floor(current);
                    stat.innerText = val >= 1000 ? Math.floor(val / 1000) + 'K+' : val + '+';
                } else if (format === 'percent') stat.innerText = current.toFixed(1) + '%';
                else if (format === '247') stat.innerText = Math.floor(current) + '/7';
                else if (format === 'plus') stat.innerText = Math.floor(current).toLocaleString() + '+';
                else stat.innerText = Math.floor(current).toLocaleString();
            }
        }, 16);
    });
}

function initStatsBar() {
    const statsBar = document.querySelector('.stats-bar');
    if (!statsBar) return;

    let animated = false;
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting && !animated) {
                animated = true;
                animateStats();
                observer.unobserve(statsBar);
            }
        });
    }, { threshold: 0.2 });
    observer.observe(statsBar);
}

/* ============================================
   BLOG PAGE
   ============================================ */

const blogPosts = [
    { id: 1, title: "How AI is Revolutionizing Electrical Inspections", excerpt: "Discover how artificial intelligence is transforming the way we detect faults.", category: "AI Technology", date: "Jan 26, 2026", readTime: "5 min read", image: "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?w=400&h=220&fit=crop" },
    { id: 2, title: "Top 5 Electrical Safety Tips", excerpt: "Essential safety practices every facility manager should implement.", category: "Safety Tips", date: "Jan 24, 2026", readTime: "4 min read", image: "https://images.unsplash.com/photo-1581092921461-eab62e97a782?w=400&h=220&fit=crop" },
    { id: 3, title: "NEC 2026 Updates", excerpt: "Key changes in the National Electrical Code.", category: "Industry News", date: "Jan 22, 2026", readTime: "6 min read", image: "https://images.unsplash.com/photo-1504328345606-18bbc8c9d7d1?w=400&h=220&fit=crop" },
    { id: 4, title: "Case Study: 50% Faster Inspections with AI", excerpt: "How a major manufacturing plant reduced inspection time.", category: "Case Studies", date: "Jan 20, 2026", readTime: "7 min read", image: "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=220&fit=crop" },
    { id: 5, title: "The Future of Predictive Maintenance", excerpt: "Explore how machine learning can predict equipment failures.", category: "AI Technology", date: "Jan 18, 2026", readTime: "5 min read", image: "https://images.unsplash.com/photo-1581091226033-d5c48150dbaa?w=400&h=220&fit=crop" },
    { id: 6, title: "Common Electrical Hazards in Older Buildings", excerpt: "Identify and mitigate risks in aging electrical infrastructure.", category: "Safety Tips", date: "Jan 16, 2026", readTime: "4 min read", image: "https://images.unsplash.com/photo-1581092160562-40aa08e7882d?w=400&h=220&fit=crop" }
];

let currentBlogFilter = "all";
let blogSearchTerm = "";

function renderBlogs() {
    const grid = document.getElementById('blogGrid');
    if (!grid) return;

    let filtered = blogPosts.filter(post => {
        const matchCat = currentBlogFilter === "all" || post.category === currentBlogFilter;
        const matchSearch = post.title.toLowerCase().includes(blogSearchTerm) || post.excerpt.toLowerCase().includes(blogSearchTerm);
        return matchCat && matchSearch;
    });

    if (filtered.length === 0) {
        grid.innerHTML = '<div class="no-results"><i class="fas fa-search"></i><h3>No articles found</h3><p>Try adjusting your search or filter.</p></div>';
        return;
    }

    grid.innerHTML = filtered.map(post => `
        <div class="blog-card">
            <div class="blog-img"><img src="${post.image}" alt="${post.title}" loading="lazy"></div>
            <div class="blog-content">
                <span class="blog-category">${post.category}</span>
                <h3 class="blog-title">${post.title}</h3>
                <p class="blog-excerpt">${post.excerpt}</p>
                <div class="blog-meta">
                    <span><i class="far fa-calendar"></i> ${post.date}</span>
                    <span><i class="far fa-clock"></i> ${post.readTime}</span>
                    <a href="#" class="read-more" data-id="${post.id}">Read More →</a>
                </div>
            </div>
        </div>
    `).join('');
}

function initBlogPage() {
    renderBlogs();

    const categoryBtns = document.querySelectorAll('.category-btn');
    const searchBtn = document.getElementById('searchBtn');
    const searchInput = document.getElementById('searchInput');
    const newsletterBtn = document.getElementById('newsletterBtn');
    const newsletterEmail = document.getElementById('newsletterEmail');

    categoryBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            categoryBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            currentBlogFilter = btn.getAttribute('data-cat');
            renderBlogs();
        });
    });

    if (searchBtn && searchInput) {
        searchBtn.addEventListener('click', () => {
            blogSearchTerm = searchInput.value.toLowerCase();
            renderBlogs();
        });
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                blogSearchTerm = e.target.value.toLowerCase();
                renderBlogs();
            }
        });
    }

    if (newsletterBtn && newsletterEmail) {
        newsletterBtn.addEventListener('click', () => {
            const email = newsletterEmail.value.trim();
            if (email && email.includes('@') && email.includes('.')) {
                showToast(`✅ Subscribed! ${email} will receive our weekly newsletter.`);
                newsletterEmail.value = '';
            } else {
                showToast('Please enter a valid email address', true);
            }
        });
    }

    const blogGrid = document.getElementById('blogGrid');
    if (blogGrid) {
        blogGrid.addEventListener('click', (e) => {
            const readMore = e.target.closest('.read-more');
            if (readMore) {
                e.preventDefault();
                const postId = readMore.getAttribute('data-id');
                const post = blogPosts.find(p => p.id == postId);
                if (post) {
                    showToast(`📖 Opening: ${post.title}`);
                }
            }
        });
    }
}

/* ============================================
   LOCAL STORAGE CALCULATOR & COST PREFERENCES ENGINE
   ============================================ */

const ES_PREF_KEY = "electroscann_calc_settings";

const ES_CURRENCIES = {
    PKR: { code: "PKR", symbol: "₨ ", name: "Pakistani Rupee", rateFromPKR: 1.0 },
    USD: { code: "USD", symbol: "$", name: "US Dollar", rateFromPKR: 0.0036 },
    EUR: { code: "EUR", symbol: "€", name: "Euro", rateFromPKR: 0.0033 },
    GBP: { code: "GBP", symbol: "£", name: "British Pound", rateFromPKR: 0.0028 },
    AED: { code: "AED", symbol: "AED ", name: "UAE Dirham", rateFromPKR: 0.013 }
};

const ES_POWER_UNITS = {
    W: { code: "W", label: "Watts", factorFromW: 1.0, precision: 0 },
    kW: { code: "kW", label: "Kilowatts", factorFromW: 0.001, precision: 2 },
    HP: { code: "HP", label: "Horsepower", factorFromW: 0.00134102, precision: 2 }
};

function getCalcPreferences() {
    try {
        const stored = localStorage.getItem(ES_PREF_KEY);
        if (stored) {
            const parsed = JSON.parse(stored);
            return {
                currency: parsed.currency && ES_CURRENCIES[parsed.currency] ? parsed.currency : "PKR",
                powerUnit: parsed.powerUnit && ES_POWER_UNITS[parsed.powerUnit] ? parsed.powerUnit : "W",
                tariffRate: parseFloat(parsed.tariffRate) || 35.0
            };
        }
    } catch (e) {
        console.warn("Failed to load preferences from localStorage:", e);
    }
    return { currency: "PKR", powerUnit: "W", tariffRate: 35.0 };
}

function saveCalcPreferences(prefs) {
    try {
        const current = getCalcPreferences();
        const updated = { ...current, ...prefs };
        localStorage.setItem(ES_PREF_KEY, JSON.stringify(updated));
        
        window.dispatchEvent(new CustomEvent("es:preferencesUpdated", { detail: updated }));
        
        if (typeof showToast === "function") {
            const currObj = ES_CURRENCIES[updated.currency] || ES_CURRENCIES.PKR;
            const unitObj = ES_POWER_UNITS[updated.powerUnit] || ES_POWER_UNITS.W;
            showToast(`Saved to LocalStorage: ${currObj.code} (${currObj.symbol.trim()}) | ${unitObj.label} (${unitObj.code})`);
        }
    } catch (e) {
        console.error("Failed to save preferences to localStorage:", e);
    }
}

function convertCurrencyAmount(amountInPKR, currencyCode) {
    const curr = ES_CURRENCIES[currencyCode] || ES_CURRENCIES.PKR;
    const converted = amountInPKR * curr.rateFromPKR;
    const formattedNum = curr.code === "PKR" ?
        Math.round(converted).toLocaleString() :
        converted.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return {
        code: curr.code,
        symbol: curr.symbol,
        amount: converted,
        formatted: `${curr.symbol}${formattedNum}`
    };
}

function convertPowerAmount(watts, unitCode) {
    const unit = ES_POWER_UNITS[unitCode] || ES_POWER_UNITS.W;
    const converted = watts * unit.factorFromW;
    const formattedNum = unit.code === "W" ?
        Math.round(converted).toLocaleString() :
        converted.toLocaleString(undefined, { minimumFractionDigits: unit.precision, maximumFractionDigits: unit.precision });
    return {
        code: unit.code,
        label: unit.label,
        value: converted,
        formatted: `${formattedNum} ${unit.code}`
    };
}

function setupPreferencesUIControls() {
    const currencySel = document.getElementById("prefCurrencySelect");
    const powerUnitSel = document.getElementById("prefPowerUnitSelect");
    const tariffRateInp = document.getElementById("prefTariffRateInput");
    const tariffCodeLbl = document.getElementById("prefRateCurrencyCode");

    const currentPrefs = getCalcPreferences();

    if (currencySel) currencySel.value = currentPrefs.currency;
    if (powerUnitSel) powerUnitSel.value = currentPrefs.powerUnit;
    if (tariffRateInp) tariffRateInp.value = currentPrefs.tariffRate;
    if (tariffCodeLbl) tariffCodeLbl.innerText = currentPrefs.currency;

    if (currencySel) {
        currencySel.addEventListener("change", (e) => {
            saveCalcPreferences({ currency: e.target.value });
            if (tariffCodeLbl) tariffCodeLbl.innerText = e.target.value;
        });
    }

    if (powerUnitSel) {
        powerUnitSel.addEventListener("change", (e) => {
            saveCalcPreferences({ powerUnit: e.target.value });
        });
    }

    if (tariffRateInp) {
        tariffRateInp.addEventListener("change", (e) => {
            const val = parseFloat(e.target.value) || 35.0;
            saveCalcPreferences({ tariffRate: val });
        });
    }
}

/* ============================================
   CALCULATOR PAGE
   ============================================ */

let counters = { lights: 6, fans: 2, sockets: 6, ac: 1, heavy: 0 };

const counterInputMap = {
    lights: 'lightsInput',
    fans: 'fansInput',
    sockets: 'socketsInput',
    ac: 'acInput',
    heavy: 'heavyInput'
};

const counterDisplayMap = {
    lights: 'lightVal',
    fans: 'fanVal',
    sockets: 'socketVal',
    ac: 'acVal',
    heavy: 'heavyVal'
};

function syncCounterInputs() {
    Object.keys(counterInputMap).forEach(type => {
        const input = document.getElementById(counterInputMap[type]);
        if (input) input.value = counters[type];
    });
}

function modifyCounter(type, delta) {
    let currentVal = Number(counters[type]);
    if (isNaN(currentVal)) currentVal = 0;
    let newVal = currentVal + delta;
    if (newVal >= 0 && newVal <= 30) {
        counters[type] = newVal;
        const element = document.getElementById(counterDisplayMap[type]);
        if (element) element.innerText = counters[type];
        syncCounterInputs();
        updateCalculatorDisplayWithPreferences();
    }
}

function updateCalculatorDisplayWithPreferences() {
    const prefs = getCalcPreferences();

    let loadWatts = (counters.lights * 60) + (counters.fans * 80) + (counters.sockets * 150) + (counters.ac * 2000) + (counters.heavy * 3000);

    const loadDisp = document.getElementById('loadDisplay');
    if (loadDisp && loadDisp.innerText && loadDisp.innerText !== '--') {
        const parsedLoad = parseFloat(loadDisp.innerText.replace(/[^0-9.]/g, ''));
        if (!isNaN(parsedLoad) && parsedLoad > 0) loadWatts = parsedLoad;
    }

    // Convert Power Unit
    const convertedPower = convertPowerAmount(loadWatts, prefs.powerUnit);
    const loadConvEl = document.getElementById('loadConvertedDisplay');
    if (loadConvEl) {
        loadConvEl.innerText = convertedPower.formatted;
    }

    // Convert Cost (Daily & Monthly based on tariff rate per kWh in PKR base)
    const kwhPerDay = (loadWatts / 1000) * 8; // 8 hours avg daily operational usage
    const dailyCostPKR = kwhPerDay * prefs.tariffRate;
    const monthlyCostPKR = dailyCostPKR * 30;

    const dailyCostConv = convertCurrencyAmount(dailyCostPKR, prefs.currency);
    const monthlyCostConv = convertCurrencyAmount(monthlyCostPKR, prefs.currency);

    const costDailyEl = document.getElementById('costDailyDisplay');
    const costMonthlyEl = document.getElementById('costMonthlyDisplay');

    if (costDailyEl) costDailyEl.innerText = dailyCostConv.formatted + " / day";
    if (costMonthlyEl) costMonthlyEl.innerText = monthlyCostConv.formatted + " / mo";

    // Update Fixture Labels dynamically based on preferred unit
    const counterGroups = document.querySelectorAll('.counter-group');
    const fixtureConfigs = [
        { name: 'Lights', watts: 60 },
        { name: 'Fans', watts: 80 },
        { name: 'Sockets', watts: 150 },
        { name: 'AC Units', watts: 2000 },
        { name: 'Heavy Loads', watts: 3000 }
    ];

    counterGroups.forEach((group, idx) => {
        if (fixtureConfigs[idx]) {
            const labelEl = group.querySelector('.counter-label');
            if (labelEl) {
                const p = convertPowerAmount(fixtureConfigs[idx].watts, prefs.powerUnit);
                labelEl.innerText = `${fixtureConfigs[idx].name} (${p.formatted})`;
            }
        }
    });
}

async function performApiCalculation() {
    const form = document.getElementById('calculatorForm');
    const calculateBtn = document.getElementById('calculateBtn');
    if (!form) return;

    syncCounterInputs();

    const payload = {
        roomLength: parseFloat(document.getElementById('roomLength')?.value) || 0,
        roomWidth: parseFloat(document.getElementById('roomWidth')?.value) || 0,
        roomHeight: parseFloat(document.getElementById('roomHeight')?.value) || 0,
        lights: Number(counters.lights) || 0,
        fans: Number(counters.fans) || 0,
        sockets: Number(counters.sockets) || 0,
        acUnits: Number(counters.ac) || 0,
        heavyLoads: Number(counters.heavy) || 0
    };

    let originalBtnHtml = '';
    if (calculateBtn) {
        originalBtnHtml = calculateBtn.innerHTML;
        calculateBtn.disabled = true;
        calculateBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Calculating...';
    }

    try {
        const response = await fetch('/api/calculator/calculate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!response.ok) throw new Error('API request failed');

        const data = await response.json();

        if (data && data.success) {
            // Update UI with API response
            const wireLengthDisplay = document.getElementById('wireLengthDisplay');
            const wireGaugeDisplay = document.getElementById('wireGaugeDisplay');
            const loadDisplay = document.getElementById('loadDisplay');
            const currentDisplay = document.getElementById('currentDisplay');
            const breakerDisplay = document.getElementById('breakerDisplay');
            const areaDisplay = document.getElementById('areaDisplay');
            const safetyMessage = document.getElementById('safetyMessage');

            if (wireLengthDisplay) wireLengthDisplay.innerText = data.wireLength;
            if (wireGaugeDisplay) wireGaugeDisplay.innerText = data.wireGauge;
            if (loadDisplay) loadDisplay.innerText = data.load;
            if (currentDisplay) currentDisplay.innerText = data.currentAmps;
            if (breakerDisplay) breakerDisplay.innerText = data.breakerRating;
            if (areaDisplay) areaDisplay.innerText = data.roomArea;
            if (safetyMessage) safetyMessage.textContent = data.safetyMessage;

            updateCalculatorDisplayWithPreferences();

            // Render History
            if (data.history && data.history.length > 0) {
                const historyPanel = document.getElementById('historyPanel');
                const historyTbody = document.getElementById('historyTableBody');
                if (historyPanel) historyPanel.classList.remove('d-none');
                if (historyTbody) {
                    historyTbody.innerHTML = data.history.map(item => `
                        <tr>
                            <td>${item.dateFormatted}</td>
                            <td><strong>${item.loadFormatted}</strong></td>
                            <td>${item.distanceFormatted}</td>
                            <td><span class="badge bg-secondary">${item.wireSize}</span></td>
                            <td class="history-summary">${item.result}</td>
                        </tr>
                    `).join('');
                }
            }

            if (typeof showToast === 'function') {
                showToast('Calculation computed & history updated via API! ⚡');
            }
        }
    } catch (err) {
        console.error('API Error:', err);
        if (typeof showToast === 'function') {
            showToast('Failed to calculate. Please check inputs and try again.');
        }
    } finally {
        if (calculateBtn) {
            calculateBtn.disabled = false;
            calculateBtn.innerHTML = originalBtnHtml;
        }
    }
}

function initCalculatorPage() {
    const form = document.getElementById('calculatorForm');
    if (!form) return;

    // Initialize Preferences Controls & Listeners
    setupPreferencesUIControls();
    updateCalculatorDisplayWithPreferences();
    window.addEventListener("es:preferencesUpdated", updateCalculatorDisplayWithPreferences);

    // Seed counters safely from hidden input values or defaults
    const parseSafeVal = (id, fallback) => {
        const val = parseInt(document.getElementById(id)?.value, 10);
        return isNaN(val) ? fallback : val;
    };

    counters.lights = parseSafeVal('lightsInput', 6);
    counters.fans = parseSafeVal('fansInput', 2);
    counters.sockets = parseSafeVal('socketsInput', 6);
    counters.ac = parseSafeVal('acInput', 1);
    counters.heavy = parseSafeVal('heavyInput', 0);

    Object.keys(counterDisplayMap).forEach(type => {
        const el = document.getElementById(counterDisplayMap[type]);
        if (el) el.innerText = counters[type];
    });
    syncCounterInputs();
    updateCalculatorDisplayWithPreferences();

    // Intercept form submission to calculate via API asynchronously
    form.addEventListener('submit', function (e) {
        e.preventDefault();
        performApiCalculation();
    });
}

/* ============================================
   ABOUT PAGE
   ============================================ */

function initAboutPage() {
    const footerSubscribe = document.getElementById('footerSubscribeBtn');
    if (footerSubscribe) {
        footerSubscribe.addEventListener('click', () => {
            const emailField = document.getElementById('footerEmail');
            if (emailField && emailField.value.includes('@')) {
                showToast(`✅ Subscribed! ${emailField.value} will receive the latest AI electrical insights.`);
                emailField.value = '';
            } else {
                showToast('Please enter a valid email address.', true);
            }
        });
    }
}

/* ============================================
   SERVICES PAGE
   ============================================ */

function initServicesPage() {
    const serviceBtns = document.querySelectorAll('.service-card .btn-premium');
    serviceBtns.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            showToast('🔍 Service details — contact our team for a personalized demo.');
        });
    });
}

/* ============================================
   CONTACT PAGE
   ============================================ */

function initContactPage() {
    const contactForm = document.getElementById('contactFormAction');
    const contactSubmitBtn = document.getElementById('contactSubmitBtn');
    const contactAlert = document.getElementById('contactFormAlert');

    if (contactForm) {
        contactForm.addEventListener('submit', async (e) => {
            e.preventDefault();

            if (contactAlert) {
                contactAlert.className = 'alert d-none align-items-center gap-2 mb-4';
                contactAlert.innerHTML = '';
            }

            let originalBtnContent = '';
            if (contactSubmitBtn) {
                originalBtnContent = contactSubmitBtn.innerHTML;
                contactSubmitBtn.disabled = true;
                contactSubmitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Sending...';
            }

            try {
                const formData = new FormData(contactForm);
                const response = await fetch('/Home/Contact', {
                    method: 'POST',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'Accept': 'application/json'
                    },
                    body: formData
                });

                if (response.ok) {
                    const contentType = response.headers.get('content-type') || '';
                    if (contentType.includes('application/json')) {
                        const data = await response.json();
                        if (data && data.success) {
                            if (contactAlert) {
                                contactAlert.className = 'alert alert-success d-flex align-items-center gap-2 mb-4';
                                contactAlert.style.borderRadius = '12px';
                                contactAlert.innerHTML = `<i class="fas fa-check-circle fs-5"></i> <div>${data.message}</div>`;
                            }
                            if (typeof showToast === 'function') {
                                showToast(data.message);
                            }
                            contactForm.reset();
                        } else {
                            const errorText = (data && data.message) ? data.message : 'Please check your inputs and try again.';
                            if (contactAlert) {
                                contactAlert.className = 'alert alert-danger d-flex align-items-center gap-2 mb-4';
                                contactAlert.style.borderRadius = '12px';
                                contactAlert.innerHTML = `<i class="fas fa-exclamation-triangle fs-5"></i> <div>${errorText}</div>`;
                            }
                            if (typeof showToast === 'function') {
                                showToast(errorText, true);
                            }
                        }
                    } else {
                        // Regular HTML response fallback
                        if (contactAlert) {
                            contactAlert.className = 'alert alert-success d-flex align-items-center gap-2 mb-4';
                            contactAlert.style.borderRadius = '12px';
                            contactAlert.innerHTML = `<i class="fas fa-check-circle fs-5"></i> <div>Your message has been sent successfully!</div>`;
                        }
                        if (typeof showToast === 'function') {
                            showToast('Your message has been sent successfully!');
                        }
                        contactForm.reset();
                    }
                } else {
                    throw new Error('Server returned error status ' + response.status);
                }
            } catch (err) {
                console.error('Contact Form Submit Error:', err);
                if (contactAlert) {
                    contactAlert.className = 'alert alert-danger d-flex align-items-center gap-2 mb-4';
                    contactAlert.style.borderRadius = '12px';
                    contactAlert.innerHTML = `<i class="fas fa-exclamation-triangle fs-5"></i> <div>Unable to send message right now. Please try again.</div>`;
                }
                if (typeof showToast === 'function') {
                    showToast('Unable to send message right now. Please try again.', true);
                }
            } finally {
                if (contactSubmitBtn) {
                    contactSubmitBtn.disabled = false;
                    contactSubmitBtn.innerHTML = originalBtnContent;
                }
            }
        });
    }

    const footerBtn = document.getElementById('footerSubscribeBtnContact');
    if (footerBtn) {
        footerBtn.addEventListener('click', () => {
            const emailField = document.getElementById('footerEmailContact');
            if (emailField && emailField.value.includes('@')) {
                showToast(`Subscribed! ${emailField.value} will receive updates.`);
                emailField.value = '';
            } else {
                showToast('Please enter a valid email address.', true);
            }
        });
    }

    updateOfficeStatus();
    setInterval(updateOfficeStatus, 60000);
}

function updateOfficeStatus() {
    const dot = document.getElementById('statusDot');
    const text = document.getElementById('statusText');
    if (!dot || !text) return;

    const now = new Date();
    const hour = now.getHours();
    const day = now.getDay();
    const isWeekday = day >= 1 && day <= 5;
    const isBusinessHours = hour >= 9 && hour < 18;

    if (isWeekday && isBusinessHours) {
        dot.style.background = '#22C55E';
        text.innerHTML = 'Office Open<span>Mon–Fri, 9am – 6pm</span>';
    } else {
        dot.style.background = '#F59E0B';
        text.innerHTML = 'Currently Closed<span>We\'ll reply when we\'re back</span>';
    }
}

/* ============================================
   PRICING PAGE
   ============================================ */

let isYearly = false;

function updatePricing() {
    const basicPrice = document.getElementById('basicPrice');
    const proPrice = document.getElementById('proPrice');
    const entPrice = document.getElementById('entPrice');
    const basicPeriod = document.getElementById('basicPeriod');
    const proPeriod = document.getElementById('proPeriod');
    const entPeriod = document.getElementById('entPeriod');

    if (isYearly) {
        if (basicPrice) basicPrice.innerHTML = '$23<span>/mo</span>';
        if (proPrice) proPrice.innerHTML = '$63<span>/mo</span>';
        if (entPrice) entPrice.innerHTML = '$159<span>/mo</span>';
        if (basicPeriod) basicPeriod.innerText = 'Billed annually ($276/year)';
        if (proPeriod) proPeriod.innerText = 'Billed annually ($756/year)';
        if (entPeriod) entPeriod.innerText = 'Billed annually ($1,908/year)';
    } else {
        if (basicPrice) basicPrice.innerHTML = '$29<span>/mo</span>';
        if (proPrice) proPrice.innerHTML = '$79<span>/mo</span>';
        if (entPrice) entPrice.innerHTML = '$199<span>/mo</span>';
        if (basicPeriod) basicPeriod.innerText = 'Billed monthly';
        if (proPeriod) proPeriod.innerText = 'Billed monthly';
        if (entPeriod) entPeriod.innerText = 'Billed monthly';
    }
}

function initPricingPage() {
    const toggleSwitch = document.getElementById('toggleSwitch');
    const monthlyLabel = document.getElementById('monthlyLabel');
    const yearlyLabel = document.getElementById('yearlyLabel');

    if (toggleSwitch) {
        toggleSwitch.addEventListener('click', () => {
            isYearly = !isYearly;
            toggleSwitch.classList.toggle('yearly', isYearly);
            if (monthlyLabel) monthlyLabel.classList.toggle('active', !isYearly);
            if (yearlyLabel) yearlyLabel.classList.toggle('active', isYearly);
            updatePricing();
        });
    }
}

/* ============================================
   FAQ PAGE
   ============================================ */

function initFaqPage() {
    const accordionHeaders = document.querySelectorAll('.accordion-header');

    accordionHeaders.forEach(header => {
        header.addEventListener('click', () => {
            const item = header.parentElement;
            const parentGroup = item.closest('.accordion-list');
            if (parentGroup) {
                parentGroup.querySelectorAll('.accordion-item').forEach(i => {
                    if (i !== item) i.classList.remove('active');
                });
            }
            item.classList.toggle('active');
        });
    });

    const searchInput = document.getElementById('faqSearch');
    const searchBtn = document.getElementById('searchBtn');

    if (searchBtn && searchInput) {
        searchBtn.addEventListener('click', () => {
            const query = searchInput.value.trim().toLowerCase();
            const faqItems = document.querySelectorAll('.accordion-item');
            faqItems.forEach(item => {
                const question = item.querySelector('.accordion-header')?.innerText.toLowerCase() || '';
                const answer = item.querySelector('.accordion-content')?.innerText.toLowerCase() || '';
                if (question.includes(query) || answer.includes(query)) {
                    item.style.display = 'block';
                } else {
                    item.style.display = 'none';
                }
            });
        });
    }
}

/* ============================================
   DEMO PAGE
   ============================================ */

function initDemoPage() {
    // Camera Scan
    const startBtn = document.getElementById('startScanBtn');
    const resetCameraBtn = document.getElementById('resetCameraBtn');
    const cameraPlaceholder = document.getElementById('cameraPlaceholder');
    const scanResultArea = document.getElementById('scanResultArea');
    let isScanning = false;

    if (startBtn) {
        startBtn.addEventListener('click', () => {
            if (isScanning) {
                showToast('Scan already in progress...', true);
                return;
            }
            isScanning = true;
            if (cameraPlaceholder) {
                cameraPlaceholder.innerHTML = '<div style="width: 30px; height: 30px; border: 2px solid rgba(255,255,255,0.2); border-top-color: #1F8A6B; border-radius: 50%; animation: spin 0.8s linear infinite; margin: 0 auto;"></div><p style="margin-top: 12px;">AI analyzing wires...</p>';
            }

            setTimeout(() => {
                const random = Math.random();
                let html = '';
                if (random > 0.7) {
                    html = `<div class="alert-card alert-warning"><i class="fas fa-exclamation-triangle"></i> <strong>Warning:</strong> Faulty wire detected. Risk Level: HIGH. Confidence: 87%</div>`;
                    showToast('⚠️ Faulty wire detected!', true);
                } else if (random > 0.3) {
                    html = `<div class="alert-card alert-success"><i class="fas fa-check-circle"></i> <strong>All Systems Normal</strong> — Phase: OK | Neutral: OK | No faults found. Confidence: 96%</div>`;
                    showToast('✅ Scan complete. All systems normal.');
                } else {
                    html = `<div class="alert-card alert-critical"><i class="fas fa-fire"></i> <strong>CRITICAL:</strong> Burned wire detected! Immediate action required. Confidence: 94%</div>`;
                    showToast('🔥 CRITICAL: Burned wire detected!', true);
                }
                if (scanResultArea) scanResultArea.innerHTML = html;
                if (cameraPlaceholder) cameraPlaceholder.innerHTML = '<i class="fas fa-camera"></i><p>Ready to scan</p>';
                isScanning = false;
            }, 2500);
        });
    }

    if (resetCameraBtn) {
        resetCameraBtn.addEventListener('click', () => {
            if (scanResultArea) scanResultArea.innerHTML = '';
            if (cameraPlaceholder) cameraPlaceholder.innerHTML = '<i class="fas fa-camera"></i><p>Ready to scan</p>';
            isScanning = false;
            showToast('Camera reset. Ready for new scan.');
        });
    }

    // Upload Zone
    const uploadZone = document.getElementById('uploadZone');
    const fileInput = document.getElementById('fileInput');
    const imagePreview = document.getElementById('imagePreview');
    const previewImg = document.getElementById('previewImg');
    const uploadProgress = document.getElementById('uploadProgress');
    const uploadResultArea = document.getElementById('uploadResultArea');

    if (uploadZone && fileInput) {
        uploadZone.addEventListener('click', () => fileInput.click());
        uploadZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            uploadZone.classList.add('drag-over');
        });
        uploadZone.addEventListener('dragleave', () => uploadZone.classList.remove('drag-over'));
        uploadZone.addEventListener('drop', (e) => {
            e.preventDefault();
            uploadZone.classList.remove('drag-over');
            const file = e.dataTransfer.files[0];
            if (file && file.type.match('image.*')) processImage(file);
            else showToast('Please upload an image file', true);
        });
        fileInput.addEventListener('change', (e) => {
            if (e.target.files.length) processImage(e.target.files[0]);
        });
    }

    function processImage(file) {
        const reader = new FileReader();
        reader.onload = function (ev) {
            if (previewImg) previewImg.src = ev.target.result;
            if (imagePreview) imagePreview.style.display = 'block';
            if (uploadResultArea) {
                uploadResultArea.innerHTML = '<div style="background: var(--slate-100); border-radius: 20px; padding: 16px; text-align: center;"><div style="width: 30px; height: 30px; border: 2px solid var(--slate-200); border-top-color: #1F8A6B; border-radius: 50%; animation: spin 0.8s linear infinite; margin: 0 auto;"></div><p style="margin-top: 10px;">AI analyzing image...</p></div>';
            }

            let progress = 0;
            const interval = setInterval(() => {
                progress += Math.random() * 15 + 10;
                if (progress >= 100) {
                    progress = 100;
                    clearInterval(interval);
                    const random = Math.random() > 0.5;
                    const resultHtml = random ?
                        `<div class="alert-card alert-success"><i class="fas fa-check-circle"></i> <strong>Analysis Complete:</strong> Phase wire detected. No faults found. Confidence: 94%</div>` :
                        `<div class="alert-card alert-warning"><i class="fas fa-exclamation-triangle"></i> <strong>Warning:</strong> Potential wiring issue detected. Inspection recommended. Confidence: 87%</div>`;
                    if (uploadResultArea) uploadResultArea.innerHTML = resultHtml;
                    showToast('AI analysis complete!');
                }
                if (uploadProgress) uploadProgress.style.width = `${Math.floor(progress)}%`;
            }, 200);
        };
        reader.readAsDataURL(file);
    }

    const sampleImageBtn = document.getElementById('sampleImageBtn');
    if (sampleImageBtn) {
        sampleImageBtn.addEventListener('click', () => {
            if (previewImg) previewImg.src = 'https://images.unsplash.com/photo-1581091226033-d5c48150dbaa?w=400&q=80';
            if (imagePreview) imagePreview.style.display = 'block';
            if (uploadProgress) uploadProgress.style.width = '96%';
            if (uploadResultArea) {
                uploadResultArea.innerHTML = `<div class="alert-card alert-success"><i class="fas fa-check-circle"></i> <strong>Sample Analysis Complete:</strong> Phase Wire: Detected ✓ | Neutral: OK ✓ | Faulty: None<br><small>Confidence: 96% | Risk: LOW</small></div>`;
            }
            showToast('Sample image analyzed successfully!');
        });
    }

    // Wire Tester
    document.querySelectorAll('.wire-badge').forEach(badge => {
        badge.addEventListener('click', () => {
            const wireType = badge.getAttribute('data-wire');
            const resultDiv = document.getElementById('wireTestResult');
            if (resultDiv) {
                resultDiv.innerHTML = '<div style="width: 30px; height: 30px; border: 2px solid var(--slate-200); border-top-color: #1F8A6B; border-radius: 50%; animation: spin 0.8s linear infinite; margin: 0 auto;"></div><p style="margin-top: 10px;">AI analyzing...</p>';

                setTimeout(() => {
                    let html = '';
                    if (wireType === 'phase') {
                        html = `<div><i class="fas fa-bolt" style="font-size: 2rem; color: #F5B700;"></i><h4 style="margin: 12px 0;">Phase Wire Detected</h4><span class="risk-low" style="display: inline-block; background: #DCFCE7; padding: 4px 12px; border-radius: 20px;">Active ✓</span><p style="margin-top: 12px;">Voltage: 220V | Frequency: 50Hz | Confidence: 98%</p></div>`;
                    } else if (wireType === 'neutral') {
                        html = `<div><i class="fas fa-minus-circle" style="font-size: 2rem; color: #00E0FF;"></i><h4 style="margin: 12px 0;">Neutral Wire Identified</h4><span class="risk-low" style="display: inline-block; background: #DCFCE7; padding: 4px 12px; border-radius: 20px;">OK ✓</span><p style="margin-top: 12px;">Continuity: Good | Confidence: 97%</p></div>`;
                    } else if (wireType === 'faulty') {
                        html = `<div><i class="fas fa-exclamation-triangle" style="font-size: 2rem; color: #F59E0B;"></i><h4 style="margin: 12px 0; color: #F59E0B;">⚠️ FAULTY WIRE DETECTED</h4><span style="display: inline-block; background: #FEF3C7; padding: 4px 12px; border-radius: 20px;">Action Required</span><p style="margin-top: 12px;">Issue: Insulation breakdown | Confidence: 89%</p></div>`;
                    } else {
                        html = `<div><i class="fas fa-fire" style="font-size: 2rem; color: #DC2626;"></i><h4 style="margin: 12px 0; color: #DC2626;">🔥 BURNED WIRE DETECTED</h4><span style="display: inline-block; background: #FEE2E2; padding: 4px 12px; border-radius: 20px;">CRITICAL</span><p style="margin-top: 12px;">Emergency: Turn off power NOW | Confidence: 92%</p></div>`;
                    }
                    resultDiv.innerHTML = html;
                    showToast(`AI detected: ${wireType.toUpperCase()} wire`);
                }, 800);
            }
        });
    });

    // Risk Slider
    const riskSlider = document.getElementById('riskSlider');
    const riskValueSpan = document.getElementById('riskValue');
    const riskMeter = document.getElementById('riskMeter');
    const safetyRecommendation = document.getElementById('safetyRecommendation');

    if (riskSlider) {
        riskSlider.addEventListener('input', (e) => {
            const val = parseInt(e.target.value);
            if (riskMeter) riskMeter.style.width = `${val}%`;

            let riskClass = '', riskText = '', recIcon = '', recText = '', recBg = '';

            if (val < 30) {
                riskClass = 'risk-low';
                riskText = `${val}% (Low)`;
                recIcon = '<i class="fas fa-check-circle" style="color: #22C55E;"></i>';
                recText = 'System appears safe. Regular maintenance recommended.';
                recBg = '#F0FDF4';
            } else if (val < 70) {
                riskClass = 'risk-medium';
                riskText = `${val}% (Medium)`;
                recIcon = '<i class="fas fa-exclamation-triangle" style="color: #F59E0B;"></i>';
                recText = 'Some concerns detected. Schedule inspection soon.';
                recBg = '#FEF3C7';
            } else {
                riskClass = 'risk-high';
                riskText = `${val}% (High)`;
                recIcon = '<i class="fas fa-fire" style="color: #DC2626;"></i>';
                recText = 'CRITICAL: Immediate action required. Contact electrician now!';
                recBg = '#FEE2E2';
            }

            if (riskValueSpan) riskValueSpan.innerHTML = `Current Risk: <span class="${riskClass}">${riskText}</span>`;
            if (safetyRecommendation) {
                safetyRecommendation.innerHTML = `${recIcon}<span>${recText}</span>`;
                safetyRecommendation.style.background = recBg;
            }
        });
    }
}

/* ============================================
   ESTIMATION PAGE
   ============================================ */

let inventory = [
    { id: 1, code: "WIRE-001", name: "7/0.029 Wire (Pakistan Cable)", unit: "Meter", rate: 45, qty: 500 },
    { id: 2, code: "WIRE-002", name: "7/0.036 Wire (Pakistan Cable)", unit: "Meter", rate: 65, qty: 200 },
    { id: 3, code: "PIPE-001", name: "PVC Conduit Pipe (1/2\")", unit: "Pieces", rate: 120, qty: 50 },
    { id: 4, code: "PIPE-002", name: "PVC Conduit Pipe (3/4\")", unit: "Pieces", rate: 180, qty: 30 },
    { id: 5, code: "SW-001", name: "Switches (Pakistan Switch)", unit: "Pieces", rate: 250, qty: 15 },
    { id: 6, code: "SK-001", name: "Sockets (3-Pin)", unit: "Pieces", rate: 350, qty: 10 },
    { id: 7, code: "MCB-001", name: "MCB Breaker (Schneider 20A)", unit: "Pieces", rate: 1200, qty: 5 },
    { id: 8, code: "DB-001", name: "Distribution Box (8-Way)", unit: "Piece", rate: 3500, qty: 1 }
];

function renderEstimator() {
    const tbody = document.getElementById('estimateBody');
    if (!tbody) return;

    const prefs = getCalcPreferences();

    tbody.innerHTML = '';
    let subtotal = 0;

    inventory.forEach(item => {
        const amount = item.rate * item.qty;
        subtotal += amount;
        const convRate = convertCurrencyAmount(item.rate, prefs.currency);
        const convAmount = convertCurrencyAmount(amount, prefs.currency);
        const row = `
            <tr>
                <td><i class="fas fa-plug" style="color: var(--moss); margin-right: 8px;"></i>${item.name}</td>
                <td><input type="number" class="quantity-input" data-id="${item.id}" value="${item.qty}" min="0" style="width:80px;"></td>
                <td>${item.unit}</td>
                <td>${convRate.formatted}</td>
                <td class="fw-bold">${convAmount.formatted}</td>
            </tr>`;
        tbody.insertAdjacentHTML('beforeend', row);
    });

    document.querySelectorAll('.quantity-input').forEach(input => {
        input.addEventListener('change', (e) => {
            const id = parseInt(e.target.getAttribute('data-id'));
            const newQty = parseInt(e.target.value) || 0;
            const itemIndex = inventory.findIndex(i => i.id === id);
            if (itemIndex !== -1) inventory[itemIndex].qty = newQty;
            renderEstimator();
        });
    });

    const labor = subtotal * 0.15;
    const tax = subtotal * 0.05;
    const grand = subtotal + labor + tax;

    const subtotalVal = document.getElementById('subtotalVal');
    const laborVal = document.getElementById('laborVal');
    const taxVal = document.getElementById('taxVal');
    const grandTotalVal = document.getElementById('grandTotalVal');

    if (subtotalVal) subtotalVal.innerText = convertCurrencyAmount(subtotal, prefs.currency).formatted;
    if (laborVal) laborVal.innerText = convertCurrencyAmount(labor, prefs.currency).formatted;
    if (taxVal) taxVal.innerText = convertCurrencyAmount(tax, prefs.currency).formatted;
    if (grandTotalVal) grandTotalVal.innerText = convertCurrencyAmount(grand, prefs.currency).formatted;
}

function renderAdminRates() {
    const adminBody = document.getElementById('adminRatesBody');
    if (!adminBody) return;

    adminBody.innerHTML = '';
    inventory.forEach(item => {
        const row = `
            </tr>
                <td>${item.code}</td>
                <td>${item.name}</td>
                <td>${item.rate.toLocaleString()} PKR</td>
                <td><input type="number" class="rate-input-admin" data-rate-id="${item.id}" value="${item.rate}" step="10"></td>
            </tr>`;
        adminBody.insertAdjacentHTML('beforeend', row);
    });
}

function saveRatesFromAdmin() {
    const rateInputs = document.querySelectorAll('.rate-input-admin');
    rateInputs.forEach(input => {
        const id = parseInt(input.getAttribute('data-rate-id'));
        const newRate = parseFloat(input.value);
        if (!isNaN(newRate) && newRate > 0) {
            const itemIndex = inventory.findIndex(i => i.id === id);
            if (itemIndex !== -1) inventory[itemIndex].rate = newRate;
        }
    });
    renderEstimator();
    renderAdminRates();
    showToast('✅ Global market rates updated successfully');
}

function initEstimationPage() {
    setupPreferencesUIControls();
    window.addEventListener("es:preferencesUpdated", renderEstimator);

    const printBtn = document.getElementById('printEstimateBtn');
    const downloadBtn = document.getElementById('downloadEstimateBtn');
    const estimateTab = document.getElementById('estimateTabBtn');
    const adminTab = document.getElementById('adminTabBtn');
    const estimatorPanel = document.getElementById('estimatorPanel');
    const adminPanelDiv = document.getElementById('adminPanel');
    const saveRatesBtn = document.getElementById('saveRatesBtn');

    if (printBtn) printBtn.addEventListener('click', () => window.print());
    if (downloadBtn) {
        downloadBtn.addEventListener('click', () => {
            showToast('📄 Generating PDF report... (demo)');
            setTimeout(() => alert('PDF Export: ElectroScan_Cost_Estimate.pdf would be downloaded.'), 500);
        });
    }

    if (estimateTab && adminTab && estimatorPanel && adminPanelDiv) {
        estimateTab.addEventListener('click', () => {
            estimateTab.classList.add('active');
            adminTab.classList.remove('active');
            estimatorPanel.style.display = 'block';
            adminPanelDiv.style.display = 'none';
            renderEstimator();
        });
        adminTab.addEventListener('click', () => {
            adminTab.classList.add('active');
            estimateTab.classList.remove('active');
            estimatorPanel.style.display = 'none';
            adminPanelDiv.style.display = 'block';
            renderAdminRates();
        });
    }

    if (saveRatesBtn) saveRatesBtn.addEventListener('click', saveRatesFromAdmin);
    renderEstimator();
    renderAdminRates();
}


/* ============================================
   MARKET RATES PAGE
   ============================================ */

let marketItems = [
    { code: "WIRE-001", name: "7/0.029 Wire (Pakistan Cable)", cat: "Wire", unit: "meter", price: 45, trend: "+2%" },
    { code: "WIRE-002", name: "7/0.036 Wire (Pakistan Cable)", cat: "Wire", unit: "meter", price: 65, trend: "+3%" },
    { code: "WIRE-003", name: "7/0.044 Wire (Pakistan Cable)", cat: "Wire", unit: "meter", price: 85, trend: "-1%" },
    { code: "MCB-001", name: "20A MCB Breaker (Schneider)", cat: "Protection", unit: "piece", price: 1200, trend: "-2%" },
    { code: "MCB-002", name: "32A MCB Breaker (Schneider)", cat: "Protection", unit: "piece", price: 1500, trend: "+1%" },
    { code: "PIPE-001", name: "PVC Conduit Pipe 1/2\" (Heavy)", cat: "Conduit", unit: "piece", price: 120, trend: "+5%" },
    { code: "PIPE-002", name: "PVC Conduit Pipe 3/4\" (Heavy)", cat: "Conduit", unit: "piece", price: 180, trend: "+4%" },
    { code: "SW-001", name: "10A Switch (Pakistan Standard)", cat: "Accessories", unit: "piece", price: 250, trend: "-2%" },
    { code: "SK-001", name: "3-Pin Socket (Premium)", cat: "Accessories", unit: "piece", price: 350, trend: "0%" },
    { code: "LABOR-001", name: "Electrician Labor (Standard)", cat: "Labor", unit: "day", price: 1200, trend: "+8%" },
    { code: "CABLE-001", name: "3-Core 2.5mm² Armoured Cable", cat: "Wire", unit: "meter", price: 180, trend: "+2%" }
];

let marketActiveCategory = "all";
let marketActiveSearch = "";

function renderMarketTable() {
    const tbody = document.getElementById('marketTableBody');
    if (!tbody) return;

    const prefs = getCalcPreferences();

    const filtered = marketItems.filter(item => {
        const matchCat = marketActiveCategory === "all" || item.cat === marketActiveCategory;
        const matchSearch = item.name.toLowerCase().includes(marketActiveSearch) || item.code.toLowerCase().includes(marketActiveSearch);
        return matchCat && matchSearch;
    });

    tbody.innerHTML = '';
    filtered.forEach((item, idx) => {
        const converted = convertCurrencyAmount(item.price, prefs.currency);
        const row = document.createElement('tr');
        row.innerHTML = `
            <td style="font-family: monospace; color: var(--moss);">${item.code}</td>
            <td>${item.name}</td>
            <td>${item.cat}</td>
            <td>per ${item.unit}</td>
            <td><strong>${converted.formatted}</strong></td>
            <td class="${item.trend.includes('+') ? 'trend-up' : (item.trend.includes('-') ? 'trend-down' : '')}">${item.trend}</td>
            <td><input type="number" class="price-input" data-idx="${idx}" value="${item.price}" step="10"></td>
        `;
        tbody.appendChild(row);
    });

    document.querySelectorAll('.price-input').forEach(input => {
        input.addEventListener('change', (e) => {
            const idx = parseInt(e.target.getAttribute('data-idx'));
            const newPrice = parseFloat(e.target.value);
            if (!isNaN(newPrice) && newPrice > 0) {
                marketItems[idx].price = newPrice;
                const converted = convertCurrencyAmount(newPrice, prefs.currency);
                showToast(`✅ ${marketItems[idx].code} updated to ${converted.formatted}`);
                renderMarketTable();
            }
        });
    });
}

function initMarketRatesPage() {
    setupPreferencesUIControls();
    window.addEventListener("es:preferencesUpdated", renderMarketTable);

    const categoryBtns = document.querySelectorAll('.market-category-btn');
    const searchInput = document.getElementById('marketSearchInput');
    const bulkSaveBtn = document.getElementById('bulkSaveBtn');

    categoryBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            categoryBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            marketActiveCategory = btn.getAttribute('data-cat');
            renderMarketTable();
        });
    });

    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            marketActiveSearch = e.target.value.toLowerCase();
            renderMarketTable();
        });
    }

    if (bulkSaveBtn) {
        bulkSaveBtn.addEventListener('click', () => {
            showToast('💾 All market rates saved successfully! (Simulated)');
        });
    }

    renderMarketTable();
}

/* ============================================
   QUOTE PAGE
   ============================================ */

function initQuotePage() {
    const quoteForm = document.getElementById('quoteForm');
    if (quoteForm) {
        quoteForm.addEventListener('submit', (e) => {
            e.preventDefault();
            showToast('📋 Quote request submitted! Our team will contact you within 24 hours.');
            quoteForm.reset();
        });
    }
}

/* ============================================
   CAREER PAGE
   ============================================ */

function initCareerPage() {
    const applyBtns = document.querySelectorAll('.apply-btn');
    const speculativeBtn = document.getElementById('speculativeBtn');

    applyBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const jobTitle = btn.getAttribute('data-job') || 'this position';
            showToast(`✨ Application started for ${jobTitle}. Our recruiting team will reach out within 48 hours.`);
        });
    });

    if (speculativeBtn) {
        speculativeBtn.addEventListener('click', () => {
            showToast('📄 Thank you for your interest! We\'ve received your general application.');
        });
    }
}

/* ============================================
   PAGE INITIALIZATION
   ============================================ */

document.addEventListener('DOMContentLoaded', () => {
    // Common initializations
    initRevealAnimations();
    initMobileMenu();
    initScrollToTop();
    initStatsBar();

    // Page detection
    const path = window.location.pathname;
    const pageId = document.body.id || '';

    if (pageId === 'blog' || path.includes('blog')) initBlogPage();
    if (pageId === 'service' || path.includes('service')) initServicesPage();
    if (pageId === 'contact' || path.includes('contact')) initContactPage();
    if (pageId === 'about' || path.includes('about')) initAboutPage();
    if (pageId === 'pricing' || path.includes('pricing')) initPricingPage();
    if (pageId === 'calculator' || path.toLowerCase().includes('calculator')) initCalculatorPage();
    if (pageId === 'faq' || path.includes('faq')) initFaqPage();
    if (pageId === 'demo' || path.includes('demo')) initDemoPage();
    if (pageId === 'estimation' || path.includes('estimation')) initEstimationPage();
    if (pageId === 'login' || path.includes('login')) initLoginPage();
    if (pageId === 'market-rates' || path.includes('market-rates')) initMarketRatesPage();
    if (pageId === 'quote' || path.includes('quote')) initQuotePage();
    if (pageId === 'career' || path.includes('career')) initCareerPage();
});

// Add spin animation keyframes for demo page
const style = document.createElement('style');
style.textContent = `
    @keyframes spin {
        0% { transform: rotate(0deg); }
        100% { transform: rotate(360deg); }
    }
`;
document.head.appendChild(style);




// ============================================
// ADMIN DASHBOARD JS - ElectroScan AI
// Shared functions for all admin pages
// ============================================
(function ($) {
    "use strict";

    // Spinner
    var spinner = function () {
        setTimeout(function () {
            if ($('#spinner').length > 0) {
                $('#spinner').removeClass('show');
            }
        }, 1);
    };
    spinner();


    // Back to top button
    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('.back-to-top').fadeIn('slow');
        } else {
            $('.back-to-top').fadeOut('slow');
        }
    });
    $('.back-to-top').click(function () {
        $('html, body').animate({ scrollTop: 0 }, 1500, 'easeInOutExpo');
        return false;
    });


    // Sidebar Toggler
    $('.sidebar-toggler').click(function () {
        $('.sidebar, .content').toggleClass("open");
        return false;
    });


    // Progress Bar
    if ($.fn.waypoint && $('.pg-bar').length) {
        $('.pg-bar').waypoint(function () {
            $('.progress .progress-bar').each(function () {
                $(this).css("width", $(this).attr("aria-valuenow") + '%');
            });
        }, { offset: '80%' });
    } else {
        $('.progress .progress-bar').each(function () {
            $(this).css("width", $(this).attr("aria-valuenow") + '%');
        });
    }


    // Calender
    if ($.fn.datetimepicker && $('#calender').length) {
        $('#calender').datetimepicker({
            inline: true,
            format: 'L'
        });
    }


    // Testimonials carousel
    if ($.fn.owlCarousel && $(".testimonial-carousel").length) {
        $(".testimonial-carousel").owlCarousel({
            autoplay: true,
            smartSpeed: 1000,
            items: 1,
            dots: true,
            loop: true,
            nav: false
        });
    }


    // Chart Global Color & Dashboard Charts
    if (typeof Chart !== 'undefined') {
        Chart.defaults.color = "#6C7293";
        Chart.defaults.borderColor = "#000000";

        // Worldwide Sales Chart
        var el1 = $("#worldwide-sales").get(0);
        if (el1) {
            new Chart(el1.getContext("2d"), {
                type: "bar",
                data: {
                    labels: ["2016", "2017", "2018", "2019", "2020", "2021", "2022"],
                    datasets: [{
                        label: "USA",
                        data: [15, 30, 55, 65, 60, 80, 95],
                        backgroundColor: "rgba(235, 22, 22, .7)"
                    },
                    {
                        label: "UK",
                        data: [8, 35, 40, 60, 70, 55, 75],
                        backgroundColor: "rgba(235, 22, 22, .5)"
                    },
                    {
                        label: "AU",
                        data: [12, 25, 45, 55, 65, 70, 60],
                        backgroundColor: "rgba(235, 22, 22, .3)"
                    }
                    ]
                },
                options: { responsive: true }
            });
        }


        // Salse & Revenue Chart
        var el2 = $("#salse-revenue").get(0);
        if (el2) {
            new Chart(el2.getContext("2d"), {
                type: "line",
                data: {
                    labels: ["2016", "2017", "2018", "2019", "2020", "2021", "2022"],
                    datasets: [{
                        label: "Salse",
                        data: [15, 30, 55, 45, 70, 65, 85],
                        backgroundColor: "rgba(235, 22, 22, .7)",
                        fill: true
                    },
                    {
                        label: "Revenue",
                        data: [99, 135, 170, 130, 190, 180, 270],
                        backgroundColor: "rgba(235, 22, 22, .5)",
                        fill: true
                    }
                    ]
                },
                options: { responsive: true }
            });
        }


        // Single Line Chart
        var el3 = $("#line-chart").get(0);
        if (el3) {
            new Chart(el3.getContext("2d"), {
                type: "line",
                data: {
                    labels: [50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150],
                    datasets: [{
                        label: "Salse",
                        fill: false,
                        backgroundColor: "rgba(235, 22, 22, .7)",
                        data: [7, 8, 8, 9, 9, 9, 10, 11, 14, 14, 15]
                    }]
                },
                options: { responsive: true }
            });
        }


        // Single Bar Chart
        var el4 = $("#bar-chart").get(0);
        if (el4) {
            new Chart(el4.getContext("2d"), {
                type: "bar",
                data: {
                    labels: ["Italy", "France", "Spain", "USA", "Argentina" , "Pakistani"],
                    datasets: [{
                        backgroundColor: [
                            "rgba(235, 22, 22, .7)",
                            "rgba(235, 22, 22, .6)",
                            "rgba(235, 22, 22, .5)",
                            "rgba(235, 22, 22, .4)",
                            "rgba(235, 22, 22, .3)"
                        ],
                        data: [55, 49, 44, 24, 15]
                    }]
                },
                options: { responsive: true }
            });
        }


        // Pie Chart
        var el5 = $("#pie-chart").get(0);
        if (el5) {
            new Chart(el5.getContext("2d"), {
                type: "pie",
                data: {
                    labels: ["Italy", "France", "Spain", "USA", "Argentina"],
                    datasets: [{
                        backgroundColor: [
                            "rgba(235, 22, 22, .7)",
                            "rgba(235, 22, 22, .6)",
                            "rgba(235, 22, 22, .5)",
                            "rgba(235, 22, 22, .4)",
                            "rgba(235, 22, 22, .3)"
                        ],
                        data: [55, 49, 44, 24, 15]
                    }]
                },
                options: { responsive: true }
            });
        }


        // Doughnut Chart
        var el6 = $("#doughnut-chart").get(0);
        if (el6) {
            new Chart(el6.getContext("2d"), {
                type: "doughnut",
                data: {
                    labels: ["Italy", "France", "Spain", "USA", "Argentina"],
                    datasets: [{
                        backgroundColor: [
                            "rgba(235, 22, 22, .7)",
                            "rgba(235, 22, 22, .6)",
                            "rgba(235, 22, 22, .5)",
                            "rgba(235, 22, 22, .4)",
                            "rgba(235, 22, 22, .3)"
                        ],
                        data: [55, 49, 44, 24, 15]
                    }]
                },
                options: { responsive: true }
            });
        }
    }


})(jQuery);


