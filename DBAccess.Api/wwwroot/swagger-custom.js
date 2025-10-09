(function() {
  const storagePrefix = 'swagger_saved_';

  function getOpId(opBlock) {
    if (!opBlock) return '';
    const id = opBlock.id || '';
    if (id.startsWith('operations-')) {
      return id.substring('operations-'.length);
    }
    return id;
  }

  function key(opId, name) {
    return storagePrefix + opId + '::' + name;
  }

  function restoreInputs(opBlock) {
    const opId = getOpId(opBlock);
    if (!opId) return;

    const inputs = opBlock.querySelectorAll('input[type="text"], input[type="number"], input[type="search"], textarea');
    inputs.forEach(function(inp) {
      const paramName = inp.getAttribute('name')
        || (inp.dataset ? inp.dataset.name : null)
        || (inp.closest('[data-param-name]') && inp.closest('[data-param-name]').getAttribute('data-param-name'));
      if (!paramName) return;
      const k = key(opId, paramName);
      const v = localStorage.getItem(k);
      if (v !== null) {
        inp.value = v;
        try { inp.dispatchEvent(new Event('input', { bubbles: true })); } catch (e) {}
        try { inp.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) {}
      }
      inp.addEventListener('input', function() { localStorage.setItem(k, inp.value); });
      inp.addEventListener('change', function() { localStorage.setItem(k, inp.value); });
    });

    const selects = opBlock.querySelectorAll('select');
    selects.forEach(function(sel) {
      const paramName = sel.getAttribute('name')
        || (sel.closest('[data-param-name]') && sel.closest('[data-param-name]').getAttribute('data-param-name'));
      if (!paramName) return;
      const k = key(opId, paramName);
      const v = localStorage.getItem(k);
      if (v !== null) {
        sel.value = v;
        try { sel.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) {}
      }
      sel.addEventListener('change', function() { localStorage.setItem(k, sel.value); });
    });
  }

  function attachTryOutListeners() {
    document.body.addEventListener('click', function(e) {
      const btn = e.target.closest('button.try-out__btn');
      if (!btn) return;
      const opBlock = btn.closest('.opblock');
      setTimeout(function() { restoreInputs(opBlock); }, 80);
    });

    document.body.addEventListener('click', function(e) {
      const summary = e.target.closest('.opblock-summary');
      if (!summary) return;
      const opBlock = summary.closest('.opblock');
      setTimeout(function() { restoreInputs(opBlock); }, 120);
    });
  }

  if (document.readyState === 'complete' || document.readyState === 'interactive') {
    attachTryOutListeners();
  } else {
    document.addEventListener('DOMContentLoaded', attachTryOutListeners);
  }
})();
