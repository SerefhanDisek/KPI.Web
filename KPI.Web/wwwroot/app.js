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
  const request = async (path, init) => {
    try { return await fetch(path, init); }
    catch (error) {
      if (error.name === 'AbortError') throw error;
      throw new Error('Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin.', {cause:error});
    }
  };
  let csrfToken;
  if (!['GET','HEAD','OPTIONS'].includes((options.method||'GET').toUpperCase())) {
    const tokenResponse=await request('/api/auth/csrf',{cache:'no-store',signal:options.signal});
    if(!tokenResponse.ok)throw new Error('Güvenlik doğrulaması alınamadı. Sayfayı yenileyip tekrar deneyin.');
    csrfToken=(await tokenResponse.json()).token;
  }
  const response = await request(url, {
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

function spreadsheetActions(canImport = true, importOnly = false, exportOnly = false, canTemplate = canImport) {
  const description=exportOnly?'Seçili dönemlerdeki onaylı amir sonuçlarını veya aynı sütunlardaki boş şablonu indirin.':importOnly?(canImport?'Çalışanların izinlerini Excel dosyasından yükleyip aşağıdaki listede görüntüleyin.':'Excel yüklemek için izin yükleme yetkisi gerekir.'):(canImport?'Seçili filtrelerdeki kayıtları indirin veya Excel dosyasından aktarın.':'Kayıtları indirebilirsiniz. İçe aktarma ve şablon indirme için kayıt yönetme yetkisi gerekir.');
  return `<section class="card spreadsheet-toolbar" aria-labelledby="excel-toolbar-title"><div><h2 id="excel-toolbar-title">Excel işlemleri</h2><p>${description}</p></div><div class="actions">${importOnly?'':'<button class="button secondary" id="excel-export">Excel dışa aktar</button>'}${exportOnly&&!canTemplate?'':`<button class="button secondary" id="excel-template" ${canTemplate?'':'disabled'}>Şablon indir</button>`}${exportOnly?'':`<button class="button primary" id="excel-import" ${canImport?'':'disabled'}>Excel ${importOnly?'yükle':'içe aktar'}</button>`}</div></section>`;
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
    const employee = resource === 'employees';
    const leave = resource === 'leaves';
    const kpi = resource === 'kpis';
    openDialog('Excel içe aktar', `<div class="form-grid"><p class="field full">${leave ? 'Adı Soyadı, Kalan İzin Gün Sayısı ve İzin Hak Ediş Tarihi sütunlarını içeren dosyanızı veya indirdiğiniz şablonu kullanın.' : 'Önce şablonu indirip doldurun veya dışa aktardığınız dosyayı kullanın.'} En fazla 10 MB ve 10.000 veri satırı yükleyebilirsiniz.</p>
      <div class="field full"><label for="excel-file">Excel dosyası (.xlsx)</label><input class="input" id="excel-file" type="file" name="file" accept=".xlsx" required></div>
      <p class="field full">${evaluation ? 'Her KPI ayrı satırda olmalı. Çalışan ve Ekip, Dönem ve KPI adlarıyla eşleştirilir; aynı değerlendirmenin Amir Görüşü tüm satırlarda aynı olmalıdır. Değerlendiren Amir bilgi amaçlıdır; kayıt size ait oluşturulur. Size ait mevcut çalışan/dönem kayıtları atlanır.' : employee ? 'Ad Soyad ile mevcut çalışan eşleştirilir; eşleşme yoksa yeni çalışan oluşturulur. Belirtilen meslek uygulanır; sistemde yoksa meslek de oluşturulur. Meslek boşsa mevcut meslek korunur, yeni çalışan için Meslek atanmadı kullanılır. Aktiflik durumu Evet veya Hayır olmalıdır; Hayır mevcut kaydı pasife alır. Belirsiz eşleşmeler alınmaz. Yalnızca personel kaydı oluşturulur.' : leave ? 'Adı Soyadı ile mevcut çalışan eşleştirilir. Sistemde bulunmayan kişiler Meslek atanmadı mesleğiyle yeni çalışan olarak eklenir ve izin listesinde görünür. Yalnızca personel kaydı oluşturulur. Tekrar yükleme çalışanın mevcut bakiyesini ve hak ediş tarihini günceller; dosyada aynı çalışan adı tekrar edemez. Kalan gün sayısında yarım gün ve negatif değerler desteklenir. Tarihi Excel tarihi veya gg.aa.yyyy biçiminde yazabilirsiniz. Dosyadaki bakiye ve tarih aynen alınır; otomatik izin hesabı yapılmaz.' : kpi ? 'KPI başlığı ve ekip kapsamı birlikte eşleştirilir. Ekip adlarını noktalı virgülle ayırın; Herkes=Evet tüm ekipleri kapsar. Şirket hedefi adla eşleştirilir ve isteğe bağlıdır. Tek eşleşmede mevcut KPI güncellenir; eşleşme yoksa standart 1–5 puanlamalı yeni KPI oluşturulur. Aynı KPI ve kapsam dosyada tekrar edemez.' : 'ID boşsa yeni kayıt oluşturulur; doluysa mevcut kayıt güncellenir. Aktif alanı Hayır olan kayıtlar pasif yapılır.'} Başarılı kayıtlar saklanır; hatalar satır numarasıyla gösterilir.</p>
      ${evaluation && hasPermission(2) ? '<label class="check-row"><input type="checkbox" name="permanentEvaluations"> Kalıcı kaydet (onaylanır)</label>' : ''}
      <label class="check-row"><input type="checkbox" name="confirmed" required> Dosyadaki kayıtların aktarılmasını ve belirtilen değişiklikleri onaylıyorum.</label></div>`, async form => {
      const file = form.get('file');
      if (!file?.size || !file.name.toLowerCase().endsWith('.xlsx')) throw new Error('Dolu bir .xlsx dosyası seçmelisiniz.');
      if (file.size > 10 * 1024 * 1024) throw new Error('Dosya en fazla 10 MB olabilir.');
      const permanent = form.get('permanentEvaluations') === 'on';
      const body = new FormData(); body.append('file', file); body.append('permanentEvaluations', String(permanent));
      const result = await api(`/api/spreadsheets/${resource}/import`, {method:'POST', body});
      await reload();
      setTimeout(() => showSpreadsheetResult(result, resource), 0);
    });
    dialogContent.querySelector('[type="submit"]').textContent = 'İçe aktar';
  };
}

function showSpreadsheetResult(result, resource) {
  dialog.classList.remove('wide');
  dialogContent.innerHTML = `<div class="dialog-head"><h2>Excel aktarım sonucu</h2><button class="icon-button" data-close aria-label="Kapat">${icon('close')}</button></div>
    <div class="dialog-body"><p role="status">${result.importedCount} kayıt aktarıldı · ${result.skippedCount} kayıt atlandı · ${result.errors.length} kayıt hatalı.</p>
    ${result.errors.length ? `<p>${resource === 'kpis' ? 'Hatalı kayıtları düzeltip yeniden yükleyebilirsiniz. Altı sütunlu dosya aynı başlık ve ekip kapsamıyla eşleşen mevcut KPI kayıtlarını günceller.' : resource === 'leaves' ? 'Hatalı kayıtları düzeltip yeniden yükleyebilirsiniz. Üç sütunlu dosyayı tekrar yüklemek çalışanın mevcut bakiyesini ve hak ediş tarihini günceller.' : 'Hatalı kayıtları düzeltip yeniden yükleyebilirsiniz. Başarıyla oluşturulan kayıtların tekrar eklenmemesi için onları dosyadan çıkarın.'}</p><ul>${result.errors.slice(0,100).map(error => `<li>${esc(error)}</li>`).join('')}</ul>${result.errors.length>100?'<p>İlk 100 hata gösteriliyor. Tümünü hata listesinden indirebilirsiniz.</p>':''}` : ''}</div>
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
  return `<div class="field"><span class="filter-label" id="${id}-label">${esc(label)}</span><details class="multi-filter" id="${id}" data-multiple="${multiple}" data-placeholder="${esc(placeholder)}"><summary class="filter-select" aria-labelledby="${id}-label ${id}-summary"><span id="${id}-summary">${esc(names.length===1?names[0]:names.length?`${names.length} seçim`:placeholder)}</span></summary><div class="multi-filter-options" popover="manual"><div class="multi-filter-search">${icon('search')}<input type="search" class="input" aria-label="${esc(label)} seçeneklerinde ara" placeholder="Seçeneklerde ara…" autocomplete="off" spellcheck="false"></div>${clearable?`<button type="button" class="multi-filter-reset">${esc(placeholder)}</button>`:''}${items.map(x=>`<label><input type="${multiple?'checkbox':'radio'}" name="${id}" value="${esc(x.id)}" ${values.includes(String(x.id))?'checked':''}><span>${esc(x.name??x.fullName)}</span></label>`).join('')}<p class="multi-filter-empty" role="status" hidden>Seçenek bulunamadı.</p></div></details></div>`;
}

const selectedFilterValues = id => [...document.getElementById(id).querySelectorAll('input:checked')].map(x=>x.value);

function bindSelectionFilters(onChange, root = app) {
  const controller = new AbortController();
  const controls = [...root.querySelectorAll('.multi-filter')];
  controls.forEach(control => {
    const panel = control.querySelector('.multi-filter-options');
    const summary = control.querySelector('summary');
    const position = () => {
      if (!control.open || !panel.matches(':popover-open')) return;
      const anchor = summary.getBoundingClientRect(), gap = 6, edge = 12;
      if (anchor.bottom <= edge || anchor.top >= innerHeight - edge) { close(); return; }
      const below = Math.max(0, innerHeight - anchor.bottom - gap - edge);
      const above = Math.max(0, anchor.top - gap - edge);
      const opensAbove = below < Math.min(260, panel.scrollHeight) && above > below;
      panel.style.width = `${Math.min(anchor.width, innerWidth - edge * 2)}px`;
      panel.style.maxHeight = `${Math.min(260, opensAbove ? above : below)}px`;
      panel.style.left = `${Math.max(edge, Math.min(anchor.left, innerWidth - panel.offsetWidth - edge))}px`;
      const top = opensAbove ? anchor.top - gap - panel.offsetHeight : anchor.bottom + gap;
      panel.style.top = `${Math.max(edge, Math.min(top, innerHeight - panel.offsetHeight - edge))}px`;
    };
    const close = () => { control.open = false; if (panel.matches(':popover-open')) panel.hidePopover(); summary.setAttribute('aria-expanded','false'); };
    summary.setAttribute('aria-expanded', String(control.open));
    summary.setAttribute('aria-controls', panel.id = `${control.id}-options`);
    const search = control.querySelector('.multi-filter-search input');
    const choices = [...control.querySelectorAll('.multi-filter-options > label')];
    const filterOptions = () => {
      const term = search.value.trim().normalize('NFC').toLocaleLowerCase('tr-TR');
      choices.forEach(choice => {
        choice.hidden = !choice.textContent.normalize('NFC').toLocaleLowerCase('tr-TR').includes(term);
      });
      control.querySelector('.multi-filter-empty').hidden = choices.some(choice => !choice.hidden);
      position();
    };
    search.addEventListener('input', filterOptions, {signal:controller.signal});
    search.addEventListener('keydown', event => {
      if (event.key === 'Enter' && !event.isComposing) event.preventDefault();
      if (event.key === 'ArrowDown') {
        const first = choices.find(choice => !choice.hidden && !choice.querySelector('input').disabled);
        if (first) { event.preventDefault(); first.querySelector('input').focus(); }
      }
    }, {signal:controller.signal});
    filterOptions();
    const update = () => {
      const selected = [...control.querySelectorAll('input:checked')];
      control.querySelector('summary span').textContent = selected.length===1 ? selected[0].nextElementSibling.textContent : selected.length ? `${selected.length} seçim` : control.dataset.placeholder;
      if (control.dataset.multiple==='false') { close(); summary.focus(); }
      onChange(control);
    };
    control.onchange = event => {
      if (event.target.matches('input[type="checkbox"], input[type="radio"]')) update();
    };
    const reset = control.querySelector('.multi-filter-reset');
    if (reset) reset.onclick = () => {
      control.querySelectorAll('input[type="checkbox"], input[type="radio"]').forEach(input=>input.checked=false);
      search.value = ''; filterOptions();
      update(); close(); summary.focus();
    };
    control.ontoggle = () => {
      if (controller.signal.aborted) return;
      if (control.open) {
        document.querySelectorAll('.multi-filter[open]').forEach(other => { if (other !== control) other.open = false; });
        panel.showPopover(); summary.setAttribute('aria-expanded','true'); position();
      } else close();
    };
    window.addEventListener('resize', position, {signal:controller.signal});
    document.addEventListener('scroll', position, {capture:true,signal:controller.signal});
    controller.signal.addEventListener('abort', close, {once:true});
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
      picker.querySelectorAll('input[type="checkbox"], input[type="radio"]').forEach(input => {
        input.disabled = select.disabled || [...select.options].some(option => option.value===input.value && option.disabled);
      });
      picker.querySelector('.multi-filter-search input').disabled = select.disabled;
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
  state.actionMenuController?.abort();
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
  const region = document.querySelector('#toasts');
  region.append(element);
  if (!region.matches(':popover-open')) region.showPopover();
  setTimeout(() => { element.remove(); if (!region.childElementCount) region.hidePopover(); }, 5000);
}

let activeMessageBox;

function messageBox(message, {title = 'Bilgilendirme', confirm = false, confirmText = 'Tamam', type = 'info', signal} = {}) {
  if (signal?.aborted) return Promise.resolve(false);
  activeMessageBox?.abort();
  const controller = new AbortController();
  activeMessageBox = controller;
  const box = document.createElement('dialog');
  box.id = 'message-box';
  box.className = `dialog message-dialog ${type}`;
  box.setAttribute('role', 'alertdialog');
  box.setAttribute('aria-labelledby', 'message-box-title');
  box.setAttribute('aria-describedby', 'message-box-description');
  box.innerHTML = `<div class="message-heading"><span class="message-icon">${icon(type === 'info' ? 'info' : 'warning')}</span><button type="button" class="icon-button" data-dismiss aria-label="Kapat">${icon('close')}</button></div><div class="message-content"><h2 id="message-box-title">${esc(title)}</h2><p id="message-box-description">${esc(message)}</p></div><div class="dialog-foot">${confirm?'<button type="button" class="button secondary" data-dismiss>Vazgeç</button>':''}<button type="button" class="button ${type === 'danger' ? 'danger' : 'primary'}" data-accept>${esc(confirmText)}</button></div>`;
  const previousFocus = document.activeElement.closest('.row-actions')?.querySelector('[data-action-menu]') || document.activeElement;
  const owner = dialog.open ? dialogContent.firstElementChild : null;
  return new Promise(resolve => {
    let settled = false;
    const finish = accepted => {
      if (settled) return;
      settled = true;
      controller.abort();
      if (box.open) box.close();
      box.remove();
      if (activeMessageBox === controller) activeMessageBox = null;
      if (previousFocus?.isConnected && !previousFocus.disabled) previousFocus.focus({preventScroll:true});
      resolve(accepted);
    };
    controller.signal.addEventListener('abort', () => finish(false), {once:true});
    signal?.addEventListener('abort', () => finish(false), {once:true, signal:controller.signal});
    window.addEventListener('hashchange', () => finish(false), {signal:controller.signal});
    if (owner) {
      dialog.addEventListener('close', () => { if (!dialog.open || !owner.isConnected) finish(false); }, {signal:controller.signal});
      const observer = new MutationObserver(() => { if (!owner.isConnected) finish(false); });
      observer.observe(dialogContent, {childList:true});
      controller.signal.addEventListener('abort', () => observer.disconnect(), {once:true});
    }
    box.addEventListener('cancel', event => { event.preventDefault(); finish(false); }, {signal:controller.signal});
    box.addEventListener('close', () => finish(false), {signal:controller.signal});
    box.addEventListener('click', event => {
      const bounds = box.getBoundingClientRect();
      if (event.target === box && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) finish(false);
    }, {signal:controller.signal});
    box.querySelectorAll('[data-dismiss]').forEach(button => button.onclick = () => finish(false));
    box.querySelector('[data-accept]').onclick = () => finish(true);
    document.body.append(box);
    box.showModal();
    box.querySelector(confirm ? '.dialog-foot [data-dismiss]' : '[data-accept]').focus({preventScroll:true});
  });
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
    try { const result = await onSubmit(new FormData(form)); if (result !== false && form.isConnected) dialog.close(); }
    catch (error) { toast(error.message, 'error'); }
    finally { form.dataset.saving = 'false'; submits.forEach(x => x.disabled = false); }
  };
  const selections = enhanceSelects(dialogContent);
  dialog.addEventListener('close', () => selections.abort(), {once:true});
  dialog.showModal();
  bindGrowingNotes(dialogContent);
}

function resizeNote(input) {
  input.style.height = 'auto';
  input.style.height = `${input.scrollHeight + input.offsetHeight - input.clientHeight}px`;
}

function bindGrowingNotes(root) {
  root.querySelectorAll('textarea[data-growing-note]').forEach(input => {
    resizeNote(input);
    input.addEventListener('input', () => resizeNote(input));
  });
}

function pageHead(title, description, actions = '') {
  return `<div class="page-head"><div><h1>${title}</h1><p>${description}</p></div><div class="actions">${actions}</div></div>`;
}

function rowActionMenu(id, label, actions) {
  const menuId = `row-actions-${id}`;
  return `<div class="row-actions"><button type="button" class="row-action" data-action-menu="${esc(id)}" popovertarget="${esc(menuId)}" aria-label="${esc(label)} · İşlemler" aria-haspopup="menu" aria-expanded="false" aria-controls="${esc(menuId)}" title="İşlemler">${icon('edit')}</button><div class="row-action-menu" id="${esc(menuId)}" popover="auto" role="menu" aria-label="${esc(label)} · İşlemler">${actions}</div></div>`;
}

function managementActionButtons(resource, row) {
  return `<button type="button" class="row-action" role="menuitem" data-deactivate-record="${row.id}" data-record-resource="${esc(resource)}" ${!row.isActive ? 'disabled' : ''}>Pasife al</button><button type="button" class="row-action danger" role="menuitem" data-delete-record="${row.id}" data-record-resource="${esc(resource)}">Sil</button>`;
}

function bindManagementActions(resource, rows, refresh) {
  const change = async (button, permanent) => {
    const id = Number(permanent ? button.dataset.deleteRecord : button.dataset.deactivateRecord);
    const row = rows.find(x => x.id === id);
    const label = row.fullName ?? row.displayName ?? row.name;
    const owner = button.closest('.card');
    if (!await messageBox(permanent ? `“${label}” kalıcı olarak silinsin mi? Bu işlem geri alınamaz. Başka kayıtlarda kullanılan kayıtlar silinemez.` : `“${label}” pasife alınsın mı? Mevcut kayıtlar korunacak.`, {title:permanent?'Kaydı kalıcı olarak sil':'Kaydı pasife al', confirm:true, confirmText:permanent?'Kalıcı olarak sil':'Pasife al', type:permanent?'danger':'warning', signal:state.actionMenuController?.signal}) || !button.isConnected) return;
    const buttons = [...button.closest('.row-actions').querySelectorAll('button')];
    const disabled = buttons.map(x => x.disabled);
    buttons.forEach(x => x.disabled = true);
    try {
      await api(`/api/${resource}/${id}${permanent ? '' : '/deactivate'}`, {method:permanent?'DELETE':'POST'});
      if ((resource === 'authorization/users' && id === state.user.id) || (resource === 'authorization/roles' && id === state.user.roleId)) {
        try { await api('/api/auth/logout', {method:'POST'}); }
        finally { state.user = null; location.hash = '#dashboard'; showLogin(); }
        toast(permanent ? 'Hesabınız silindi.' : 'Oturumunuzun erişimi pasife alındı.');
        return;
      }
      if (owner.isConnected) {
        if (permanent) rows.splice(rows.indexOf(row), 1);
        else row.isActive = false;
        refresh();
      }
      toast(permanent ? 'Kayıt kalıcı olarak silindi.' : 'Kayıt pasife alındı.');
    } catch (error) { toast(error.message, 'error'); }
    finally { buttons.forEach((button,index) => button.disabled = disabled[index]); }
  };
  app.querySelectorAll(`[data-record-resource="${resource}"][data-deactivate-record]`).forEach(button => button.onclick = () => change(button, false));
  app.querySelectorAll(`[data-record-resource="${resource}"][data-delete-record]`).forEach(button => button.onclick = () => change(button, true));
}

function bindRowActionMenus(root = app) {
  const controller = new AbortController();
  const menus = [...root.querySelectorAll('.row-action-menu')];
  const close = () => menus.filter(menu => menu.matches(':popover-open')).forEach(menu => menu.hidePopover());
  const position = menu => {
    const bounds = menu.previousElementSibling.getBoundingClientRect();
    menu.style.left = `${Math.max(12, Math.min(bounds.right-menu.offsetWidth, innerWidth-menu.offsetWidth-12))}px`;
    menu.style.top = `${Math.max(12, bounds.bottom+menu.offsetHeight+6 <= innerHeight-12 ? bounds.bottom+6 : bounds.top-menu.offsetHeight-6)}px`;
  };
  menus.forEach(menu => {
    const trigger = menu.previousElementSibling;
    menu.addEventListener('toggle', event => {
      trigger.setAttribute('aria-expanded', String(event.newState === 'open'));
      if (event.newState !== 'open') return;
      position(menu);
      menu.querySelector('button:not(:disabled)').focus({preventScroll:true});
    }, {signal:controller.signal});
    menu.onkeydown = event => {
      const items = [...menu.querySelectorAll('button:not(:disabled)')];
      const index = items.indexOf(document.activeElement);
      if (!['ArrowDown','ArrowUp','Home','End'].includes(event.key)) return;
      event.preventDefault();
      const next = event.key==='Home' ? 0 : event.key==='End' ? items.length-1 : (index+(event.key==='ArrowDown'?1:-1)+items.length)%items.length;
      items[next].focus();
    };
  });
  root.addEventListener('click', event => {
    if (event.target.closest('.row-action-menu [role="menuitem"]')) close();
  }, {capture:true,signal:controller.signal});
  const reposition = () => menus.filter(menu => menu.matches(':popover-open')).forEach(position);
  window.addEventListener('resize', reposition, {signal:controller.signal});
  document.addEventListener('scroll', reposition, {capture:true,signal:controller.signal});
  controller.signal.addEventListener('abort', close, {once:true});
  return controller;
}

function bindPageActionMenus() {
  state.actionMenuController?.abort();
  state.actionMenuController = bindRowActionMenus();
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
    const results = data.combinedResults || [];
    app.innerHTML = `${pageHead('Gösterge Paneli', 'Ekip performansını ve hedef ilerlemesini tek görünümde izleyin.', selectionFilter('dashboard-period','Dönem',state.periods,selected,'Tüm dönemler',{multiple:false})) }
      <div class="manager-grid">${managers.map(x => `<button type="button" class="manager-card" data-manager="${x.userId}" aria-haspopup="dialog" aria-label="${esc(x.managerName)} · Değerlendirmeleri görüntüle" ${hasPermission(3)?'':'disabled'}><small>${esc(x.roleName)}</small><strong>${esc(x.managerName)}</strong><div class="manager-teams">Bağlı ekipler: ${x.teams?.length ? x.teams.map(team => `<span class="badge info">${esc(team.name)}</span>`).join('') : 'Henüz ekip bağlı değil'}</div><div class="manager-metrics"><span>Çalışan<b>${x.employeeCount}</b></span><span>Onaylı<b>${x.approvedEvaluationCount}</b></span><span>Bekleyen<b>${x.pendingEvaluationCount}</b></span></div><span class="manager-card-link">${icon('eye')}${hasPermission(3)?'Değerlendirmeleri görüntüle':'Değerlendirme geçmişi yetkisi gerekli'}</span></button>`).join('')}</div>
      <div class="stats-grid">
        ${statCard('users','teal','Toplam çalışan',data.totalEmployees)}
        ${statCard('check','blue','Değerlendirilen',data.evaluatedEmployees)}
        ${statCard('history','amber','Bekleyen',data.notEvaluatedEmployees)}
      </div>
      <div class="grid-2 dashboard-results">
        <section class="card"><div class="card-head"><h2>Birleşik performans sıralaması</h2><span class="badge info">${results.length} kayıt</span></div>
          <div class="card-body"><p>Birleşik sonuç, ekip amirlerinin Yetkilendirme ekranında belirlenen güncel ağırlıklarıyla hesaplanır.</p></div>
          <div class="table-wrap">${results.length ? `<table class="table"><thead><tr><th>Çalışan / ekip</th><th>Dönem</th><th>Amir sonuçları</th><th>Ortalama Puan</th></tr></thead><tbody>${results.slice(0, 12).map(row => `<tr><td><div class="person"><span class="person-avatar">${initials(row.employeeName)}</span>${esc(row.employeeName)}</div><small>${esc(row.professionName)}</small></td><td>${esc(row.periodName)}</td><td>${managerScores(row.scores)}</td><td>${combinedScore(row)}</td></tr>`).join('')}</tbody></table>` : emptyState('Henüz onaylı değerlendirme yok','Ekipte tanımlanan amirlerin onaylı puanları güncel ağırlıklarıyla birleştirilir.')}</div>
        </section>
        <section class="card"><div class="card-head"><h2>Şirket hedefleri</h2></div><div class="card-body">${(data.goalProgress || []).length ? data.goalProgress.map(goal => `<div class="goal-row"><div class="goal-top"><span>${esc(goal.goalName)}</span><strong>${formatNumber(goal.averageScore)} / 5</strong></div><div class="progress"><span style="width:${Math.min(100, goal.averageScore * 20)}%"></span></div></div>`).join('') : emptyState('Hedef verisi yok','KPI maddelerini şirket hedefleriyle ilişkilendirin.')}</div></section>
      </div>`;
    state.filterController = bindSelectionFilters(() => { sessionStorage.setItem('dashboard-period', selectedFilterValues('dashboard-period')[0]||''); dashboardPage(); });
    document.querySelectorAll('[data-manager]').forEach(button => button.onclick = () => showManagerEvaluations(managers.find(x => String(x.userId) === button.dataset.manager), selected));
  } catch (error) { showError(error, dashboardPage); }
}

function showManagerEvaluations(manager, periodId, initialFilters = {}) {
  const controller = new AbortController();
  let rows = [], filterRows = [], employeeIds = initialFilters.employeeIds || [], professionIds = initialFilters.professionIds || [],
    status = initialFilters.status || '', filterController, actionController;
  const periodName = state.periods.find(x => String(x.id) === periodId)?.name || 'Tüm dönemler';
  const heading = `${manager.managerName} · Değerlendirmeler`;
  const shell = body => {
    filterController?.abort(); actionController?.abort();
    dialog.classList.add('wide');
    dialog.setAttribute('aria-labelledby', 'manager-dialog-title');
    dialogContent.innerHTML = `<div class="dialog-head"><h2 id="manager-dialog-title" tabindex="-1">${esc(heading)}</h2><button type="button" class="icon-button" data-close aria-label="Kapat">${icon('close')}</button></div><div class="dialog-body"><p class="manager-dialog-context">${esc(manager.roleName)} · ${esc(periodName)}<br>Bu amirin hazırladığı, görüntüleme yetkiniz olan değerlendirmeler.</p>${body}</div><div class="dialog-foot"><button type="button" class="button secondary" data-close>Kapat</button></div>`;
    dialogContent.querySelectorAll('[data-close]').forEach(button => button.onclick = () => dialog.close());
    document.querySelector('#manager-dialog-title').focus();
  };
  const render = () => {
    const approved = rows.filter(x => x.status === 4);
    const employees = [...new Map(filterRows.map(x => [x.employeeId, {id:x.employeeId, name:x.employeeName}])).values()].sort((a,b) => a.name.localeCompare(b.name, 'tr'));
    const professions = [...new Map(filterRows.map(x => [x.professionId, {id:x.professionId, name:x.professionName}])).values()].sort((a,b) => a.name.localeCompare(b.name, 'tr'));
    shell(`<div class="detail-grid"><div class="detail-box"><small>Çalışan</small><strong>${manager.employeeCount}</strong></div><div class="detail-box"><small>Onaylı</small><strong>${approved.length}</strong></div><div class="detail-box"><small>Bekleyen</small><strong>${rows.length-approved.length}</strong></div></div>
      <div class="toolbar history-filters">${selectionFilter('manager-evaluation-employee','Çalışan',employees,employeeIds,'Tüm çalışanlar')}${selectionFilter('manager-evaluation-profession','Ekip',professions,professionIds,'Tüm ekipler')}${selectionFilter('manager-evaluation-status','Durum',statusNames.map((name,id)=>({id,name})),status,'Tüm durumlar',{multiple:false})}</div><p id="manager-evaluation-count" role="status"></p><div class="table-wrap" id="manager-evaluation-list"></div>`);
    const apply = () => {
      const filtered = rows.filter(x => (!employeeIds.length || employeeIds.includes(String(x.employeeId)))
        && (!professionIds.length || professionIds.includes(String(x.professionId))) && (status==='' || String(x.status)===status));
      document.querySelector('#manager-evaluation-count').textContent = `${filtered.length} değerlendirme`;
      document.querySelector('#manager-evaluation-list').innerHTML = filtered.length ? `<table class="table"><thead><tr><th>Çalışan / Ekip</th><th>Dönem / Tarih</th><th>Puan</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${filtered.map(row=>`<tr><td><strong>${esc(row.employeeName)}</strong><br><small>${esc(row.professionName)}</small></td><td>${esc(row.periodName)}<br><small>${formatDate(row.evaluationDate)}</small></td><td>${formatNumber(row.scorePercentage)} / 100<br><small>%${formatNumber(row.scorePercentage)}</small></td><td>${statusBadge(row.status)}</td><td>${rowActionMenu(`manager-evaluation-${row.id}`, `${row.employeeName} · ${row.periodName}`, `<button type="button" class="row-action" role="menuitem" data-manager-evaluation="${row.id}">Ayrıntıyı görüntüle</button>${row.canEdit&&row.evaluatorUserId===state.user.id?`<button type="button" class="row-action" role="menuitem" data-edit-evaluation="${row.id}">Düzenle</button>`:''}${row.canDelete?`<button type="button" class="row-action danger" role="menuitem" data-delete-evaluation="${row.id}">Sil</button>`:''}`)}</td></tr>`).join('')}</tbody></table>` : emptyState('Değerlendirme bulunamadı', rows.length?'Çalışan, ekip veya durum filtrelerini değiştirebilirsiniz.':'Bu amir için seçili dönemde görüntüleyebileceğiniz değerlendirme bulunmuyor.');
      actionController?.abort(); actionController = bindRowActionMenus(dialogContent);
      document.querySelectorAll('[data-manager-evaluation]').forEach(button=>button.onclick=async()=>{
        button.disabled = true;
        try { await showEvaluationDetail(Number(button.dataset.managerEvaluation), rows, render, controller.signal); }
        finally { button.disabled = false; }
      });
      dialogContent.querySelectorAll('[data-edit-evaluation]').forEach(button=>button.onclick=async()=>{
        const id = Number(button.dataset.editEvaluation);
        const filters = {employeeIds, professionIds, status};
        await new Promise(resolve => {
          dialog.addEventListener('close', resolve, {once:true});
          dialog.close();
        });
        if ((location.hash || '#dashboard') !== '#dashboard') return;
        await editEvaluation(id, {
          onSaved: async () => { if ((location.hash || '#dashboard') === '#dashboard') await dashboardPage(); },
          onClose: () => { if ((location.hash || '#dashboard') === '#dashboard') showManagerEvaluations(manager, periodId, filters); }
        });
      });
      dialogContent.querySelectorAll('[data-delete-evaluation]').forEach(button=>button.onclick=()=>deleteEvaluation(
        Number(button.dataset.deleteEvaluation), rows, dialogContent.querySelector('#manager-evaluation-list'), async () => {
          if (!controller.signal.aborted) render();
          if ((location.hash || '#dashboard') === '#dashboard') await dashboardPage();
        }));
    };
    filterController = bindSelectionFilters(() => {
      employeeIds = selectedFilterValues('manager-evaluation-employee');
      professionIds = selectedFilterValues('manager-evaluation-profession');
      status = selectedFilterValues('manager-evaluation-status')[0]||'';
      apply();
    }, dialogContent);
    apply();
  };
  const load = async () => {
    shell('<div class="loading" role="status" aria-busy="true"><div><div class="spinner"></div>Değerlendirmeler yükleniyor…</div></div>');
    try {
      const query = new URLSearchParams({evaluatorUserId:String(manager.userId)});
      if (periodId) query.set('periodId', periodId);
      rows = await api(`/api/evaluations?${query}`, {signal:controller.signal});
      filterRows = [...rows];
      if (!controller.signal.aborted) render();
    } catch (error) {
      if (controller.signal.aborted) return;
      shell(`<div class="empty" role="alert"><strong>Değerlendirmeler yüklenemedi</strong><p>${esc(error.message)}</p><button type="button" class="button secondary" id="manager-retry">Tekrar dene</button></div>`);
      document.querySelector('#manager-retry').onclick = load;
    }
  };
  dialog.addEventListener('close', () => { controller.abort(); filterController?.abort(); actionController?.abort(); dialog.removeAttribute('aria-labelledby'); }, {once:true});
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
  state.filterController?.abort();
  state.employeeFilters ||= {employee:[], profession:[]};
  const filters = state.employeeFilters;
  app.innerHTML = `${pageHead('Çalışanlar','Ekip üyelerini, mesleklerini ve aktiflik durumlarını yönetin.',`<button class="button primary" id="add-employee">${icon('plus')}Çalışan ekle</button>`)}${spreadsheetActions()}
    <section class="card"><div class="card-body"><div class="toolbar history-filters">${selectionFilter('employee-name','Çalışanlar',rows.filter(x=>x.isActive),filters.employee,'Tüm çalışanlar')}${selectionFilter('employee-profession','Meslekler',state.professions,filters.profession,'Tüm meslekler')}</div></div>
      <div class="table-wrap"><table class="table"><thead><tr><th>Çalışan</th><th>Meslek</th><th>Durum</th><th>İşlem</th></tr></thead><tbody id="employee-rows">${employeeRows(rows)}</tbody></table></div></section>`;
  document.querySelector('#add-employee').onclick = () => employeeEditor();
  bindSpreadsheetActions('employees', () => ({ids:[...document.querySelectorAll('[data-employee]')].map(x=>Number(x.dataset.employee))}), employeesPage);
  state.filterController = bindSelectionFilters(filterEmployees);
  filterEmployees();
}

function filterEmployees() {
  const filters = state.employeeFilters;
  filters.employee = selectedFilterValues('employee-name');
  filters.profession = selectedFilterValues('employee-profession');
  const filtered = state.employees.filter(x => (!filters.employee.length || filters.employee.includes(String(x.id)))
    && (!filters.profession.length || filters.profession.includes(String(x.professionId))));
  document.querySelector('#employee-rows').innerHTML = employeeRows(filtered);
  bindEmployeeActions();
}

function employeeRows(rows) {
  rows = rows.filter(row => row.isActive);
  return rows.length ? rows.map(row => `<tr><td><div class="person"><span class="person-avatar">${initials(row.fullName)}</span>${esc(row.fullName)}</div></td><td>${esc(row.professionName)}</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td>${rowActionMenu(`employee-${row.id}`, row.fullName, `<button type="button" class="row-action" role="menuitem" data-employee="${row.id}">Düzenle</button>${managementActionButtons('employees', row)}`)}</td></tr>`).join('') : `<tr><td colspan="4">${emptyState('Çalışan bulunamadı','Arama ölçütlerini değiştirebilirsiniz.')}</td></tr>`;
}

function bindEmployeeActions() {
  bindPageActionMenus();
  bindManagementActions('employees', state.employees, filterEmployees);
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
  rows = rows.filter(row => row.isActive);
  app.innerHTML = `${pageHead(title,description,`<button class="button primary" id="add-simple">${icon('plus')}${itemName} ekle</button>`)}<section class="card"><div class="table-wrap"><table class="table"><thead><tr><th>${itemName} adı</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${rows.map(row => `<tr><td><strong>${esc(row.name)}</strong></td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td>${rowActionMenu(`${key}-${row.id}`, row.name, `<button type="button" class="row-action" role="menuitem" data-${key}="${row.id}">Düzenle</button>${managementActionButtons('professions', row)}`)}</td></tr>`).join('')}</tbody></table></div></section>`;
  bindPageActionMenus();
  bindManagementActions('professions', rows, () => renderSimpleManagement(title, description, rows, itemName, key, editor));
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
  app.innerHTML = `${pageHead('Dönemler','Değerlendirmelerin tarih aralıklarını ve kullanım durumlarını yönetin.',`<button class="button primary" id="add-period">${icon('plus')}Dönem ekle</button>`)}<section class="card"><div class="table-wrap"><table class="table"><thead><tr><th>Dönem</th><th>Başlangıç</th><th>Bitiş</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${state.periods.filter(row => row.isActive).map(row => `<tr><td><strong>${esc(row.name)}</strong></td><td>${formatDate(row.startDate)}</td><td>${formatDate(row.endDate)}</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td>${rowActionMenu(`period-${row.id}`, row.name, `<button type="button" class="row-action" role="menuitem" data-period="${row.id}">Düzenle</button>${managementActionButtons('periods', row)}`)}</td></tr>`).join('')}</tbody></table></div></section>`;
  bindPageActionMenus();
  bindManagementActions('periods', state.periods, renderPeriods);
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
  app.innerHTML = `${pageHead('Şirket Hedefleri','KPI ölçümlerini stratejik hedefler ve dönemlerle ilişkilendirin.',`<button class="button primary" id="add-goal">${icon('plus')}Hedef ekle</button>`)}<section class="card"><div class="table-wrap"><table class="table"><thead><tr><th>Hedef</th><th>Dönem</th><th>Öncelik</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${state.goals.filter(row => row.isActive).map(row => `<tr><td><strong>${esc(row.name)}</strong><br><small>${esc(row.description || '')}</small></td><td>${esc(row.periodName || 'Tüm dönemler')}</td><td>${row.priority}</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td>${rowActionMenu(`goal-${row.id}`, row.name, `<button type="button" class="row-action" role="menuitem" data-goal="${row.id}">Düzenle</button>${managementActionButtons('goals', row)}`)}</td></tr>`).join('')}</tbody></table></div></section>`;
  bindPageActionMenus();
  bindManagementActions('goals', state.goals, renderGoals);
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
    state.kpis = kpis; state.professions = professions; state.kpiGoals = goals;
    renderKpis(kpis);
  } catch (error) { showError(error, kpisPage); }
}

function renderKpis(rows) {
  state.filterController?.abort();
  state.kpiFilters ||= {kpi:[], category:[], profession:[]};
  const filters = state.kpiFilters;
  const active = rows.filter(x=>x.isActive);
  const categories = [...new Set(active.map(x=>x.category || ''))].map(category=>({id:category, name:category || 'Kategorisiz'}))
    .sort((a,b)=>a.name.localeCompare(b.name,'tr'));
  app.innerHTML = `${pageHead('KPI Yönetimi','Ortak ve mesleki değerlendirme ölçütlerini yönetin.',`<button class="button primary" id="add-kpi">${icon('plus')}KPI ekle</button>`)}${spreadsheetActions()}<section class="card"><div class="card-body"><div class="toolbar history-filters">${selectionFilter('kpi-title','KPI ölçütleri',active.map(x=>({id:x.id,name:x.title})),filters.kpi,'Tüm KPI ölçütleri')}${selectionFilter('kpi-category','Kategoriler',categories,filters.category,'Tüm kategoriler')}${selectionFilter('kpi-profession','Meslekler',state.professions,filters.profession,'Tüm meslekler')}</div></div><div class="table-wrap"><table class="table"><thead><tr><th>KPI</th><th>Kapsam</th><th>Durum</th><th>İşlem</th></tr></thead><tbody id="kpi-rows">${kpiRows(rows)}</tbody></table></div></section>`;
  document.querySelector('#add-kpi').onclick = () => kpiEditor();
  bindSpreadsheetActions('kpis', () => ({ids:[...document.querySelectorAll('[data-kpi]')].map(x=>Number(x.dataset.kpi))}), kpisPage);
  state.filterController = bindSelectionFilters(filterKpis);
  filterKpis();
}

function filterKpis() {
  const filters = state.kpiFilters;
  filters.kpi = selectedFilterValues('kpi-title');
  filters.category = selectedFilterValues('kpi-category');
  filters.profession = selectedFilterValues('kpi-profession');
  const filtered = state.kpis.filter(x => (!filters.kpi.length || filters.kpi.includes(String(x.id)))
    && (!filters.category.length || filters.category.includes(x.category || ''))
    && (!filters.profession.length || x.isCommon || filters.profession.some(id=>String(x.professionId)===id || x.professions?.some(p=>String(p.id)===id))));
  document.querySelector('#kpi-rows').innerHTML = kpiRows(filtered);
  bindKpiActions();
}

function kpiRows(rows) {
  rows = rows.filter(row => row.isActive);
  if (!rows.length) return `<tr><td colspan="4">${emptyState('KPI bulunamadı','Filtreleri değiştirebilirsiniz.')}</td></tr>`;
  return rows.map(row => `<tr><td><strong>${esc(row.title)}</strong><br><small>${esc(row.category || 'Kategorisiz')}</small>${row.companyGoalName?`<br><small>Şirket hedefi: ${esc(row.companyGoalName)}</small>`:''}</td><td>${row.isCommon ? '<span class="badge info">Herkes</span>' : esc(row.professions?.length ? row.professions.map(x => x.name).join(', ') : row.professionName || '—')}</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td>${rowActionMenu(`kpi-${row.id}`, row.title, `<button type="button" class="row-action" role="menuitem" data-kpi="${row.id}">Düzenle</button><button type="button" class="row-action" role="menuitem" data-deactivate-kpi="${row.id}" ${row.isActive ? '' : 'disabled'}>Pasife al</button><button type="button" class="row-action danger" role="menuitem" data-delete-kpi="${row.id}">Sil</button>`)}</td></tr>`).join('');
}

function bindKpiActions() {
  bindPageActionMenus();
  document.querySelectorAll('[data-kpi]').forEach(button => button.onclick = () => kpiEditor(state.kpis.find(x => x.id === Number(button.dataset.kpi))));
  document.querySelectorAll('[data-deactivate-kpi]').forEach(button => button.onclick = () => changeKpi(button, false));
  document.querySelectorAll('[data-delete-kpi]').forEach(button => button.onclick = () => changeKpi(button, true));
}

async function changeKpi(button, permanent) {
  const id = Number(permanent ? button.dataset.deleteKpi : button.dataset.deactivateKpi);
  const row = state.kpis.find(x => x.id === id);
  const message = permanent ? `“${row.title}” kalıcı olarak silinsin mi? Bu işlem geri alınamaz. Değerlendirmelerde kullanılan KPI’lar silinemez.` : `“${row.title}” pasife alınsın mı? Yeni değerlendirmelerde gösterilmeyecek; kayıtlı değerlendirmeler korunacak.`;
  const table = document.querySelector('#kpi-rows');
  if (!await messageBox(message, {title:permanent?'KPI’ı kalıcı olarak sil':'KPI’ı pasife al', confirm:true, confirmText:permanent?'Kalıcı olarak sil':'Pasife al', type:permanent?'danger':'warning', signal:state.actionMenuController?.signal}) || !button.isConnected || document.querySelector('#kpi-rows') !== table) return;
  const actions = button.closest('.row-actions');
  actions.querySelectorAll('button').forEach(x => x.disabled = true);
  try {
    await api(permanent ? `/api/kpis/${id}` : `/api/kpis/${id}/deactivate`, {method:permanent?'DELETE':'POST'});
    if (document.querySelector('#kpi-rows') === table) {
      state.kpis = permanent ? state.kpis.filter(x => x.id !== id) : state.kpis.map(x => x.id === id ? {...x, isActive:false} : x);
      filterKpis();
    }
    toast(permanent ? 'KPI kalıcı olarak silindi.' : 'KPI pasife alındı.');
  } catch (error) { toast(error.message, 'error'); }
  finally { actions.querySelectorAll('button').forEach(x => x.disabled = x.hasAttribute('data-deactivate-kpi') && !row.isActive); }
}

function kpiEditor(row = {}, {onSaved = kpisPage, professions = state.professions, goals = state.kpiGoals || []} = {}) {
  const goalOptions = goals.filter(goal => goal.isActive || goal.id === row.companyGoalId)
    .map(goal => ({id:goal.id, name:`${goal.name} · ${goal.periodName || 'Tüm dönemler'}${goal.isActive?'':' (Pasif)'}`}));
  openDialog(row.id ? 'KPI ölçütünü düzenle' : 'Yeni KPI ölçütü', `<div class="form-grid">
    <div class="field full"><label for="kpi-title-field">KPI başlığı</label><textarea class="textarea kpi-editor-text" data-growing-note id="kpi-title-field" name="title" rows="3" required maxlength="1000">${esc(row.title)}</textarea></div>
    <div class="field full"><label for="kpi-description-field">Açıklama</label><textarea class="textarea kpi-editor-text" data-growing-note id="kpi-description-field" name="description" rows="3" maxlength="2000">${esc(row.description)}</textarea></div>
    <div class="field full"><label for="kpi-goal-field">Bağlı olduğu şirket hedefi (isteğe bağlı)</label><select class="filter-select" id="kpi-goal-field" name="companyGoalId">${optionList(goalOptions, row.companyGoalId, 'Şirket hedefi seçilmedi')}</select><small>Bu KPI sonucunun katkı sağlayacağı şirket hedefini seçin; boş bırakabilirsiniz.</small></div>
    <label class="check-row"><input type="checkbox" name="isCommon" id="kpi-common" ${row.isCommon ? 'checked' : ''}> Herkes (tüm ekipler)</label>
    <div class="field"><label>Ekipler (meslek grupları)</label><select class="filter-select multi-select" name="professionIds" id="kpi-profession-field" multiple>${professions.map(x => `<option value="${x.id}" ${(row.professions?.length ? row.professions.some(p => p.id === x.id) : row.professionId === x.id) ? 'selected' : ''}>${esc(x.name)}${x.isActive ? '' : ' (Pasif)'}</option>`).join('')}</select><small>Bir veya birden fazla meslek grubu seçin; tüm ekipler için Herkes seçeneğini işaretleyin.</small></div>
    <div class="field"><label>Sıra</label><input class="input" type="number" min="1" name="displayOrder" value="${row.displayOrder || 1}" required></div>
    <label class="check-row"><input type="checkbox" name="isActive" ${row.isActive !== false ? 'checked':''}> Aktif KPI</label></div>`, async form => {
    const isCommon = form.get('isCommon') === 'on';
    const saved = await api('/api/kpis', { method:'POST', body:JSON.stringify({ id:row.id || null, title:form.get('title'), description:form.get('description'), professionId:null, professionIds:isCommon ? [] : form.getAll('professionIds').map(Number), isCommon, displayOrder:Number(form.get('displayOrder')), isActive:form.get('isActive') === 'on', category:row.category ?? null, companyGoalId:form.get('companyGoalId') ? Number(form.get('companyGoalId')) : null, measurementType:row.measurementType ?? 0, unit:row.unit ?? null, targetValue:row.targetValue ?? null, targetDirection:row.targetDirection ?? 0, weightPercent:row.weightPercent ?? 0, dataSource:row.dataSource ?? null, reviewFrequency:row.reviewFrequency ?? 2, validFrom:row.validFrom || null, validTo:row.validTo || null, isSpecific:row.isSpecific || false, isMeasurable:row.isMeasurable || false, isAchievable:row.isAchievable || false, isRelevant:row.isRelevant || false, isTimeBound:row.isTimeBound || false }) });
    toast(row.id ? 'KPI güncellendi.' : 'KPI eklendi.'); await onSaved(saved);
  });
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
  app.innerHTML = `${pageHead('Değerlendirme Yap','Çalışanı ve dönemi seçin, her KPI ölçütünü puanlayın.',hasPermission(6)?`<button class="button primary" id="add-evaluation-kpi">${icon('plus')}KPI ekle</button>`:'')}
    <section class="card setup-card"><div class="setup-grid">${selectionFilter('eval-profession','Meslek',state.professions,'','Tüm meslekler',{multiple:false})}<div class="field"><label>Çalışan</label><select class="filter-select" id="eval-employee">${optionList(state.employees,'','Çalışan seçiniz')}</select></div><div class="field"><label>Dönem</label><select class="filter-select" id="eval-period">${optionList(state.periods,'','Dönem seçiniz')}</select></div></div></section>
    <div class="toolbar"><p id="evaluation-state" role="status">Başlamak veya kayıtlı taslağa devam etmek için çalışan ve dönem seçin.</p><button class="button secondary" id="retry-evaluation" hidden>Tekrar dene</button></div>
    <div class="evaluation-layout"><section class="card" id="criteria-card">${emptyState('Değerlendirmeye başlayın','KPI maddelerini görmek için çalışan ve dönem seçin.')}</section><aside class="card summary-card"><div class="card-head"><h2>Değerlendirme özeti</h2></div><div class="card-body" id="evaluation-summary">${evaluationSummary()}</div></aside></div>`;
  state.filterController = bindSelectionFilters(filterEvaluationEmployees);
  document.querySelector('#eval-employee').onchange = loadEvaluationCriteria;
  document.querySelector('#eval-period').onchange = loadEvaluationCriteria;
  document.querySelector('#retry-evaluation').onclick = loadEvaluationCriteria;
  const addKpi = document.querySelector('#add-evaluation-kpi');
  if (addKpi) addKpi.onclick = () => evaluationKpiEditor(addKpi);
  enhancePageSelects();
}

async function evaluationKpiEditor(button) {
  button.disabled = true;
  const card = document.querySelector('#criteria-card');
  try {
    const [professions, goals] = await Promise.all([api('/api/professions'), api('/api/goals')]);
    if (document.querySelector('#criteria-card') !== card) return;
    const employee = state.employees.find(x => x.id === Number(document.querySelector('#eval-employee').value));
    const professionId = employee?.professionId || Number(selectedFilterValues('eval-profession')[0]) || null;
    kpiEditor({professionId}, {professions, goals, onSaved: async saved => {
      if (document.querySelector('#criteria-card') !== card) return;
      if (state.evaluationEdit) {
        toast('Yeni KPI, yeni değerlendirmelerde kullanılacak. Kayıtlı değerlendirmenin soruları korunur.');
        return;
      }
      if (!state.evaluationReady) return;
      const employeeId = document.querySelector('#eval-employee').value;
      const periodId = document.querySelector('#eval-period').value;
      const selected = state.employees.find(x => String(x.id) === employeeId);
      try {
        const items = await api(`/api/kpis/profession/${selected.professionId}?periodId=${periodId}`);
        if (document.querySelector('#criteria-card') !== card || state.evaluationEdit || !state.evaluationReady
          || document.querySelector('#eval-employee').value !== employeeId || document.querySelector('#eval-period').value !== periodId) return;
        const item = items.find(x => x.id === saved.id);
        if (!item || state.evaluationItems.some(x => x.id === item.id)) return;
        state.evaluationItems.push({...item, score:null, note:'', actualValue:null});
        state.evaluationItems.sort((a,b) => Number(a.isCommon)-Number(b.isCommon) || a.displayOrder-b.displayOrder);
        renderCriteria();
      } catch (error) {
        toast(`KPI kaydedildi ancak değerlendirme soruları yenilenemedi: ${error.message}`, 'error');
      }
    }});
  } catch (error) { toast(error.message, 'error'); }
  finally { button.disabled = false; }
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
      notice.textContent = 'Bu çalışan ve dönem için size ait onaylanmış değerlendirme var. Ayrıntıları Değerlendirmeler ekranından görebilir veya Düzenle ile taslağa alabilirsiniz. Diğer amirin değerlendirmesi ayrıdır.';
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
  bindGrowingNotes(card);
  updateEvaluationSummary();
}

function criterionTemplate(item) {
  const metric = item.measurementType !== 0;
  return `<article class="criterion"><div class="criterion-head"><div><h3>${esc(item.title)}</h3><p>${esc(item.description || item.category || (item.isCommon ? 'Ortak değerlendirme ölçütü' : item.professionName))}</p></div><div class="score-buttons" aria-label="Puan">${[1,2,3,4,5].map(score => `<button type="button" class="score-button ${item.score===score?'selected':''}" aria-pressed="${item.score===score}" aria-label="${score} puan${item.score===score?' · Seçimi kaldır':''}" data-score="${score}" data-id="${item.id}" ${metric?'disabled':''}>${score}</button>`).join('')}</div></div><div class="criterion-extra">${metric ? `<input class="input" type="number" step="0.01" data-actual="${item.id}" value="${item.actualValue ?? ''}" placeholder="Gerçekleşen (${esc(item.unit || 'değer')})">` : '<span></span>'}<textarea class="textarea evaluation-note" data-growing-note data-note="${item.id}" rows="3" maxlength="2000" aria-label="${esc(item.title)} · Kısa not" placeholder="Kısa not (isteğe bağlı)">${esc(item.note)}</textarea></div></article>`;
}

function metricScore(actual,target,direction) {
  if (actual == null || target == null) return null;
  if (direction === 1) { if (target===0) return actual<=0?5:1; const r=actual/target; return r<=1?5:r<=1.1?4:r<=1.25?3:r<=1.5?2:1; }
  if (target===0) return actual>=0?5:1; const r=actual/target; return r>=1?5:r>=.9?4:r>=.75?3:r>=.6?2:1;
}

function evaluationSummary() {
  const scored = state.evaluationItems.filter(x => x.score != null); const total = scored.reduce((sum,x)=>sum+x.score,0); const weight = state.evaluationItems.reduce((sum,x)=>sum+(x.weightPercent||0),0); const average = state.evaluationItems.length ? total/state.evaluationItems.length : 0; const weighted = weight ? scored.reduce((sum,x)=>sum+x.score*(x.weightPercent||0),0)/weight : average; const complete = state.evaluationItems.length && scored.length === state.evaluationItems.length;
  return `<div class="score-ring" style="--value:${Math.min(100,weighted*20)}%"><div><strong>%${formatNumber(weighted*20)}</strong><small>normalize başarı</small></div></div><div class="summary-line"><span>Puanlanan</span><strong>${scored.length} / ${state.evaluationItems.length}</strong></div><div class="summary-line"><span>Toplam puan</span><strong>${formatNumber(weighted*20)} / 100</strong></div><div class="summary-line"><span>Soru başına azami puan</span><strong>${formatNumber(state.evaluationItems.length ? 100/state.evaluationItems.length : 0)}</strong></div><div class="field" style="margin-top:14px"><label>Genel not</label><textarea class="textarea" id="general-note" placeholder="Değerlendirme notu" maxlength="2000"></textarea></div><div class="actions" style="margin-top:16px"><button class="button secondary" id="save-draft" ${state.evaluationReady&&state.evaluationItems.length&&!state.savingEvaluation?'':'disabled'}>Geçici Kaydet</button><button class="button primary" id="save-evaluation" ${state.evaluationReady&&complete&&hasPermission(2)&&!state.savingEvaluation?'':'disabled'}>${icon('check')}Kalıcı Kaydet</button>${state.evaluationEdit?'<button class="button secondary" id="reload-evaluation">Taslağı yeniden yükle</button>':''}<small>Geçici kayıt daha sonra tamamlanabilir. Kalıcı kayıt onaylanır. Yalnızca değerlendiren kişi düzenleyebilir; değişiklikler yeniden onay gerektirir.</small></div>`;
}

function updateEvaluationSummary() {
  const summary = document.querySelector('#evaluation-summary'); if (!summary) return; const oldNote = document.querySelector('#general-note')?.value || ''; summary.innerHTML = evaluationSummary(); document.querySelector('#general-note').value = oldNote; document.querySelector('#save-evaluation').onclick = () => saveEvaluation(4); document.querySelector('#save-draft').onclick = () => saveEvaluation(0);
  const reload = document.querySelector('#reload-evaluation');
  if (reload) reload.onclick = async () => {
    if (await messageBox('Kaydedilmemiş değişiklikler bırakılıp güncel taslak yüklensin mi?', {title:'Taslağı yeniden yükle', confirm:true, confirmText:'Yeniden yükle', type:'warning', signal:state.evaluationController?.signal}) && reload.isConnected) loadEvaluationCriteria();
  };
}

async function saveEvaluation(status) {
  if (state.savingEvaluation || !state.evaluationReady) return;
  const card = document.querySelector('#criteria-card');
  const employeeId = Number(document.querySelector('#eval-employee').value);
  const periodId = Number(document.querySelector('#eval-period').value);
  if (!employeeId || !periodId) { toast('Çalışan ve dönem seçmelisiniz.','error'); return; }
  if (status === 4 && (!hasPermission(2) || state.evaluationItems.some(x => x.score == null))) { toast('Kalıcı kayıt için tüm soruları puanlamalı ve onaylama yetkisine sahip olmalısınız.','error'); return; }
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

async function editEvaluation(id, {onSaved = historyPage, onClose = null} = {}) {
  let opened = false;
  try {
    const edit = await api(`/api/evaluations/${id}/edit`);
    const wasApproved = edit.status === 4;
    let saveStatus = 0;
    openDialog('Değerlendirmeyi düzenle', `<div class="form-grid">${wasApproved?'<p class="field full" role="status">Bu değerlendirme onaylıdır. Değişiklikler taslağa kaydedilir ve yeniden onay gerekir.</p>':''}${edit.items.map(x=>`<div class="field full"><span class="filter-label" id="edit-kpi-${x.detailId}">${esc(x.kpiTitle)}</span>${x.measurementType===0?`<div class="score-buttons" role="group" aria-labelledby="edit-kpi-${x.detailId}"><input type="hidden" name="score-${x.detailId}" value="${x.score||0}">${[1,2,3,4,5].map(score=>`<button type="button" class="score-button ${score===x.score?'selected':''}" data-edit-score="${score}" aria-pressed="${score===x.score}" aria-label="${score} puan${score===x.score?' · Seçimi kaldır':''}">${score}</button>`).join('')}</div>`:`<input class="input" type="number" step="0.01" name="actual-${x.detailId}" aria-labelledby="edit-kpi-${x.detailId}" value="${x.actualValue??''}" placeholder="Gerçekleşen (${esc(x.unit||'değer')})"><small>Hedef: ${esc(x.targetValue)} · Puan otomatik hesaplanır.</small>`}<textarea class="textarea evaluation-note" data-growing-note name="note-${x.detailId}" rows="3" aria-label="${esc(x.kpiTitle)} · Kısa not" placeholder="Kısa not" maxlength="2000">${esc(x.note)}</textarea></div>`).join('')}<div class="field full"><label>Genel not</label><textarea class="textarea" name="generalNote" maxlength="2000">${esc(edit.generalNote)}</textarea></div></div>`, async form => {
      const editorForm = dialogContent.querySelector('#dialog-form');
      const items = edit.items.map(x => {
        const raw = form.get(`actual-${x.detailId}`);
        const actualValue = raw === '' || raw == null ? null : Number(raw);
        return {detailId:x.detailId, score:x.measurementType===0?Number(form.get(`score-${x.detailId}`)):metricScore(actualValue,x.targetValue,x.targetDirection)??0, actualValue, note:form.get(`note-${x.detailId}`)};
      });
      if (saveStatus === 4 && items.some(x=>x.score===0)) throw new Error('Kalıcı kayıt için tüm soruları puanlamalısınız.');
      await api(`/api/evaluations/${id}`, {method:'PUT',body:JSON.stringify({id,generalNote:form.get('generalNote'),items,status:saveStatus,expectedUpdatedAt:edit.updatedAt,managerFeedback:edit.managerFeedback,employeeFeedback:edit.employeeFeedback,developmentAction:edit.developmentAction,followUpDate:edit.followUpDate})});
      if (!editorForm.isConnected || !dialog.open) return;
      if (wasApproved) await messageBox('Değerlendirme tekrar taslağa döndü. Düzenleme sonrasında yeniden onaylanması gerekir.', {title:'Yeniden onay gerekiyor'});
      else toast(saveStatus===0?'Geçici kayıt güncellendi.':'Değerlendirme kalıcı olarak kaydedildi ve onaylandı.');
      if (!editorForm.isConnected || !dialog.open) return;
      await onSaved();
    }, true);
    opened = true;
    dialogContent.querySelectorAll('[data-edit-score]').forEach(button => button.onclick = () => {
      const group = button.closest('.score-buttons');
      const input = group.querySelector('input[type="hidden"]');
      const score = Number(button.dataset.editScore);
      input.value = Number(input.value) === score ? '0' : String(score);
      group.querySelectorAll('[data-edit-score]').forEach(choice => {
        const selected = Number(choice.dataset.editScore) === Number(input.value);
        choice.classList.toggle('selected', selected);
        choice.setAttribute('aria-pressed', String(selected));
        choice.setAttribute('aria-label', `${choice.dataset.editScore} puan${selected?' · Seçimi kaldır':''}`);
      });
    });
    if (onClose) dialog.addEventListener('close', () => queueMicrotask(onClose), {once:true});
    const footer = dialogContent.querySelector('.dialog-foot');
    footer.innerHTML = `<button type="button" class="button secondary" id="cancel-evaluation-edit">Vazgeç</button><button type="submit" class="button ${wasApproved?'primary':'secondary'}" id="update-draft">${wasApproved?'Taslağa Döndür ve Kaydet':'Geçici Kaydet'}</button>${!wasApproved&&hasPermission(2)?'<button type="submit" class="button primary" id="finalize-evaluation">Kalıcı Kaydet</button>':''}`;
    footer.querySelector('#cancel-evaluation-edit').onclick = () => dialog.close();
    footer.querySelector('#update-draft').onclick = () => { saveStatus = 0; };
    if (!wasApproved && hasPermission(2)) footer.querySelector('#finalize-evaluation').onclick = () => { saveStatus = 4; };
  } catch(error) { toast(error.message,'error'); if (!opened && onClose) onClose(); }
}

async function historyPage() {
  loading();
  try { const [rows, employees, professions, periods] = await Promise.all([api('/api/evaluations'),api('/api/employees'),api('/api/professions'),api('/api/periods')]); state.history=rows;state.employees=employees;state.professions=professions;state.periods=periods;renderHistory(rows); }
  catch(error){showError(error,historyPage);}
}

function renderHistory(rows) {
  state.filterController?.abort();
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

function historyRows(rows){return rows.length?rows.map(row=>`<tr><td><div class="person"><span class="person-avatar">${initials(row.employeeName)}</span><div>${esc(row.employeeName)}<br><small>${esc(row.professionName)}</small></div></div></td><td>${esc(row.evaluatorName||'Sistem')}</td><td>${esc(row.periodName)}</td><td><span class="score ${row.scorePercentage>=80?'good':row.scorePercentage>=60?'mid':'low'}">${formatNumber(row.scorePercentage)} / 100<br><small>%${formatNumber(row.scorePercentage)}</small></span></td><td>${statusBadge(row.status)}</td><td>${rowActionMenu(`history-${row.id}`, `${row.employeeName} · ${row.periodName}`, `<button type="button" class="row-action" role="menuitem" data-history="${row.id}">Ayrıntıyı görüntüle</button>${row.canEdit?`<button type="button" class="row-action" role="menuitem" data-edit-evaluation="${row.id}">Düzenle</button>`:''}${row.canApprove?`<button type="button" class="row-action" role="menuitem" data-approve="${row.id}">Kalıcı Kaydet</button>`:''}${row.canDelete?`<button type="button" class="row-action danger" role="menuitem" data-delete-evaluation="${row.id}">Sil</button>`:''}`)}</td></tr>`).join(''):`<tr><td colspan="6">${emptyState('Kayıt bulunamadı','Filtreleri değiştirebilirsiniz.')}</td></tr>`;}
function bindHistoryActions(){bindPageActionMenus();document.querySelectorAll('[data-delete-evaluation]').forEach(button=>button.onclick=()=>deleteEvaluation(Number(button.dataset.deleteEvaluation)));document.querySelectorAll('[data-edit-evaluation]').forEach(button=>button.onclick=()=>editEvaluation(Number(button.dataset.editEvaluation)));document.querySelectorAll('[data-history]').forEach(button=>button.onclick=()=>showEvaluationDetail(Number(button.dataset.history)));document.querySelectorAll('[data-approve]').forEach(button=>button.onclick=()=>approveEvaluation(Number(button.dataset.approve)));}
async function deleteEvaluation(id, rows = state.history, table = document.querySelector('#history-rows'), onDeleted = null) {
  const row = rows.find(x => x.id === id);
  if (!row?.canDelete) return;
  const action = table?.querySelector(`[data-delete-evaluation="${id}"]`);
  if (!action || !await messageBox(`“${row.employeeName} · ${row.periodName}” değerlendirmesi ve KPI puanları kalıcı olarak silinsin mi? Bu işlem geri alınamaz.`, {title:'Değerlendirmeyi kalıcı olarak sil', confirm:true, confirmText:'Kalıcı olarak sil', type:'danger'}) || !action.isConnected) return;
  const buttons = [...action.closest('.row-actions').querySelectorAll('button')];
  buttons.forEach(button => button.disabled = true);
  try {
    await api(`/api/evaluations/${id}`, {method:'DELETE'});
    rows.splice(rows.indexOf(row), 1);
    if (onDeleted) await onDeleted();
    else if (table.isConnected) renderHistory(rows);
    toast('Değerlendirme kalıcı olarak silindi.');
  } catch (error) { toast(error.message, 'error'); }
  finally { buttons.forEach(button => button.disabled = false); }
}

async function approveEvaluation(id){try{await api(`/api/evaluations/${id}/approve`,{method:'POST'});toast('Değerlendirme onaylandı ve resmî sonuçlara eklendi.');await historyPage();}catch(error){toast(error.message,'error');}}

async function showEvaluationDetail(id, rows = state.history, onBack = null, signal = undefined) {
  try {
    const row = rows.find(x=>x.id===id);
    const details = await api(`/api/evaluations/${id}/details`, {signal});
    if (signal?.aborted) return;
    dialog.classList.add('wide');
    dialog.setAttribute('aria-labelledby', 'evaluation-dialog-title');
    dialogContent.innerHTML = `<div class="dialog-head"><h2 id="evaluation-dialog-title" tabindex="-1">${esc(row.employeeName)} · Değerlendirme ayrıntısı</h2><button type="button" class="icon-button" data-close aria-label="Kapat">${icon('close')}</button></div><div class="dialog-body"><p class="manager-dialog-context">Değerlendiren: ${esc(row.evaluatorName||'Sistem')} · ${formatDate(row.evaluationDate)} · ${statusBadge(row.status)}</p><div class="detail-grid"><div class="detail-box"><small>Çalışan</small><strong>${esc(row.employeeName)}</strong></div><div class="detail-box"><small>Dönem</small><strong>${esc(row.periodName)}</strong></div><div class="detail-box"><small>Puan</small><strong>${formatNumber(row.scorePercentage)} / 100</strong></div></div><div class="table-wrap"><table class="table"><thead><tr><th>KPI</th><th>Tür</th><th>Puan</th><th>Not</th></tr></thead><tbody>${details.map(x=>`<tr><td><strong>${esc(x.kpiTitle)}</strong></td><td>${x.isCommon?'Ortak':'Mesleki'}</td><td><span class="score">${x.score || '—'}</span></td><td class="evaluation-note-text">${esc(x.note||'—')}</td></tr>`).join('')}</tbody></table></div>${row.generalNote?`<div class="field" style="margin-top:18px"><label>Genel not</label><div class="detail-box">${esc(row.generalNote)}</div></div>`:''}</div><div class="dialog-foot">${onBack?'<button type="button" class="button secondary" id="back-to-manager">Amirin değerlendirmelerine dön</button>':''}<button type="button" class="button secondary" data-close>Kapat</button></div>`;
    dialogContent.querySelectorAll('[data-close]').forEach(button=>button.onclick=()=>dialog.close());
    if (onBack) document.querySelector('#back-to-manager').onclick = onBack;
    if (!dialog.open) {
      dialog.addEventListener('close', () => dialog.removeAttribute('aria-labelledby'), {once:true});
      dialog.showModal();
    }
    document.querySelector('#evaluation-dialog-title').focus();
  } catch(error) { if (!signal?.aborted) toast(error.message,'error'); }
}

const retiredPermissions=new Set([9,13,14,15,17,18]);
const permissionNames=['Gösterge paneli','Değerlendirme oluşturma','Değerlendirme onaylama','Değerlendirme geçmişi','Çalışan yönetimi','Meslek yönetimi','KPI yönetimi','Dönem yönetimi','Hedef yönetimi','Sayfa tasarımcısı','Yetkilendirme yönetimi','İzin takibi · Görüntüle','İzin listesi · Excel yükleme','Denetim kayıtları','Çalışan değerlendirme yanıtı','Değerlendirme görevleri','Onaylı amir sonuçlarını karşılaştırma','Yetkinlik/kıdem yönetimi','Bireysel gelişim yönetimi'];

function managerScores(scores) {
  return `<span class="manager-summary">${scores.map(score => `<span>${score.managerLevel ? `${score.managerLevel}. amir · %${formatNumber(score.weightPercent)} · ` : ''}${esc(score.evaluatorName)}: <strong>%${formatNumber(score.percentage)}</strong>${score.managerLevel ? '' : ' <small>Güncel ağırlıklandırmaya dahil değil</small>'}</span>`).join(' - ')}</span>`;
}

function combinedScore(row) {
  return row.combinedPercentage == null ? `<small>${esc(row.combinedStatus || 'Atanmış amirlerin onaylı değerlendirmeleri bekleniyor')}</small>`
    : `<strong class="score ${row.combinedPercentage >= 80 ? 'good' : row.combinedPercentage >= 60 ? 'mid' : 'low'}">%${formatNumber(row.combinedPercentage)}</strong>`;
}

async function calibrationPage() {
  loading();
  try {
    const rows=await api('/api/workflow/calibration');
    app.innerHTML=`${pageHead('Onaylı Amir Sonuçları','Ortalama Puan, ekip amirlerinin güncel ağırlıklarıyla hesaplanır. Tüm atanmış amirlerin onaylı değerlendirmeleri gerekir.')}${spreadsheetActions(false,false,true,true)}<section class="card card-body"><div class="toolbar">${selectionFilter('calibration-period','Dönem',[...new Map(rows.map(x=>[x.periodId,{id:x.periodId,name:x.periodName}])).values()],[],'Tüm dönemler')}</div><div id="calibration-list"></div></section>`;
    bindSpreadsheetActions('calibration', () => ({periodIds:selectedFilterValues('calibration-period').map(Number)}), calibrationPage);
    const render=()=>{
      const periods=selectedFilterValues('calibration-period'); const filtered=rows.filter(x=>!periods.length||periods.includes(String(x.periodId)));
      document.querySelector('#calibration-list').innerHTML=filtered.length?`<div class="table-wrap"><table class="table"><thead><tr><th>Çalışan / ekip</th><th>Dönem</th><th>Amir sonuçları</th><th>Ortalama Puan</th></tr></thead><tbody>${filtered.map(x=>`<tr><td>${esc(x.employeeName)}<br><small>${esc(x.professionName)}</small></td><td>${esc(x.periodName)}</td><td>${managerScores(x.scores)}</td><td>${combinedScore(x)}</td></tr>`).join('')}</tbody></table></div>`:emptyState('Onaylı sonuç yok','Erişebildiğiniz onaylı değerlendirmeler burada karşılaştırılır.');
    };state.filterController=bindSelectionFilters(render);render();
  } catch(error) { showError(error,calibrationPage); }
}

async function leavesPage() {
  loading();
  try {
    state.leaveFilters ||= {employeeId:'', professionId:'', from:'', to:'', activeOnly:'true', page:1};
    const query = new URLSearchParams(Object.entries(state.leaveFilters).filter(([,value])=>value!==''));
    const [data, employees, professions] = await Promise.all([api(`/api/leaves/balances?${query}`), api('/api/employees'), api('/api/professions')]);
    state.leaveEmployees = employees;
    state.leaveProfessions = professions;
    state.leaveRows = data.items;
    renderLeaves(data);
  } catch(error) { showError(error, leavesPage); }
}

function renderLeaves(data) {
  const filter=state.leaveFilters;
  const pageCount=Math.max(1,Math.ceil(data.totalCount/data.pageSize));
  app.innerHTML=`${pageHead('İzin Takibi','Excel dosyasından yüklenen kalan izin günlerini ve izin hak ediş tarihlerini görüntüleyin.')}${spreadsheetActions(hasPermission(12),true)}
    <section class="card"><div class="card-body"><div class="toolbar" id="leave-filters">
      ${selectionFilter('leave-employee','Çalışan',state.leaveEmployees,filter.employeeId,'Tüm çalışanlar',{multiple:false})}
      ${selectionFilter('leave-profession','Ekip (meslek grubu)',state.leaveProfessions,filter.professionId,'Tüm ekipler',{multiple:false})}
      <div class="field"><label for="leave-from">Hak ediş tarihi (başlangıç)</label><input class="input" type="date" id="leave-from" name="from" value="${esc(filter.from)}"></div>
      <div class="field"><label for="leave-to">Hak ediş tarihi (bitiş)</label><input class="input" type="date" id="leave-to" name="to" value="${esc(filter.to)}"></div>
      </div></div>
      <div class="table-wrap"><table class="table"><thead><tr><th>Adı Soyadı</th><th>Ekip</th><th>Kalan İzin Gün Sayısı</th><th>İzin Hak Ediş Tarihi</th></tr></thead><tbody>${data.items.length?data.items.map(row=>`<tr><td>${esc(row.employeeName)}</td><td>${esc(row.professionName)}</td><td>${formatNumber(row.remainingDays)}</td><td>${formatDate(row.entitlementDate)}</td></tr>`).join(''):`<tr><td colspan="4">${emptyState('İzin bakiyesi bulunamadı','Bakiyeleri görmek için Excel yükleyin veya filtreleri değiştirin.')}</td></tr>`}</tbody></table></div>
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
    const [users,roles,permissions,teamManagers]=await Promise.all([api('/api/authorization/users'),api('/api/authorization/roles'),api('/api/authorization/permissions'),api('/api/authorization/team-managers')]);
    state.authUsers=users;state.roles=roles;state.rolePermissions=permissions;state.teamManagers=teamManagers;renderAuthorization();
  }catch(error){showError(error,authorizationPage);}
}
function renderAuthorization(){
  state.filterController?.abort();
  state.teamManagerFilters ||= {team:[], manager:[]};
  const filters = state.teamManagerFilters;
  const managerOptions = new Map();
  state.teamManagers.forEach(row => {
    if (row.firstManagerUserId) managerOptions.set(row.firstManagerUserId, {id:row.firstManagerUserId,name:row.firstManagerName});
    if (row.secondManagerUserId) managerOptions.set(row.secondManagerUserId, {id:row.secondManagerUserId,name:row.secondManagerName});
    (row.managers || []).forEach(manager => managerOptions.set(manager.managerUserId, {id:manager.managerUserId,name:manager.managerName}));
  });
  const managers = [...managerOptions.values()].sort((a,b)=>a.name.localeCompare(b.name,'tr'));
  app.innerHTML=`${pageHead('Yetkilendirme','Kullanıcı hesaplarını, rolleri ve rol yetkilerini yönetin.',`<button class="button secondary" id="add-role">${icon('plus')}Rol ekle</button><button class="button primary" id="add-user">${icon('plus')}Kullanıcı ekle</button>`)}
  <section class="card" style="margin-bottom:20px"><div class="card-head"><h2>Kullanıcı hesapları</h2></div><div class="table-wrap"><table class="table"><thead><tr><th>Kullanıcı</th><th>Rol</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${state.authUsers.filter(x=>x.isActive).map(x=>`<tr><td><strong>${esc(x.displayName)}</strong><br><small>${esc(x.userName)}</small></td><td>${esc(x.roleName)}</td><td><span class="badge ${x.isActive?'active':'inactive'}">${x.isActive?'Aktif':'Pasif'}</span></td><td>${rowActionMenu(`user-${x.id}`, x.displayName, `<button type="button" class="row-action" role="menuitem" data-user="${x.id}">Düzenle</button>${managementActionButtons('authorization/users', x)}`)}</td></tr>`).join('')}</tbody></table></div></section>
  <section class="card"><div class="card-head"><h2>Rol ve yetki yönetimi</h2></div><div class="card-body">${state.roles.filter(role=>role.isActive).map(role=>`<div class="actions" style="margin-bottom:8px"><span style="flex:1">${esc(role.name)} · ${state.rolePermissions.filter(x=>x.roleId===role.id&&x.isAllowed&&!retiredPermissions.has(x.permission)).length} yetki</span>${rowActionMenu(`role-${role.id}`, role.name, `<button type="button" class="row-action" role="menuitem" data-role-edit="${role.id}">Düzenle</button><button type="button" class="row-action" role="menuitem" data-role-permissions="${role.id}">Yetkileri yönet</button>${managementActionButtons('authorization/roles', role)}`)}</div>`).join('')}</div></section>`;
  bindPageActionMenus();
  bindManagementActions('authorization/users', state.authUsers, renderAuthorization);
  bindManagementActions('authorization/roles', state.roles, renderAuthorization);
  document.querySelector('#add-user').onclick=()=>userEditor();document.querySelector('#add-role').onclick=()=>roleEditor();document.querySelectorAll('[data-user]').forEach(b=>b.onclick=()=>userEditor(state.authUsers.find(x=>x.id===Number(b.dataset.user))));
  document.querySelectorAll('[data-role-permissions]').forEach(b=>b.onclick=()=>permissionEditor(state.roles.find(x=>x.id===Number(b.dataset.rolePermissions))));document.querySelectorAll('[data-role-edit]').forEach(b=>b.onclick=()=>roleEditor(state.roles.find(x=>x.id===Number(b.dataset.roleEdit))));
  app.insertAdjacentHTML('beforeend', `<section class="card" style="margin-top:20px"><div class="card-head"><h2>Ekip ve amir bağlılıkları</h2></div><div class="card-body"><p>İstediğiniz sayıda amir ekleyip toplam %100 olacak şekilde ağırlıklarını belirleyin. Güncel oranlar mevcut birleşik sonuçlara da uygulanır. Ekip amirleri tüm çalışanlara uygulanır.</p><div class="toolbar history-filters" id="manager-assignment-filters">${selectionFilter('manager-assignment-team','Ekipler',state.teamManagers.map(row=>({id:row.professionId,name:row.professionName})),filters.team,'Tüm ekipler')}${selectionFilter('manager-assignment-manager','Amirler',managers,filters.manager,'Tüm amirler')}</div></div><div class="table-wrap"><table class="table"><thead><tr><th>Ekip</th><th>Çalışan</th><th>Amirler ve ağırlıkları</th><th>İşlem</th></tr></thead><tbody id="manager-assignment-rows"></tbody></table></div></section>`);
  state.filterController = bindSelectionFilters(renderTeamManagers, document.querySelector('#manager-assignment-filters'));
  renderTeamManagers();
}

function renderTeamManagers() {
  const filters = state.teamManagerFilters;
  filters.team = selectedFilterValues('manager-assignment-team');
  filters.manager = selectedFilterValues('manager-assignment-manager');
  const rows = state.teamManagers.filter(row => (!filters.team.length || filters.team.includes(String(row.professionId)))
    && (!filters.manager.length || [row.firstManagerUserId,row.secondManagerUserId,...(row.managers || []).map(x=>x.managerUserId)]
      .some(id=>filters.manager.includes(String(id)))));
  document.querySelector('#manager-assignment-rows').innerHTML = rows.length ? rows.map(row => `<tr><td><strong>${esc(row.professionName)}</strong>${row.hasMixedAssignments ? '<br><small>Mevcut çalışan bağlılıkları farklı; ekip amirlerini seçin.</small>' : ''}</td><td>${row.employeeCount}</td><td class="manager-summary">${row.managers?.length ? row.managers.map(manager=>`<span>${manager.position}. amir · ${esc(manager.managerName)} · %${formatNumber(manager.weightPercent)}</span>`).join(' - ') : 'Atanmadı'}</td><td>${rowActionMenu(`team-manager-${row.professionId}`, row.professionName, `<button type="button" class="row-action" role="menuitem" data-team-manager="${row.professionId}">Amirleri düzenle</button>`)}</td></tr>`).join('') : `<tr><td colspan="4">${emptyState('Ekip bulunamadı', 'Arama ölçütlerini değiştirebilirsiniz.')}</td></tr>`;
  bindPageActionMenus();
  document.querySelectorAll('[data-team-manager]').forEach(button => button.onclick = () => teamManagersEditor(state.teamManagers.find(row => row.professionId === Number(button.dataset.teamManager))));
}

function teamManagersEditor(row) {
  const assigned = new Set((row.managers || []).map(x=>x.managerUserId));
  const users = state.authUsers.filter(user => assigned.has(user.id) || (user.isActive
    && state.roles.some(role => role.id === user.roleId && role.isActive)
    && ['first-manager', 'second-manager', 'administrator'].includes(user.roleCode)))
    .map(user=>({id:user.id, name:user.displayName}));
  let nextKey = 0;
  const managerRow = (manager={}) => {
    const key = nextKey++;
    return `<div class="team-manager-row" data-manager-row="${key}"><div class="field"><label for="manager-user-${key}">Amir</label><select id="manager-user-${key}" class="filter-select" name="manager-${key}" required>${optionList(users, manager.managerUserId, 'Amir seçiniz')}</select></div><div class="field"><label for="manager-weight-${key}">Etki oranı (%)</label><input id="manager-weight-${key}" class="input" name="weight-${key}" type="number" min="0.01" max="100" step="0.01" value="${manager.weightPercent ?? ''}" required></div><button class="icon-button team-manager-remove" type="button" data-remove-manager aria-label="Amiri kaldır" title="Amiri kaldır">${icon('close')}</button></div>`;
  };
  openDialog(`${row.professionName} · Ekip amirleri`, `<p>Bu atama, pasif kayıtlar dahil ekipteki ${row.employeeCount} çalışana ve sonradan eklenen çalışanlara uygulanır. Oran değişiklikleri mevcut birleşik sonuçları da günceller.</p><div id="team-manager-fields">${(row.managers || []).map(managerRow).join('')}</div><p id="team-manager-total" role="status"></p><p>Bir oran değiştiğinde kalan yüzde diğer amirler arasında orantılı paylaşılır.</p><button class="button secondary" id="add-team-manager" type="button">${icon('plus')}Amir ekle</button>`, async form => {
    const editorForm = dialogContent.querySelector('#dialog-form');
    const managers = [...dialogContent.querySelectorAll('[data-manager-row]')].map(element=>({managerUserId:Number(form.get(`manager-${element.dataset.managerRow}`)), weightPercent:Number(form.get(`weight-${element.dataset.managerRow}`))}));
    if (new Set(managers.map(x=>x.managerUserId)).size !== managers.length) throw new Error('Aynı amir birden fazla kez seçilemez.');
    if (managers.some(x=>!x.managerUserId || x.weightPercent<=0 || x.weightPercent>100)
      || (managers.length && managers.reduce((sum,x)=>sum+Math.round(x.weightPercent*100),0)!==10000)) throw new Error('Amirleri seçin ve pozitif ağırlıkların toplamını %100 yapın.');
    if (!managers.length && !await messageBox('Ekibin tüm amir atamaları kaldırılsın mı?', {title:'Amir atamalarını kaldır', confirm:true, confirmText:'Atamaları kaldır', type:'warning'})) return false;
    if (!editorForm.isConnected || !dialog.open) return false;
    await api(`/api/authorization/team-managers/${row.professionId}`, {method:'PUT', body:JSON.stringify({professionId:row.professionId, managers})});
    state.teamManagers = await api('/api/authorization/team-managers');
    renderAuthorization();
    toast('Ekip amirleri ve ağırlıkları kaydedildi.');
  });
  const updateTotal = () => {
    const total = [...dialogContent.querySelectorAll('[name^="weight-"]')].reduce((sum,input)=>sum+Math.round(Number(input.value)*100),0)/100;
    dialogContent.querySelector('#team-manager-total').textContent = `Toplam etki: %${formatNumber(total)} · Hedef: %100`;
  };
  const weightInputs = () => [...dialogContent.querySelectorAll('input[name^="weight-"]')];
  const distributeWeights = (inputs, total) => {
    if (!inputs.length || total < inputs.length) return;
    const entries = inputs.map((input, index) => ({input, index, weight:Math.max(0, Math.min(10000, Math.round(Number(input.value) * 100) || 0)), units:0}));
    let pending = [...entries];
    let remaining = total;
    while (pending.length) {
      const weightSum = pending.reduce((sum, entry) => sum + entry.weight, 0);
      const minimums = weightSum > 0 ? pending.filter(entry => remaining * entry.weight < weightSum) : [];
      if (minimums.length) {
        minimums.forEach(entry => { entry.units = 1; remaining--; });
        pending = pending.filter(entry => !minimums.includes(entry));
        continue;
      }
      const divisor = weightSum || pending.length;
      pending.forEach(entry => {
        const numerator = remaining * (weightSum ? entry.weight : 1);
        entry.units = Math.floor(numerator / divisor);
        entry.remainder = numerator % divisor;
      });
      const extra = remaining - pending.reduce((sum, entry) => sum + entry.units, 0);
      pending.sort((a, b) => b.remainder - a.remainder || a.index - b.index)
        .slice(0, extra).forEach(entry => entry.units++);
      break;
    }
    entries.forEach(entry => { entry.input.value = String(entry.units / 100); });
  };
  const balanceWeights = changed => {
    const inputs = weightInputs();
    if (changed.value !== '' && changed.validity.valid) {
      if (inputs.length === 1) changed.value = '100';
      else distributeWeights(inputs.filter(input => input !== changed), 10000 - Math.round(Number(changed.value) * 100));
    }
    updateTotal();
  };
  const bindRows = () => {
    dialogContent.querySelectorAll('[data-remove-manager]').forEach(button=>button.onclick=()=>{
      button.closest('[data-manager-row]').remove();
      const inputs = weightInputs();
      distributeWeights(inputs, 10000);
      bindRows(); updateTotal();
    });
    const inputs = weightInputs();
    inputs.forEach(input => {
      input.max = String((10000 - inputs.length + 1) / 100);
      input.oninput = () => balanceWeights(input);
    });
  };
  dialogContent.querySelector('#add-team-manager').onclick = () => {
    dialogContent.querySelector('#team-manager-fields').insertAdjacentHTML('beforeend', managerRow());
    bindRows(); updateTotal();
    const selections = enhanceSelects(dialogContent.querySelector('#team-manager-fields').lastElementChild);
    dialog.addEventListener('close',()=>selections.abort(),{once:true});
  };
  bindRows(); updateTotal();
}
function userEditor(row={}){openDialog(row.id?'Kullanıcıyı düzenle':'Yeni kullanıcı',`<div class="form-grid"><div class="field"><label>Kullanıcı adı</label><input class="input" name="userName" value="${esc(row.userName)}" required></div><div class="field"><label>Görünen ad</label><input class="input" name="displayName" value="${esc(row.displayName)}" required></div><div class="field"><label>Rol</label><select class="filter-select" name="roleId">${state.roles.filter(x=>x.isActive||x.id===row.roleId).map(x=>`<option value="${x.id}" ${row.roleId===x.id?'selected':''}>${esc(x.name)}</option>`).join('')}</select></div>${row.id?'':'<div class="field full"><label>İlk giriş parolası</label><input class="input" type="password" name="password" autocomplete="new-password" required><small>En az 10 karakter; büyük/küçük harf, rakam ve özel karakter.</small></div>'}<label class="check-row"><input type="checkbox" name="isActive" ${row.isActive!==false?'checked':''}> Aktif kullanıcı</label></div>`,async form=>{await api('/api/authorization/users',{method:'POST',body:JSON.stringify({id:row.id||null,userName:form.get('userName'),displayName:form.get('displayName'),roleId:Number(form.get('roleId')),employeeId:row.employeeId??null,isActive:form.get('isActive')==='on',password:row.id?null:form.get('password')})});toast('Kullanıcı kaydedildi.');await authorizationPage();});}
function roleEditor(row={}){openDialog(row.id?'Rolü düzenle':'Yeni rol',`<div class="form-grid"><div class="field"><label>Rol adı</label><input class="input" name="name" value="${esc(row.name)}" required></div><div class="field"><label>Rol kodu</label><input class="input" name="code" value="${esc(row.code)}" ${row.isSystem?'readonly':''} required><small>Küçük harf ve tire kullanın.</small></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive!==false?'checked':''}> Aktif rol</label></div>`,async form=>{await api('/api/authorization/roles',{method:'POST',body:JSON.stringify({id:row.id||null,name:form.get('name'),code:form.get('code'),isActive:form.get('isActive')==='on'})});toast('Rol kaydedildi.');await authorizationPage();});}
function permissionEditor(role) {
  const administrator = role.code==='administrator';
  const active = new Set(state.rolePermissions.filter(x=>x.roleId===role.id&&x.isAllowed).map(x=>x.permission));
  openDialog(`${role.name} yetkileri`, `${administrator?'<p>Sistem yöneticisi tüm yetkilere sahiptir. Bu rolün yetkileri kaldırılamaz.</p>':''}<div class="permission-grid">${permissionNames.map((name,id)=>retiredPermissions.has(id)?'':`<label class="permission-item"><input type="checkbox" name="permissions" value="${id}" ${administrator||active.has(id)?'checked':''} ${administrator?'disabled':''}>${name}</label>`).join('')}</div>`, async form=>{
    await api(`/api/authorization/permissions/${role.id}`,{method:'PUT',body:JSON.stringify(form.getAll('permissions').map(Number))});
    toast('Rol yetkileri güncellendi.'); await authorizationPage();
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

Object.assign(titles, {calibration:'Onaylı Amir Sonuçları'});
const routes = { dashboard:dashboardPage, evaluation:evaluationPage, history:historyPage, employees:employeesPage, professions:professionsPage, kpis:kpisPage, periods:periodsPage, goals:goalsPage, authorization:authorizationPage, leaves:leavesPage, calibration:calibrationPage };
async function navigate(){if(!state.user)return;const route=(location.hash||'#dashboard').slice(1);const page=routes[route]||dashboardPage;document.querySelector('#page-name').textContent=titles[route]||titles.dashboard;document.querySelectorAll('[data-route]').forEach(x=>x.classList.toggle('active',x.dataset.route===route));closeMenu();await page();if(app.querySelector('select:not([data-selection-enhanced])'))enhancePageSelects();app.focus({preventScroll:true});}
function closeMenu(){document.querySelector('#sidebar').classList.remove('open');document.querySelector('#mobile-overlay').classList.remove('open');}
document.querySelector('#menu-button').onclick=()=>{document.querySelector('#sidebar').classList.add('open');document.querySelector('#mobile-overlay').classList.add('open');};
document.querySelector('#mobile-overlay').onclick=closeMenu;
window.addEventListener('hashchange',navigate);
document.querySelector('#today').textContent=new Intl.DateTimeFormat('tr-TR',{weekday:'long',day:'numeric',month:'long'}).format(new Date());
bootstrap();
