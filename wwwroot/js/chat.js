// ── SignalR Live Chat ─────────────────────────────────────────── //
(function () {
  'use strict';

  var CONNECTION_URL = document.location.origin + '/chatHub';
  var STORAGE_KEY_NAME = 'istoriaChatVisitorName';
  var isChatOpen = false;
  var connection = null;

  // ── DOM refs ────────────────────────────────────────────────────
  var fab       = document.getElementById('chatFab');
  var fabIcon   = document.getElementById('chatFabIcon');
  var badge     = document.getElementById('chatBadge');
  var panel     = document.getElementById('chatPanel');
  var overlay   = document.getElementById('chatPanelOverlay');
  var closeBtn  = document.getElementById('chatClose');
  var msgList   = document.getElementById('chatMessages');
  var input     = document.getElementById('chatMessageInput');
  var form      = document.getElementById('chatForm');
  var nameInput = document.getElementById('chatVisitorName');
  var statusDot = document.getElementById('chatStatusDot');
  var statusTxt = document.getElementById('chatStatusText');

  var unreadCount = 0;
  var _pollTimer  = null;

  // ── Restore visitor name ────────────────────────────────────────
  if (nameInput) {
    var saved = localStorage.getItem(STORAGE_KEY_NAME);
    if (saved) nameInput.value = saved;
  }

  // ── Status ──────────────────────────────────────────────────────
  function setStatus(state) {
    if (statusDot) statusDot.className = 'status-dot ' + state;
    if (statusTxt) statusTxt.textContent =
      state === 'connected'   ? 'Connected'
      : state === 'connecting' ? 'Connecting\u2026'
      : 'Disconnected';
  }

  // ── Helpers ─────────────────────────────────────────────────────
  function saveName() {
    if (nameInput && nameInput.value.trim())
      localStorage.setItem(STORAGE_KEY_NAME, nameInput.value.trim());
  }

  function scrollBottom() { msgList && (msgList.scrollTop = msgList.scrollHeight); }

  function clearUnread() { unreadCount = 0; updateBadge(); }

  function updateBadge() {
    if (!badge) return;
    badge.style.display = unreadCount > 0 ? 'block' : 'none';
    badge.textContent   = unreadCount > 9 ? '9+' : unreadCount;
  }

  function createMsgRow(data) {
    var row  = document.createElement('div');
    row.className = 'msg-row ' + (data.senderIsAdmin ? 'admin' : 'visitor');
    var hasName = data.senderName && data.senderName !== 'Visitor';

    var bubble = document.createElement('div');
    bubble.className = 'msg-bubble';
    bubble.textContent = data.messageText;

    var meta = document.createElement('div');
    meta.className = 'msg-meta';
    var label = hasName ? data.senderName + '\u00A0\u00A0' : '';
    meta.textContent = label + data.sentAt;

    row.appendChild(bubble);
    row.appendChild(meta);
    return row;
  }

  function addMessage(data) {
    // Guard against duplicates (reconnect re-broadcasts)
    if (document.getElementById('msg-' + data.id)) return;
    if (!isChatOpen && !data.senderIsAdmin) unreadCount++;
    updateBadge();
    if (msgList) msgList.appendChild(createMsgRow(data));
    scrollBottom();
    setStatus('connected');
  }

  function toggleChat(open) {
    isChatOpen = open;
    panel   && panel.classList.toggle('open', open);
    overlay && overlay.classList.toggle('open', open);
    fabIcon && (fabIcon.className = open ? 'fas fa-times' : 'fas fa-comment-dots');
    if (open) { clearUnread(); input && input.focus(); scrollBottom(); }
  }

  // ── Fallback: POST via fetch when SignalR is unavailable ────────
  function postMessage(text) {
    if (!text || !form) return;
    var btn = form.querySelector('button[type=submit]');
    if (btn) { btn.disabled = true; btn.textContent = '...'; }

    var tokenEl  = form.querySelector('input[name="__RequestVerificationToken"]');
    var body     = new URLSearchParams({ message: text });
    if (tokenEl) body.append('__RequestVerificationToken', tokenEl.value);

    fetch(form.action, {
      method  : 'POST',
      headers : { 'Content-Type': 'application/x-www-form-urlencoded' },
      body    : body.toString()
    }).then(function (res) {
      if (!res.ok) throw new Error('HTTP ' + res.status);
      return res.text();
    }).then(function () {
      input && (input.value = ''); input && (input.style.height = 'auto');
      input && input.focus();
    }).catch(function (err) {
      console.warn('[Chat] fallback POST failed:', err);
      showError('Your message was not saved. Please try again.');
    }).finally(function () {
      if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fas fa-paper-plane"></i>'; }
    });
  }

  function showError(msg) {
    if (!msgList) return;
    var el = document.createElement('div');
    el.className = 'system-error';
    el.style.cssText = 'text-align:center;color:#e53935;font-family:sans-serif;font-size:.75rem;padding:.25rem 0 .5rem;';
    el.textContent = msg;
    msgList.appendChild(el);
    scrollBottom();
  }

  // ── Send ────────────────────────────────────────────────────────
  function doSend() {
    var text = input ? input.value.trim() : '';
    if (!text) return;

    var nameStr = (nameInput && nameInput.value.trim()) || '';

    // Try SignalR first
    if (connection) {
      var ready = connection.state === 0 /* Connected */;
      if (ready) {
        connection.invoke('SendMessage', nameStr, false, text)
          .then(function () {
            if (input) { input.value = ''; input.style.height = 'auto'; input.focus(); }
            clearUnread();
          })
          .catch(function (err) {
            console.warn('[Chat] SendMessage via hub failed, falling back:', err);
            postMessage(text);
          });
        return;
      }
    }

    // SignalR not available yet — fall back to HTTP POST
    postMessage(text);
  }

  // ── SignalR ─────────────────────────────────────────────────────
  function initHub() {
    if (typeof signalR === 'undefined') return false;
    try {
      connection = new signalR.HubConnectionBuilder()
        .withUrl(CONNECTION_URL)
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();

      connection.on('ReceiveMessage', function (data) { addMessage(data); });
      connection.on('ChatError',     function (err)  { showError(err); });
      connection.on('LoadHistory',   function (hist) {
        if (!Array.isArray(hist)) return;
        var sys = msgList && msgList.querySelector('.chat-system-msg');
        if (sys && hist.length > 0) sys.remove();
        hist.forEach(function (m) {
          var row = createMsgRow(m); row.id = 'msg-' + m.id;
          if (msgList) msgList.appendChild(row);
        });
        scrollBottom();
        setStatus('connected');
      });

      connection.start().then(function () { setStatus('connected'); })
                    .catch(function (err) {
        console.warn('[Chat] Hub start failed:', err);
        setStatus('disconnected');
        // Retry after back-off
        setTimeout(initHub, 5000);
      });
      return true;
    } catch (e) {
      console.warn('[Chat] Hub init error:', e);
      return false;
    }
  }

  // Poll until signalR script has loaded (covers slow-CDN cases)
  function waitForSignalR() {
    if (!signalR) { _pollTimer = setTimeout(waitForSignalR, 200); return; }
    setStatus('connecting');
    var ok = initHub();
    if (!ok) setTimeout(waitForSignalR, 500);
  }

  // ── Event bindings ──────────────────────────────────────────────
  document.addEventListener('DOMContentLoaded', function () {

    // Form submit → SignalR (no page reload)
    if (form) {
      form.addEventListener('submit', function (e) {
        e.preventDefault();
        doSend();
      });
    }

    if (fab)      fab.addEventListener('click',    function () { toggleChat(!isChatOpen); saveName(); });
    if (closeBtn) closeBtn.addEventListener('click',  function () { toggleChat(false); });
    if (overlay)  overlay.addEventListener('click',   function () { toggleChat(false); });
    if (nameInput) nameInput.addEventListener('change', saveName);

    if (input) {
      input.addEventListener('input', function () {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 90) + 'px';
      });
      input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
          e.preventDefault();
          if (form) form.dispatchEvent(new Event('submit', { cancelable: true }));
        }
      });
    }

    // Kick off SignalR (it should already be in window from the <head> CDN)
    waitForSignalR();
  });

})();
