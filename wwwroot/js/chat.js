// ── SignalR Live Chat ─────────────────────────────────────────── //
(function () {
  'use strict';

  var CONNECTION_URL = document.location.origin + '/chatHub';
  var STORAGE_KEY_NAME = 'istoriaChatVisitorName';
  var isChatOpen = false;
  var connection = null;

  // ── DOM refs ────────────────────────────────────────────────────
  var fab        = document.getElementById('chatFab');
  var fabIcon    = document.getElementById('chatFabIcon');
  var badge      = document.getElementById('chatBadge');
  var panel      = document.getElementById('chatPanel');
  var overlay    = document.getElementById('chatPanelOverlay');
  var closeBtn   = document.getElementById('chatClose');
  var msgList    = document.getElementById('chatMessages');
  var input      = document.getElementById('chatMessageInput');
  var form       = document.getElementById('chatForm');
  var nameInput  = document.getElementById('chatVisitorName');

  // ── State ───────────────────────────────────────────────────────
  var unreadCount = 0;

  // ── Restore visitor name ────────────────────────────────────────
  var savedName = localStorage.getItem(STORAGE_KEY_NAME);
  if (savedName && nameInput) {
    nameInput.value = savedName;
  }

  // ── Helpers ─────────────────────────────────────────────────────
  function saveVisitorName() {
    if (nameInput && nameInput.value.trim()) {
      localStorage.setItem(STORAGE_KEY_NAME, nameInput.value.trim());
    }
  }

  function scrollBottom() {
    msgList.scrollTop = msgList.scrollHeight;
  }

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
    var existing = document.getElementById('msg-' + data.id);
    if (existing) return; // avoid duplicates

    if ((!isChatOpen) && !data.senderIsAdmin) unreadCount++;
    updateBadge();

    msgList.appendChild(createMsgRow(data));
    scrollBottom();
  }

  function updateBadge() {
    if (!badge) return;
    if (unreadCount > 0) {
      badge.style.display = 'block';
      badge.textContent = unreadCount > 9 ? '9+' : unreadCount;
    } else {
      badge.style.display = 'none';
    }
  }

  function clearUnread() { unreadCount = 0; updateBadge(); }

  function toggleChat(open) {
    isChatOpen = open;
    if (panel)  panel.classList.toggle('open', open);
    if (overlay) overlay.classList.toggle('open', open);
    if (fabIcon) fabIcon.className = open ? 'fas fa-times' : 'fas fa-comment-dots';
    if (open) {
      clearUnread();
      if (input) input.focus();
      scrollBottom();
    }
  }

  // ── SignalR ─────────────────────────────────────────────────────
  function startHub() {
    if (typeof signalR === 'undefined') return;

    connection = new signalR.HubConnectionBuilder()
      .withUrl('/chatHub')
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', function (data) {
      addMessage(data);
    });

    connection.on('LoadHistory', function (history) {
      if (!Array.isArray(history)) return;
      // Remove system welcome message if history exists
      var sys = msgList.querySelector('.chat-system-msg');
      if (sys && history.length > 1) sys.remove();
      history.forEach(function (m) {
        var row = createMsgRow(m);
        row.id = 'msg-' + m.id;
        msgList.appendChild(row);
      });
      scrollBottom();
    });

    connection.start().catch(function (err) {
      console.warn('[Chat] SignalR connection failed:', err);
    });
  }

  // ── Event bindings ──────────────────────────────────────────────
  document.addEventListener('DOMContentLoaded', function () {
    if (fab)  fab.addEventListener('click', function () { toggleChat(!isChatOpen); saveVisitorName(); });
    if (closeBtn) closeBtn.addEventListener('click', function () { toggleChat(false); });
    if (overlay) overlay.addEventListener('click', function () { toggleChat(false); });
    if (nameInput) nameInput.addEventListener('change', saveVisitorName);

    // Auto-resize textarea
    if (input) {
      input.addEventListener('input', function () {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 90) + 'px';
      });

      input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
          e.preventDefault();
          form.dispatchEvent(new Event('submit', { cancelable: true }));
        }
      });
    }

    startHub();
  });

})();
