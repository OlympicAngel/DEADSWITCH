// Per-frame DOM patching that skips writes when nothing changed. Rewriting an unchanged value still
// costs a style/layout/paint pass, ten times a second, on every visible number and bar.
export function put(el, value) {
  const v = String(value);
  if (el.__t !== v) {
    el.__t = v;
    // Changing a text node's data is a text change; replacing the node is a layout change.
    const t = el.firstChild;
    if (t && t === el.lastChild && t.nodeType === 3) t.data = v;
    else el.textContent = v;
  }
}

export function putHtml(el, html) {
  if (el.__h !== html) {
    el.__h = html;
    el.innerHTML = html;
  }
}

export function setW(el, width) {
  if (el.__w !== width) {
    el.__w = width;
    el.style.width = width;
  }
}

export function setCls(el, cls) {
  if (el.className !== cls) el.className = cls;
}

export function setAttr(el, name, value) {
  const v = String(value);
  if (el.getAttribute(name) !== v) el.setAttribute(name, v);
}

export function setData(el, key, value) {
  const v = String(value);
  if (el.dataset[key] !== v) el.dataset[key] = v;
}

export function setHidden(el, hidden) {
  if (el.hidden !== !!hidden) el.hidden = !!hidden;
}

export function setStyle(el, prop, value) {
  const k = '__s_' + prop;
  if (el[k] !== value) {
    el[k] = value;
    el.style[prop] = value;
  }
}
