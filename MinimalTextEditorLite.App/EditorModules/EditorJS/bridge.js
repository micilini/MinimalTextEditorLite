(function () {
  "use strict";

  const DEBOUNCE_MS = 400;
  let saveTimer = null;
  let lastSaveRequestId = 0;

  function hasFunction(name) {
    return typeof window[name] === "function";
  }

  function callHandleLoad(data) {
    if (!hasFunction("handleLoad")) {
      console.warn("[MTEBridge] window.handleLoad is not available.");
      return;
    }

    const payload = typeof data === "string" ? data : JSON.stringify(data ?? { blocks: [] });
    window.handleLoad(payload);
  }

  function callHandleSave() {
    if (!hasFunction("handleSave")) {
      console.warn("[MTEBridge] window.handleSave is not available.");
      return;
    }

    window.handleSave();
  }

  function callHandleClear() {
    if (hasFunction("handleClear")) {
      window.handleClear();
      return;
    }

    callHandleLoad({ blocks: [] });
  }

  function saveDebounced() {
    const requestId = ++lastSaveRequestId;

    clearTimeout(saveTimer);

    saveTimer = setTimeout(function () {
      if (requestId !== lastSaveRequestId) return;
      callHandleSave();
    }, DEBOUNCE_MS);
  }

  function postToHost(message) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage(message);
    }
  }

  window.MTEBridge = {
    version: "1.0.0",

    load(data) {
      callHandleLoad(data);
    },

    save() {
      clearTimeout(saveTimer);
      callHandleSave();
    },

    saveDebounced() {
      saveDebounced();
    },

    clear() {
      clearTimeout(saveTimer);
      callHandleClear();
    },

    setTheme(themeName) {
      document.body.classList.remove("mte-theme-light", "mte-theme-dark");
      document.body.classList.add("mte-theme-" + (themeName === "dark" ? "dark" : "light"));
    },

    getStats() {
      const text = document.body.innerText || "";
      const words = text.trim().split(/\s+/).filter(Boolean).length;
      const readingTimeMin = Math.max(1, Math.round(words / 200));

      return {
        words,
        readingTimeMin
      };
    }
  };

  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener("message", function (event) {
      const msg = event.data;

      if (!msg || typeof msg !== "object") {
        return;
      }

      switch (msg.action) {
        case "load":
          window.MTEBridge.load(msg.data);
          break;

        case "save":
          window.MTEBridge.save();
          break;

        case "saveDebounced":
          window.MTEBridge.saveDebounced();
          break;

        case "clear":
          window.MTEBridge.clear();
          break;

        case "setTheme":
          window.MTEBridge.setTheme(msg.data);
          break;

        case "getStats":
          postToHost({
            event: "stats",
            data: window.MTEBridge.getStats()
          });
          break;

        default:
          console.warn("[MTEBridge] Unknown action:", msg.action);
          break;
      }
    });

    postToHost({
      event: "bridgeReady",
      data: {
        version: window.MTEBridge.version
      }
    });
  }
})();
