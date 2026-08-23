// Wires click-to-switch behavior for a `.tabs` / `.tab[data-tab]` block already rendered into `container`
// (this only wires behavior — it doesn't render the tab markup itself, since every caller's tab bar sits
// inside a larger template it controls). `tabContent` is reset to a spinner and the matching renderer from
// `renderers` (keyed by each tab's `data-tab`) is invoked on every click.
export function wireTabs(container, tabContent, renderers) {
  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((el) => el.classList.remove('active'));
      tabEl.classList.add('active');
      tabContent.innerHTML = '<div class="spinner"></div>';
      renderers[tabEl.dataset.tab]();
    });
  });
}
