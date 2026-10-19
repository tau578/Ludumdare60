// The browser side of BrowserStorage.cs (Step 9b, D26): saves in localStorage, and a hook that saves when the page
// is hidden. Every localStorage access is wrapped in try/catch: blocked site data, blocked third-party storage (itch
// runs the game in an iframe) and some private modes throw, and a failure must come back as a status, not a crash.
var OperatorBrowserStorage = {
  $OperatorStorage: {
    warnings: {},
    hiddenCallback: 0,
    listening: false
  },

  OperatorStorage_IsAvailable: function () {
    try {
      var probe = "operator.probe";
      window.localStorage.setItem(probe, "1");
      window.localStorage.removeItem(probe);
      return 1;
    } catch (e) {
      return 0;
    }
  },

  // Returns null (0) when the key holds nothing or can't be read. Unity frees the returned buffer.
  OperatorStorage_Read: function (keyPtr) {
    var value = null;
    try {
      value = window.localStorage.getItem(UTF8ToString(keyPtr));
    } catch (e) {
      value = null;
    }
    if (value === null) return 0;
    var size = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(size);
    stringToUTF8(value, buffer, size);
    return buffer;
  },

  OperatorStorage_Write: function (keyPtr, valuePtr) {
    try {
      window.localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valuePtr));
      return 1;
    } catch (e) {
      return 0;
    }
  },

  OperatorStorage_Delete: function (keyPtr) {
    try {
      window.localStorage.removeItem(UTF8ToString(keyPtr));
      return 1;
    } catch (e) {
      return 0;
    }
  },

  // 1 the first time a warning id is claimed in this page load, 0 after that, so each warning is logged once.
  OperatorStorage_ClaimWarning: function (id) {
    if (OperatorStorage.warnings[id]) return 0;
    OperatorStorage.warnings[id] = true;
    return 1;
  },

  // A hidden tab runs no frames and phones may kill it, so the save is written right inside the event (5.12).
  // callback 0 removes the handler; the listeners stay and do nothing.
  OperatorPage_SetHiddenCallback__deps: ['$getWasmTableEntry'],
  OperatorPage_SetHiddenCallback: function (callback) {
    OperatorStorage.hiddenCallback = callback;
    if (OperatorStorage.listening) return;
    OperatorStorage.listening = true;
    var onHidden = function () {
      var cb = OperatorStorage.hiddenCallback;
      if (!cb) return;
      try {
        {{{ makeDynCall('v', 'cb') }}}();
      } catch (e) {
        console.warn("Operator: saving when the page was hidden failed.", e);
      }
    };
    document.addEventListener("visibilitychange", function () {
      if (document.visibilityState === "hidden") onHidden();
    });
    window.addEventListener("pagehide", onHidden);
  }
};

autoAddDeps(OperatorBrowserStorage, '$OperatorStorage');
mergeInto(LibraryManager.library, OperatorBrowserStorage);
