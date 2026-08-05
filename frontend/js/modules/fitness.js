import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { escapeHtml, rawHtml } from '../utils/html.js';
import { t } from '../i18n/index.js';

const canManageNutrition = () => authStore.hasPermission('nutrition:manage');
const canManageWorkouts = () => authStore.hasPermission('workouts:manage');

/* ---------------- Member picker ---------------- */

function renderMemberPicker(container, onSelect) {
  container.innerHTML = `
    <div class="picker" id="member-picker">
      <input type="search" placeholder="${t('fitness.searchPlaceholder')}" id="member-search" />
      <div class="picker-results hidden" id="member-results"></div>
    </div>
  `;

  const input = container.querySelector('#member-search');
  const results = container.querySelector('#member-results');
  let debounceHandle;

  input.addEventListener('input', () => {
    clearTimeout(debounceHandle);
    const term = input.value.trim();
    if (!term) { results.classList.add('hidden'); return; }
    debounceHandle = setTimeout(async () => {
      try {
        const page = await api.get('/members', { searchTerm: term, pageSize: 10 });
        const items = page.items || page;
        results.innerHTML = items.length
          ? items.map((m) => `<div class="picker-result" data-id="${m.id}" data-name="${escapeHtml(`${m.firstName} ${m.lastName}`)}">${escapeHtml(`${m.firstName} ${m.lastName}`)} <span class="text-muted">· ${escapeHtml(m.memberCode)}</span></div>`).join('')
          : `<div class="picker-result text-muted">${t('fitness.noMembersFound')}</div>`;
        results.classList.remove('hidden');
        results.querySelectorAll('.picker-result[data-id]').forEach((el) => {
          el.addEventListener('click', () => {
            results.classList.add('hidden');
            input.value = '';
            onSelect({ id: el.dataset.id, name: el.dataset.name });
          });
        });
      } catch (error) {
        toastError(error.message || t('fitness.searchMembersFailed'));
      }
    }, 300);
  });
}

/* ---------------- Nutrition ---------------- */

function nutritionMealRowsEditor(initialMeals = []) {
  const wrap = document.createElement('div');
  wrap.className = 'sub-list';
  let meals = initialMeals.map((m) => ({ ...m }));

  function draw() {
    wrap.innerHTML = meals.map((m, i) => `
      <div class="sub-list__row">
        <div><div class="sub-list__row-main">${escapeHtml(m.name || t('fitness.unnamed'))}</div>
        <div class="sub-list__row-meta">${m.timeOfDay || ''} ${m.calories ? `· ${m.calories} ${t('fitness.unitKcal')}` : ''}</div></div>
        <div class="inline-actions"><button class="btn btn-sm btn-danger" data-i="${i}">${t('common.delete')}</button></div>
      </div>`).join('') || `<div class="sub-list__row text-muted">${t('fitness.noEntriesYet')}</div>`;
    wrap.querySelectorAll('button[data-i]').forEach((btn) => {
      btn.addEventListener('click', () => { meals.splice(Number(btn.dataset.i), 1); draw(); });
    });
  }
  draw();

  return {
    element: wrap,
    addMeal(meal) { meals.push(meal); draw(); },
    getMeals: () => meals,
  };
}

async function openNutritionPlanModal(memberId, existing, onSaved) {
  const fields = [
    { name: 'name', label: t('fitness.planName'), value: existing?.name, required: true },
    { name: 'description', label: t('fitness.description'), value: existing?.description, span2: true },
    { name: 'dailyCalorieTarget', label: t('fitness.calorieTarget'), type: 'number', value: existing?.dailyCalorieTarget },
    { name: 'proteinTargetG', label: t('fitness.proteinTarget'), type: 'number', step: '0.1', value: existing?.proteinTargetG },
    { name: 'carbsTargetG', label: t('fitness.carbsTarget'), type: 'number', step: '0.1', value: existing?.carbsTargetG },
    { name: 'fatTargetG', label: t('fitness.fatTarget'), type: 'number', step: '0.1', value: existing?.fatTargetG },
  ];
  const formBody = renderForm(fields);
  const mealEditor = nutritionMealRowsEditor(existing?.meals || []);

  const addMealRow = document.createElement('div');
  addMealRow.className = 'form-grid';
  addMealRow.style.marginTop = '8px';
  addMealRow.innerHTML = `
    <div class="form-field"><label>${t('fitness.mealName')}</label><input type="text" id="meal-name" /></div>
    <div class="form-field"><label>${t('fitness.timeOfDay')}</label><input type="text" id="meal-time" placeholder="${t('fitness.timeOfDayPlaceholder')}" /></div>
    <div class="form-field"><label>${t('fitness.calories')}</label><input type="number" id="meal-cal" /></div>
  `;
  const addMealBtn = document.createElement('button');
  addMealBtn.className = 'btn btn-sm btn-secondary';
  addMealBtn.textContent = t('fitness.addMeal');
  addMealBtn.style.marginTop = '8px';

  const body = document.createElement('div');
  body.appendChild(formBody);
  const mealsHeading = document.createElement('h4');
  mealsHeading.style.marginTop = 'var(--spacing-4)';
  mealsHeading.textContent = t('fitness.meals');
  body.appendChild(mealsHeading);
  body.appendChild(mealEditor.element);
  if (!existing) { body.appendChild(addMealRow); body.appendChild(addMealBtn); }

  openModal({
    title: existing ? t('fitness.editNutritionPlanTitle') : t('fitness.newNutritionPlanTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      addMealBtn?.addEventListener('click', () => {
        const name = body.querySelector('#meal-name').value.trim();
        if (!name) { toastError(t('fitness.mealNameRequired')); return; }
        mealEditor.addMeal({
          name,
          order: mealEditor.getMeals().length + 1,
          timeOfDay: body.querySelector('#meal-time').value || null,
          calories: body.querySelector('#meal-cal').value ? Number(body.querySelector('#meal-cal').value) : null,
        });
        body.querySelector('#meal-name').value = '';
        body.querySelector('#meal-time').value = '';
        body.querySelector('#meal-cal').value = '';
      });
    },
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('common.save'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(formBody, fields);
          try {
            if (existing) {
              await api.put(`/nutrition-plans/${existing.id}`, { ...values, isActive: true });
            } else {
              await api.post('/nutrition-plans', { ...values, memberId, trainerId: null, meals: mealEditor.getMeals() });
            }
            toastSuccess(existing ? t('fitness.planUpdated') : t('fitness.planCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('fitness.saveFailed'));
          }
        },
      },
    ],
  });
}

async function openNutritionLogModal(memberId, onSaved) {
  const entries = [];
  const body = document.createElement('div');
  body.innerHTML = `
    <div class="form-grid">
      <div class="form-field"><label>${t('fitness.dateField')}</label><input type="date" id="log-date" value="${new Date().toISOString().slice(0, 10)}" /></div>
      <div class="form-field span-2"><label>${t('fitness.notesField')}</label><input type="text" id="log-notes" /></div>
    </div>
    <h4 style="margin-top: var(--spacing-4);">${t('fitness.entries')}</h4>
    <div class="sub-list" id="entries-list"><div class="sub-list__row text-muted">${t('fitness.noEntriesYet')}</div></div>
    <div class="form-grid" style="margin-top:8px;">
      <div class="form-field"><label>${t('fitness.food')}</label><input type="text" id="entry-food" /></div>
      <div class="form-field"><label>${t('fitness.calories')}</label><input type="number" id="entry-cal" /></div>
    </div>
    <button class="btn btn-sm btn-secondary" id="add-entry-btn" style="margin-top:8px;">${t('fitness.addEntry')}</button>
  `;

  function redrawEntries() {
    const list = body.querySelector('#entries-list');
    list.innerHTML = entries.length ? entries.map((e, i) => `
      <div class="sub-list__row">
        <div class="sub-list__row-main">${escapeHtml(e.foodName)}${e.calories ? ` · ${e.calories} ${t('fitness.unitKcal')}` : ''}</div>
        <div class="inline-actions"><button class="btn btn-sm btn-danger" data-i="${i}">${t('common.delete')}</button></div>
      </div>`).join('') : `<div class="sub-list__row text-muted">${t('fitness.noEntriesYet')}</div>`;
    list.querySelectorAll('button[data-i]').forEach((btn) => btn.addEventListener('click', () => { entries.splice(Number(btn.dataset.i), 1); redrawEntries(); }));
  }

  openModal({
    title: t('fitness.logNutritionTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      body.querySelector('#add-entry-btn').addEventListener('click', () => {
        const food = body.querySelector('#entry-food').value.trim();
        if (!food) { toastError(t('fitness.foodRequired')); return; }
        entries.push({ foodName: food, calories: body.querySelector('#entry-cal').value ? Number(body.querySelector('#entry-cal').value) : null });
        body.querySelector('#entry-food').value = '';
        body.querySelector('#entry-cal').value = '';
        redrawEntries();
      });
    },
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('fitness.saveLog'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          if (!entries.length) { toastError(t('fitness.addAtLeastOneEntry')); return; }
          try {
            await api.post('/nutrition-logs', {
              memberId,
              nutritionPlanId: null,
              loggedOn: body.querySelector('#log-date').value,
              notes: body.querySelector('#log-notes').value || null,
              entries,
            });
            toastSuccess(t('fitness.logRecorded'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('fitness.logSaveFailed'));
          }
        },
      },
    ],
  });
}

function renderNutritionPlansTab(container, memberId) {
  container.innerHTML = `
    <div class="card-header">
      ${canManageNutrition() ? `<button class="btn btn-primary" id="new-plan-btn">${t('fitness.newPlan')}</button>` : '<span></span>'}
    </div>
    <div id="plans-table"></div>
  `;

  const table = createDataTable(document.getElementById('plans-table'), {
    searchable: false,
    emptyMessage: t('fitness.noNutritionPlans'),
    columns: [
      { label: t('fitness.nameCol'), key: 'name' },
      { label: t('fitness.caloriesCol'), render: (p) => p.dailyCalorieTarget ?? '—' },
      { label: t('fitness.proteinCol'), render: (p) => p.proteinTargetG ?? '—' },
      { label: t('fitness.mealsCol'), render: (p) => p.meals.length },
      { label: t('fitness.statusCol'), render: (p) => rawHtml(`<span class="badge badge-${p.isActive ? 'success' : 'neutral'}">${p.isActive ? t('fitness.active') : t('fitness.inactive')}</span>`) },
    ],
    fetchPage: () => api.get('/nutrition-plans', { memberId, pageSize: 100 }),
    rowActions: canManageNutrition() ? (plan) => [
      { label: t('common.edit'), onClick: (row, reload) => openNutritionPlanModal(memberId, row, reload) },
      { label: t('common.delete'), className: 'btn-danger', onClick: async (row, reload) => {
        if (await confirmDialog(t('fitness.deleteConfirm', { name: row.name }))) { await api.delete(`/nutrition-plans/${row.id}`); toastSuccess(t('fitness.planDeleted')); reload(); }
      } },
    ] : null,
  });

  document.getElementById('new-plan-btn')?.addEventListener('click', () => openNutritionPlanModal(memberId, null, table.refresh));
}

function renderNutritionLogsTab(container, memberId) {
  container.innerHTML = `
    <div class="card-header">
      ${canManageNutrition() ? `<button class="btn btn-primary" id="new-log-btn">${t('fitness.logEntry')}</button>` : '<span></span>'}
    </div>
    <div id="logs-table"></div>
  `;

  const table = createDataTable(document.getElementById('logs-table'), {
    searchable: false,
    emptyMessage: t('fitness.noNutritionLogs'),
    columns: [
      { label: t('fitness.dateCol'), render: (l) => l.loggedOn },
      { label: t('fitness.caloriesColTotal'), key: 'totalCalories' },
      { label: t('fitness.proteinColTotal'), key: 'totalProteinG' },
      { label: t('fitness.carbsColTotal'), key: 'totalCarbsG' },
      { label: t('fitness.fatColTotal'), key: 'totalFatG' },
      { label: t('fitness.entriesCol'), render: (l) => l.entries.length },
    ],
    fetchPage: () => api.get('/nutrition-logs', { memberId, pageSize: 100 }),
  });

  document.getElementById('new-log-btn')?.addEventListener('click', () => openNutritionLogModal(memberId, table.refresh));
}

/* ---------------- Workouts ---------------- */

function workoutExerciseEditor(initial = []) {
  const wrap = document.createElement('div');
  wrap.className = 'sub-list';
  let exercises = initial.map((e) => ({ ...e }));

  function draw() {
    wrap.innerHTML = exercises.map((e, i) => `
      <div class="sub-list__row">
        <div><div class="sub-list__row-main">${t('fitness.dayLabel', { n: e.dayNumber })} · ${escapeHtml(e.exerciseName)}</div>
        <div class="sub-list__row-meta">${e.sets ? `${e.sets} ${t('fitness.unitSets')}` : ''}${e.reps ? ` × ${e.reps} ${t('fitness.unitReps')}` : ''}${e.weightKg ? ` @ ${e.weightKg}${t('fitness.unitKg')}` : ''}</div></div>
        <div class="inline-actions"><button class="btn btn-sm btn-danger" data-i="${i}">${t('common.delete')}</button></div>
      </div>`).join('') || `<div class="sub-list__row text-muted">${t('fitness.noExercisesYet')}</div>`;
    wrap.querySelectorAll('button[data-i]').forEach((btn) => btn.addEventListener('click', () => { exercises.splice(Number(btn.dataset.i), 1); draw(); }));
  }
  draw();

  return { element: wrap, addExercise: (e) => { exercises.push(e); draw(); }, getExercises: () => exercises };
}

async function openWorkoutPlanModal(memberId, existing, onSaved) {
  const fields = [
    { name: 'name', label: t('fitness.planName'), value: existing?.name, required: true },
    { name: 'description', label: t('fitness.description'), value: existing?.description, span2: true },
  ];
  const formBody = renderForm(fields);
  const exerciseEditor = workoutExerciseEditor(existing?.exercises || []);

  const addRow = document.createElement('div');
  addRow.className = 'form-grid';
  addRow.style.marginTop = '8px';
  addRow.innerHTML = `
    <div class="form-field"><label>${t('fitness.exercise')}</label><input type="text" id="ex-name" /></div>
    <div class="form-field"><label>${t('fitness.day')}</label><input type="number" id="ex-day" value="1" /></div>
    <div class="form-field"><label>${t('fitness.sets')}</label><input type="number" id="ex-sets" /></div>
    <div class="form-field"><label>${t('fitness.reps')}</label><input type="number" id="ex-reps" /></div>
  `;
  const addBtn = document.createElement('button');
  addBtn.className = 'btn btn-sm btn-secondary';
  addBtn.textContent = t('fitness.addExercise');
  addBtn.style.marginTop = '8px';

  const body = document.createElement('div');
  body.appendChild(formBody);
  const heading = document.createElement('h4');
  heading.style.marginTop = 'var(--spacing-4)';
  heading.textContent = t('fitness.exercises');
  body.appendChild(heading);
  body.appendChild(exerciseEditor.element);
  if (!existing) { body.appendChild(addRow); body.appendChild(addBtn); }

  openModal({
    title: existing ? t('fitness.editWorkoutPlanTitle') : t('fitness.newWorkoutPlanTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      addBtn?.addEventListener('click', () => {
        const name = body.querySelector('#ex-name').value.trim();
        if (!name) { toastError(t('fitness.exerciseRequired')); return; }
        exerciseEditor.addExercise({
          exerciseName: name,
          dayNumber: Number(body.querySelector('#ex-day').value || 1),
          order: exerciseEditor.getExercises().length + 1,
          sets: body.querySelector('#ex-sets').value ? Number(body.querySelector('#ex-sets').value) : null,
          reps: body.querySelector('#ex-reps').value ? Number(body.querySelector('#ex-reps').value) : null,
        });
        body.querySelector('#ex-name').value = '';
        body.querySelector('#ex-sets').value = '';
        body.querySelector('#ex-reps').value = '';
      });
    },
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('common.save'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(formBody, fields);
          try {
            if (existing) await api.put(`/workout-plans/${existing.id}`, { ...values, isActive: true });
            else await api.post('/workout-plans', { ...values, memberId, trainerId: null, exercises: exerciseEditor.getExercises() });
            toastSuccess(existing ? t('fitness.planUpdated') : t('fitness.planCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('fitness.saveFailed'));
          }
        },
      },
    ],
  });
}

async function openWorkoutLogModal(memberId, onSaved) {
  const exercises = [];
  const body = document.createElement('div');
  body.innerHTML = `
    <div class="form-grid">
      <div class="form-field"><label>${t('fitness.completedOn')}</label><input type="datetime-local" id="log-when" value="${new Date().toISOString().slice(0, 16)}" /></div>
      <div class="form-field"><label>${t('fitness.durationMin')}</label><input type="number" id="log-duration" /></div>
      <div class="form-field span-2"><label>${t('fitness.notesField')}</label><input type="text" id="log-notes" /></div>
    </div>
    <h4 style="margin-top: var(--spacing-4);">${t('fitness.exercisesCompleted')}</h4>
    <div class="sub-list" id="exercises-list"><div class="sub-list__row text-muted">${t('fitness.noExercisesYet')}</div></div>
    <div class="form-grid" style="margin-top:8px;">
      <div class="form-field"><label>${t('fitness.exercise')}</label><input type="text" id="ex-name" /></div>
      <div class="form-field"><label>${t('fitness.sets')}</label><input type="number" id="ex-sets" /></div>
      <div class="form-field"><label>${t('fitness.reps')}</label><input type="number" id="ex-reps" /></div>
    </div>
    <button class="btn btn-sm btn-secondary" id="add-ex-btn" style="margin-top:8px;">${t('fitness.addExercise')}</button>
  `;

  function redraw() {
    const list = body.querySelector('#exercises-list');
    list.innerHTML = exercises.length ? exercises.map((e, i) => `
      <div class="sub-list__row">
        <div class="sub-list__row-main">${escapeHtml(e.exerciseName)}${e.setsCompleted ? ` · ${e.setsCompleted} ${t('fitness.unitSets')}` : ''}${e.repsCompleted ? ` × ${e.repsCompleted}` : ''}</div>
        <div class="inline-actions"><button class="btn btn-sm btn-danger" data-i="${i}">${t('common.delete')}</button></div>
      </div>`).join('') : `<div class="sub-list__row text-muted">${t('fitness.noExercisesYet')}</div>`;
    list.querySelectorAll('button[data-i]').forEach((btn) => btn.addEventListener('click', () => { exercises.splice(Number(btn.dataset.i), 1); redraw(); }));
  }

  openModal({
    title: t('fitness.logWorkoutTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      body.querySelector('#add-ex-btn').addEventListener('click', () => {
        const name = body.querySelector('#ex-name').value.trim();
        if (!name) { toastError(t('fitness.exerciseRequired')); return; }
        exercises.push({
          exerciseName: name,
          setsCompleted: body.querySelector('#ex-sets').value ? Number(body.querySelector('#ex-sets').value) : null,
          repsCompleted: body.querySelector('#ex-reps').value ? Number(body.querySelector('#ex-reps').value) : null,
        });
        body.querySelector('#ex-name').value = '';
        body.querySelector('#ex-sets').value = '';
        body.querySelector('#ex-reps').value = '';
        redraw();
      });
    },
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('fitness.saveLog'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          if (!exercises.length) { toastError(t('fitness.addAtLeastOneExercise')); return; }
          try {
            await api.post('/workout-logs', {
              memberId,
              workoutPlanId: null,
              completedOnUtc: new Date(body.querySelector('#log-when').value).toISOString(),
              durationMinutes: body.querySelector('#log-duration').value ? Number(body.querySelector('#log-duration').value) : null,
              notes: body.querySelector('#log-notes').value || null,
              exercises,
            });
            toastSuccess(t('fitness.workoutLogRecorded'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('fitness.logSaveFailed'));
          }
        },
      },
    ],
  });
}

function renderWorkoutPlansTab(container, memberId) {
  container.innerHTML = `
    <div class="card-header">
      ${canManageWorkouts() ? `<button class="btn btn-primary" id="new-plan-btn">${t('fitness.newPlan')}</button>` : '<span></span>'}
    </div>
    <div id="plans-table"></div>
  `;

  const table = createDataTable(document.getElementById('plans-table'), {
    searchable: false,
    emptyMessage: t('fitness.noWorkoutPlans'),
    columns: [
      { label: t('fitness.nameCol'), key: 'name' },
      { label: t('fitness.exercisesCol'), render: (p) => p.exercises.length },
      { label: t('fitness.statusCol'), render: (p) => rawHtml(`<span class="badge badge-${p.isActive ? 'success' : 'neutral'}">${p.isActive ? t('fitness.active') : t('fitness.inactive')}</span>`) },
    ],
    fetchPage: () => api.get('/workout-plans', { memberId, pageSize: 100 }),
    rowActions: canManageWorkouts() ? (plan) => [
      { label: t('common.edit'), onClick: (row, reload) => openWorkoutPlanModal(memberId, row, reload) },
      { label: t('common.delete'), className: 'btn-danger', onClick: async (row, reload) => {
        if (await confirmDialog(t('fitness.deleteConfirm', { name: row.name }))) { await api.delete(`/workout-plans/${row.id}`); toastSuccess(t('fitness.planDeleted')); reload(); }
      } },
    ] : null,
  });

  document.getElementById('new-plan-btn')?.addEventListener('click', () => openWorkoutPlanModal(memberId, null, table.refresh));
}

function renderWorkoutLogsTab(container, memberId) {
  container.innerHTML = `
    <div class="card-header">
      ${canManageWorkouts() ? `<button class="btn btn-primary" id="new-log-btn">${t('fitness.logWorkout')}</button>` : '<span></span>'}
    </div>
    <div id="logs-table"></div>
  `;

  const table = createDataTable(document.getElementById('logs-table'), {
    searchable: false,
    emptyMessage: t('fitness.noWorkoutLogs'),
    columns: [
      { label: t('fitness.dateCol'), render: (l) => new Date(l.completedOnUtc).toLocaleString() },
      { label: t('fitness.durationColTotal'), render: (l) => (l.durationMinutes ? `${l.durationMinutes} ${t('fitness.unitMin')}` : '—') },
      { label: t('fitness.exercisesColTotal'), render: (l) => l.exercises.length },
    ],
    fetchPage: () => api.get('/workout-logs', { memberId, pageSize: 100 }),
  });

  document.getElementById('new-log-btn')?.addEventListener('click', () => openWorkoutLogModal(memberId, table.refresh));
}

/* ---------------- Page shell ---------------- */

const TABS = () => [
  { key: 'nutrition-plans', label: t('fitness.tabNutritionPlans'), render: renderNutritionPlansTab },
  { key: 'nutrition-logs', label: t('fitness.tabNutritionLogs'), render: renderNutritionLogsTab },
  { key: 'workout-plans', label: t('fitness.tabWorkoutPlans'), render: renderWorkoutPlansTab },
  { key: 'workout-logs', label: t('fitness.tabWorkoutLogs'), render: renderWorkoutLogsTab },
];

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="section-title">
        <h2>${t('fitness.title')}</h2>
        <p class="text-muted">${t('fitness.subtitle')}</p>
      </div>
      <div id="picker-slot"></div>
      <div id="selected-slot"></div>
      <div id="fitness-body" class="hidden"></div>
    </div>
  `;

  renderMemberPicker(document.getElementById('picker-slot'), (member) => selectMember(container, member));
}

function selectMember(container, member) {
  document.getElementById('selected-slot').innerHTML = `
    <div class="selected-member-card">
      <span>${t('fitness.memberSelected', { name: `<strong>${escapeHtml(member.name)}</strong>` })}</span>
      <button class="btn btn-sm btn-ghost" id="change-member-btn">${t('fitness.change')}</button>
    </div>
  `;
  document.getElementById('change-member-btn').addEventListener('click', () => {
    document.getElementById('selected-slot').innerHTML = '';
    document.getElementById('fitness-body').classList.add('hidden');
  });

  const body = document.getElementById('fitness-body');
  body.classList.remove('hidden');
  const tabs = TABS();
  body.innerHTML = `
    <div class="tabs">${tabs.map((tab, i) => `<div class="tab${i === 0 ? ' active' : ''}" data-key="${tab.key}">${tab.label}</div>`).join('')}</div>
    <div id="fitness-tab-content"><div class="spinner"></div></div>
  `;

  const tabContent = document.getElementById('fitness-tab-content');
  tabs[0].render(tabContent, member.id);

  body.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      body.querySelectorAll('.tab').forEach((el) => el.classList.remove('active'));
      tabEl.classList.add('active');
      tabContent.innerHTML = '<div class="spinner"></div>';
      tabs.find((tab) => tab.key === tabEl.dataset.key).render(tabContent, member.id);
    });
  });
}
