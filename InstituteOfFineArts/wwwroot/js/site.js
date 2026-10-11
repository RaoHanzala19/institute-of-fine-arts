/* Institute of Fine Arts — front-end behaviour (vanilla JS, no build step) */
(function () {
  'use strict';

  const $ = (s, r) => (r || document).querySelector(s);
  const $$ = (s, r) => Array.from((r || document).querySelectorAll(s));
  const csrf = () => ($('meta[name="csrf-token"]') || {}).content || '';

  /* ---- Theme ----------------------------------------------------------- */
  const root = document.documentElement;
  function applyTheme(t) {
    root.setAttribute('data-bs-theme', t);
    try { localStorage.setItem('ifa-theme', t); } catch (e) { /* private mode */ }
    $$('[data-theme-toggle] i').forEach(i => { i.className = t === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars'; });
    window.dispatchEvent(new Event('themechange'));
  }
  $$('[data-theme-toggle]').forEach(b => b.addEventListener('click', () =>
    applyTheme(root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark')));
  applyTheme(root.getAttribute('data-bs-theme') || 'light');

  /* ---- Sidebar (mobile) -------------------------------------------------- */
  $$('[data-nav-toggle]').forEach(b => b.addEventListener('click', () => document.body.classList.toggle('nav-open')));
  $$('.sidebar-backdrop').forEach(b => b.addEventListener('click', () => document.body.classList.remove('nav-open')));

  /* ---- Toasts ------------------------------------------------------------ */
  $$('.toast-x').forEach(t => {
    const hide = () => { t.classList.add('hiding'); setTimeout(() => t.remove(), 320); };
    const close = $('.btn-close', t);
    if (close) close.addEventListener('click', hide);
    setTimeout(hide, t.classList.contains('error') ? 9000 : 5500);
  });

  /* ---- Confirmation modal for destructive actions ------------------------- */
  let modalEl, modal, pendingForm;
  function ensureModal() {
    if (modalEl) return;
    modalEl = document.createElement('div');
    modalEl.className = 'modal fade';
    modalEl.tabIndex = -1;
    modalEl.innerHTML = `
      <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
        <div class="modal-body p-4 text-center">
          <div class="feature-ico mx-auto" style="background:var(--danger-soft);color:var(--danger)"><i class="bi bi-exclamation-triangle"></i></div>
          <h4 class="mb-2" data-c-title>Are you sure?</h4>
          <p class="text-muted mb-4" data-c-text></p>
          <div class="d-flex gap-2 justify-content-center">
            <button type="button" class="btn btn-soft" data-bs-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-primary" data-c-ok>Yes, continue</button>
          </div>
        </div></div></div>`;
    document.body.appendChild(modalEl);
    modal = new bootstrap.Modal(modalEl);
    $('[data-c-ok]', modalEl).addEventListener('click', () => { if (pendingForm) { pendingForm.dataset.confirmed = '1'; modal.hide(); pendingForm.requestSubmit ? pendingForm.requestSubmit() : pendingForm.submit(); } });
  }
  document.addEventListener('submit', e => {
    const f = e.target;
    const trigger = e.submitter && e.submitter.dataset.confirm ? e.submitter : (f.dataset.confirm ? f : null);
    if (!trigger || f.dataset.confirmed === '1') return;
    e.preventDefault();
    ensureModal();
    pendingForm = f;
    $('[data-c-title]', modalEl).textContent = trigger.dataset.confirmTitle || 'Are you sure?';
    $('[data-c-text]', modalEl).textContent = trigger.dataset.confirm;
    $('[data-c-ok]', modalEl).textContent = trigger.dataset.confirmOk || 'Yes, continue';
    modal.show();
  }, true);

  /* ---- Loading state on submit buttons ------------------------------------ */
  document.addEventListener('submit', e => {
    if (e.defaultPrevented) return;
    const f = e.target;
    if (f.matches('[data-no-loading]')) return;
    if (window.jQuery && jQuery(f).data('validator') && !jQuery(f).valid()) return;
    const b = (e.submitter && e.submitter.matches('.btn')) ? e.submitter : $('button[type=submit].btn-primary', f);
    if (b && !b.dataset.noLoading) setTimeout(() => b.classList.add('btn-loading'), 0);
  });
  window.addEventListener('pageshow', () => $$('.btn-loading').forEach(b => b.classList.remove('btn-loading')));

  /* ---- Filters auto-submit ------------------------------------------------- */
  $$('select[data-autosubmit]').forEach(s => s.addEventListener('change', () => s.form.submit()));
  $$('input[data-debounce]').forEach(i => {
    let t; i.addEventListener('input', () => { clearTimeout(t); t = setTimeout(() => i.form.submit(), 550); });
  });

  /* ---- Char counters -------------------------------------------------------- */
  $$('textarea[maxlength], input[data-counter]').forEach(el => {
    const max = el.getAttribute('maxlength'); if (!max) return;
    const c = document.createElement('div'); c.className = 'form-text text-end';
    el.insertAdjacentElement('afterend', c);
    const upd = () => { c.textContent = `${el.value.length} / ${max}`; };
    el.addEventListener('input', upd); upd();
  });

  /* ---- Password visibility ---------------------------------------------------- */
  $$('[data-toggle-password]').forEach(b => b.addEventListener('click', () => {
    const inp = $(b.dataset.togglePassword); if (!inp) return;
    inp.type = inp.type === 'password' ? 'text' : 'password';
    b.querySelector('i').className = inp.type === 'password' ? 'bi bi-eye' : 'bi bi-eye-slash';
  }));

  /* ---- Print ---------------------------------------------------------------------- */
  $$('[data-print]').forEach(b => b.addEventListener('click', () => window.print()));

  /* ---- Image dropzone with preview -------------------------------------------- */
  $$('[data-dropzone]').forEach(zone => {
    const input = $('input[type=file]', zone);
    const preview = $('[data-preview]', zone);
    const idle = $('[data-idle]', zone);
    const show = file => {
      if (!file) return;
      const okType = /jpe?g$/i.test(file.name);
      const err = $('[data-file-error]', zone.parentElement);
      if (err) err.textContent = '';
      if (!okType || file.size > 10 * 1024 * 1024) {
        if (err) err.textContent = !okType ? 'Only JPEG files (.jpg / .jpeg) are allowed.' : 'The file is larger than 10 MB.';
        input.value = ''; return;
      }
      const r = new FileReader();
      r.onload = ev => {
        preview.innerHTML = `<img src="${ev.target.result}" alt="Preview"><div class="small text-muted mt-2">${file.name} · ${(file.size / 1048576).toFixed(2)} MB</div>`;
        preview.classList.remove('d-none'); if (idle) idle.classList.add('d-none');
      };
      r.readAsDataURL(file);
    };
    zone.addEventListener('click', e => { if (e.target !== input) input.click(); });
    input.addEventListener('change', () => show(input.files[0]));
    ['dragenter', 'dragover'].forEach(ev => zone.addEventListener(ev, e => { e.preventDefault(); zone.classList.add('drag'); }));
    ['dragleave', 'drop'].forEach(ev => zone.addEventListener(ev, e => { e.preventDefault(); zone.classList.remove('drag'); }));
    zone.addEventListener('drop', e => { if (e.dataTransfer.files.length) { input.files = e.dataTransfer.files; show(input.files[0]); } });
  });

  /* ---- Like button ----------------------------------------------------------------- */
  document.addEventListener('click', async e => {
    const b = e.target.closest('.js-like'); if (!b) return;
    e.preventDefault(); e.stopPropagation();
    if (b.dataset.anon) { location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search); return; }
    try {
      const res = await fetch(`/Gallery/Like/${b.dataset.id}`, { method: 'POST', headers: { 'RequestVerificationToken': csrf() } });
      if (!res.ok) return;
      const d = await res.json();
      b.classList.toggle('liked', d.liked);
      $('.count', b).textContent = d.count;
      $('i', b).className = d.liked ? 'bi bi-heart-fill' : 'bi bi-heart';
    } catch (err) { /* ignore */ }
  });

  /* ---- Charts --------------------------------------------------------------------------- */
  const cssVar = n => getComputedStyle(root).getPropertyValue(n).trim();
  const palette = () => [cssVar('--brand'), cssVar('--indigo'), cssVar('--gold'), cssVar('--success'), cssVar('--info'), cssVar('--warning'), cssVar('--danger'), cssVar('--muted')];
  const charts = [];
  function drawCharts() {
    if (!window.Chart) return;
    charts.splice(0).forEach(c => c.destroy());
    const ink = cssVar('--muted'), line = cssVar('--line');
    Chart.defaults.font.family = cssVar('--font-body'); Chart.defaults.color = ink;
    $$('canvas[data-chart]').forEach(cv => {
      const cfg = JSON.parse(cv.dataset.chart);
      const pal = palette();
      const isRound = cfg.type === 'doughnut' || cfg.type === 'pie';
      const gradeColors = { Pending: '#a0978a', Best: '#e0b24a', Better: '#5bc394', Good: '#6aa8e8', Moderate: '#e7ac4a', Normal: '#8f8f9a', Disqualified: '#f0776f' };
      const colors = isRound ? cfg.labels.map((l, i) => gradeColors[l] || pal[i % pal.length]) : pal[0];
      const ds = { label: cfg.label || '', data: cfg.values, backgroundColor: colors, borderRadius: isRound ? 0 : 8, borderWidth: isRound ? 2 : 0, borderColor: cssVar('--surface'), maxBarThickness: 44, tension: .35, fill: cfg.type === 'line', pointRadius: cfg.type === 'line' ? 4 : 0 };
      if (cfg.type === 'line') { ds.borderColor = pal[0]; ds.backgroundColor = pal[0] + '22'; ds.borderWidth = 3; }
      const chart = new Chart(cv, {
        type: cfg.type || 'bar',
        data: { labels: cfg.labels, datasets: [ds] },
        options: {
          responsive: true, maintainAspectRatio: false,
          cutout: isRound ? '68%' : undefined,
          plugins: { legend: { display: isRound, position: 'bottom', labels: { boxWidth: 10, boxHeight: 10, usePointStyle: true, padding: 14 } },
                     tooltip: { callbacks: { label: c => ` ${cfg.prefix || ''}${Number(c.parsed.y ?? c.parsed).toLocaleString()}` } } },
          scales: isRound ? {} : { x: { grid: { display: false }, border: { display: false } }, y: { beginAtZero: true, grid: { color: line }, border: { display: false }, ticks: { precision: 0 } } }
        }
      });
      charts.push(chart);
    });
  }
  window.addEventListener('themechange', () => setTimeout(drawCharts, 30));
  if (document.readyState === 'complete') drawCharts(); else window.addEventListener('load', drawCharts);

  /* ---- Competition form: dynamic award tiers ----------------------------------------------- */
  const tiers = $('[data-tiers]');
  if (tiers) {
    const list = $('[data-tier-list]', tiers);
    const tpl = $('template', tiers);
    const reindex = () => $$('.tier-row', list).forEach((row, i) => {
      $$('[name]', row).forEach(el => { el.name = el.name.replace(/Awards\[\d+\]/, `Awards[${i}]`); });
      $$('[data-valmsg-for]', row).forEach(el => { el.setAttribute('data-valmsg-for', el.getAttribute('data-valmsg-for').replace(/Awards\[\d+\]/, `Awards[${i}]`)); });
    });
    $('[data-add-tier]', tiers).addEventListener('click', () => {
      const n = list.children.length;
      const html = tpl.innerHTML.replace(/__i__/g, n).replace(/__rank__/g, n + 1);
      list.insertAdjacentHTML('beforeend', html); reindex();
    });
    list.addEventListener('click', e => {
      const b = e.target.closest('[data-remove-tier]'); if (!b) return;
      b.closest('.tier-row').remove(); reindex();
    });
  }

  /* ---- Award form: cascade competition -> winners & tiers ------------------------------------ */
  const awardForm = $('[data-award-form]');
  if (awardForm) {
    const comp = $('[name=CompetitionId]', awardForm);
    const sub = $('[name=SubmissionId]', awardForm);
    const tier = $('[name=CompetitionAwardId]', awardForm);
    const awardId = awardForm.dataset.awardId || '';
    const load = async (keep) => {
      if (!comp.value) return;
      const r = await fetch(`/Awards/Candidates?competitionId=${comp.value}&awardId=${awardId}`);
      if (!r.ok) return; const d = await r.json();
      const selSub = keep ? sub.dataset.selected : '';
      sub.innerHTML = '<option value="">— choose the winning painting —</option>' + d.submissions.map(s => `<option value="${s.id}" ${String(s.id) === selSub ? 'selected' : ''}>${s.label}</option>`).join('');
      const selTier = keep ? tier.dataset.selected : '';
      tier.innerHTML = '<option value="">— no tier (use custom title) —</option>' + d.tiers.map(t => `<option value="${t.id}" ${t.taken ? 'disabled' : ''} ${String(t.id) === selTier ? 'selected' : ''}>${t.label}${t.taken ? ' — already awarded' : ''}</option>`).join('');
      const hint = $('[data-candidate-hint]', awardForm);
      if (hint) hint.textContent = d.submissions.length ? '' : 'No evaluated, eligible paintings for this competition yet.';
    };
    comp.addEventListener('change', () => load(false));
    if (!comp.disabled) load(true);
  }

  /* ---- Demo credentials helper on the login page ------------------------------------------------- */
  $$('[data-fill-login]').forEach(b => b.addEventListener('click', () => {
    $('#Email').value = b.dataset.email; $('#Password').value = b.dataset.password;
  }));
})();
