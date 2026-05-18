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

  // ── State ───────────────────────────────────────────────────────
  var unreadCount = 0;

  // ── Restore visitor name ────────────────────────────────────────
  var savedName = localStorage.getItem(STORAGE_KEY_NAME);
  if (savedName && nameInput) { nameInput.value = savedName; }

  // ── Helpers ─────────────────────────────────────────────────────
  function saveVisitorName() {
    if (nameInput && nameInput.value.trim())
      localStorage.setItem(STORAGE_KEY_NAME, nameInput.value.trim());
  }

  function scrollBottom() { msgList.scrollTop = msgList.scrollHeight; }

  function createMsgRow(data) {
    var row  = document.createElement('div');
    row.className = 'msg-row ' + (data.senderIsAdmin ? 'admin' : 'visitor');
    var hasSender = data.senderName && data.senderName !== 'Visitor';

    var bubble = document.createElement('div');
    bubble.className = 'msg-bubble';
    bubble.textContent = data.messageText;

    var meta = document.createElement('div');
    meta.className = 'msg-meta';
    var name = hasSender ? data.senderName + '  ' : '';
    meta.textContent = name + data.sentAt;
    if (data.isRead) meta.textContent += '  \u2713';

    row.appendChild(bubble);
    row.appendChild(meta);
    return row;
  }

  function addMessage(data) {
    if (document.getElementById('msg-' + data.id)) return;
    if (!isChatOpen && !data.senderIsAdmin) unreadCount++;
    updateBadge();
    msgList.appendChild(createMsgRow(data));
    scrollBottom();
    setChatStatus('connected');
  }

  function updateBadge() {
    if (!badge) return;
    badge.style.display = (unreadCount > 0) ? 'block' : 'none';
    badge.textContent   = (unreadCount > 9) ? '9+' : unreadCount;
  }

  function clearUnread() { unreadCount = 0; updateBadge(); }

  function toggleChat(open) {
    isChatOpen = open;
    panel   && panel.classList.toggle('open', open);
    overlay && overlay.classList.toggle('open', open);
    if (fabIcon) fabIcon.className = open ? 'fas fa-times' : 'fas fa-comment-dots';
    if (open) {
      clearUnread();
      input && input.focus();
      scrollBottom();
    }
  }

  function setChatStatus(state) {
    var dot   = document.getElementById('chatStatusDot');
    var label = document.getElementById('chatStatusText');
    if (dot)   dot.className   = 'status-dot ' + state;
    if (label) label.textContent =
      state === 'connected'   ? 'Connected' :
      state === 'connecting'  ? 'Connecting\u2026' :
                                'Disconnected';
  }

  function doSend() {
    var text = input ? input.value.trim() : '';
    if (!text || !connection || connection.state !== 'Connected') return;

    var name = (nameInput && nameInput.value.trim()) || '';
    connection.invoke('SendMessage', name, false, text)
      .then(function () {
        if (input) { input.value = ''; input.style.height = 'auto'; input.focus(); }
        clearUnread();
      })
      .catch(function (err) { console.warn('[Chat] SendMessage failed:', err); });
  }

  // ── SignalR ─────────────────────────────────────────────────────
  function startHub() {
    if (typeof signalR === 'undefined') { setChatStatus('connecting'); return; }

    setChatStatus('connecting');

    connection = new signalR.HubConnectionBuilder()
      .withUrl('/chatHub')
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', function (data) {
      addMessage(data);
    });

    connection.on('ChatError', function (err) {
      var errDiv = document.createElement('div');
      errDiv.className = 'system-error';
      errDiv.style.cssText = 'text-align:center;font-size:.75rem;color:#e53935;font-family:sans-serif;padding:.25rem 0 .5rem;';
      errDiv.textContent = err;
      msgList && msgList.appendChild(errDiv);
      scrollBottom();
    });

    connection.on('LoadHistory', function (history) {
      if (!Array.isArray(history)) return;
      var sys = msgList && msgList.querySelector('.chat-system-msg');
      if (sys && history.length > 0) sys.remove();
      history.forEach(function (m) {
        var row = createMsgRow(m);
        row.id = 'msg-' + m.id;
        msgList && msgList.appendChild(row);
      });
      scrollBottom();
      setChatStatus('connected');
    });

    connection.start().catch(function (err) {
      console.warn('[Chat] SignalR start failed:', err);
      setChatStatus('disconnected');
    });
  }

  // ── Event bindings ──────────────────────────────────────────────
  document.addEventListener('DOMContentLoaded', function () {

    // Form → SignalR (no page reload)
    if (form) {
      form.addEventListener('submit', function (e) {
        e.preventDefault();
        doSend();
      });
    }

    if (fab)      fab.addEventListener('click',    function () { toggleChat(!isChatOpen); saveVisitorName(); });
    if (closeBtn) closeBtn.addEventListener('click',  function () { toggleChat(false); });
    if (overlay)  overlay.addEventListener('click',   function () { toggleChat(false); });
    if (nameInput) nameInput.addEventListener('change', saveVisitorName);

    if (input) {
      input.addEventListener('input', function () {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 90) + 'px';
      });
      input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
          e.preventDefault();
          form && form.dispatchEvent(new Event('submit', { cancelable: true }));
        }
      });
    }

    startHub();
  });

})();
