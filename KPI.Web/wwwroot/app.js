const app = document.querySelector('#app');
const dialog = document.querySelector('#dialog');
const dialogContent = document.querySelector('#dialog-content');
const connectionState = document.querySelector('#connection-state');
const statusDot = document.querySelector('.status-dot');
const titles = {
  dashboard: 'Gösterge Paneli', evaluation: 'Değerlendirme Yap', history: 'Değerlendirmeler',
  employees: 'Çalışanlar', professions: 'Meslekler', kpis: 'KPI Yönetimi', periods: 'Dönemler', goals: 'Şirket Hedefleri',
  authorization: 'Yetkilendirme', leaves: 'İzin Takibi'
};
const state = { periods: [], professions: [], employees: [], goals: [], evaluationItems: [], user: null, pages: [] };

const icon = name => `<svg aria-hidden="true"><use href="#i-${name}"/></svg>`;
const esc = value => String(value ?? '').replace(/[&<>'"]/g, character => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[character]));
const initials = value => String(value || '').split(/\s+/).slice(0, 2).map(x => x[0]).join('').toLocaleUpperCase('tr-TR');
const formatDate = value => value ? new Intl.DateTimeFormat('tr-TR', { day:'2-digit', month:'short', year:'numeric' }).format(new Date(value)) : '—';
const formatNumber = value => new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 2 }).format(value ?? 0);
const optionList = (items, selected, placeholder = 'Seçiniz') => `<option value="">${placeholder}</option>${items.map(x => `<option value="${x.id}" ${String(x.id) === String(selected ?? '') ? 'selected' : ''}>${esc(x.name ?? x.fullName)}</option>`).join('')}`;
const statusNames = ['Taslak', 'Tamamlandı', 'Çalışana İletildi', 'Çalışan Görüşü Alındı', 'Onaylandı'];
const statusBadge = status => `<span class="badge ${status === 0 ? 'draft' : status === 4 ? 'approved' : 'info'}">${statusNames[status] || 'Tamamlandı'}</span>`;

async function api(url, options = {}) {
  const { responseType, ...fetchOptions } = options;
  let csrfToken;
  if (!['GET','HEAD','OPTIONS'].includes((options.method||'GET').toUpperCase())) {
    const tokenResponse=await fetch('/api/auth/csrf',{cache:'no-store'});
    if(!tokenResponse.ok)throw new Error('Güvenlik doğrulaması alınamadı. Sayfayı yenileyip tekrar deneyin.');
    csrfToken=(await tokenResponse.json()).token;
  }
  const response = await fetch(url, {
    ...fetchOptions,
    headers: { ...(options.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }), ...(csrfToken?{'X-CSRF-TOKEN':csrfToken}:{}), ...(options.headers || {}) }
  });
  if (!response.ok) {
    let problem;
    try { problem = await response.json(); } catch { problem = {}; }
    const error = new Error(problem.detail || problem.title || (response.status === 413 ? 'Dosya çok büyük. En fazla 10 MB yükleyebilirsiniz.' : 'İşlem tamamlanamadı.'));
    error.status = response.status;
    throw error;
  }
  connectionState.textContent = 'Bağlı';
  statusDot.className = 'status-dot online';
  return response.status === 204 ? null : responseType === 'blob' ? response.blob() : response.json();
}

function spreadsheetActions(canImport = true, importOnly = false) {
  const description=importOnly?(canImport?'Çalışanların izinlerini Excel dosyasından yükleyip aşağıdaki listede görüntüleyin.':'Excel yüklemek için izin yükleme yetkisi gerekir.'):(canImport?'Seçili filtrelerdeki kayıtları indirin veya Excel dosyasından aktarın.':'Kayıtları indirebilirsiniz. İçe aktarma ve şablon indirme için kayıt yönetme yetkisi gerekir.');
  return `<section class="card spreadsheet-toolbar" aria-labelledby="excel-toolbar-title"><div><h2 id="excel-toolbar-title">Excel işlemleri</h2><p>${description}</p></div><div class="actions">${importOnly?'':'<button class="button secondary" id="excel-export">Excel dışa aktar</button>'}<button class="button secondary" id="excel-template" ${canImport?'':'disabled'}>Şablon indir</button><button class="button primary" id="excel-import" ${canImport?'':'disabled'}>Excel ${importOnly?'yükle':'içe aktar'}</button></div></section>`;
}

function downloadFile(blob, fileName) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url; link.download = fileName;
  document.body.append(link); link.click(); link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

function bindSpreadsheetActions(resource, getFilters, reload) {
  const download = async (button, template) => {
    button.disabled = true;
    try {
      const blob = await api(`/api/spreadsheets/${resource}/export`, {method:'POST', body:JSON.stringify(template ? {template:true} : getFilters()), responseType:'blob'});
      downloadFile(blob, `${resource}-${template ? 'sablon' : 'excel'}.xlsx`);
      toast(template ? 'Excel şablonu indirildi.' : 'Excel dosyası indirildi.');
    } catch (error) { toast(error.message, 'error'); }
    finally { button.disabled = false; }
  };
  const exportButton=document.querySelector('#excel-export');
  if(exportButton)exportButton.onclick = event => download(event.currentTarget, false);
  const template = document.querySelector('#excel-template');
  if (template) template.onclick = event => download(event.currentTarget, true);
  const upload = document.querySelector('#excel-import');
  if (upload) upload.onclick = () => {
    const evaluation = resource === 'evaluations';
    openDialog('Excel içe aktar', `<div class="form-grid"><p class="field full">Önce şablonu indirip doldurun veya dışa aktardığınız dosyayı kullanın. En fazla 10 MB ve 10.000 veri satırı yükleyebilirsiniz.</p>
      <div class="field full"><label for="excel-file">Excel dosyası (.xlsx)</label><input class="input" id="excel-file" type="file" name="file" accept=".xlsx" required></div>
      <p class="field full">${evaluation ? 'Her KPI ayrı satırda olmalı; aynı değerlendirme için aynı Aktarım Grubu kullanılmalıdır. Kayıtlar geçici olarak saklanır. Aynı çalışan ve döneme ait mevcut değerlendirmeler atlanır.' : 'ID boşsa yeni kayıt oluşturulur; doluysa mevcut kayıt güncellenir. Aktif alanı Hayır olan kayıtlar pasif yapılır.'} Başarılı kayıtlar saklanır; hatalar satır numarasıyla gösterilir.</p>
      ${evaluation && hasPermission(2) ? '<label class="check-row"><input type="checkbox" name="permanentEvaluations"> Kalıcı kaydet (onaylanır ve değiştirilemez)</label>' : ''}
      <label class="check-row"><input type="checkbox" name="confirmed" required> Dosyadaki kayıtların aktarılmasını ve belirtilen değişiklikleri onaylıyorum.</label></div>`, async form => {
      const file = form.get('file');
      if (!file?.size || !file.name.toLowerCase().endsWith('.xlsx')) throw new Error('Dolu bir .xlsx dosyası seçmelisiniz.');
      if (file.size > 10 * 1024 * 1024) throw new Error('Dosya en fazla 10 MB olabilir.');
      const permanent = form.get('permanentEvaluations') === 'on';
      if (permanent && !confirm('Değerlendirmeler kalıcı olarak onaylanacak ve değiştirilemeyecek. Devam edilsin mi?')) throw new Error('Kalıcı kayıt iptal edildi.');
      const body = new FormData(); body.append('file', file); body.append('permanentEvaluations', String(permanent));
      const result = await api(`/api/spreadsheets/${resource}/import`, {method:'POST', body});
      await reload();
      setTimeout(() => showSpreadsheetResult(result), 0);
    });
    dialogContent.querySelector('[type="submit"]').textContent = 'İçe aktar';
  };
}

function showSpreadsheetResult(result) {
  dialog.classList.remove('wide');
  dialogContent.innerHTML = `<div class="dialog-head"><h2>Excel aktarım sonucu</h2><button class="icon-button" data-close aria-label="Kapat">${icon('close')}</button></div>
    <div class="dialog-body"><p role="status">${result.importedCount} kayıt aktarıldı · ${result.skippedCount} kayıt atlandı · ${result.errors.length} kayıt hatalı.</p>
    ${result.errors.length ? `<p>Hatalı kayıtları düzeltip yeniden yükleyebilirsiniz. Başarıyla oluşturulan kayıtların tekrar eklenmemesi için onları dosyadan çıkarın.</p><ul>${result.errors.slice(0,100).map(error => `<li>${esc(error)}</li>`).join('')}</ul>${result.errors.length>100?'<p>İlk 100 hata gösteriliyor. Tümünü hata listesinden indirebilirsiniz.</p>':''}` : ''}</div>
    <div class="dialog-foot">${result.errors.length ? '<button class="button secondary" id="excel-errors">Hata listesini indir</button>' : ''}<button class="button primary" data-close>Kapat</button></div>`;
  dialogContent.querySelectorAll('[data-close]').forEach(button => button.onclick = () => dialog.close());
  const errors = document.querySelector('#excel-errors');
  if (errors) errors.onclick = () => downloadFile(new Blob(['\uFEFF' + result.errors.join('\r\n')], {type:'text/plain;charset=utf-8'}), 'excel-aktarim-hatalari.txt');
  dialog.showModal();
}

const hasPermission = permission => state.user?.permissions?.includes(permission);

function selectionFilter(id, label, items, selected, placeholder, {multiple = true, clearable = true} = {}) {
  const values = Array.isArray(selected) ? selected.map(String) : selected==null||selected==='' ? [] : [String(selected)];
  const names = items.filter(x=>values.includes(String(x.id))).map(x=>x.name??x.fullName);
  return `<div class="field"><span class="filter-label" id="${id}-label">${esc(label)}</span><details class="multi-filter" id="${id}" data-multiple="${multiple}" data-placeholder="${esc(placeholder)}"><summary class="filter-select" aria-labelledby="${id}-label ${id}-summary"><span id="${id}-summary">${esc(names.length===1?names[0]:names.length?`${names.length} seçim`:placeholder)}</span></summary><div class="multi-filter-options">${clearable?`<button type="button" class="multi-filter-reset">${esc(placeholder)}</button>`:''}${items.map(x=>`<label><input type="${multiple?'checkbox':'radio'}" name="${id}" value="${esc(x.id)}" ${values.includes(String(x.id))?'checked':''}><span>${esc(x.name??x.fullName)}</span></label>`).join('')}</div></details></div>`;
}

const selectedFilterValues = id => [...document.getElementById(id).querySelectorAll('input:checked')].map(x=>x.value);

function bindSelectionFilters(onChange, root = app) {
  const controller = new AbortController();
  const controls = [...root.querySelectorAll('.multi-filter')];
  controls.forEach(control => {
    const update = () => {
      const selected = [...control.querySelectorAll('input:checked')];
      control.querySelector('summary span').textContent = selected.length===1 ? selected[0].nextElementSibling.textContent : selected.length ? `${selected.length} seçim` : control.dataset.placeholder;
      if (control.dataset.multiple==='false') { control.open=false; control.querySelector('summary').focus(); }
      onChange(control);
    };
    control.onchange = update;
    const reset = control.querySelector('.multi-filter-reset');
    if (reset) reset.onclick = () => {
      control.querySelectorAll('input').forEach(input=>input.checked=false);
      update(); control.open=false; control.querySelector('summary').focus();
    };
    control.ontoggle = () => { if (control.open) controls.filter(x=>x!==control).forEach(x=>x.open=false); };
  });
  document.addEventListener('click', event => controls.filter(x=>!x.contains(event.target)).forEach(x=>x.open=false), {signal:controller.signal});
  document.addEventListener('keydown', event => {
    if (event.key!=='Escape') return;
    const open = controls.find(x=>x.open);
    if (open) { open.open=false; open.querySelector('summary').focus(); event.preventDefault(); event.stopPropagation(); }
  }, {signal:controller.signal});
  return controller;
}

let selectionControlSequence = 0;

function enhanceSelects(root) {
  const controller = new AbortController();
  root.querySelectorAll('select:not([data-selection-enhanced])').forEach(select => {
    const id = `${select.id || `select-${++selectionControlSequence}`}-selection`;
    const label = select.labels[0]?.textContent.trim()
      || select.parentElement.querySelector(':scope > label')?.textContent.trim()
      || select.getAttribute('aria-label') || select.options[0]?.textContent || 'Seçiniz';
    const originallyHidden = select.hidden;
    let picker, binding, changing = false;
    select.dataset.selectionEnhanced = 'true';
    select.hidden = true;
    const syncState = () => {
      const summary = picker.querySelector('summary');
      summary.setAttribute('aria-disabled', String(select.disabled));
      picker.querySelector('.filter-label').textContent = select.required ? `${label} (zorunlu)` : label;
      summary.tabIndex = select.disabled ? -1 : 0;
      picker.querySelector('details').setAttribute('aria-disabled', String(select.disabled));
      picker.querySelectorAll('input').forEach(input => {
        input.disabled = select.disabled || [...select.options].some(option => option.value===input.value && option.disabled);
      });
      const reset = picker.querySelector('.multi-filter-reset');
      if (reset) reset.disabled = select.disabled;
      if (select.disabled) picker.querySelector('details').open = false;
      if (select.validity.valid || select.disabled) {
        picker.querySelector('.selection-error').hidden = true;
        summary.removeAttribute('aria-invalid');
      }
    };
    const render = () => {
      binding?.abort();
      picker?.remove();
      const options = [...select.options];
      const empty = options.find(option=>option.value==='');
      const template = document.createElement('template');
      template.innerHTML = selectionFilter(id, label, options.filter(option=>option.value!=='').map(option=>({id:option.value,name:option.textContent})),
        [...select.selectedOptions].map(option=>option.value), empty?.textContent || 'Seçiniz',
        {multiple:select.multiple,clearable:select.multiple || Boolean(empty)});
      picker = template.content.firstElementChild;
      picker.classList.add('selection-field');
      const error = document.createElement('p');
      error.className = 'selection-error'; error.id = `${id}-error`; error.hidden = true; error.setAttribute('role','alert');
      picker.append(error);
      picker.querySelector('summary').setAttribute('aria-describedby',error.id);
      // Form values and native validity remain on the original select.
      // Only the native select contributes values to the owning form.
      picker.querySelectorAll('input').forEach(input=>input.setAttribute('form', `${id}-choices`));
      select.insertAdjacentElement('afterend',picker);
      picker.querySelector('summary').addEventListener('click', event=>{ if (select.disabled) event.preventDefault(); });
      binding = bindSelectionFilters(() => {
        const values = selectedFilterValues(id);
        if (select.multiple) options.forEach(option=>option.selected=values.includes(option.value));
        else select.value = values[0] ?? '';
        changing = true;
        try { select.dispatchEvent(new Event('change',{bubbles:true})); }
        finally { changing = false; syncState(); }
      }, picker);
      syncState();
    };
    render();
    select.addEventListener('change', () => { if (!changing) render(); }, {signal:controller.signal});
    select.addEventListener('invalid', event => {
      event.preventDefault();
      const error = picker.querySelector('.selection-error');
      error.textContent = select.validationMessage; error.hidden = false;
      const summary = picker.querySelector('summary');
      summary.setAttribute('aria-invalid','true');
      const firstInvalid = select.form?.querySelector('.selection-field summary[aria-invalid="true"]');
      if (!firstInvalid || firstInvalid===summary) {
        picker.querySelector('details').open = true;
        summary.focus();
      }
    }, {signal:controller.signal});
    select.form?.addEventListener('reset', () => queueMicrotask(() => { if (!controller.signal.aborted) render(); }), {signal:controller.signal});
    const observer = new MutationObserver(records => {
      if (records.some(record=>record.type==='childList' || record.target!==select)) render();
      else syncState();
    });
    observer.observe(select,{childList:true,subtree:true,attributes:true,attributeFilter:['disabled','required','selected','value','label']});
    controller.signal.addEventListener('abort', () => {
      observer.disconnect(); binding?.abort(); picker.remove();
      select.hidden = originallyHidden; delete select.dataset.selectionEnhanced;
    }, {once:true});
  });
  return controller;
}

function enhancePageSelects() {
  state.selectController?.abort();
  state.selectController = enhanceSelects(app);
}

function loading() {
  state.filterController?.abort();
  state.selectController?.abort();
  state.evaluationController?.abort();
  app.innerHTML = '<div class="loading"><div><div class="spinner"></div>Veriler hazırlanıyor…</div></div>';
}

function showError(error, retry) {
  connectionState.textContent = 'Bağlantı gerekli';
  statusDot.className = 'status-dot offline';
  app.innerHTML = `<div class="error-state">${icon('target')}<h2>Verilere ulaşılamadı</h2><p>${esc(error.message)}</p><button class="button primary" id="retry">Tekrar dene</button></div>`;
  document.querySelector('#retry').onclick = retry;
}

function toast(message, type = 'success') {
  const element = document.createElement('div');
  element.className = `toast ${type}`;
  element.textContent = message;
  document.querySelector('#toasts').append(element);
  setTimeout(() => element.remove(), 4200);
}

function openDialog(title, body, onSubmit, wide = false) {
  dialog.classList.toggle('wide', wide);
  dialogContent.innerHTML = `<form id="dialog-form"><div class="dialog-head"><h2>${esc(title)}</h2><button type="button" class="icon-button" data-close aria-label="Kapat">${icon('close')}</button></div><div class="dialog-body">${body}</div><div class="dialog-foot"><button type="button" class="button secondary" data-close>Vazgeç</button><button type="submit" class="button primary">Kaydet</button></div></form>`;
  dialogContent.querySelectorAll('[data-close]').forEach(x => x.onclick = () => dialog.close());
  dialogContent.querySelector('form').onsubmit = async event => {
    event.preventDefault();
    const form = event.currentTarget;
    if (form.dataset.saving === 'true') return;
    form.dataset.saving = 'true';
    const submits = [...form.querySelectorAll('[type="submit"]')];
    submits.forEach(x => x.disabled = true);
    try { await onSubmit(new FormData(form)); dialog.close(); }
    catch (error) { toast(error.message, 'error'); }
    finally { form.dataset.saving = 'false'; submits.forEach(x => x.disabled = false); }
  };
  const selections = enhanceSelects(dialogContent);
  dialog.addEventListener('close', () => selections.abort(), {once:true});
  dialog.showModal();
}

function pageHead(title, description, actions = '') {
  return `<div class="page-head"><div><h1>${title}</h1><p>${description}</p></div><div class="actions">${actions}</div></div>`;
}

function emptyState(title, description) {
  return `<div class="empty">${icon('target')}<strong>${title}</strong><span>${description}</span></div>`;
}

async function ensureLookups() {
  const [periods, professions] = await Promise.all([
    state.periods.length ? state.periods : api('/api/periods'),
    state.professions.length ? state.professions : api('/api/professions')
  ]);
  state.periods = periods;
  state.professions = professions;
}

async function dashboardPage() {
  loading();
  try {
    await ensureLookups();
    const selected = sessionStorage.getItem('dashboard-period') || '';
    const query = new URLSearchParams(); if (selected) query.set('periodId', selected);
    const [data, managers] = await Promise.all([api(`/api/dashboard?${query}`), api(`/api/dashboard/managers${selected ? `?periodId=${selected}` : ''}`)]);
    const results = data.results || [];
    app.innerHTML = `${pageHead('Gösterge Paneli', 'Ekip performansını ve hedef ilerlemesini tek görünümde izleyin.', selectionFilter('dashboard-period','Dönem',state.periods,selected,'Tüm dönemler',{multiple:false})) }
      <div class="manager-grid">${managers.map(x => `<button type="button" class="manager-card" data-manager="${x.userId}" aria-haspopup="dialog" aria-label="${esc(x.managerName)} · Değerlendirmeleri görüntüle" ${hasPermission(3)?'':'disabled'}><small>${esc(x.roleName)}</small><strong>${esc(x.managerName)}</strong><div class="manager-metrics"><span>Çalışan<b>${x.employeeCount}</b></span><span>Onaylı<b>${x.approvedEvaluationCount}</b></span><span>Bekleyen<b>${x.pendingEvaluationCount}</b></span><span>Ortalama<b>%${formatNumber(x.averagePercentage)}</b></span></div><span class="manager-card-link">${icon('eye')}${hasPermission(3)?'Değerlendirmeleri görüntüle':'Değerlendirme geçmişi yetkisi gerekli'}</span></button>`).join('')}</div>
      <div class="stats-grid">
        ${statCard('users','teal','Toplam çalışan',data.totalEmployees)}
        ${statCard('check','blue','Değerlendirilen',data.evaluatedEmployees)}
        ${statCard('history','amber','Bekleyen',data.notEvaluatedEmployees)}
        ${statCard('score','navy','Normalize başarı',`%${formatNumber(data.overallPercentage)}`)}
      </div>
      <div class="grid-2 dashboard-results">
        <section class="card"><div class="card-head"><h2>Performans sıralaması</h2><span class="badge info">${results.length} kayıt</span></div>
          <div class="table-wrap">${results.length ? `<table class="table"><thead><tr><th>Çalışan</th><th>Değerlendiren</th><th>Dönem</th><th>Puan</th><th>Başarı</th></tr></thead><tbody>${results.slice(0, 12).map(row => `<tr><td><div class="person"><span class="person-avatar">${initials(row.employeeName)}</span>${esc(row.employeeName)}</div></td><td>${esc(row.evaluatorName || 'Sistem')}</td><td>${esc(row.periodName)}</td><td><span class="score">${row.totalScore} / ${row.maximumScore}</span></td><td><span class="score ${row.scorePercentage >= 80 ? 'good' : row.scorePercentage >= 60 ? 'mid' : 'low'}">%${formatNumber(row.scorePercentage)}</span></td></tr>`).join('')}</tbody></table>` : emptyState('Henüz onaylı değerlendirme yok','Taslak ve onay bekleyen kayıtlar resmî sonuçlara katılmaz.')}</div>
        </section>
        <section class="card"><div class="card-head"><h2>Şirket hedefleri</h2></div><div class="card-body">${(data.goalProgress || []).length ? data.goalProgress.map(goal => `<div class="goal-row"><div class="goal-top"><span>${esc(goal.goalName)}</span><strong>${formatNumber(goal.averageScore)} / 5</strong></div><div class="progress"><span style="width:${Math.min(100, goal.averageScore * 20)}%"></span></div></div>`).join('') : emptyState('Hedef verisi yok','KPI maddelerini şirket hedefleriyle ilişkilendirin.')}</div></section>
      </div>`;
    state.filterController = bindSelectionFilters(() => { sessionStorage.setItem('dashboard-period', selectedFilterValues('dashboard-period')[0]||''); dashboardPage(); });
    document.querySelectorAll('[data-manager]').forEach(button => button.onclick = () => showManagerEvaluations(managers.find(x => String(x.userId) === button.dataset.manager), selected));
  } catch (error) { showError(error, dashboardPage); }
}

function showManagerEvaluations(manager, periodId) {
  const controller = new AbortController();
  let rows = [], search = '', status = '', filterController;
  const periodName = state.periods.find(x => String(x.id) === periodId)?.name || 'Tüm dönemler';
  const heading = `${manager.managerName} · Değerlendirmeler`;
  const shell = body => {
    filterController?.abort();
    dialog.classList.add('wide');
    dialog.setAttribute('aria-labelledby', 'manager-dialog-title');
    dialogContent.innerHTML = `<div class="dialog-head"><h2 id="manager-dialog-title" tabindex="-1">${esc(heading)}</h2><button type="button" class="icon-button" data-close aria-label="Kapat">${icon('close')}</button></div><div class="dialog-body"><p class="manager-dialog-context">${esc(manager.roleName)} · ${esc(periodName)}<br>Bu amirin hazırladığı, görüntüleme yetkiniz olan değerlendirmeler.</p>${body}</div><div class="dialog-foot"><button type="button" class="button secondary" data-close>Kapat</button></div>`;
    dialogContent.querySelectorAll('[data-close]').forEach(button => button.onclick = () => dialog.close());
    document.querySelector('#manager-dialog-title').focus();
  };
  const render = () => {
    const approved = rows.filter(x => x.status === 4);
    shell(`<div class="detail-grid"><div class="detail-box"><small>Görüntülenebilir kayıt</small><strong>${rows.length}</strong></div><div class="detail-box"><small>Onaylı / Diğer kayıtlar</small><strong>${approved.length} / ${rows.length-approved.length}</strong></div><div class="detail-box"><small>Onaylı kayıtların ortalaması</small><strong>${approved.length?'%'+formatNumber(approved.reduce((sum,x)=>sum+x.scorePercentage,0)/approved.length):'—'}</strong></div></div>
      <div class="toolbar"><div class="field"><label for="manager-evaluation-search">Çalışan veya ekip ara</label><input class="input" id="manager-evaluation-search" value="${esc(search)}" type="search"></div>${selectionFilter('manager-evaluation-status','Durum',statusNames.map((name,id)=>({id,name})),status,'Tüm durumlar',{multiple:false})}</div><p id="manager-evaluation-count" role="status"></p><div class="table-wrap" id="manager-evaluation-list"></div>`);
    const apply = () => {
      const term = search.toLocaleLowerCase('tr-TR');
      const filtered = rows.filter(x => (!term || `${x.employeeName} ${x.professionName}`.toLocaleLowerCase('tr-TR').includes(term)) && (status==='' || String(x.status)===status));
      document.querySelector('#manager-evaluation-count').textContent = `${filtered.length} değerlendirme`;
      document.querySelector('#manager-evaluation-list').innerHTML = filtered.length ? `<table class="table"><thead><tr><th>Çalışan / Ekip</th><th>Dönem / Tarih</th><th>Puan</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${filtered.map(row=>`<tr><td><strong>${esc(row.employeeName)}</strong><br><small>${esc(row.professionName)}</small></td><td>${esc(row.periodName)}<br><small>${formatDate(row.evaluationDate)}</small></td><td>${row.totalScore} / ${row.maximumScore}<br><small>%${formatNumber(row.scorePercentage)}</small></td><td>${statusBadge(row.status)}</td><td><button type="button" class="button secondary" data-manager-evaluation="${row.id}" aria-label="${esc(row.employeeName)} · ${esc(row.periodName)} değerlendirme ayrıntısını görüntüle">${icon('eye')}Ayrıntıyı görüntüle</button></td></tr>`).join('')}</tbody></table>` : emptyState('Değerlendirme bulunamadı', rows.length?'Arama veya durum filtresini değiştirebilirsiniz.':'Bu amir için seçili dönemde görüntüleyebileceğiniz değerlendirme bulunmuyor.');
      document.querySelectorAll('[data-manager-evaluation]').forEach(button=>button.onclick=async()=>{
        button.disabled = true;
        try { await showEvaluationDetail(Number(button.dataset.managerEvaluation), rows, render, controller.signal); }
        finally { button.disabled = false; }
      });
    };
    document.querySelector('#manager-evaluation-search').oninput = event => { search = event.target.value; apply(); };
    filterController = bindSelectionFilters(() => { status = selectedFilterValues('manager-evaluation-status')[0]||''; apply(); }, dialogContent);
    apply();
  };
  const load = async () => {
    shell('<div class="loading" role="status" aria-busy="true"><div><div class="spinner"></div>Değerlendirmeler yükleniyor…</div></div>');
    try {
      const query = new URLSearchParams({evaluatorUserId:String(manager.userId)});
      if (periodId) query.set('periodId', periodId);
      rows = await api(`/api/evaluations?${query}`, {signal:controller.signal});
      if (!controller.signal.aborted) render();
    } catch (error) {
      if (controller.signal.aborted) return;
      shell(`<div class="empty" role="alert"><strong>Değerlendirmeler yüklenemedi</strong><p>${esc(error.message)}</p><button type="button" class="button secondary" id="manager-retry">Tekrar dene</button></div>`);
      document.querySelector('#manager-retry').onclick = load;
    }
  };
  dialog.addEventListener('close', () => { controller.abort(); filterController?.abort(); dialog.removeAttribute('aria-labelledby'); }, {once:true});
  load();
  dialog.showModal();
}

function statCard(iconName, color, label, value) {
  return `<article class="stat-card"><span class="stat-icon ${color}">${icon(iconName)}</span><div><small>${label}</small><strong>${value}</strong></div></article>`;
}

async function employeesPage() {
  loading();
  try {
    const [employees, professions] = await Promise.all([api('/api/employees'), api('/api/professions')]);
    state.employees = employees; state.professions = professions;
    renderEmployees(employees);
  } catch (error) { showError(error, employeesPage); }
}

function renderEmployees(rows) {
  app.innerHTML = `${pageHead('Çalışanlar','Ekip üyelerini, mesleklerini ve aktiflik durumlarını yönetin.',`<button class="button primary" id="add-employee">${icon('plus')}Çalışan ekle</button>`)}${spreadsheetActions()}
    <section class="card"><div class="card-body"><div class="toolbar"><div class="search">${icon('search')}<input class="input" id="employee-search" placeholder="Ad veya meslek ara"></div>${selectionFilter('employee-profession','Meslekler',state.professions,[],'Tüm meslekler')}</div></div>
      <div class="table-wrap"><table class="table"><thead><tr><th>Çalışan</th><th>Meslek</th><th>Durum</th><th>İşlem</th></tr></thead><tbody id="employee-rows">${employeeRows(rows)}</tbody></table></div></section>`;
  document.querySelector('#add-employee').onclick = () => employeeEditor();
  bindSpreadsheetActions('employees', () => ({ids:[...document.querySelectorAll('[data-employee]')].map(x=>Number(x.dataset.employee))}), employeesPage);
  const apply = () => {
    const term = document.querySelector('#employee-search').value.toLocaleLowerCase('tr-TR');
    const professions = selectedFilterValues('employee-profession');
    const filtered = state.employees.filter(x => (!term || `${x.fullName} ${x.professionName}`.toLocaleLowerCase('tr-TR').includes(term)) && (!professions.length || professions.includes(String(x.professionId))));
    document.querySelector('#employee-rows').innerHTML = employeeRows(filtered);
    bindEmployeeActions();
  };
  document.querySelector('#employee-search').oninput = apply;
  state.filterController = bindSelectionFilters(apply);
  bindEmployeeActions();
}

function employeeRows(rows) {
  return rows.length ? rows.map(row => `<tr><td><div class="person"><span class="person-avatar">${initials(row.fullName)}</span>${esc(row.fullName)}</div></td><td>${esc(row.professionName)}</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td><div class="row-actions"><button class="row-action" data-employee="${row.id}" aria-label="Düzenle">${icon('edit')}</button></div></td></tr>`).join('') : `<tr><td colspan="4">${emptyState('Çalışan bulunamadı','Arama ölçütlerini değiştirebilirsiniz.')}</td></tr>`;
}

function bindEmployeeActions() {
  document.querySelectorAll('[data-employee]').forEach(button => button.onclick = () => employeeEditor(state.employees.find(x => x.id === Number(button.dataset.employee))));
}

function employeeEditor(row = {}) {
  openDialog(row.id ? 'Çalışanı düzenle' : 'Yeni çalışan', `<div class="form-grid"><div class="field full"><label>Ad soyad</label><input class="input" name="fullName" value="${esc(row.fullName)}" required maxlength="200"></div><div class="field"><label>Meslek</label><select class="filter-select" name="professionId" required>${optionList(state.professions, row.professionId)}</select></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive !== false ? 'checked' : ''}> Aktif çalışan</label></div>`, async form => {
    await api('/api/employees', { method:'POST', body:JSON.stringify({ id:row.id || null, fullName:form.get('fullName'), professionId:Number(form.get('professionId')), isActive:form.get('isActive') === 'on' }) });
    toast(row.id ? 'Çalışan güncellendi.' : 'Çalışan eklendi.');
    await employeesPage();
  });
}

async function professionsPage() {
  loading();
  try { state.professions = await api('/api/professions'); renderSimpleManagement('Meslekler','KPI gruplarının ve çalışan rollerinin temelini yönetin.',state.professions,'Meslek','profession',professionEditor); }
  catch (error) { showError(error, professionsPage); }
}

function renderSimpleManagement(title, description, rows, itemName, key, editor) {
  app.innerHTML = `${pageHead(title,description,`<button class="button primary" id="add-simple">${icon('plus')}${itemName} ekle</button>`)}<section class="card"><div class="table-wrap"><table class="table"><thead><tr><th>${itemName} adı</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${rows.map(row => `<tr><td><strong>${esc(row.name)}</strong></td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td><button class="row-action" data-${key}="${row.id}" aria-label="Düzenle">${icon('edit')}</button></td></tr>`).join('')}</tbody></table></div></section>`;
  document.querySelector('#add-simple').onclick = () => editor();
  document.querySelectorAll(`[data-${key}]`).forEach(button => button.onclick = () => editor(rows.find(x => x.id === Number(button.dataset[key]))));
}

function professionEditor(row = {}) {
  openDialog(row.id ? 'Mesleği düzenle' : 'Yeni meslek', `<div class="form-grid"><div class="field full"><label>Meslek adı</label><input class="input" name="name" value="${esc(row.name)}" required maxlength="100"></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive !== false ? 'checked' : ''}> Aktif meslek</label></div>`, async form => {
    await api('/api/professions', { method:'POST', body:JSON.stringify({ id:row.id || null, name:form.get('name'), isActive:form.get('isActive') === 'on' }) });
    state.professions = []; toast(row.id ? 'Meslek güncellendi.' : 'Meslek eklendi.'); await professionsPage();
  });
}

async function periodsPage() {
  loading();
  try { state.periods = await api('/api/periods'); renderPeriods(); }
  catch (error) { showError(error, periodsPage); }
}

function renderPeriods() {
  app.innerHTML = `${pageHead('Dönemler','Değerlendirmelerin tarih aralıklarını ve kullanım durumlarını yönetin.',`<button class="button primary" id="add-period">${icon('plus')}Dönem ekle</button>`)}<section class="card"><div class="table-wrap"><table class="table"><thead><tr><th>Dönem</th><th>Başlangıç</th><th>Bitiş</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${state.periods.map(row => `<tr><td><strong>${esc(row.name)}</strong></td><td>${formatDate(row.startDate)}</td><td>${formatDate(row.endDate)}</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td><button class="row-action" data-period="${row.id}" aria-label="Düzenle">${icon('edit')}</button></td></tr>`).join('')}</tbody></table></div></section>`;
  document.querySelector('#add-period').onclick = () => periodEditor();
  document.querySelectorAll('[data-period]').forEach(button => button.onclick = () => periodEditor(state.periods.find(x => x.id === Number(button.dataset.period))));
}

function periodEditor(row = {}) {
  openDialog(row.id ? 'Dönemi düzenle' : 'Yeni dönem', `<div class="form-grid"><div class="field full"><label>Dönem adı</label><input class="input" name="name" value="${esc(row.name)}" required maxlength="100"></div><div class="field"><label>Başlangıç</label><input class="input" type="date" name="startDate" value="${esc(row.startDate)}" required></div><div class="field"><label>Bitiş</label><input class="input" type="date" name="endDate" value="${esc(row.endDate)}" required></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive !== false ? 'checked' : ''}> Aktif dönem</label></div>`, async form => {
    await api('/api/periods', { method:'POST', body:JSON.stringify({ id:row.id || null, name:form.get('name'), startDate:form.get('startDate'), endDate:form.get('endDate'), isActive:form.get('isActive') === 'on' }) });
    state.periods = []; toast(row.id ? 'Dönem güncellendi.' : 'Dönem eklendi.'); await periodsPage();
  });
}

async function goalsPage() {
  loading();
  try { const [goals, periods] = await Promise.all([api('/api/goals'), api('/api/periods')]); state.goals = goals; state.periods = periods; renderGoals(); }
  catch (error) { showError(error, goalsPage); }
}

function renderGoals() {
  app.innerHTML = `${pageHead('Şirket Hedefleri','KPI ölçümlerini stratejik hedefler ve dönemlerle ilişkilendirin.',`<button class="button primary" id="add-goal">${icon('plus')}Hedef ekle</button>`)}<section class="card"><div class="table-wrap"><table class="table"><thead><tr><th>Hedef</th><th>Dönem</th><th>Öncelik</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${state.goals.map(row => `<tr><td><strong>${esc(row.name)}</strong><br><small>${esc(row.description || '')}</small></td><td>${esc(row.periodName || 'Tüm dönemler')}</td><td>${row.priority}</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td><button class="row-action" data-goal="${row.id}" aria-label="Düzenle">${icon('edit')}</button></td></tr>`).join('')}</tbody></table></div></section>`;
  document.querySelector('#add-goal').onclick = () => goalEditor();
  document.querySelectorAll('[data-goal]').forEach(button => button.onclick = () => goalEditor(state.goals.find(x => x.id === Number(button.dataset.goal))));
}

function goalEditor(row = {}) {
  openDialog(row.id ? 'Hedefi düzenle' : 'Yeni şirket hedefi', `<div class="form-grid"><div class="field full"><label>Hedef adı</label><input class="input" name="name" value="${esc(row.name)}" required></div><div class="field full"><label>Açıklama</label><textarea class="textarea" name="description">${esc(row.description)}</textarea></div><div class="field"><label>Dönem</label><select class="filter-select" name="evaluationPeriodId">${optionList(state.periods, row.evaluationPeriodId, 'Tüm dönemler')}</select></div><div class="field"><label>Öncelik</label><input class="input" type="number" min="1" name="priority" value="${row.priority || 1}" required></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive !== false ? 'checked' : ''}> Aktif hedef</label></div>`, async form => {
    await api('/api/goals', { method:'POST', body:JSON.stringify({ id:row.id || null, name:form.get('name'), description:form.get('description'), evaluationPeriodId:form.get('evaluationPeriodId') ? Number(form.get('evaluationPeriodId')) : null, priority:Number(form.get('priority')), isActive:form.get('isActive') === 'on' }) });
    state.goals = []; toast(row.id ? 'Hedef güncellendi.' : 'Hedef eklendi.'); await goalsPage();
  });
}

async function kpisPage() {
  loading();
  try {
    const [kpis, professions, goals] = await Promise.all([api('/api/kpis'), api('/api/professions'), api('/api/goals')]);
    state.kpis = kpis; state.professions = professions; state.goals = goals;
    renderKpis(kpis);
  } catch (error) { showError(error, kpisPage); }
}

function renderKpis(rows) {
  app.innerHTML = `${pageHead('KPI Yönetimi','Ortak ve mesleki ölçütleri, hedefleri ve ağırlıkları yönetin.',`<button class="button primary" id="add-kpi">${icon('plus')}KPI ekle</button>`)}${spreadsheetActions()}<section class="card"><div class="card-body"><div class="toolbar"><div class="search">${icon('search')}<input class="input" id="kpi-search" placeholder="KPI veya kategori ara"></div>${selectionFilter('kpi-profession','Meslekler',state.professions,[],'Tüm meslekler')}</div></div><div class="table-wrap"><table class="table"><thead><tr><th>KPI</th><th>Kapsam</th><th>Ölçüm</th><th>Ağırlık</th><th>Durum</th><th>İşlem</th></tr></thead><tbody id="kpi-rows">${kpiRows(rows)}</tbody></table></div></section>`;
  document.querySelector('#add-kpi').onclick = () => kpiEditor();
  bindSpreadsheetActions('kpis', () => ({ids:[...document.querySelectorAll('[data-kpi]')].map(x=>Number(x.dataset.kpi))}), kpisPage);
  const apply = () => { const term = document.querySelector('#kpi-search').value.toLocaleLowerCase('tr-TR'); const professions = selectedFilterValues('kpi-profession'); const filtered = state.kpis.filter(x => (!term || `${x.title} ${x.category || ''}`.toLocaleLowerCase('tr-TR').includes(term)) && (!professions.length || professions.some(id=>String(x.professionId)===id || x.professions?.some(p=>String(p.id)===id)))); document.querySelector('#kpi-rows').innerHTML = kpiRows(filtered); bindKpiActions(); };
  document.querySelector('#kpi-search').oninput = apply; state.filterController = bindSelectionFilters(apply); bindKpiActions();
}

function kpiRows(rows) {
  return rows.map(row => `<tr><td><strong>${esc(row.title)}</strong><br><small>${esc(row.category || 'Kategorisiz')}</small></td><td>${row.isCommon ? '<span class="badge info">Herkes</span>' : esc(row.professions?.length ? row.professions.map(x => x.name).join(', ') : row.professionName || '—')}</td><td>${esc(row.measurementName)}</td><td>${formatNumber(row.weightPercent)}%</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td><button class="row-action" data-kpi="${row.id}" aria-label="Düzenle">${icon('edit')}</button></td></tr>`).join('');
}

function bindKpiActions() { document.querySelectorAll('[data-kpi]').forEach(button => button.onclick = () => kpiEditor(state.kpis.find(x => x.id === Number(button.dataset.kpi)))); }

function kpiEditor(row = {}) {
  const measurement = row.measurementType ?? 0;
  openDialog(row.id ? 'KPI ölçütünü düzenle' : 'Yeni KPI ölçütü', `<div class="form-grid"><div class="field full"><label>KPI başlığı</label><input class="input" name="title" value="${esc(row.title)}" required maxlength="1000"></div><div class="field full"><label>Açıklama</label><textarea class="textarea" name="description">${esc(row.description)}</textarea></div><div class="field"><label>Kategori</label><input class="input" name="category" value="${esc(row.category)}"></div><div class="field"><label>Şirket hedefi</label><select class="filter-select" name="companyGoalId">${optionList(state.goals, row.companyGoalId, 'Hedef seçilmedi')}</select></div><label class="check-row"><input type="checkbox" name="isCommon" id="kpi-common" ${row.isCommon ? 'checked' : ''}> Herkes (tüm ekipler)</label><div class="field"><label>Ekipler (meslek grupları)</label><select class="filter-select multi-select" name="professionIds" id="kpi-profession-field" multiple>${state.professions.map(x => `<option value="${x.id}" ${(row.professions?.length ? row.professions.some(p => p.id === x.id) : row.professionId === x.id) ? 'selected' : ''}>${esc(x.name)}${x.isActive ? '' : ' (Pasif)'}</option>`).join('')}</select><small>Bir veya birden fazla meslek grubu seçin; tüm ekipler için Herkes seçeneğini işaretleyin.</small></div><div class="field"><label>Ölçüm türü</label><select class="filter-select" name="measurementType">${['Manuel 1–5','Yüzde','Adet','Süre','Sayısal'].map((x,i)=>`<option value="${i}" ${measurement===i?'selected':''}>${x}</option>`).join('')}</select></div><div class="field"><label>Birim</label><input class="input" name="unit" value="${esc(row.unit)}"></div><div class="field"><label>Hedef değer</label><input class="input" type="number" step="0.01" name="targetValue" value="${row.targetValue ?? ''}"></div><div class="field"><label>Başarı yönü</label><select class="filter-select" name="targetDirection"><option value="0" ${row.targetDirection !== 1 ? 'selected':''}>En az</option><option value="1" ${row.targetDirection === 1 ? 'selected':''}>En fazla</option></select></div><div class="field"><label>Ağırlık (%)</label><input class="input" type="number" step="0.01" min="0" max="100" name="weightPercent" value="${row.weightPercent ?? 0}"></div><div class="field"><label>Sıra</label><input class="input" type="number" min="1" name="displayOrder" value="${row.displayOrder || 1}" required></div><div class="field full"><label>Veri kaynağı</label><input class="input" name="dataSource" value="${esc(row.dataSource)}"></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive !== false ? 'checked':''}> Aktif KPI</label></div>`, async form => {
    const isCommon = form.get('isCommon') === 'on';
    await api('/api/kpis', { method:'POST', body:JSON.stringify({ id:row.id || null, title:form.get('title'), description:form.get('description'), professionId:null, professionIds:isCommon ? [] : form.getAll('professionIds').map(Number), isCommon, displayOrder:Number(form.get('displayOrder')), isActive:form.get('isActive') === 'on', category:form.get('category'), companyGoalId:form.get('companyGoalId') ? Number(form.get('companyGoalId')) : null, measurementType:Number(form.get('measurementType')), unit:form.get('unit'), targetValue:form.get('targetValue') ? Number(form.get('targetValue')) : null, targetDirection:Number(form.get('targetDirection')), weightPercent:Number(form.get('weightPercent')), dataSource:form.get('dataSource'), reviewFrequency:row.reviewFrequency ?? 2, validFrom:row.validFrom || null, validTo:row.validTo || null, isSpecific:row.isSpecific || false, isMeasurable:row.isMeasurable || false, isAchievable:row.isAchievable || false, isRelevant:row.isRelevant || false, isTimeBound:row.isTimeBound || false }) });
    toast(row.id ? 'KPI güncellendi.' : 'KPI eklendi.'); await kpisPage();
  }, true);
  const common = document.querySelector('#kpi-common'); const profession = document.querySelector('#kpi-profession-field');
  const sync = () => { profession.disabled = common.checked; profession.required = !common.checked; }; common.onchange = sync; sync();
}

async function evaluationPage() {
  loading();
  try {
    const [employees, periods, professions] = await Promise.all([api('/api/employees?activeOnly=true'), api('/api/periods?activeOnly=true'), api('/api/employees/profession-lookups')]);
    state.employees = employees; state.periods = periods; state.professions = professions; state.evaluationItems = []; state.evaluationEdit = null; state.evaluationReady = false;
    renderEvaluation();
    if (state.pendingEvaluation) {
      const selected=state.pendingEvaluation; state.pendingEvaluation=null;
      document.querySelector('#eval-employee').value=selected.employeeId;
      document.querySelector('#eval-period').value=selected.periodId;
      enhancePageSelects(); await loadEvaluationCriteria();
    }
  } catch (error) { showError(error, evaluationPage); }
}

function renderEvaluation() {
  app.innerHTML = `${pageHead('Değerlendirme Yap','Çalışanı ve dönemi seçin, her KPI ölçütünü puanlayın.')}
    <section class="card setup-card"><div class="setup-grid">${selectionFilter('eval-profession','Meslek',state.professions,'','Tüm meslekler',{multiple:false})}<div class="field"><label>Çalışan</label><select class="filter-select" id="eval-employee">${optionList(state.employees,'','Çalışan seçiniz')}</select></div><div class="field"><label>Dönem</label><select class="filter-select" id="eval-period">${optionList(state.periods,'','Dönem seçiniz')}</select></div></div></section>
    <div class="toolbar"><p id="evaluation-state" role="status">Başlamak veya kayıtlı taslağa devam etmek için çalışan ve dönem seçin.</p><button class="button secondary" id="retry-evaluation" hidden>Tekrar dene</button></div>
    <div class="evaluation-layout"><section class="card" id="criteria-card">${emptyState('Değerlendirmeye başlayın','KPI maddelerini görmek için çalışan ve dönem seçin.')}</section><aside class="card summary-card"><div class="card-head"><h2>Değerlendirme özeti</h2></div><div class="card-body" id="evaluation-summary">${evaluationSummary()}</div></aside></div>`;
  state.filterController = bindSelectionFilters(filterEvaluationEmployees);
  document.querySelector('#eval-employee').onchange = loadEvaluationCriteria;
  document.querySelector('#eval-period').onchange = loadEvaluationCriteria;
  document.querySelector('#retry-evaluation').onclick = loadEvaluationCriteria;
  enhancePageSelects();
}

function filterEvaluationEmployees() {
  const profession = selectedFilterValues('eval-profession')[0]||'';
  const rows = state.employees.filter(x => !profession || String(x.professionId) === profession);
  document.querySelector('#eval-employee').innerHTML = optionList(rows,'','Çalışan seçiniz');
  loadEvaluationCriteria();
}

async function loadEvaluationCriteria() {
  state.evaluationController?.abort();
  const controller = state.evaluationController = new AbortController();
  const { signal } = controller;
  const employeeId = Number(document.querySelector('#eval-employee').value);
  const periodId = Number(document.querySelector('#eval-period').value);
  state.evaluationItems = []; state.evaluationEdit = null; state.evaluationReady = false;
  document.querySelector('#general-note').value = '';
  renderCriteria();
  const notice = document.querySelector('#evaluation-state');
  document.querySelector('#retry-evaluation').hidden = true;
  if (!employeeId || !periodId) { notice.textContent = 'Başlamak veya kayıtlı taslağa devam etmek için çalışan ve dönem seçin.'; return; }
  notice.textContent = 'Kayıtlı değerlendirme kontrol ediliyor…';
  const employee = state.employees.find(x => x.id === employeeId);
  try {
    const existing = await api(`/api/evaluations/resume?employeeId=${employeeId}&periodId=${periodId}`, { signal });
    if (signal.aborted) return;
    if (existing && !existing.canEdit) {
      notice.textContent = 'Bu çalışan ve dönem için size ait onaylanmış değerlendirme var. Ayrıntıları Değerlendirmeler ekranından görebilirsiniz. Diğer amirin değerlendirmesi ayrıdır.';
      return;
    }
    if (existing) {
      const edit = await api(`/api/evaluations/${existing.id}/edit`, { signal });
      if (signal.aborted) return;
      state.evaluationEdit = edit;
      state.evaluationItems = edit.items.map(x => ({ ...x, id:x.kpiCriterionId, title:x.kpiTitle, score:x.score || null }));
      notice.textContent = `Kayıt #${edit.id} · ${statusNames[edit.status]}. Kayıtlı puanlar ve notlar yüklendi; aynı değerlendirmeye devam ediyorsunuz.`;
    } else {
      const items = await api(`/api/kpis/profession/${employee.professionId}?periodId=${periodId}`, { signal });
      if (signal.aborted) return;
      state.evaluationItems = items.map(x => ({ ...x, score:null, note:'', actualValue:null }));
      notice.textContent = 'Size ait yeni değerlendirme. Her amir kendi puanlarını ayrı kaydeder; Geçici Kaydet ile eksik puanları da saklayabilirsiniz.';
    }
    state.evaluationReady = true;
    renderCriteria();
    document.querySelector('#general-note').value = state.evaluationEdit?.generalNote || '';
  } catch (error) {
    if (signal.aborted) return;
    notice.textContent = 'Değerlendirme yüklenemedi. Tekrar deneyebilirsiniz.';
    document.querySelector('#retry-evaluation').hidden = false;
    toast(error.message,'error');
  }
}

function renderCriteria() {
  const card = document.querySelector('#criteria-card');
  if (!card) return;
  if (!state.evaluationItems.length) { card.innerHTML = emptyState('KPI maddesi bulunamadı','Çalışan seçin veya aktif KPI tanımlarını kontrol edin.'); updateEvaluationSummary(); return; }
  const groups = [{common:false,label:'Mesleki KPI’lar'},{common:true,label:'Ortak KPI’lar'}];
  card.innerHTML = groups.map(group => { const items = state.evaluationItems.filter(x => x.isCommon === group.common); return items.length ? `<div class="section-label">${group.label} · ${items.length} madde</div>${items.map(criterionTemplate).join('')}` : ''; }).join('');
  document.querySelectorAll('[data-score]').forEach(button => button.onclick = () => { const item = state.evaluationItems.find(x => x.id === Number(button.dataset.id)); const score = Number(button.dataset.score); item.score = item.score===score ? null : score; renderCriteria(); document.querySelector(`[data-id="${item.id}"][data-score="${score}"]`).focus({preventScroll:true}); });
  document.querySelectorAll('[data-actual]').forEach(input => input.onchange = () => { const item = state.evaluationItems.find(x => x.id === Number(input.dataset.actual)); item.actualValue = input.value === '' ? null : Number(input.value); item.score = metricScore(item.actualValue,item.targetValue,item.targetDirection); renderCriteria(); });
  document.querySelectorAll('[data-note]').forEach(input => input.oninput = () => { state.evaluationItems.find(x => x.id === Number(input.dataset.note)).note = input.value; });
  updateEvaluationSummary();
}

function criterionTemplate(item) {
  const metric = item.measurementType !== 0;
  return `<article class="criterion"><div class="criterion-head"><div><h3>${esc(item.title)}</h3><p>${esc(item.description || item.category || (item.isCommon ? 'Ortak değerlendirme ölçütü' : item.professionName))}</p></div><div class="score-buttons" aria-label="Puan">${[1,2,3,4,5].map(score => `<button type="button" class="score-button ${item.score===score?'selected':''}" aria-pressed="${item.score===score}" aria-label="${score} puan${item.score===score?' · Seçimi kaldır':''}" data-score="${score}" data-id="${item.id}" ${metric?'disabled':''}>${score}</button>`).join('')}</div></div><div class="criterion-extra">${metric ? `<input class="input" type="number" step="0.01" data-actual="${item.id}" value="${item.actualValue ?? ''}" placeholder="Gerçekleşen (${esc(item.unit || 'değer')})">` : '<span></span>'}<input class="input" data-note="${item.id}" value="${esc(item.note)}" placeholder="Kısa not (isteğe bağlı)"></div></article>`;
}

function metricScore(actual,target,direction) {
  if (actual == null || target == null) return null;
  if (direction === 1) { if (target===0) return actual<=0?5:1; const r=actual/target; return r<=1?5:r<=1.1?4:r<=1.25?3:r<=1.5?2:1; }
  if (target===0) return actual>=0?5:1; const r=actual/target; return r>=1?5:r>=.9?4:r>=.75?3:r>=.6?2:1;
}

function evaluationSummary() {
  const scored = state.evaluationItems.filter(x => x.score != null); const total = scored.reduce((sum,x)=>sum+x.score,0); const weight = scored.reduce((sum,x)=>sum+(x.weightPercent||0),0); const average = scored.length ? total/scored.length : 0; const weighted = weight ? scored.reduce((sum,x)=>sum+x.score*(x.weightPercent||0),0)/weight : average; const complete = state.evaluationItems.length && scored.length === state.evaluationItems.length;
  return `<div class="score-ring" style="--value:${Math.min(100,weighted*20)}%"><div><strong>%${formatNumber(weighted*20)}</strong><small>normalize başarı</small></div></div><div class="summary-line"><span>Puanlanan</span><strong>${scored.length} / ${state.evaluationItems.length}</strong></div><div class="summary-line"><span>Toplam puan</span><strong>${total} / ${state.evaluationItems.length*5}</strong></div><div class="summary-line"><span>Ağırlıklı ortalama</span><strong>${formatNumber(weighted)} / 5</strong></div><div class="field" style="margin-top:14px"><label>Genel not</label><textarea class="textarea" id="general-note" placeholder="Değerlendirme notu" maxlength="2000"></textarea></div><div class="actions" style="margin-top:16px"><button class="button secondary" id="save-draft" ${state.evaluationReady&&state.evaluationItems.length&&!state.savingEvaluation?'':'disabled'}>Geçici Kaydet</button><button class="button primary" id="save-evaluation" ${state.evaluationReady&&complete&&hasPermission(2)&&!state.savingEvaluation?'':'disabled'}>${icon('check')}Kalıcı Kaydet</button>${state.evaluationEdit?'<button class="button secondary" id="reload-evaluation">Taslağı yeniden yükle</button>':''}<small>Geçici kayıt daha sonra tamamlanabilir. Kalıcı kayıt onaylanır ve değiştirilemez.</small></div>`;
}

function updateEvaluationSummary() {
  const summary = document.querySelector('#evaluation-summary'); if (!summary) return; const oldNote = document.querySelector('#general-note')?.value || ''; summary.innerHTML = evaluationSummary(); document.querySelector('#general-note').value = oldNote; document.querySelector('#save-evaluation').onclick = () => saveEvaluation(4); document.querySelector('#save-draft').onclick = () => saveEvaluation(0);
  const reload = document.querySelector('#reload-evaluation');
  if (reload) reload.onclick = () => { if (confirm('Kaydedilmemiş değişiklikler bırakılıp güncel taslak yüklensin mi?')) loadEvaluationCriteria(); };
}

async function saveEvaluation(status) {
  if (state.savingEvaluation || !state.evaluationReady) return;
  const card = document.querySelector('#criteria-card');
  const employeeId = Number(document.querySelector('#eval-employee').value);
  const periodId = Number(document.querySelector('#eval-period').value);
  if (!employeeId || !periodId) { toast('Çalışan ve dönem seçmelisiniz.','error'); return; }
  if (status === 4 && (!hasPermission(2) || state.evaluationItems.some(x => x.score == null))) { toast('Kalıcı kayıt için tüm soruları puanlamalı ve onaylama yetkisine sahip olmalısınız.','error'); return; }
  if (status === 4 && !confirm('Kalıcı kayıt onaylanacak ve değiştirilemeyecek. Devam edilsin mi?')) return;
  state.savingEvaluation = true;
  document.querySelector('.setup-card').inert = true;
  document.querySelector('#criteria-card').inert = true;
  document.querySelector('#general-note').disabled = true;
  document.querySelectorAll('#save-draft, #save-evaluation, #reload-evaluation').forEach(x => x.disabled = true);
  try {
    const edit = state.evaluationEdit;
    const generalNote = document.querySelector('#general-note').value;
    if (edit) {
      await api(`/api/evaluations/${edit.id}`, {method:'PUT',body:JSON.stringify({id:edit.id,generalNote,status,expectedUpdatedAt:edit.updatedAt,managerFeedback:edit.managerFeedback,employeeFeedback:edit.employeeFeedback,developmentAction:edit.developmentAction,followUpDate:edit.followUpDate,items:state.evaluationItems.map(x=>({detailId:x.detailId,score:x.score??0,note:x.note,actualValue:x.actualValue}))})});
    } else {
      await api('/api/evaluations',{method:'POST',body:JSON.stringify({employeeId,evaluationPeriodId:periodId,generalNote,status,items:state.evaluationItems.map(x=>({kpiCriterionId:x.id,score:x.score??0,note:x.note,actualValue:x.actualValue}))})});
    }
    toast(status === 0 ? 'Taslak kaydedildi. Bu ekranda devam edebilir veya aynı çalışan ve dönemi yeniden seçerek açabilirsiniz.' : 'Değerlendirme kalıcı olarak kaydedildi ve onaylandı.');
    if (document.querySelector('#criteria-card') === card) await loadEvaluationCriteria();
  } catch(error) { toast(error.message,'error'); }
  finally {
    state.savingEvaluation = false;
    if (document.querySelector('#criteria-card') === card) {
      document.querySelectorAll('.setup-card, #criteria-card').forEach(x => x.inert = false);
      updateEvaluationSummary();
    }
  }
}

async function editEvaluation(id) {
  try {
    const edit = await api(`/api/evaluations/${id}/edit`);
    let saveStatus = 0;
    openDialog('Değerlendirmeyi düzenle', `<div class="form-grid">${edit.items.map(x=>`<div class="field full"><label>${esc(x.kpiTitle)}</label>${x.measurementType===0?`<select class="filter-select" name="score-${x.detailId}"><option value="0">Henüz puanlanmadı</option>${[1,2,3,4,5].map(score=>`<option value="${score}" ${score===x.score?'selected':''}>${score}</option>`).join('')}</select>`:`<input class="input" type="number" step="0.01" name="actual-${x.detailId}" value="${x.actualValue??''}" placeholder="Gerçekleşen (${esc(x.unit||'değer')})"><small>Hedef: ${esc(x.targetValue)} · Puan otomatik hesaplanır.</small>`}<input class="input" name="note-${x.detailId}" value="${esc(x.note)}" placeholder="Kısa not" maxlength="2000"></div>`).join('')}<div class="field full"><label>Genel not</label><textarea class="textarea" name="generalNote" maxlength="2000">${esc(edit.generalNote)}</textarea></div></div>`, async form => {
      const items = edit.items.map(x => {
        const raw = form.get(`actual-${x.detailId}`);
        const actualValue = raw === '' || raw == null ? null : Number(raw);
        return {detailId:x.detailId, score:x.measurementType===0?Number(form.get(`score-${x.detailId}`)):metricScore(actualValue,x.targetValue,x.targetDirection)??0, actualValue, note:form.get(`note-${x.detailId}`)};
      });
      if (saveStatus === 4 && items.some(x=>x.score===0)) throw new Error('Kalıcı kayıt için tüm soruları puanlamalısınız.');
      if (saveStatus === 4 && !confirm('Kalıcı kayıt onaylanacak ve değiştirilemeyecek. Devam edilsin mi?')) throw new Error('Kalıcı kayıt iptal edildi.');
      await api(`/api/evaluations/${id}`, {method:'PUT',body:JSON.stringify({id,generalNote:form.get('generalNote'),items,status:saveStatus,expectedUpdatedAt:edit.updatedAt,managerFeedback:edit.managerFeedback,employeeFeedback:edit.employeeFeedback,developmentAction:edit.developmentAction,followUpDate:edit.followUpDate})});
      toast(saveStatus===0?'Geçici kayıt güncellendi.':'Değerlendirme kalıcı olarak kaydedildi ve onaylandı.');
      await historyPage();
    }, true);
    const footer = dialogContent.querySelector('.dialog-foot');
    footer.innerHTML = `<button type="button" class="button secondary" id="cancel-evaluation-edit">Vazgeç</button><button type="submit" class="button secondary" id="update-draft">Geçici Kaydet</button>${hasPermission(2)?'<button type="submit" class="button primary" id="finalize-evaluation">Kalıcı Kaydet</button>':''}`;
    footer.querySelector('#cancel-evaluation-edit').onclick = () => dialog.close();
    footer.querySelector('#update-draft').onclick = () => { saveStatus = 0; };
    if (hasPermission(2)) footer.querySelector('#finalize-evaluation').onclick = () => { saveStatus = 4; };
  } catch(error) { toast(error.message,'error'); }
}

async function historyPage() {
  loading();
  try { const [rows, employees, professions, periods] = await Promise.all([api('/api/evaluations'),api('/api/employees'),api('/api/professions'),api('/api/periods')]); state.history=rows;state.employees=employees;state.professions=professions;state.periods=periods;renderHistory(rows); }
  catch(error){showError(error,historyPage);}
}

function renderHistory(rows) {
  state.historyFilters ||= {employee:[], profession:[], period:[]};
  const filters = state.historyFilters;
  app.innerHTML = `${pageHead('Değerlendirmeler','Taslak, onay bekleyen ve onaylı kayıtları çoklu filtrelerle inceleyin.')}${spreadsheetActions(hasPermission(1))}<section class="card"><div class="card-body"><div class="toolbar history-filters">${selectionFilter('history-employee','Çalışanlar',state.employees,filters.employee,'Tüm çalışanlar')}${selectionFilter('history-profession','Meslekler',state.professions,filters.profession,'Tüm meslekler')}${selectionFilter('history-period','Dönemler',state.periods,filters.period,'Tüm dönemler')}</div></div><div class="table-wrap"><table class="table"><thead><tr><th>Çalışan</th><th>Değerlendiren</th><th>Dönem</th><th>Puan</th><th>Durum</th><th>İşlem</th></tr></thead><tbody id="history-rows">${historyRows(rows)}</tbody></table></div></section>`;
  bindSpreadsheetActions('evaluations', () => ({ids:[...document.querySelectorAll('[data-history]')].map(x=>Number(x.dataset.history))}), historyPage);
  const apply = () => {
    ['employee','profession','period'].forEach(key => filters[key] = [...document.querySelectorAll(`#history-${key} input:checked`)].map(x=>x.value));
    const filtered = rows.filter(x=>(!filters.employee.length || filters.employee.includes(String(x.employeeId)))
      && (!filters.profession.length || filters.profession.includes(String(x.professionId)))
      && (!filters.period.length || filters.period.includes(String(x.evaluationPeriodId))));
    document.querySelector('#history-rows').innerHTML = historyRows(filtered);
    bindHistoryActions();
  };
  state.filterController = bindSelectionFilters(apply); apply();
}

function historyRows(rows){return rows.length?rows.map(row=>`<tr><td><div class="person"><span class="person-avatar">${initials(row.employeeName)}</span><div>${esc(row.employeeName)}<br><small>${esc(row.professionName)}</small></div></div></td><td>${esc(row.evaluatorName||'Sistem')}</td><td>${esc(row.periodName)}</td><td><span class="score ${row.scorePercentage>=80?'good':row.scorePercentage>=60?'mid':'low'}">${row.totalScore} / ${row.maximumScore}<br><small>%${formatNumber(row.scorePercentage)}</small></span></td><td>${statusBadge(row.status)}</td><td><div class="row-actions"><button class="row-action" data-history="${row.id}" aria-label="Detayı görüntüle">${icon('eye')}</button>${row.canEdit?`<button class="row-action" data-edit-evaluation="${row.id}" aria-label="Değerlendirmeyi düzenle">${icon('edit')}</button>`:''}${row.canApprove?`<button class="row-action" data-approve="${row.id}" aria-label="Onayla">${icon('check')}</button>`:''}</div></td></tr>`).join(''):`<tr><td colspan="6">${emptyState('Kayıt bulunamadı','Filtreleri değiştirebilirsiniz.')}</td></tr>`;}
function bindHistoryActions(){document.querySelectorAll('[data-edit-evaluation]').forEach(button=>button.onclick=()=>editEvaluation(Number(button.dataset.editEvaluation)));document.querySelectorAll('[data-history]').forEach(button=>button.onclick=()=>showEvaluationDetail(Number(button.dataset.history)));document.querySelectorAll('[data-approve]').forEach(button=>button.onclick=()=>approveEvaluation(Number(button.dataset.approve)));}
async function approveEvaluation(id){try{await api(`/api/evaluations/${id}/approve`,{method:'POST'});toast('Değerlendirme onaylandı ve resmî sonuçlara eklendi.');await historyPage();}catch(error){toast(error.message,'error');}}

async function showEvaluationDetail(id, rows = state.history, onBack = null, signal = undefined) {
  try {
    const row = rows.find(x=>x.id===id);
    const [details, responses] = await Promise.all([api(`/api/evaluations/${id}/details`, {signal}), api(`/api/workflow/evaluations/${id}/responses`, {signal})]);
    if (signal?.aborted) return;
    dialog.classList.add('wide');
    dialog.setAttribute('aria-labelledby', 'evaluation-dialog-title');
    dialogContent.innerHTML = `<div class="dialog-head"><h2 id="evaluation-dialog-title" tabindex="-1">${esc(row.employeeName)} · Değerlendirme ayrıntısı</h2><button type="button" class="icon-button" data-close aria-label="Kapat">${icon('close')}</button></div><div class="dialog-body"><p class="manager-dialog-context">Değerlendiren: ${esc(row.evaluatorName||'Sistem')} · ${formatDate(row.evaluationDate)} · ${statusBadge(row.status)}</p><div class="detail-grid"><div class="detail-box"><small>Çalışan</small><strong>${esc(row.employeeName)}</strong></div><div class="detail-box"><small>Dönem</small><strong>${esc(row.periodName)}</strong></div><div class="detail-box"><small>Ağırlıklı puan</small><strong>${formatNumber(row.weightedScore)} / 5</strong></div></div><div class="table-wrap"><table class="table"><thead><tr><th>KPI</th><th>Tür</th><th>Puan</th><th>Not</th></tr></thead><tbody>${details.map(x=>`<tr><td><strong>${esc(x.kpiTitle)}</strong></td><td>${x.isCommon?'Ortak':'Mesleki'}</td><td><span class="score">${x.score || '—'}</span></td><td>${esc(x.note||'—')}</td></tr>`).join('')}</tbody></table></div>${row.generalNote?`<div class="field" style="margin-top:18px"><label>Genel not</label><div class="detail-box">${esc(row.generalNote)}</div></div>`:''}</div><div class="dialog-foot">${onBack?'<button type="button" class="button secondary" id="back-to-manager">Amirin değerlendirmelerine dön</button>':''}<button type="button" class="button secondary" data-close>Kapat</button></div>`;
    dialogContent.querySelectorAll('[data-close]').forEach(button=>button.onclick=()=>dialog.close());
    const responseSection = document.createElement('section');
    responseSection.innerHTML = `<h3>Çalışan görüşü</h3><div id="employee-response-list">${employeeResponseList(responses)}</div>${hasPermission(14)&&state.user.employeeId===row.employeeId&&row.status===4?'<form id="employee-response-form"><div class="field"><label>Yanıtınız</label><select class="filter-select" name="decision"><option value="2">Görüş bildir</option><option value="0">Kabul ediyorum</option></select></div><div class="field"><label>Açıklama</label><textarea class="textarea" name="comment" maxlength="2000"></textarea></div><button class="button primary" type="submit">Yanıtı kaydet</button><small>Yanıt geçmişe eklenir; onaylı puanlar değişmez.</small></form>':''}`;
    dialogContent.querySelector('.dialog-body').append(responseSection);
    const responseSelections = enhanceSelects(responseSection);
    dialog.addEventListener('close',()=>responseSelections.abort(),{once:true});
    const responseForm = responseSection.querySelector('form');
    if (responseForm) responseForm.onsubmit = async event => {
      event.preventDefault(); const button = responseForm.querySelector('button'); if (button.disabled) return; button.disabled=true;
      try {
        const data = new FormData(responseForm);
        await api(`/api/workflow/evaluations/${id}/responses`,{method:'POST',body:JSON.stringify({evaluationId:id,decision:Number(data.get('decision')),comment:data.get('comment')})});
        responseSection.querySelector('#employee-response-list').innerHTML=employeeResponseList(await api(`/api/workflow/evaluations/${id}/responses`));
        responseForm.reset(); toast('Yanıtınız kaydedildi.');
      } catch(error) { toast(error.message,'error'); } finally { button.disabled=false; }
    };
    if (onBack) document.querySelector('#back-to-manager').onclick = onBack;
    if (!dialog.open) {
      dialog.addEventListener('close', () => dialog.removeAttribute('aria-labelledby'), {once:true});
      dialog.showModal();
    }
    document.querySelector('#evaluation-dialog-title').focus();
  } catch(error) { if (!signal?.aborted) toast(error.message,'error'); }
}

const retiredPermissions=new Set([9,13,15,17]);
const permissionNames=['Gösterge paneli','Değerlendirme oluşturma','Değerlendirme onaylama','Değerlendirme geçmişi','Çalışan yönetimi','Meslek yönetimi','KPI yönetimi','Dönem yönetimi','Hedef yönetimi','Sayfa tasarımcısı','Yetkilendirme yönetimi','İzin takibi · Görüntüle','İzin listesi · Excel yükleme','Denetim kayıtları','Çalışan değerlendirme yanıtı','Değerlendirme görevleri','Kalibrasyon karşılaştırması','Yetkinlik/kıdem yönetimi','Bireysel gelişim yönetimi'];

function employeeResponseList(rows) {
  return rows.length?rows.map(x=>`<div class="detail-box"><strong>${{0:'Kabul',2:'Görüş'}[x.decision]}</strong><small>${esc(x.userName)} · ${formatDate(x.createdAt)}</small><p>${esc(x.comment||'—')}</p></div>`).join(''):'<p>Henüz çalışan yanıtı yok.</p>';
}

async function developmentPage() {
  loading();
  const filters=state.developmentFilters||{employeeId:'',activeOnly:true,page:1};state.developmentFilters=filters;
  try {
    const query=new URLSearchParams({activeOnly:String(filters.activeOnly),page:String(filters.page),pageSize:'50'});if(filters.employeeId)query.set('employeeId',filters.employeeId);
    const data=await api(`/api/development?${query}`);state.developmentBoard=data;
    app.innerHTML=`${pageHead('Bireysel Hedefler ve Gelişim','Hedefleri ve gelişim planlarını takip edin; görüşmelerde ilerlemeyi elle güncelleyin.',`<button class="button primary" id="add-development" ${data.employees.length?'':'disabled'}>${icon('plus')}Hedef / plan ekle</button>`)}<section class="card card-body"><div class="toolbar">${selectionFilter('development-employee','Çalışan',data.employees,filters.employeeId,'Tüm çalışanlar',{multiple:false})}<label class="check-row"><input type="checkbox" id="development-active" ${filters.activeOnly?'checked':''}> Yalnızca aktif kayıtlar</label></div><div class="table-wrap"><table class="table"><thead><tr><th>Çalışan</th><th>Hedef / plan</th><th>Hedef tarihi</th><th>İlerleme</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${data.items.length?data.items.map(x=>`<tr><td>${esc(x.employeeName)}</td><td><strong>${esc(x.title)}</strong><br><small>${x.kind===0?'Bireysel hedef':'Gelişim planı'}</small></td><td>${formatDate(x.targetDate)}</td><td>%${formatNumber(x.progress)}</td><td><span class="badge ${x.isActive?'active':'inactive'}">${x.isActive?'Aktif':'Pasif'}</span></td><td><div class="actions"><button class="button secondary" data-development-edit="${x.id}">Düzenle</button><button class="button secondary" data-development-checkin="${x.id}">Görüşmeler</button></div></td></tr>`).join(''):'<tr><td colspan="6">Henüz kayıt yok.</td></tr>'}</tbody></table></div><div class="actions"><button class="button secondary" id="development-prev" ${filters.page<=1?'disabled':''}>Önceki</button><span>${data.totalCount} kayıt · Sayfa ${filters.page}</span><button class="button secondary" id="development-next" ${filters.page*50>=data.totalCount?'disabled':''}>Sonraki</button></div></section>`;
    const apply=()=>{filters.employeeId=selectedFilterValues('development-employee')[0]||'';filters.activeOnly=document.querySelector('#development-active').checked;filters.page=1;developmentPage();};
    state.filterController=bindSelectionFilters(apply);document.querySelector('#development-active').onchange=apply;
    document.querySelector('#add-development').onclick=()=>developmentEditor();
    document.querySelectorAll('[data-development-edit]').forEach(button=>button.onclick=()=>developmentEditor(data.items.find(x=>x.id===Number(button.dataset.developmentEdit))));
    document.querySelectorAll('[data-development-checkin]').forEach(button=>button.onclick=()=>developmentCheckIns(data.items.find(x=>x.id===Number(button.dataset.developmentCheckin))));
    document.querySelector('#development-prev').onclick=()=>{filters.page--;developmentPage();};document.querySelector('#development-next').onclick=()=>{filters.page++;developmentPage();};
  }catch(error){showError(error,developmentPage);}
}

function developmentEditor(row={}) {
  openDialog(row.id?'Hedef / planı düzenle':'Yeni hedef / gelişim planı',`<div class="form-grid"><div class="field"><label>Çalışan</label><select class="filter-select" name="employeeId" ${row.id?'disabled':'required'}>${optionList(state.developmentBoard.employees,row.employeeId)}</select></div><div class="field"><label>Tür</label><select class="filter-select" name="kind"><option value="0" ${row.kind!==1?'selected':''}>Bireysel hedef</option><option value="1" ${row.kind===1?'selected':''}>Gelişim planı</option></select></div><div class="field full"><label>Başlık</label><input class="input" name="title" value="${esc(row.title)}" required maxlength="200"></div><div class="field full"><label>Açıklama / başarı ölçütü</label><textarea class="textarea" name="description" maxlength="2000">${esc(row.description)}</textarea></div><div class="field"><label>Hedef tarihi</label><input class="input" type="date" name="targetDate" value="${esc(row.targetDate)}"></div><div class="field"><label>Elle girilen ilerleme (%)</label><input class="input" type="number" min="0" max="100" step="0.01" name="progress" value="${row.progress??0}" required></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive!==false?'checked':''}> Aktif kayıt</label></div>`,async form=>{
    const isActive=form.get('isActive')==='on';if(row.isActive&&!isActive&&!confirm('Bu hedef / plan pasife alınsın mı?'))throw new Error('İşlem iptal edildi.');
    await api('/api/development',{method:'POST',body:JSON.stringify({id:row.id||null,employeeId:row.employeeId||Number(form.get('employeeId')),kind:Number(form.get('kind')),title:form.get('title'),description:form.get('description'),targetDate:form.get('targetDate')||null,progress:Number(form.get('progress')),isActive,expectedVersion:row.version||null})});toast('Hedef / plan kaydedildi.');await developmentPage();
  },true);
}

async function developmentCheckIns(row) {
  try {
    const rows=await api(`/api/development/${row.id}/check-ins`);
    openDialog(`${row.title} · Görüşmeler`,`<p>${esc(row.description||'')}</p>${rows.length?rows.map(x=>`<div class="detail-box"><strong>%${formatNumber(x.progress)}</strong><small>${esc(x.createdByName)} · ${formatDate(x.createdAt)}</small><p>${esc(x.note)}</p></div>`).join(''):'<p>Henüz görüşme notu yok.</p>'}${row.isActive?`<div class="field"><label>Yeni görüşme notu</label><textarea class="textarea" name="note" maxlength="2000" required></textarea></div><div class="field"><label>Görüşme sonrası ilerleme (%)</label><input class="input" type="number" name="progress" value="${row.progress}" min="0" max="100" step="0.01" required></div>`:'<p>Pasif kayda yeni görüşme eklenemez.</p>'}`,async form=>{
      await api(`/api/development/${row.id}/check-ins`,{method:'POST',body:JSON.stringify({id:row.id,note:form.get('note'),progress:Number(form.get('progress')),expectedVersion:row.version})});toast('Görüşme kaydedildi, ilerleme güncellendi.');await developmentPage();
    },true);
    if(!row.isActive)dialogContent.querySelector('[type="submit"]').hidden=true;
  }catch(error){toast(error.message,'error');}
}

async function calibrationPage() {
  loading();
  try {
    const rows=await api('/api/workflow/calibration');
    app.innerHTML=`${pageHead('Kalibrasyon','Onaylı amir sonuçlarını yan yana karşılaştırın. Fark, en yüksek ve en düşük başarı yüzdesi arasındaki puan farkıdır.')}<section class="card card-body"><div class="toolbar">${selectionFilter('calibration-period','Dönem',[...new Map(rows.map(x=>[x.periodId,{id:x.periodId,name:x.periodName}])).values()],[],'Tüm dönemler')}</div><div id="calibration-list"></div></section>`;
    const render=()=>{
      const periods=selectedFilterValues('calibration-period'); const filtered=rows.filter(x=>!periods.length||periods.includes(String(x.periodId)));
      document.querySelector('#calibration-list').innerHTML=filtered.length?`<div class="table-wrap"><table class="table"><thead><tr><th>Çalışan / ekip</th><th>Dönem</th><th>Amir sonuçları</th><th>Fark (yüzde puan)</th></tr></thead><tbody>${filtered.map(x=>`<tr><td>${esc(x.employeeName)}<br><small>${esc(x.professionName)}</small></td><td>${esc(x.periodName)}</td><td>${x.scores.map(score=>`<div>${esc(score.evaluatorName)}: <strong>%${formatNumber(score.percentage)}</strong></div>`).join('')}</td><td>${x.scores.length>1?formatNumber(x.difference):'İkinci değerlendirme bekleniyor'}</td></tr>`).join('')}</tbody></table></div>`:emptyState('Onaylı sonuç yok','Erişebildiğiniz onaylı değerlendirmeler burada karşılaştırılır.');
    };state.filterController=bindSelectionFilters(render);render();
  } catch(error) { showError(error,calibrationPage); }
}

async function leavesPage() {
  loading();
  try {
    state.leaveFilters ||= {employeeId:'', professionId:'', from:'', to:'', activeOnly:'true', page:1};
    const query = new URLSearchParams(Object.entries(state.leaveFilters).filter(([,value])=>value!==''));
    const [data, employees, professions] = await Promise.all([api(`/api/leaves?${query}`), api('/api/employees'), api('/api/professions')]);
    state.leaveEmployees = employees;
    state.leaveProfessions = professions;
    state.leaveRows = data.items;
    renderLeaves(data);
  } catch(error) { showError(error, leavesPage); }
}

function renderLeaves(data) {
  const filter=state.leaveFilters;
  const pageCount=Math.max(1,Math.ceil(data.totalCount/data.pageSize));
  app.innerHTML=`${pageHead('İzin Takibi','Excel dosyasından yüklenen çalışan izinlerini görüntüleyin.')}${spreadsheetActions(hasPermission(12),true)}
    <section class="card"><div class="card-body"><div class="toolbar" id="leave-filters">
      ${selectionFilter('leave-employee','Çalışan',state.leaveEmployees,filter.employeeId,'Tüm çalışanlar',{multiple:false})}
      ${selectionFilter('leave-profession','Ekip (meslek grubu)',state.leaveProfessions,filter.professionId,'Tüm ekipler',{multiple:false})}
      <div class="field"><label for="leave-from">Başlangıç</label><input class="input" type="date" id="leave-from" name="from" value="${esc(filter.from)}"></div>
      <div class="field"><label for="leave-to">Bitiş</label><input class="input" type="date" id="leave-to" name="to" value="${esc(filter.to)}"></div>
      </div></div>
      <div class="table-wrap"><table class="table"><thead><tr><th>Çalışan</th><th>Ekip</th><th>İzin türü</th><th>Başlangıç</th><th>Bitiş</th><th>Açıklama</th></tr></thead><tbody>${data.items.length?data.items.map(row=>`<tr><td>${esc(row.employeeName)}</td><td>${esc(row.professionName)}</td><td>${esc(row.leaveType)}</td><td>${formatDate(row.startDate)}</td><td>${formatDate(row.endDate)}</td><td>${esc(row.note||'—')}</td></tr>`).join(''):`<tr><td colspan="6">${emptyState('İzin kaydı bulunamadı','İzinleri görmek için Excel yükleyin veya filtreleri değiştirin.')}</td></tr>`}</tbody></table></div>
      <div class="card-body actions"><span>${data.totalCount} kayıt · Sayfa ${data.page} / ${pageCount}</span><button class="button secondary" id="leave-previous" ${data.page>1?'':'disabled'}>Önceki</button><button class="button secondary" id="leave-next" ${data.page<pageCount?'':'disabled'}>Sonraki</button></div></section>`;
  const from=document.querySelector('#leave-from'),to=document.querySelector('#leave-to');
  const apply=()=>{
    from.max=to.value;to.min=from.value;
    if(!from.reportValidity() || !to.reportValidity())return;
    state.leaveFilters={employeeId:selectedFilterValues('leave-employee')[0]||'',professionId:selectedFilterValues('leave-profession')[0]||'',activeOnly:'true',from:from.value,to:to.value,page:1};
    leavesPage();
  };
  state.filterController=bindSelectionFilters(apply,document.querySelector('#leave-filters'));
  from.onchange=apply;to.onchange=apply;
  document.querySelector('#leave-previous').onclick=()=>{filter.page--;leavesPage();};
  document.querySelector('#leave-next').onclick=()=>{filter.page++;leavesPage();};
  bindSpreadsheetActions('leaves',()=>({}),leavesPage);
}

async function authorizationPage(){
  loading();
  try{
    const [users,roles,permissions,assignments,employees]=await Promise.all([api('/api/authorization/users'),api('/api/authorization/roles'),api('/api/authorization/permissions'),api('/api/authorization/employee-managers'),api('/api/employees')]);
    state.authUsers=users;state.roles=roles;state.rolePermissions=permissions;state.assignments=assignments;state.employees=employees;renderAuthorization();
  }catch(error){showError(error,authorizationPage);}
}
function renderAuthorization(){
  const first=state.authUsers.filter(x=>x.roleCode==='first-manager'),second=state.authUsers.filter(x=>x.roleCode==='second-manager');
  app.innerHTML=`${pageHead('Yetkilendirme','Kullanıcıları, rolleri, izinleri ve çalışan–amir ilişkilerini yönetin.',`<button class="button secondary" id="add-role">${icon('plus')}Rol ekle</button><button class="button primary" id="add-user">${icon('plus')}Kullanıcı ekle</button>`)}
  <section class="card" style="margin-bottom:20px"><div class="card-head"><h2>Kullanıcı hesapları</h2></div><div class="table-wrap"><table class="table"><thead><tr><th>Kullanıcı</th><th>Rol</th><th>Çalışan</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${state.authUsers.map(x=>`<tr><td><strong>${esc(x.displayName)}</strong><br><small>${esc(x.userName)}</small></td><td>${esc(x.roleName)}</td><td>${esc(x.employeeName||'—')}</td><td><span class="badge ${x.isActive?'active':'inactive'}">${x.isActive?'Aktif':'Pasif'}</span></td><td><button class="row-action" data-user="${x.id}">${icon('edit')}</button></td></tr>`).join('')}</tbody></table></div></section>
  <div class="grid-2"><section class="card"><div class="card-head"><h2>Rol ve izin yönetimi</h2></div><div class="card-body">${state.roles.map(role=>`<div class="actions" style="margin-bottom:8px"><button class="button secondary" data-role-permissions="${role.id}" style="flex:1;justify-content:space-between">${esc(role.name)}<span>${state.rolePermissions.filter(x=>x.roleId===role.id&&x.isAllowed&&!retiredPermissions.has(x.permission)).length} izin</span></button><button class="row-action" data-role-edit="${role.id}" aria-label="Rolü düzenle">${icon('edit')}</button></div>`).join('')}</div></section>
  <section class="card"><div class="card-head"><h2>Amir atamaları</h2></div><div class="card-body" style="max-height:440px;overflow:auto">${state.assignments.map(x=>`<div class="field" style="padding:12px 0;border-bottom:1px solid var(--line)"><label>${esc(x.employeeName)}</label><div class="actions"><select class="filter-select" data-first="${x.employeeId}">${optionList(first,x.firstManagerUserId,'1. Amir')}</select><select class="filter-select" data-second="${x.employeeId}">${optionList(second,x.secondManagerUserId,'2. Amir')}</select><button class="button secondary" data-save-managers="${x.employeeId}">Kaydet</button></div></div>`).join('')}</div></section></div>`;
  document.querySelector('#add-user').onclick=()=>userEditor();document.querySelector('#add-role').onclick=()=>roleEditor();document.querySelectorAll('[data-user]').forEach(b=>b.onclick=()=>userEditor(state.authUsers.find(x=>x.id===Number(b.dataset.user))));
  document.querySelectorAll('[data-role-permissions]').forEach(b=>b.onclick=()=>permissionEditor(state.roles.find(x=>x.id===Number(b.dataset.rolePermissions))));document.querySelectorAll('[data-role-edit]').forEach(b=>b.onclick=()=>roleEditor(state.roles.find(x=>x.id===Number(b.dataset.roleEdit))));
  document.querySelectorAll('[data-save-managers]').forEach(b=>b.onclick=async()=>{const id=Number(b.dataset.saveManagers);try{await api(`/api/authorization/employee-managers/${id}`,{method:'PUT',body:JSON.stringify({employeeId:id,firstManagerUserId:Number(document.querySelector(`[data-first="${id}"]`).value)||null,secondManagerUserId:Number(document.querySelector(`[data-second="${id}"]`).value)||null})});toast('Amir ataması güncellendi.');}catch(error){toast(error.message,'error');}});
  enhancePageSelects();
}
function userEditor(row={}){openDialog(row.id?'Kullanıcıyı düzenle':'Yeni kullanıcı',`<div class="form-grid"><div class="field"><label>Kullanıcı adı</label><input class="input" name="userName" value="${esc(row.userName)}" required></div><div class="field"><label>Görünen ad</label><input class="input" name="displayName" value="${esc(row.displayName)}" required></div><div class="field"><label>Rol</label><select class="filter-select" name="roleId">${state.roles.filter(x=>x.isActive||x.id===row.roleId).map(x=>`<option value="${x.id}" ${row.roleId===x.id?'selected':''}>${esc(x.name)}</option>`).join('')}</select></div><div class="field"><label>Çalışan hesabı</label><select class="filter-select" name="employeeId">${optionList(state.employees,row.employeeId,'Bağlantı yok')}</select></div><div class="field full"><label>${row.id?'Yeni parola (değiştirmeyecekseniz boş bırakın)':'Parola'}</label><input class="input" type="password" name="password" ${row.id?'':'required'}><small>En az 10 karakter; büyük/küçük harf, rakam ve özel karakter.</small></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive!==false?'checked':''}> Aktif kullanıcı</label></div>`,async form=>{await api('/api/authorization/users',{method:'POST',body:JSON.stringify({id:row.id||null,userName:form.get('userName'),displayName:form.get('displayName'),roleId:Number(form.get('roleId')),employeeId:Number(form.get('employeeId'))||null,isActive:form.get('isActive')==='on',password:form.get('password')||null})});toast('Kullanıcı kaydedildi.');await authorizationPage();});}
function roleEditor(row={}){openDialog(row.id?'Rolü düzenle':'Yeni rol',`<div class="form-grid"><div class="field"><label>Rol adı</label><input class="input" name="name" value="${esc(row.name)}" required></div><div class="field"><label>Rol kodu</label><input class="input" name="code" value="${esc(row.code)}" ${row.isSystem?'readonly':''} required><small>Küçük harf ve tire kullanın.</small></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive!==false?'checked':''}> Aktif rol</label></div>`,async form=>{await api('/api/authorization/roles',{method:'POST',body:JSON.stringify({id:row.id||null,name:form.get('name'),code:form.get('code'),isActive:form.get('isActive')==='on'})});toast('Rol kaydedildi.');await authorizationPage();});}
function permissionEditor(role) {
  const administrator = role.code==='administrator';
  const active = new Set(state.rolePermissions.filter(x=>x.roleId===role.id&&x.isAllowed).map(x=>x.permission));
  openDialog(`${role.name} izinleri`, `${administrator?'<p>Sistem yöneticisi tüm izinlere sahiptir. Bu rolün izinleri kaldırılamaz.</p>':''}<div class="permission-grid">${permissionNames.map((name,id)=>retiredPermissions.has(id)?'':`<label class="permission-item"><input type="checkbox" name="permissions" value="${id}" ${administrator||active.has(id)?'checked':''} ${administrator?'disabled':''}>${name}</label>`).join('')}</div>`, async form=>{
    await api(`/api/authorization/permissions/${role.id}`,{method:'PUT',body:JSON.stringify(form.getAll('permissions').map(Number))});
    toast('Rol izinleri güncellendi.'); await authorizationPage();
  });
}

async function afterLogin(user){state.user=user;document.querySelector('#login-screen').hidden=true;document.querySelector('.app-shell').removeAttribute('aria-hidden');document.querySelector('#user-menu').textContent=initials(user.displayName);document.querySelector('#user-menu').title=`${user.displayName} · ${user.roleName}`;document.querySelectorAll('[data-permission]').forEach(x=>x.hidden=!hasPermission(Number(x.dataset.permission)));await navigate();}
function openPasswordDialog(){openDialog('Parola değiştir',`<div class="form-grid"><div class="field full"><label>Mevcut parola</label><input class="input" type="password" name="currentPassword" required></div><div class="field full"><label>Yeni parola</label><input class="input" type="password" name="newPassword" required><small>En az 10 karakter; büyük/küçük harf, rakam ve özel karakter.</small></div></div>`,async form=>{await api('/api/auth/change-password',{method:'POST',body:JSON.stringify({currentPassword:form.get('currentPassword'),newPassword:form.get('newPassword')})});toast('Parolanız değiştirildi.');});}
async function showLogin(){
  document.querySelector('#login-screen').hidden=false;
  document.querySelector('.app-shell').setAttribute('aria-hidden','true');
  try {
    const defaults=await api('/api/auth/login-defaults');
    const form=document.querySelector('#login-form');
    if(defaults && form.elements.userName.value===defaults.userName && !form.elements.password.value)form.elements.password.value=defaults.password;
  } catch { /* Giriş formu, geliştirme varsayılanları alınamasa da kullanılabilir. */ }
}
async function bootstrap(){try{await afterLogin(await api('/api/auth/me'));}catch(error){if(error.status===401)showLogin();else showError(error,bootstrap);}}

document.querySelector('#login-form').onsubmit=async event=>{event.preventDefault();const loginForm=event.currentTarget;const error=document.querySelector('#login-error');error.textContent='';const button=event.submitter;button.disabled=true;try{const form=new FormData(loginForm);const user=await api('/api/auth/login',{method:'POST',body:JSON.stringify({userName:form.get('userName'),password:form.get('password')})});loginForm.reset();await afterLogin(user);}catch(ex){error.textContent=ex.message;}finally{button.disabled=false;}};
document.querySelector('#user-menu').onclick=()=>{openDialog(state.user.displayName,`<div class="detail-grid"><div class="detail-box"><small>Kullanıcı</small><strong>${esc(state.user.userName)}</strong></div><div class="detail-box"><small>Rol</small><strong>${esc(state.user.roleName)}</strong></div></div><button type="button" class="button secondary" id="change-password">Parola değiştir</button><button type="button" class="button danger" id="logout" style="margin-left:8px">Çıkış yap</button>`,async()=>{});document.querySelector('#dialog-form .dialog-foot').remove();document.querySelector('#change-password').onclick=()=>{dialog.close();openPasswordDialog();};document.querySelector('#logout').onclick=async()=>{await api('/api/auth/logout',{method:'POST'});dialog.close();state.user=null;location.hash='#dashboard';showLogin();};};

Object.assign(titles, {calibration:'Kalibrasyon',development:'Bireysel Hedefler ve Gelişim'});
const routes = { dashboard:dashboardPage, evaluation:evaluationPage, history:historyPage, employees:employeesPage, professions:professionsPage, kpis:kpisPage, periods:periodsPage, goals:goalsPage, authorization:authorizationPage, leaves:leavesPage, calibration:calibrationPage,development:developmentPage };
async function navigate(){if(!state.user)return;const route=(location.hash||'#dashboard').slice(1);const page=routes[route]||dashboardPage;document.querySelector('#page-name').textContent=titles[route]||titles.dashboard;document.querySelectorAll('[data-route]').forEach(x=>x.classList.toggle('active',x.dataset.route===route));closeMenu();await page();if(app.querySelector('select:not([data-selection-enhanced])'))enhancePageSelects();app.focus({preventScroll:true});}
function closeMenu(){document.querySelector('#sidebar').classList.remove('open');document.querySelector('#mobile-overlay').classList.remove('open');}
document.querySelector('#menu-button').onclick=()=>{document.querySelector('#sidebar').classList.add('open');document.querySelector('#mobile-overlay').classList.add('open');};
document.querySelector('#mobile-overlay').onclick=closeMenu;
window.addEventListener('hashchange',navigate);
document.querySelector('#today').textContent=new Intl.DateTimeFormat('tr-TR',{weekday:'long',day:'numeric',month:'long'}).format(new Date());
bootstrap();
