const app = document.querySelector('#app');
const dialog = document.querySelector('#dialog');
const dialogContent = document.querySelector('#dialog-content');
const connectionState = document.querySelector('#connection-state');
const statusDot = document.querySelector('.status-dot');
const titles = {
  dashboard: 'Gösterge Paneli', evaluation: 'Değerlendirme Yap', history: 'Değerlendirmeler',
  employees: 'Çalışanlar', professions: 'Meslekler', kpis: 'KPI Yönetimi', periods: 'Dönemler', goals: 'Şirket Hedefleri',
  authorization: 'Yetkilendirme', 'page-designer': 'Sayfa Tasarımcısı'
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
  const response = await fetch(url, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }
  });
  if (!response.ok) {
    let problem;
    try { problem = await response.json(); } catch { problem = {}; }
    const error = new Error(problem.detail || problem.title || 'İşlem tamamlanamadı.');
    error.status = response.status;
    throw error;
  }
  connectionState.textContent = 'Bağlı';
  statusDot.className = 'status-dot online';
  return response.status === 204 ? null : response.json();
}

const hasPermission = permission => state.user?.permissions?.includes(permission);
const selectedValues = select => [...select.selectedOptions].map(x => x.value).filter(Boolean);

function loading() {
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
    const submit = event.submitter;
    submit.disabled = true;
    try { await onSubmit(new FormData(event.currentTarget)); dialog.close(); }
    catch (error) { toast(error.message, 'error'); submit.disabled = false; }
  };
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
    const manager = sessionStorage.getItem('dashboard-manager') || '';
    const query = new URLSearchParams(); if (selected) query.set('periodId', selected); if (manager) query.set('evaluatorUserId', manager);
    const [data, managers] = await Promise.all([api(`/api/dashboard?${query}`), api(`/api/dashboard/managers${selected ? `?periodId=${selected}` : ''}`)]);
    const results = data.results || [];
    app.innerHTML = `${pageHead('Gösterge Paneli', 'Ekip performansını ve hedef ilerlemesini tek görünümde izleyin.', `<select class="filter-select" id="dashboard-period" aria-label="Dönem filtresi">${optionList(state.periods, selected, 'Tüm dönemler')}</select>`) }
      <div class="manager-grid">${managers.map(x => `<button class="manager-card" data-manager="${x.userId}"><small>${esc(x.roleName)}</small><strong>${esc(x.managerName)}</strong><div class="manager-metrics"><span>Çalışan<b>${x.employeeCount}</b></span><span>Onaylı<b>${x.approvedEvaluationCount}</b></span><span>Bekleyen<b>${x.pendingEvaluationCount}</b></span><span>Ortalama<b>%${formatNumber(x.averagePercentage)}</b></span></div></button>`).join('')}</div>
      <div class="stats-grid">
        ${statCard('users','teal','Toplam çalışan',data.totalEmployees)}
        ${statCard('check','blue','Değerlendirilen',data.evaluatedEmployees)}
        ${statCard('history','amber','Bekleyen',data.notEvaluatedEmployees)}
        ${statCard('score','navy','Normalize başarı',`%${formatNumber(data.overallPercentage)}`)}
      </div>
      <div class="grid-2">
        <section class="card"><div class="card-head"><h2>Performans sıralaması</h2><span class="badge info">${results.length} kayıt</span></div>
          <div class="table-wrap">${results.length ? `<table class="table"><thead><tr><th>Çalışan</th><th>Değerlendiren</th><th>Dönem</th><th>Puan</th><th>Başarı</th></tr></thead><tbody>${results.slice(0, 12).map(row => `<tr><td><div class="person"><span class="person-avatar">${initials(row.employeeName)}</span>${esc(row.employeeName)}</div></td><td>${esc(row.evaluatorName || 'Sistem')}</td><td>${esc(row.periodName)}</td><td><span class="score">${row.totalScore} / ${row.maximumScore}</span></td><td><span class="score ${row.scorePercentage >= 80 ? 'good' : row.scorePercentage >= 60 ? 'mid' : 'low'}">%${formatNumber(row.scorePercentage)}</span></td></tr>`).join('')}</tbody></table>` : emptyState('Henüz onaylı değerlendirme yok','Taslak ve onay bekleyen kayıtlar resmî sonuçlara katılmaz.')}</div>
        </section>
        <section class="card"><div class="card-head"><h2>Şirket hedefleri</h2></div><div class="card-body">${(data.goalProgress || []).length ? data.goalProgress.map(goal => `<div class="goal-row"><div class="goal-top"><span>${esc(goal.goalName)}</span><strong>${formatNumber(goal.averageScore)} / 5</strong></div><div class="progress"><span style="width:${Math.min(100, goal.averageScore * 20)}%"></span></div></div>`).join('') : emptyState('Hedef verisi yok','KPI maddelerini şirket hedefleriyle ilişkilendirin.')}</div></section>
      </div>`;
    document.querySelector('#dashboard-period').onchange = event => { sessionStorage.setItem('dashboard-period', event.target.value); dashboardPage(); };
    document.querySelectorAll('[data-manager]').forEach(button => button.onclick = () => { const active = sessionStorage.getItem('dashboard-manager'); sessionStorage.setItem('dashboard-manager', active === button.dataset.manager ? '' : button.dataset.manager); dashboardPage(); });
  } catch (error) { showError(error, dashboardPage); }
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
  app.innerHTML = `${pageHead('Çalışanlar','Ekip üyelerini, mesleklerini ve aktiflik durumlarını yönetin.',`<button class="button primary" id="add-employee">${icon('plus')}Çalışan ekle</button>`)}
    <section class="card"><div class="card-body"><div class="toolbar"><div class="search">${icon('search')}<input class="input" id="employee-search" placeholder="Ad veya meslek ara"></div><select class="filter-select" id="employee-profession">${optionList(state.professions, '', 'Tüm meslekler')}</select></div></div>
      <div class="table-wrap"><table class="table"><thead><tr><th>Çalışan</th><th>Meslek</th><th>Durum</th><th>İşlem</th></tr></thead><tbody id="employee-rows">${employeeRows(rows)}</tbody></table></div></section>`;
  document.querySelector('#add-employee').onclick = () => employeeEditor();
  const apply = () => {
    const term = document.querySelector('#employee-search').value.toLocaleLowerCase('tr-TR');
    const profession = document.querySelector('#employee-profession').value;
    const filtered = state.employees.filter(x => (!term || `${x.fullName} ${x.professionName}`.toLocaleLowerCase('tr-TR').includes(term)) && (!profession || String(x.professionId) === profession));
    document.querySelector('#employee-rows').innerHTML = employeeRows(filtered);
    bindEmployeeActions();
  };
  document.querySelector('#employee-search').oninput = apply;
  document.querySelector('#employee-profession').onchange = apply;
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
  app.innerHTML = `${pageHead('KPI Yönetimi','Ortak ve mesleki ölçütleri, hedefleri ve ağırlıkları yönetin.',`<button class="button primary" id="add-kpi">${icon('plus')}KPI ekle</button>`)}<section class="card"><div class="card-body"><div class="toolbar"><div class="search">${icon('search')}<input class="input" id="kpi-search" placeholder="KPI veya kategori ara"></div><select class="filter-select" id="kpi-profession">${optionList(state.professions, '', 'Tüm meslekler')}</select></div></div><div class="table-wrap"><table class="table"><thead><tr><th>KPI</th><th>Kapsam</th><th>Ölçüm</th><th>Ağırlık</th><th>Durum</th><th>İşlem</th></tr></thead><tbody id="kpi-rows">${kpiRows(rows)}</tbody></table></div></section>`;
  document.querySelector('#add-kpi').onclick = () => kpiEditor();
  const apply = () => { const term = document.querySelector('#kpi-search').value.toLocaleLowerCase('tr-TR'); const profession = document.querySelector('#kpi-profession').value; const filtered = state.kpis.filter(x => (!term || `${x.title} ${x.category || ''}`.toLocaleLowerCase('tr-TR').includes(term)) && (!profession || String(x.professionId) === profession)); document.querySelector('#kpi-rows').innerHTML = kpiRows(filtered); bindKpiActions(); };
  document.querySelector('#kpi-search').oninput = apply; document.querySelector('#kpi-profession').onchange = apply; bindKpiActions();
}

function kpiRows(rows) {
  return rows.map(row => `<tr><td><strong>${esc(row.title)}</strong><br><small>${esc(row.category || 'Kategorisiz')}</small></td><td>${row.isCommon ? '<span class="badge info">Ortak</span>' : esc(row.professionName || '—')}</td><td>${esc(row.measurementName)}</td><td>${formatNumber(row.weightPercent)}%</td><td><span class="badge ${row.isActive ? 'active' : 'inactive'}">${row.isActive ? 'Aktif' : 'Pasif'}</span></td><td><button class="row-action" data-kpi="${row.id}" aria-label="Düzenle">${icon('edit')}</button></td></tr>`).join('');
}

function bindKpiActions() { document.querySelectorAll('[data-kpi]').forEach(button => button.onclick = () => kpiEditor(state.kpis.find(x => x.id === Number(button.dataset.kpi)))); }

function kpiEditor(row = {}) {
  const measurement = row.measurementType ?? 0;
  openDialog(row.id ? 'KPI ölçütünü düzenle' : 'Yeni KPI ölçütü', `<div class="form-grid"><div class="field full"><label>KPI başlığı</label><input class="input" name="title" value="${esc(row.title)}" required maxlength="1000"></div><div class="field full"><label>Açıklama</label><textarea class="textarea" name="description">${esc(row.description)}</textarea></div><div class="field"><label>Kategori</label><input class="input" name="category" value="${esc(row.category)}"></div><div class="field"><label>Şirket hedefi</label><select class="filter-select" name="companyGoalId">${optionList(state.goals, row.companyGoalId, 'Hedef seçilmedi')}</select></div><label class="check-row"><input type="checkbox" name="isCommon" id="kpi-common" ${row.isCommon ? 'checked' : ''}> Ortak KPI</label><div class="field"><label>Meslek</label><select class="filter-select" name="professionId" id="kpi-profession-field">${optionList(state.professions, row.professionId)}</select></div><div class="field"><label>Ölçüm türü</label><select class="filter-select" name="measurementType">${['Manuel 1–5','Yüzde','Adet','Süre','Sayısal'].map((x,i)=>`<option value="${i}" ${measurement===i?'selected':''}>${x}</option>`).join('')}</select></div><div class="field"><label>Birim</label><input class="input" name="unit" value="${esc(row.unit)}"></div><div class="field"><label>Hedef değer</label><input class="input" type="number" step="0.01" name="targetValue" value="${row.targetValue ?? ''}"></div><div class="field"><label>Başarı yönü</label><select class="filter-select" name="targetDirection"><option value="0" ${row.targetDirection !== 1 ? 'selected':''}>En az</option><option value="1" ${row.targetDirection === 1 ? 'selected':''}>En fazla</option></select></div><div class="field"><label>Ağırlık (%)</label><input class="input" type="number" step="0.01" min="0" max="100" name="weightPercent" value="${row.weightPercent ?? 0}"></div><div class="field"><label>Sıra</label><input class="input" type="number" min="1" name="displayOrder" value="${row.displayOrder || 1}" required></div><div class="field full"><label>Veri kaynağı</label><input class="input" name="dataSource" value="${esc(row.dataSource)}"></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive !== false ? 'checked':''}> Aktif KPI</label></div>`, async form => {
    const isCommon = form.get('isCommon') === 'on';
    await api('/api/kpis', { method:'POST', body:JSON.stringify({ id:row.id || null, title:form.get('title'), description:form.get('description'), professionId:isCommon ? null : Number(form.get('professionId')), isCommon, displayOrder:Number(form.get('displayOrder')), isActive:form.get('isActive') === 'on', category:form.get('category'), companyGoalId:form.get('companyGoalId') ? Number(form.get('companyGoalId')) : null, measurementType:Number(form.get('measurementType')), unit:form.get('unit'), targetValue:form.get('targetValue') ? Number(form.get('targetValue')) : null, targetDirection:Number(form.get('targetDirection')), weightPercent:Number(form.get('weightPercent')), dataSource:form.get('dataSource'), reviewFrequency:row.reviewFrequency ?? 2, validFrom:row.validFrom || null, validTo:row.validTo || null, isSpecific:row.isSpecific || false, isMeasurable:row.isMeasurable || false, isAchievable:row.isAchievable || false, isRelevant:row.isRelevant || false, isTimeBound:row.isTimeBound || false }) });
    toast(row.id ? 'KPI güncellendi.' : 'KPI eklendi.'); await kpisPage();
  }, true);
  const common = document.querySelector('#kpi-common'); const profession = document.querySelector('#kpi-profession-field');
  const sync = () => { profession.disabled = common.checked; profession.required = !common.checked; if (common.checked) profession.value = ''; }; common.onchange = sync; sync();
}

async function evaluationPage() {
  loading();
  try {
    const [employees, periods, professions] = await Promise.all([api('/api/employees?activeOnly=true'), api('/api/periods?activeOnly=true'), api('/api/employees/profession-lookups')]);
    state.employees = employees; state.periods = periods; state.professions = professions; state.evaluationItems = [];
    renderEvaluation();
  } catch (error) { showError(error, evaluationPage); }
}

function renderEvaluation() {
  app.innerHTML = `${pageHead('Değerlendirme Yap','Çalışanı ve dönemi seçin, her KPI ölçütünü puanlayın.')}
    <section class="card setup-card"><div class="setup-grid"><div class="field"><label>Meslek</label><select class="filter-select" id="eval-profession">${optionList(state.professions,'','Tüm meslekler')}</select></div><div class="field"><label>Çalışan</label><select class="filter-select" id="eval-employee">${optionList(state.employees,'','Çalışan seçiniz')}</select></div><div class="field"><label>Dönem</label><select class="filter-select" id="eval-period">${optionList(state.periods,'','Dönem seçiniz')}</select></div></div></section>
    <div class="evaluation-layout"><section class="card" id="criteria-card">${emptyState('Değerlendirmeye başlayın','KPI maddelerini görmek için bir çalışan seçin.')}</section><aside class="card summary-card"><div class="card-head"><h2>Değerlendirme özeti</h2></div><div class="card-body" id="evaluation-summary">${evaluationSummary()}</div></aside></div>`;
  document.querySelector('#eval-profession').onchange = filterEvaluationEmployees;
  document.querySelector('#eval-employee').onchange = loadEvaluationCriteria;
  document.querySelector('#eval-period').onchange = loadEvaluationCriteria;
}

function filterEvaluationEmployees() {
  const profession = document.querySelector('#eval-profession').value;
  const rows = state.employees.filter(x => !profession || String(x.professionId) === profession);
  document.querySelector('#eval-employee').innerHTML = optionList(rows,'','Çalışan seçiniz');
  state.evaluationItems=[]; renderCriteria();
}

async function loadEvaluationCriteria() {
  const employeeId = Number(document.querySelector('#eval-employee').value);
  const periodId = document.querySelector('#eval-period').value;
  if (!employeeId) { state.evaluationItems=[]; renderCriteria(); return; }
  const employee = state.employees.find(x => x.id === employeeId);
  try {
    const items = await api(`/api/kpis/profession/${employee.professionId}${periodId ? `?periodId=${periodId}` : ''}`);
    state.evaluationItems = items.map(x => ({ ...x, score:null, note:'', actualValue:null }));
    renderCriteria();
  } catch (error) { toast(error.message,'error'); }
}

function renderCriteria() {
  const card = document.querySelector('#criteria-card');
  if (!card) return;
  if (!state.evaluationItems.length) { card.innerHTML = emptyState('KPI maddesi bulunamadı','Çalışan seçin veya aktif KPI tanımlarını kontrol edin.'); updateEvaluationSummary(); return; }
  const groups = [{common:false,label:'Mesleki KPI’lar'},{common:true,label:'Ortak KPI’lar'}];
  card.innerHTML = groups.map(group => { const items = state.evaluationItems.filter(x => x.isCommon === group.common); return items.length ? `<div class="section-label">${group.label} · ${items.length} madde</div>${items.map(criterionTemplate).join('')}` : ''; }).join('');
  document.querySelectorAll('[data-score]').forEach(button => button.onclick = () => { const item = state.evaluationItems.find(x => x.id === Number(button.dataset.id)); item.score = Number(button.dataset.score); renderCriteria(); });
  document.querySelectorAll('[data-actual]').forEach(input => input.onchange = () => { const item = state.evaluationItems.find(x => x.id === Number(input.dataset.actual)); item.actualValue = input.value === '' ? null : Number(input.value); item.score = metricScore(item.actualValue,item.targetValue,item.targetDirection); renderCriteria(); });
  document.querySelectorAll('[data-note]').forEach(input => input.oninput = () => { state.evaluationItems.find(x => x.id === Number(input.dataset.note)).note = input.value; });
  updateEvaluationSummary();
}

function criterionTemplate(item) {
  const metric = item.measurementType !== 0;
  return `<article class="criterion"><div class="criterion-head"><div><h3>${esc(item.title)}</h3><p>${esc(item.description || item.category || (item.isCommon ? 'Ortak değerlendirme ölçütü' : item.professionName))}</p></div><div class="score-buttons" aria-label="Puan">${[1,2,3,4,5].map(score => `<button class="score-button ${item.score===score?'selected':''}" data-score="${score}" data-id="${item.id}" ${metric?'disabled':''}>${score}</button>`).join('')}</div></div><div class="criterion-extra">${metric ? `<input class="input" type="number" step="0.01" data-actual="${item.id}" value="${item.actualValue ?? ''}" placeholder="Gerçekleşen (${esc(item.unit || 'değer')})">` : '<span></span>'}<input class="input" data-note="${item.id}" value="${esc(item.note)}" placeholder="Kısa not (isteğe bağlı)"></div></article>`;
}

function metricScore(actual,target,direction) {
  if (actual == null || target == null) return null;
  if (direction === 1) { if (target===0) return actual<=0?5:1; const r=actual/target; return r<=1?5:r<=1.1?4:r<=1.25?3:r<=1.5?2:1; }
  if (target===0) return actual>=0?5:1; const r=actual/target; return r>=1?5:r>=.9?4:r>=.75?3:r>=.6?2:1;
}

function evaluationSummary() {
  const scored = state.evaluationItems.filter(x => x.score != null); const total = scored.reduce((sum,x)=>sum+x.score,0); const weight = scored.reduce((sum,x)=>sum+(x.weightPercent||0),0); const average = scored.length ? total/scored.length : 0; const weighted = weight ? scored.reduce((sum,x)=>sum+x.score*(x.weightPercent||0),0)/weight : average; const complete = state.evaluationItems.length && scored.length === state.evaluationItems.length;
  return `<div class="score-ring" style="--value:${Math.min(100,weighted*20)}%"><div><strong>%${formatNumber(weighted*20)}</strong><small>normalize başarı</small></div></div><div class="summary-line"><span>Puanlanan</span><strong>${scored.length} / ${state.evaluationItems.length}</strong></div><div class="summary-line"><span>Toplam puan</span><strong>${total} / ${state.evaluationItems.length*5}</strong></div><div class="summary-line"><span>Ağırlıklı ortalama</span><strong>${formatNumber(weighted)} / 5</strong></div><div class="field" style="margin-top:14px"><label>Genel not</label><textarea class="textarea" id="general-note" placeholder="Değerlendirme notu"></textarea></div><div class="actions" style="margin-top:16px"><button class="button secondary" id="save-draft" ${complete?'':'disabled'}>Taslak kaydet</button><button class="button primary" id="save-evaluation" ${complete?'':'disabled'}>${icon('check')}Onaya gönder</button></div>`;
}

function updateEvaluationSummary() {
  const summary = document.querySelector('#evaluation-summary'); if (!summary) return; const oldNote = document.querySelector('#general-note')?.value || ''; summary.innerHTML = evaluationSummary(); document.querySelector('#general-note').value = oldNote; document.querySelector('#save-evaluation').onclick = () => saveEvaluation(1); document.querySelector('#save-draft').onclick = () => saveEvaluation(0);
}

async function saveEvaluation(status) {
  const employeeId = Number(document.querySelector('#eval-employee').value); const periodId = Number(document.querySelector('#eval-period').value);
  if (!employeeId || !periodId) { toast('Çalışan ve dönem seçmelisiniz.','error'); return; }
  const button = document.querySelector('#save-evaluation'); button.disabled = true;
  try { await api('/api/evaluations',{method:'POST',body:JSON.stringify({employeeId,evaluationPeriodId:periodId,generalNote:document.querySelector('#general-note').value,status,items:state.evaluationItems.map(x=>({kpiCriterionId:x.id,score:x.score,note:x.note,actualValue:x.actualValue}))})}); toast(status === 0 ? 'Taslak kaydedildi.' : 'Değerlendirme onaya gönderildi.'); await evaluationPage(); }
  catch(error){toast(error.message,'error');button.disabled=false;}
}

async function historyPage() {
  loading();
  try { const [rows, employees, professions, periods] = await Promise.all([api('/api/evaluations'),api('/api/employees'),api('/api/professions'),api('/api/periods')]); state.history=rows;state.employees=employees;state.professions=professions;state.periods=periods;renderHistory(rows); }
  catch(error){showError(error,historyPage);}
}

function renderHistory(rows) {
  app.innerHTML = `${pageHead('Değerlendirmeler','Taslak, onay bekleyen ve onaylı kayıtları çoklu filtrelerle inceleyin.')}<section class="card"><div class="card-body"><div class="toolbar"><div class="field"><label>Çalışanlar</label><select class="filter-select multi-select" id="history-employee" multiple>${optionList(state.employees,'','Tüm çalışanlar')}</select></div><div class="field"><label>Meslekler</label><select class="filter-select multi-select" id="history-profession" multiple>${optionList(state.professions,'','Tüm meslekler')}</select></div><div class="field"><label>Dönemler</label><select class="filter-select multi-select" id="history-period" multiple>${optionList(state.periods,'','Tüm dönemler')}</select></div></div></div><div class="table-wrap"><table class="table"><thead><tr><th>Çalışan</th><th>Değerlendiren</th><th>Dönem</th><th>Puan</th><th>Durum</th><th>İşlem</th></tr></thead><tbody id="history-rows">${historyRows(rows)}</tbody></table></div></section>`;
  const apply = () => { const employees=selectedValues(document.querySelector('#history-employee')), professions=selectedValues(document.querySelector('#history-profession')), periods=selectedValues(document.querySelector('#history-period')); const employeeNames=employees.map(id=>state.employees.find(e=>String(e.id)===id)?.fullName),professionNames=professions.map(id=>state.professions.find(e=>String(e.id)===id)?.name),periodNames=periods.map(id=>state.periods.find(e=>String(e.id)===id)?.name); const filtered=state.history.filter(x=>(!employees.length||employeeNames.includes(x.employeeName))&&(!professions.length||professionNames.includes(x.professionName))&&(!periods.length||periodNames.includes(x.periodName))); document.querySelector('#history-rows').innerHTML=historyRows(filtered);bindHistoryActions();};
  ['#history-employee','#history-profession','#history-period'].forEach(id=>document.querySelector(id).onchange=apply);bindHistoryActions();
}

function historyRows(rows){return rows.length?rows.map(row=>`<tr><td><div class="person"><span class="person-avatar">${initials(row.employeeName)}</span><div>${esc(row.employeeName)}<br><small>${esc(row.professionName)}</small></div></div></td><td>${esc(row.evaluatorName||'Sistem')}</td><td>${esc(row.periodName)}</td><td><span class="score ${row.scorePercentage>=80?'good':row.scorePercentage>=60?'mid':'low'}">${row.totalScore} / ${row.maximumScore}<br><small>%${formatNumber(row.scorePercentage)}</small></span></td><td>${statusBadge(row.status)}</td><td><div class="row-actions"><button class="row-action" data-history="${row.id}" aria-label="Detayı görüntüle">${icon('eye')}</button>${hasPermission(2)&&row.status!==4?`<button class="row-action" data-approve="${row.id}" aria-label="Onayla">${icon('check')}</button>`:''}</div></td></tr>`).join(''):`<tr><td colspan="6">${emptyState('Kayıt bulunamadı','Filtreleri değiştirebilirsiniz.')}</td></tr>`;}
function bindHistoryActions(){document.querySelectorAll('[data-history]').forEach(button=>button.onclick=()=>showEvaluationDetail(Number(button.dataset.history)));document.querySelectorAll('[data-approve]').forEach(button=>button.onclick=()=>approveEvaluation(Number(button.dataset.approve)));}
async function approveEvaluation(id){try{await api(`/api/evaluations/${id}/approve`,{method:'POST'});toast('Değerlendirme onaylandı ve resmî sonuçlara eklendi.');await historyPage();}catch(error){toast(error.message,'error');}}

async function showEvaluationDetail(id){
  try { const row=state.history.find(x=>x.id===id);const details=await api(`/api/evaluations/${id}/details`); dialog.classList.add('wide');dialogContent.innerHTML=`<div class="dialog-head"><h2>Değerlendirme ayrıntısı</h2><button class="icon-button" data-close>${icon('close')}</button></div><div class="dialog-body"><div class="detail-grid"><div class="detail-box"><small>Çalışan</small><strong>${esc(row.employeeName)}</strong></div><div class="detail-box"><small>Dönem</small><strong>${esc(row.periodName)}</strong></div><div class="detail-box"><small>Ağırlıklı puan</small><strong>${formatNumber(row.weightedScore)} / 5</strong></div></div><div class="table-wrap"><table class="table"><thead><tr><th>KPI</th><th>Tür</th><th>Puan</th><th>Not</th></tr></thead><tbody>${details.map(x=>`<tr><td><strong>${esc(x.kpiTitle)}</strong></td><td>${x.isCommon?'Ortak':'Mesleki'}</td><td><span class="score">${x.score}</span></td><td>${esc(x.note||'—')}</td></tr>`).join('')}</tbody></table></div>${row.generalNote?`<div class="field" style="margin-top:18px"><label>Genel not</label><div class="detail-box">${esc(row.generalNote)}</div></div>`:''}</div><div class="dialog-foot"><button class="button secondary" data-close>Kapat</button></div>`;dialogContent.querySelectorAll('[data-close]').forEach(x=>x.onclick=()=>dialog.close());dialog.showModal(); }
  catch(error){toast(error.message,'error');}
}

const permissionNames=['Gösterge paneli','Değerlendirme oluşturma','Değerlendirme onaylama','Değerlendirme geçmişi','Çalışan yönetimi','Meslek yönetimi','KPI yönetimi','Dönem yönetimi','Hedef yönetimi','Sayfa tasarımcısı','Yetkilendirme yönetimi'];

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
  <div class="grid-2"><section class="card"><div class="card-head"><h2>Rol ve izin yönetimi</h2></div><div class="card-body">${state.roles.map(role=>`<div class="actions" style="margin-bottom:8px"><button class="button secondary" data-role-permissions="${role.id}" style="flex:1;justify-content:space-between">${esc(role.name)}<span>${state.rolePermissions.filter(x=>x.roleId===role.id&&x.isAllowed).length} izin</span></button><button class="row-action" data-role-edit="${role.id}" aria-label="Rolü düzenle">${icon('edit')}</button></div>`).join('')}</div></section>
  <section class="card"><div class="card-head"><h2>Amir atamaları</h2></div><div class="card-body" style="max-height:440px;overflow:auto">${state.assignments.map(x=>`<div class="field" style="padding:12px 0;border-bottom:1px solid var(--line)"><label>${esc(x.employeeName)}</label><div class="actions"><select class="filter-select" data-first="${x.employeeId}">${optionList(first,x.firstManagerUserId,'1. Amir')}</select><select class="filter-select" data-second="${x.employeeId}">${optionList(second,x.secondManagerUserId,'2. Amir')}</select><button class="button secondary" data-save-managers="${x.employeeId}">Kaydet</button></div></div>`).join('')}</div></section></div>`;
  document.querySelector('#add-user').onclick=()=>userEditor();document.querySelector('#add-role').onclick=()=>roleEditor();document.querySelectorAll('[data-user]').forEach(b=>b.onclick=()=>userEditor(state.authUsers.find(x=>x.id===Number(b.dataset.user))));
  document.querySelectorAll('[data-role-permissions]').forEach(b=>b.onclick=()=>permissionEditor(state.roles.find(x=>x.id===Number(b.dataset.rolePermissions))));document.querySelectorAll('[data-role-edit]').forEach(b=>b.onclick=()=>roleEditor(state.roles.find(x=>x.id===Number(b.dataset.roleEdit))));
  document.querySelectorAll('[data-save-managers]').forEach(b=>b.onclick=async()=>{const id=Number(b.dataset.saveManagers);try{await api(`/api/authorization/employee-managers/${id}`,{method:'PUT',body:JSON.stringify({employeeId:id,firstManagerUserId:Number(document.querySelector(`[data-first="${id}"]`).value)||null,secondManagerUserId:Number(document.querySelector(`[data-second="${id}"]`).value)||null})});toast('Amir ataması güncellendi.');}catch(error){toast(error.message,'error');}});
}
function userEditor(row={}){openDialog(row.id?'Kullanıcıyı düzenle':'Yeni kullanıcı',`<div class="form-grid"><div class="field"><label>Kullanıcı adı</label><input class="input" name="userName" value="${esc(row.userName)}" required></div><div class="field"><label>Görünen ad</label><input class="input" name="displayName" value="${esc(row.displayName)}" required></div><div class="field"><label>Rol</label><select class="filter-select" name="roleId">${state.roles.filter(x=>x.isActive||x.id===row.roleId).map(x=>`<option value="${x.id}" ${row.roleId===x.id?'selected':''}>${esc(x.name)}</option>`).join('')}</select></div><div class="field"><label>Çalışan hesabı</label><select class="filter-select" name="employeeId">${optionList(state.employees,row.employeeId,'Bağlantı yok')}</select></div><div class="field full"><label>${row.id?'Yeni geçici parola (değiştirmeyecekseniz boş bırakın)':'Geçici parola'}</label><input class="input" type="password" name="password" ${row.id?'':'required'}><small>En az 10 karakter; büyük/küçük harf, rakam ve özel karakter.</small></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive!==false?'checked':''}> Aktif kullanıcı</label></div>`,async form=>{await api('/api/authorization/users',{method:'POST',body:JSON.stringify({id:row.id||null,userName:form.get('userName'),displayName:form.get('displayName'),roleId:Number(form.get('roleId')),employeeId:Number(form.get('employeeId'))||null,isActive:form.get('isActive')==='on',password:form.get('password')||null})});toast('Kullanıcı kaydedildi.');await authorizationPage();});}
function roleEditor(row={}){openDialog(row.id?'Rolü düzenle':'Yeni rol',`<div class="form-grid"><div class="field"><label>Rol adı</label><input class="input" name="name" value="${esc(row.name)}" required></div><div class="field"><label>Rol kodu</label><input class="input" name="code" value="${esc(row.code)}" ${row.isSystem?'readonly':''} required><small>Küçük harf ve tire kullanın.</small></div><label class="check-row"><input type="checkbox" name="isActive" ${row.isActive!==false?'checked':''}> Aktif rol</label></div>`,async form=>{await api('/api/authorization/roles',{method:'POST',body:JSON.stringify({id:row.id||null,name:form.get('name'),code:form.get('code'),isActive:form.get('isActive')==='on'})});toast('Rol kaydedildi.');await authorizationPage();});}
function permissionEditor(role){const active=new Set(state.rolePermissions.filter(x=>x.roleId===role.id&&x.isAllowed).map(x=>x.permission));openDialog(`${esc(role.name)} izinleri`,`<div class="permission-grid">${permissionNames.map((name,id)=>`<label class="permission-item"><input type="checkbox" name="permissions" value="${id}" ${active.has(id)?'checked':''}>${name}</label>`).join('')}</div>`,async form=>{await api(`/api/authorization/permissions/${role.id}`,{method:'PUT',body:JSON.stringify(form.getAll('permissions').map(Number))});toast('Rol izinleri güncellendi.');await authorizationPage();});}

async function pageDesignerPage(){loading();try{state.pageDefinitions=await api('/api/pages');renderPageList();}catch(error){showError(error,pageDesignerPage);}}
function renderPageList(){app.innerHTML=`${pageHead('Sayfa Tasarımcısı','Mevcut tasarım bileşenleriyle kod yazmadan çalışma sayfaları oluşturun.',`<button class="button primary" id="add-page">${icon('plus')}Sayfa oluştur</button>`)}<section class="card"><div class="table-wrap"><table class="table"><thead><tr><th>Sayfa</th><th>Adres</th><th>Gerekli izin</th><th>Durum</th><th>İşlem</th></tr></thead><tbody>${state.pageDefinitions.map(x=>`<tr><td><strong>${esc(x.title)}</strong><br><small>${esc(x.description||'')}</small></td><td>#page-${esc(x.slug)}</td><td>${permissionNames[x.requiredPermission]}</td><td><span class="badge ${x.isActive?'active':'inactive'}">${x.isActive?'Aktif':'Pasif'}</span></td><td><button class="row-action" data-page-edit="${x.id}">${icon('edit')}</button></td></tr>`).join('')}</tbody></table></div></section>`;document.querySelector('#add-page').onclick=()=>pageEditor();document.querySelectorAll('[data-page-edit]').forEach(b=>b.onclick=()=>pageEditor(state.pageDefinitions.find(x=>x.id===Number(b.dataset.pageEdit))));}
function pageEditor(row={}){let blocks=[];try{blocks=JSON.parse(row.layoutJson||'[]')}catch{};const render=()=>{app.innerHTML=`${pageHead(row.id?'Sayfayı düzenle':'Yeni sayfa','Bileşenleri ekleyin, başlıklarını değiştirin ve sıralayın.',`<button class="button secondary" id="cancel-page">Vazgeç</button><button class="button primary" id="save-page">Kaydet</button>`)}<section class="card setup-card"><div class="form-grid"><div class="field"><label>Sayfa başlığı</label><input class="input" id="page-title" value="${esc(row.title)}"></div><div class="field"><label>Adres</label><input class="input" id="page-slug" value="${esc(row.slug)}" placeholder="ornek-sayfa"></div><div class="field full"><label>Açıklama</label><input class="input" id="page-description" value="${esc(row.description)}"></div><div class="field"><label>Gerekli izin</label><select class="filter-select" id="page-permission">${permissionNames.map((x,i)=>`<option value="${i}" ${row.requiredPermission===i?'selected':''}>${x}</option>`).join('')}</select></div><label class="check-row"><input type="checkbox" id="page-active" ${row.isActive!==false?'checked':''}> Aktif sayfa</label></div></section><div class="builder"><aside class="card card-body palette"><strong>Bileşenler</strong>${[['title','Başlık'],['metric','Metrik kartı'],['text','Metin alanı'],['table','Tablo'],['select','Tekli seçim'],['multiselect','Çoklu seçim']].map(x=>`<button class="button secondary" data-add-block="${x[0]}">${icon('plus')}${x[1]}</button>`).join('')}</aside><section class="canvas" id="builder-canvas">${blocks.length?blocks.map((x,i)=>`<div class="builder-block"><div class="field" style="flex:1"><label>${esc(x.type)}</label><input class="input" data-block-label="${i}" value="${esc(x.label)}"></div><button class="row-action" data-remove-block="${i}">${icon('close')}</button></div>`).join(''):emptyState('Boş sayfa','Soldan bir bileşen ekleyin.')}</section></div>`;document.querySelector('#cancel-page').onclick=pageDesignerPage;document.querySelectorAll('[data-add-block]').forEach(b=>b.onclick=()=>{blocks.push({type:b.dataset.addBlock,label:{title:'Yeni başlık',metric:'Yeni metrik',text:'Açıklama metni',table:'Veri tablosu',select:'Seçim alanı',multiselect:'Çoklu seçim alanı'}[b.dataset.addBlock]});render();});document.querySelectorAll('[data-remove-block]').forEach(b=>b.onclick=()=>{blocks.splice(Number(b.dataset.removeBlock),1);render();});document.querySelectorAll('[data-block-label]').forEach(x=>x.oninput=()=>blocks[Number(x.dataset.blockLabel)].label=x.value);document.querySelector('#save-page').onclick=async()=>{try{row=await api('/api/pages',{method:'POST',body:JSON.stringify({id:row.id||null,title:document.querySelector('#page-title').value,slug:document.querySelector('#page-slug').value,description:document.querySelector('#page-description').value,layoutJson:JSON.stringify(blocks),requiredPermission:Number(document.querySelector('#page-permission').value),isActive:document.querySelector('#page-active').checked})});toast('Sayfa kaydedildi.');await loadCustomPages();await pageDesignerPage();}catch(error){toast(error.message,'error');}};};render();}
function renderCustomPage(page){let blocks=[];try{blocks=JSON.parse(page.layoutJson)}catch{};app.innerHTML=`${pageHead(esc(page.title),esc(page.description||''))}<div class="custom-page-grid">${blocks.map(x=>{if(x.type==='title')return `<section class="card card-body" style="grid-column:1/-1"><h2>${esc(x.label)}</h2></section>`;if(x.type==='metric')return statCard('score','teal',esc(x.label),'—');if(x.type==='text')return `<section class="card card-body"><p>${esc(x.label)}</p></section>`;if(x.type==='table')return `<section class="card" style="grid-column:1/-1"><div class="card-head"><h2>${esc(x.label)}</h2></div>${emptyState('Henüz veri yok','Bu bileşene veri kaynağı tanımlanmadı.')}</section>`;if(x.type==='multiselect')return `<section class="card card-body field"><label>${esc(x.label)}</label><select class="filter-select multi-select" multiple><option>Seçenek 1</option><option>Seçenek 2</option></select></section>`;return `<section class="card card-body field"><label>${esc(x.label)}</label><select class="filter-select"><option>Seçiniz</option></select></section>`;}).join('')}</div>`;}
async function loadCustomPages(){try{state.pages=await api('/api/pages/accessible');document.querySelector('#custom-page-nav').innerHTML=state.pages.map(x=>`<a href="#page-${esc(x.slug)}" data-route="page-${esc(x.slug)}">${icon('grid')}<span>${esc(x.title)}</span></a>`).join('');}catch{state.pages=[];}}

async function showLogin(){document.querySelector('#login-screen').hidden=false;document.querySelector('.app-shell').setAttribute('aria-hidden','true');}
async function afterLogin(user){state.user=user;document.querySelector('#login-screen').hidden=true;document.querySelector('.app-shell').removeAttribute('aria-hidden');document.querySelector('#user-menu').textContent=initials(user.displayName);document.querySelector('#user-menu').title=`${user.displayName} · ${user.roleName}`;document.querySelectorAll('[data-permission]').forEach(x=>x.hidden=!hasPermission(Number(x.dataset.permission)));await loadCustomPages();if(user.mustChangePassword)openPasswordDialog(true);await navigate();}
function openPasswordDialog(required=false){openDialog(required?'Parolanızı değiştirin':'Parola değiştir',`<div class="form-grid"><div class="field full"><label>Mevcut parola</label><input class="input" type="password" name="currentPassword" required></div><div class="field full"><label>Yeni parola</label><input class="input" type="password" name="newPassword" required><small>En az 10 karakter; büyük/küçük harf, rakam ve özel karakter.</small></div></div>`,async form=>{await api('/api/auth/change-password',{method:'POST',body:JSON.stringify({currentPassword:form.get('currentPassword'),newPassword:form.get('newPassword')})});state.user.mustChangePassword=false;toast('Parolanız değiştirildi.');});}
async function bootstrap(){try{await afterLogin(await api('/api/auth/me'));}catch(error){if(error.status===401)showLogin();else showError(error,bootstrap);}}

document.querySelector('#login-form').onsubmit=async event=>{event.preventDefault();const loginForm=event.currentTarget;const error=document.querySelector('#login-error');error.textContent='';const button=event.submitter;button.disabled=true;try{const form=new FormData(loginForm);const user=await api('/api/auth/login',{method:'POST',body:JSON.stringify({userName:form.get('userName'),password:form.get('password')})});loginForm.reset();await afterLogin(user);}catch(ex){error.textContent=ex.message;}finally{button.disabled=false;}};
document.querySelector('#user-menu').onclick=()=>{openDialog(state.user.displayName,`<div class="detail-grid"><div class="detail-box"><small>Kullanıcı</small><strong>${esc(state.user.userName)}</strong></div><div class="detail-box"><small>Rol</small><strong>${esc(state.user.roleName)}</strong></div></div><button type="button" class="button secondary" id="change-password">Parola değiştir</button><button type="button" class="button danger" id="logout" style="margin-left:8px">Çıkış yap</button>`,async()=>{});document.querySelector('#dialog-form .dialog-foot').remove();document.querySelector('#change-password').onclick=()=>{dialog.close();openPasswordDialog();};document.querySelector('#logout').onclick=async()=>{await api('/api/auth/logout',{method:'POST'});dialog.close();state.user=null;location.hash='#dashboard';showLogin();};};

const routes = { dashboard:dashboardPage, evaluation:evaluationPage, history:historyPage, employees:employeesPage, professions:professionsPage, kpis:kpisPage, periods:periodsPage, goals:goalsPage, authorization:authorizationPage, 'page-designer':pageDesignerPage };
async function navigate(){if(!state.user)return;const route=(location.hash||'#dashboard').slice(1);const custom=route.startsWith('page-')?state.pages.find(x=>`page-${x.slug}`===route):null;const page=custom?()=>renderCustomPage(custom):(routes[route]||dashboardPage);document.querySelector('#page-name').textContent=custom?.title||titles[route]||titles.dashboard;document.querySelectorAll('[data-route]').forEach(x=>x.classList.toggle('active',x.dataset.route===route));closeMenu();await page();app.focus({preventScroll:true});}
function closeMenu(){document.querySelector('#sidebar').classList.remove('open');document.querySelector('#mobile-overlay').classList.remove('open');}
document.querySelector('#menu-button').onclick=()=>{document.querySelector('#sidebar').classList.add('open');document.querySelector('#mobile-overlay').classList.add('open');};
document.querySelector('#mobile-overlay').onclick=closeMenu;
window.addEventListener('hashchange',navigate);
document.querySelector('#today').textContent=new Intl.DateTimeFormat('tr-TR',{weekday:'long',day:'numeric',month:'long'}).format(new Date());
bootstrap();
