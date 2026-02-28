// ── Configuration ────────────────────────────────────
const API_BASE = '/api/auth';

// ── DOM Elements ─────────────────────────────────────
const tabLogin = document.getElementById('tab-login');
const tabRegister = document.getElementById('tab-register');
const tabsContainer = document.querySelector('.tabs');

const loginForm = document.getElementById('login-form');
const registerForm = document.getElementById('register-form');

const responseBox = document.getElementById('response-message');

// ── Tab Switching ────────────────────────────────────
function switchTab(tab) {
    const isLogin = tab === 'login';

    tabLogin.classList.toggle('active', isLogin);
    tabRegister.classList.toggle('active', !isLogin);
    tabsContainer.setAttribute('data-active', tab);

    loginForm.classList.toggle('active', isLogin);
    registerForm.classList.toggle('active', !isLogin);

    hideMessage();
}

tabLogin.addEventListener('click', () => switchTab('login'));
tabRegister.addEventListener('click', () => switchTab('register'));

// ── Message Display ──────────────────────────────────
function showMessage(text, type) {
    responseBox.textContent = text;
    responseBox.className = `response-message ${type}`;
    responseBox.classList.remove('hidden');
}

function hideMessage() {
    responseBox.classList.add('hidden');
    responseBox.className = 'response-message hidden';
}

// ── Button Loading State ─────────────────────────────
function setLoading(button, loading) {
    const span = button.querySelector('span');
    const spinner = button.querySelector('.spinner');

    button.disabled = loading;
    span.style.opacity = loading ? '0.5' : '1';

    if (loading) {
        spinner.classList.remove('hidden');
    } else {
        spinner.classList.add('hidden');
    }
}

// ── API Calls ────────────────────────────────────────
async function apiCall(endpoint, body) {
    const response = await fetch(`${API_BASE}/${endpoint}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
    });

    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
}

// ── Register Handler ─────────────────────────────────
registerForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideMessage();

    const email = document.getElementById('register-email').value.trim();
    const password = document.getElementById('register-password').value;
    const btn = document.getElementById('register-btn');

    setLoading(btn, true);

    try {
        const { ok, data } = await apiCall('register', { email, password });

        if (ok) {
            showMessage(`✓ Account created! You can now sign in as ${data.user.role}.`, 'success');
            registerForm.reset();
            // Auto-switch to login after short delay
            setTimeout(() => switchTab('login'), 1500);
        } else {
            showMessage(data.message || 'Registration failed.', 'error');
        }
    } catch {
        showMessage('Unable to reach the server. Please try again.', 'error');
    } finally {
        setLoading(btn, false);
    }
});

// ── Login Handler ────────────────────────────────────
loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideMessage();

    const email = document.getElementById('login-email').value.trim();
    const password = document.getElementById('login-password').value;
    const btn = document.getElementById('login-btn');

    setLoading(btn, true);

    try {
        const { ok, data } = await apiCall('login', { email, password });

        if (ok) {
            showMessage(`✓ Welcome back! Signed in as ${data.user.role}.`, 'success');
        } else {
            showMessage(data.message || 'Invalid email or password.', 'error');
        }
    } catch {
        showMessage('Unable to reach the server. Please try again.', 'error');
    } finally {
        setLoading(btn, false);
    }
});
