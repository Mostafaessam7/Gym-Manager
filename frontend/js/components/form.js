// Renders a <div class="form-grid"> from a declarative field list and reads typed values back out.
// field: { name, label, type: 'text'|'number'|'email'|'password'|'date'|'datetime-local'|'select'|'textarea'|'checkbox',
//          value, options: [{value,label}], span2, required, step }
export function renderForm(fields) {
  const wrap = document.createElement('div');
  wrap.className = 'form-grid';

  fields.forEach((field) => {
    const fieldWrap = document.createElement('div');
    fieldWrap.className = `form-field${field.span2 ? ' span-2' : ''}`;

    const label = document.createElement('label');
    label.textContent = field.label;
    label.setAttribute('for', `field-${field.name}`);
    fieldWrap.appendChild(label);

    let input;
    if (field.type === 'select') {
      input = document.createElement('select');
      (field.options || []).forEach((opt) => {
        const optionEl = document.createElement('option');
        optionEl.value = opt.value;
        optionEl.textContent = opt.label;
        if (String(opt.value) === String(field.value)) optionEl.selected = true;
        input.appendChild(optionEl);
      });
    } else if (field.type === 'textarea') {
      input = document.createElement('textarea');
      input.rows = field.rows || 3;
      input.value = field.value ?? '';
    } else if (field.type === 'checkbox') {
      input = document.createElement('input');
      input.type = 'checkbox';
      input.checked = !!field.value;
    } else {
      input = document.createElement('input');
      input.type = field.type || 'text';
      if (field.step) input.step = field.step;
      input.value = field.value ?? '';
    }

    input.id = `field-${field.name}`;
    input.name = field.name;
    if (field.required) input.required = true;
    if (field.placeholder) input.placeholder = field.placeholder;

    fieldWrap.appendChild(input);
    wrap.appendChild(fieldWrap);
  });

  return wrap;
}

export function readForm(formElement, fields) {
  const values = {};
  fields.forEach((field) => {
    const el = formElement.querySelector(`#field-${field.name}`);
    if (!el) return;

    if (field.type === 'checkbox') values[field.name] = el.checked;
    else if (field.type === 'number') values[field.name] = el.value === '' ? null : Number(el.value);
    else values[field.name] = el.value === '' ? null : el.value;
  });
  return values;
}
