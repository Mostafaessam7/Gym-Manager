import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { escapeHtml, rawHtml } from '../utils/html.js';

const canManageNutrition = () => authStore.hasPermission('nutrition:manage');
const canManageWorkouts = () => authStore.hasPermission('workouts:manage');

/* ---------------- Member picker ---------------- */

function renderMemberPicker(container, onSelect) {
  container.innerHTML = `
    <div class="picker" id="member-picker">
      <input type="search" placeholder="Search a member by name…" id="member-search" />
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
          : '<div class="picker-result text-muted">No members found.</div>';
        results.classList.remove('hidden');
        results.querySelectorAll('.picker-result[data-id]').forEach((el) => {
          el.addEventListener('click', () => {
            results.classList.add('hidden');
            input.value = '';
            onSelect({ id: el.dataset.id, name: el.dataset.name });
          });
        });
      } catch (error) {
        toastError(error.message || 'Failed to search members.');
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
        <div><div class="sub-list__row-main">${escapeHtml(m.name || '(unnamed)')}</div>
        <div class="sub-list__row-meta">${m.timeOfDay || ''} ${m.calories ? `· ${m.calories} kcal` : ''}</div></div>
        <div class="inline-actions"><button class="btn btn-sm btn-danger" data-i="${i}">Remove</button></div>
      </div>`).join('') || '<div class="sub-list__row text-muted">No meals yet.</div>';
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
    { name: 'name', label: 'Plan Name', value: existing?.name, required: true },
    { name: 'description', label: 'Description', value: existing?.description, span2: true },
    { name: 'dailyCalorieTarget', label: 'Calorie Target', type: 'number', value: existing?.dailyCalorieTarget },
    { name: 'proteinTargetG', label: 'Protein Target (g)', type: 'number', step: '0.1', value: existing?.proteinTargetG },
    { name: 'carbsTargetG', label: 'Carbs Target (g)', type: 'number', step: '0.1', value: existing?.carbsTargetG },
    { name: 'fatTargetG', label: 'Fat Target (g)', type: 'number', step: '0.1', value: existing?.fatTargetG },
  ];
  const formBody = renderForm(fields);
  const mealEditor = nutritionMealRowsEditor(existing?.meals || []);

  const addMealRow = document.createElement('div');
  addMealRow.className = 'form-grid';
  addMealRow.style.marginTop = '8px';
  addMealRow.innerHTML = `
    <div class="form-field"><label>Meal Name</label><input type="text" id="meal-name" /></div>
    <div class="form-field"><label>Time of Day</label><input type="text" id="meal-time" placeholder="e.g. Breakfast" /></div>
    <div class="form-field"><label>Calories</label><input type="number" id="meal-cal" /></div>
  `;
  const addMealBtn = document.createElement('button');
  addMealBtn.className = 'btn btn-sm btn-secondary';
  addMealBtn.textContent = '+ Add Meal';
  addMealBtn.style.marginTop = '8px';

  const body = document.createElement('div');
  body.appendChild(formBody);
  const mealsHeading = document.createElement('h4');
  mealsHeading.style.marginTop = 'var(--spacing-4)';
  mealsHeading.textContent = 'Meals';
  body.appendChild(mealsHeading);
  body.appendChild(mealEditor.element);
  if (!existing) { body.appendChild(addMealRow); body.appendChild(addMealBtn); }

  openModal({
    title: existing ? 'Edit Nutrition Plan' : 'New Nutrition Plan',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      addMealBtn?.addEventListener('click', () => {
        const name = body.querySelector('#meal-name').value.trim();
        if (!name) { toastError('Meal name is required.'); return; }
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
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Save',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(formBody, fields);
          try {
            if (existing) {
              await api.put(`/nutrition-plans/${existing.id}`, { ...values, isActive: true });
            } else {
              await api.post('/nutrition-plans', { ...values, memberId, trainerId: null, meals: mealEditor.getMeals() });
            }
            toastSuccess(existing ? 'Plan updated.' : 'Plan created.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save plan.');
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
      <div class="form-field"><label>Date</label><input type="date" id="log-date" value="${new Date().toISOString().slice(0, 10)}" /></div>
      <div class="form-field span-2"><label>Notes</label><input type="text" id="log-notes" /></div>
    </div>
    <h4 style="margin-top: var(--spacing-4);">Entries</h4>
    <div class="sub-list" id="entries-list"><div class="sub-list__row text-muted">No entries yet.</div></div>
    <div class="form-grid" style="margin-top:8px;">
      <div class="form-field"><label>Food</label><input type="text" id="entry-food" /></div>
      <div class="form-field"><label>Calories</label><input type="number" id="entry-cal" /></div>
    </div>
    <button class="btn btn-sm btn-secondary" id="add-entry-btn" style="margin-top:8px;">+ Add Entry</button>
  `;

  function redrawEntries() {
    const list = body.querySelector('#entries-list');
    list.innerHTML = entries.length ? entries.map((e, i) => `
      <div class="sub-list__row">
        <div class="sub-list__row-main">${escapeHtml(e.foodName)}${e.calories ? ` · ${e.calories} kcal` : ''}</div>
        <div class="inline-actions"><button class="btn btn-sm btn-danger" data-i="${i}">Remove</button></div>
      </div>`).join('') : '<div class="sub-list__row text-muted">No entries yet.</div>';
    list.querySelectorAll('button[data-i]').forEach((btn) => btn.addEventListener('click', () => { entries.splice(Number(btn.dataset.i), 1); redrawEntries(); }));
  }

  openModal({
    title: 'Log Nutrition Entry',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      body.querySelector('#add-entry-btn').addEventListener('click', () => {
        const food = body.querySelector('#entry-food').value.trim();
        if (!food) { toastError('Food name is required.'); return; }
        entries.push({ foodName: food, calories: body.querySelector('#entry-cal').value ? Number(body.querySelector('#entry-cal').value) : null });
        body.querySelector('#entry-food').value = '';
        body.querySelector('#entry-cal').value = '';
        redrawEntries();
      });
    },
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Save Log',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          if (!entries.length) { toastError('Add at least one entry.'); return; }
          try {
            await api.post('/nutrition-logs', {
              memberId,
              nutritionPlanId: null,
              loggedOn: body.querySelector('#log-date').value,
              notes: body.querySelector('#log-notes').value || null,
              entries,
            });
            toastSuccess('Nutrition log recorded.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to record log.');
          }
        },
      },
    ],
  });
}

function renderNutritionPlansTab(container, memberId) {
  container.innerHTML = `
    <div class="card-header">
      ${canManageNutrition() ? '<button class="btn btn-primary" id="new-plan-btn">+ New Plan</button>' : '<span></span>'}
    </div>
    <div id="plans-table"></div>
  `;

  const table = createDataTable(document.getElementById('plans-table'), {
    searchable: false,
    emptyMessage: 'No nutrition plans for this member.',
    columns: [
      { label: 'Name', key: 'name' },
      { label: 'Calories', render: (p) => p.dailyCalorieTarget ?? '—' },
      { label: 'Protein (g)', render: (p) => p.proteinTargetG ?? '—' },
      { label: 'Meals', render: (p) => p.meals.length },
      { label: 'Status', render: (p) => rawHtml(`<span class="badge badge-${p.isActive ? 'success' : 'neutral'}">${p.isActive ? 'Active' : 'Inactive'}</span>`) },
    ],
    fetchPage: () => api.get('/nutrition-plans', { memberId, pageSize: 100 }),
    rowActions: canManageNutrition() ? (plan) => [
      { label: 'Edit', onClick: (row, reload) => openNutritionPlanModal(memberId, row, reload) },
      { label: 'Delete', className: 'btn-danger', onClick: async (row, reload) => {
        if (await confirmDialog(`Delete plan "${row.name}"?`)) { await api.delete(`/nutrition-plans/${row.id}`); toastSuccess('Plan deleted.'); reload(); }
      } },
    ] : null,
  });

  document.getElementById('new-plan-btn')?.addEventListener('click', () => openNutritionPlanModal(memberId, null, table.refresh));
}

function renderNutritionLogsTab(container, memberId) {
  container.innerHTML = `
    <div class="card-header">
      ${canManageNutrition() ? '<button class="btn btn-primary" id="new-log-btn">+ Log Entry</button>' : '<span></span>'}
    </div>
    <div id="logs-table"></div>
  `;

  const table = createDataTable(document.getElementById('logs-table'), {
    searchable: false,
    emptyMessage: 'No nutrition logs for this member.',
    columns: [
      { label: 'Date', render: (l) => l.loggedOn },
      { label: 'Calories', key: 'totalCalories' },
      { label: 'Protein (g)', key: 'totalProteinG' },
      { label: 'Carbs (g)', key: 'totalCarbsG' },
      { label: 'Fat (g)', key: 'totalFatG' },
      { label: 'Entries', render: (l) => l.entries.length },
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
        <div><div class="sub-list__row-main">Day ${e.dayNumber} · ${escapeHtml(e.exerciseName)}</div>
        <div class="sub-list__row-meta">${e.sets ? `${e.sets} sets` : ''}${e.reps ? ` × ${e.reps} reps` : ''}${e.weightKg ? ` @ ${e.weightKg}kg` : ''}</div></div>
        <div class="inline-actions"><button class="btn btn-sm btn-danger" data-i="${i}">Remove</button></div>
      </div>`).join('') || '<div class="sub-list__row text-muted">No exercises yet.</div>';
    wrap.querySelectorAll('button[data-i]').forEach((btn) => btn.addEventListener('click', () => { exercises.splice(Number(btn.dataset.i), 1); draw(); }));
  }
  draw();

  return { element: wrap, addExercise: (e) => { exercises.push(e); draw(); }, getExercises: () => exercises };
}

async function openWorkoutPlanModal(memberId, existing, onSaved) {
  const fields = [
    { name: 'name', label: 'Plan Name', value: existing?.name, required: true },
    { name: 'description', label: 'Description', value: existing?.description, span2: true },
  ];
  const formBody = renderForm(fields);
  const exerciseEditor = workoutExerciseEditor(existing?.exercises || []);

  const addRow = document.createElement('div');
  addRow.className = 'form-grid';
  addRow.style.marginTop = '8px';
  addRow.innerHTML = `
    <div class="form-field"><label>Exercise</label><input type="text" id="ex-name" /></div>
    <div class="form-field"><label>Day #</label><input type="number" id="ex-day" value="1" /></div>
    <div class="form-field"><label>Sets</label><input type="number" id="ex-sets" /></div>
    <div class="form-field"><label>Reps</label><input type="number" id="ex-reps" /></div>
  `;
  const addBtn = document.createElement('button');
  addBtn.className = 'btn btn-sm btn-secondary';
  addBtn.textContent = '+ Add Exercise';
  addBtn.style.marginTop = '8px';

  const body = document.createElement('div');
  body.appendChild(formBody);
  const heading = document.createElement('h4');
  heading.style.marginTop = 'var(--spacing-4)';
  heading.textContent = 'Exercises';
  body.appendChild(heading);
  body.appendChild(exerciseEditor.element);
  if (!existing) { body.appendChild(addRow); body.appendChild(addBtn); }

  openModal({
    title: existing ? 'Edit Workout Plan' : 'New Workout Plan',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      addBtn?.addEventListener('click', () => {
        const name = body.querySelector('#ex-name').value.trim();
        if (!name) { toastError('Exercise name is required.'); return; }
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
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Save',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(formBody, fields);
          try {
            if (existing) await api.put(`/workout-plans/${existing.id}`, { ...values, isActive: true });
            else await api.post('/workout-plans', { ...values, memberId, trainerId: null, exercises: exerciseEditor.getExercises() });
            toastSuccess(existing ? 'Plan updated.' : 'Plan created.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save plan.');
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
      <div class="form-field"><label>Completed On</label><input type="datetime-local" id="log-when" value="${new Date().toISOString().slice(0, 16)}" /></div>
      <div class="form-field"><label>Duration (min)</label><input type="number" id="log-duration" /></div>
      <div class="form-field span-2"><label>Notes</label><input type="text" id="log-notes" /></div>
    </div>
    <h4 style="margin-top: var(--spacing-4);">Exercises Completed</h4>
    <div class="sub-list" id="exercises-list"><div class="sub-list__row text-muted">No exercises yet.</div></div>
    <div class="form-grid" style="margin-top:8px;">
      <div class="form-field"><label>Exercise</label><input type="text" id="ex-name" /></div>
      <div class="form-field"><label>Sets</label><input type="number" id="ex-sets" /></div>
      <div class="form-field"><label>Reps</label><input type="number" id="ex-reps" /></div>
    </div>
    <button class="btn btn-sm btn-secondary" id="add-ex-btn" style="margin-top:8px;">+ Add Exercise</button>
  `;

  function redraw() {
    const list = body.querySelector('#exercises-list');
    list.innerHTML = exercises.length ? exercises.map((e, i) => `
      <div class="sub-list__row">
        <div class="sub-list__row-main">${escapeHtml(e.exerciseName)}${e.setsCompleted ? ` · ${e.setsCompleted} sets` : ''}${e.repsCompleted ? ` × ${e.repsCompleted}` : ''}</div>
        <div class="inline-actions"><button class="btn btn-sm btn-danger" data-i="${i}">Remove</button></div>
      </div>`).join('') : '<div class="sub-list__row text-muted">No exercises yet.</div>';
    list.querySelectorAll('button[data-i]').forEach((btn) => btn.addEventListener('click', () => { exercises.splice(Number(btn.dataset.i), 1); redraw(); }));
  }

  openModal({
    title: 'Log Workout',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      body.querySelector('#add-ex-btn').addEventListener('click', () => {
        const name = body.querySelector('#ex-name').value.trim();
        if (!name) { toastError('Exercise name is required.'); return; }
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
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Save Log',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          if (!exercises.length) { toastError('Add at least one exercise.'); return; }
          try {
            await api.post('/workout-logs', {
              memberId,
              workoutPlanId: null,
              completedOnUtc: new Date(body.querySelector('#log-when').value).toISOString(),
              durationMinutes: body.querySelector('#log-duration').value ? Number(body.querySelector('#log-duration').value) : null,
              notes: body.querySelector('#log-notes').value || null,
              exercises,
            });
            toastSuccess('Workout log recorded.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to record log.');
          }
        },
      },
    ],
  });
}

function renderWorkoutPlansTab(container, memberId) {
  container.innerHTML = `
    <div class="card-header">
      ${canManageWorkouts() ? '<button class="btn btn-primary" id="new-plan-btn">+ New Plan</button>' : '<span></span>'}
    </div>
    <div id="plans-table"></div>
  `;

  const table = createDataTable(document.getElementById('plans-table'), {
    searchable: false,
    emptyMessage: 'No workout plans for this member.',
    columns: [
      { label: 'Name', key: 'name' },
      { label: 'Exercises', render: (p) => p.exercises.length },
      { label: 'Status', render: (p) => rawHtml(`<span class="badge badge-${p.isActive ? 'success' : 'neutral'}">${p.isActive ? 'Active' : 'Inactive'}</span>`) },
    ],
    fetchPage: () => api.get('/workout-plans', { memberId, pageSize: 100 }),
    rowActions: canManageWorkouts() ? (plan) => [
      { label: 'Edit', onClick: (row, reload) => openWorkoutPlanModal(memberId, row, reload) },
      { label: 'Delete', className: 'btn-danger', onClick: async (row, reload) => {
        if (await confirmDialog(`Delete plan "${row.name}"?`)) { await api.delete(`/workout-plans/${row.id}`); toastSuccess('Plan deleted.'); reload(); }
      } },
    ] : null,
  });

  document.getElementById('new-plan-btn')?.addEventListener('click', () => openWorkoutPlanModal(memberId, null, table.refresh));
}

function renderWorkoutLogsTab(container, memberId) {
  container.innerHTML = `
    <div class="card-header">
      ${canManageWorkouts() ? '<button class="btn btn-primary" id="new-log-btn">+ Log Workout</button>' : '<span></span>'}
    </div>
    <div id="logs-table"></div>
  `;

  const table = createDataTable(document.getElementById('logs-table'), {
    searchable: false,
    emptyMessage: 'No workout logs for this member.',
    columns: [
      { label: 'Date', render: (l) => new Date(l.completedOnUtc).toLocaleString() },
      { label: 'Duration', render: (l) => (l.durationMinutes ? `${l.durationMinutes} min` : '—') },
      { label: 'Exercises', render: (l) => l.exercises.length },
    ],
    fetchPage: () => api.get('/workout-logs', { memberId, pageSize: 100 }),
  });

  document.getElementById('new-log-btn')?.addEventListener('click', () => openWorkoutLogModal(memberId, table.refresh));
}

/* ---------------- Page shell ---------------- */

const TABS = [
  { key: 'nutrition-plans', label: 'Nutrition Plans', render: renderNutritionPlansTab },
  { key: 'nutrition-logs', label: 'Nutrition Logs', render: renderNutritionLogsTab },
  { key: 'workout-plans', label: 'Workout Plans', render: renderWorkoutPlansTab },
  { key: 'workout-logs', label: 'Workout Logs', render: renderWorkoutLogsTab },
];

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="section-title">
        <h2>Nutrition & Workouts</h2>
        <p class="text-muted">Search for a member to view or manage their fitness plans and logs.</p>
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
      <span><strong>${escapeHtml(member.name)}</strong> selected</span>
      <button class="btn btn-sm btn-ghost" id="change-member-btn">Change</button>
    </div>
  `;
  document.getElementById('change-member-btn').addEventListener('click', () => {
    document.getElementById('selected-slot').innerHTML = '';
    document.getElementById('fitness-body').classList.add('hidden');
  });

  const body = document.getElementById('fitness-body');
  body.classList.remove('hidden');
  body.innerHTML = `
    <div class="tabs">${TABS.map((t, i) => `<div class="tab${i === 0 ? ' active' : ''}" data-key="${t.key}">${t.label}</div>`).join('')}</div>
    <div id="fitness-tab-content"><div class="spinner"></div></div>
  `;

  const tabContent = document.getElementById('fitness-tab-content');
  TABS[0].render(tabContent, member.id);

  body.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      body.querySelectorAll('.tab').forEach((t) => t.classList.remove('active'));
      tabEl.classList.add('active');
      tabContent.innerHTML = '<div class="spinner"></div>';
      TABS.find((t) => t.key === tabEl.dataset.key).render(tabContent, member.id);
    });
  });
}
