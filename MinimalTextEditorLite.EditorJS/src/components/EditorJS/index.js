import React, { useEffect, useRef } from 'react';
import EditorJS from '@editorjs/editorjs';
import Header from '@editorjs/header';
import List from '@editorjs/list';
import Checklist from '@editorjs/checklist';
import Quote from '@editorjs/quote';
import Warning from '@editorjs/warning';
import Marker from '@editorjs/marker';
import Code from '@editorjs/code';
import Delimiter from '@editorjs/delimiter';
import InlineCode from '@editorjs/inline-code';
import LinkTool from '@editorjs/link';
import Embed from '@editorjs/embed';
import Table from '@editorjs/table';
import Undo from 'editorjs-undo';
import MteImageTool, {
  createMteImageDataFromFile,
  isSupportedMteImageFile,
} from './tools/MteImageTool';

import './editorJS.css';

const DEBOUNCE_MS = 400;

function normalizeDocument(data) {
  if (!data) {
    return { blocks: [] };
  }

  if (typeof data === 'string') {
    try {
      const parsed = JSON.parse(data);
      return normalizeDocument(parsed);
    } catch (error) {
      console.log('[MTEBridge] Error parsing JSON:', error);
      return { blocks: [] };
    }
  }

  if (!Array.isArray(data.blocks)) {
    return { blocks: [] };
  }

  return data;
}

function postToHost(message) {
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.postMessage(message);
  }
}

const EditorJSComponent = () => {
  const editorInstance = useRef(null);
  const saveTimer = useRef(null);
  const lastSaveRequestId = useRef(0);

  useEffect(() => {
    const editor = new EditorJS({
      holder: 'editorjs',
      tools: {
        header: {
          class: Header,
          inlineToolbar: ['marker', 'link'],
          config: { placeholder: 'Header' },
          shortcut: 'CMD+SHIFT+H',
        },
        image: {
          class: MteImageTool,
        },
        list: { class: List, inlineToolbar: true, shortcut: 'CMD+SHIFT+L' },
        checklist: { class: Checklist, inlineToolbar: true },
        quote: {
          class: Quote,
          inlineToolbar: true,
          config: {
            quotePlaceholder: 'Enter a quote',
            captionPlaceholder: "Quote's author",
          },
          shortcut: 'CMD+SHIFT+O',
        },
        warning: Warning,
        marker: { class: Marker, shortcut: 'CMD+SHIFT+M' },
        code: { class: Code, shortcut: 'CMD+SHIFT+C' },
        delimiter: Delimiter,
        inlineCode: { class: InlineCode, shortcut: 'CMD+SHIFT+C' },
        linkTool: LinkTool,
        embed: Embed,
        table: { class: Table, inlineToolbar: true, shortcut: 'CMD+ALT+T' },
      },
      data: { blocks: [] },
      onReady: () => {
        new Undo({ editor });
        window.__editorInstance = editor;

        postToHost({
          event: 'editorReady',
          data: { source: 'react' },
        });

        console.log('Editor.js is ready to work!');
      },
    });

    editorInstance.current = editor;

    async function save() {
      if (!editorInstance.current) {
        return;
      }

      try {
        await editorInstance.current.isReady;
        const outputData = await editorInstance.current.save();
        postToHost(outputData);
      } catch (error) {
        console.log('[MTEBridge] Error on saving:', error);
        postToHost({
          event: 'editorError',
          data: {
            action: 'save',
            message: String(error && error.message ? error.message : error),
          },
        });
      }
    }

    async function load(data) {
      if (!editorInstance.current) {
        return;
      }

      try {
        const normalized = normalizeDocument(data);
        await editorInstance.current.isReady;
        await editorInstance.current.render(normalized);
        console.log('Editor loaded with new data');
      } catch (error) {
        console.log('[MTEBridge] Error loading data into the editor:', error);
        postToHost({
          event: 'editorError',
          data: {
            action: 'load',
            message: String(error && error.message ? error.message : error),
          },
        });
      }
    }

    async function clear() {
      if (!editorInstance.current) {
        return;
      }

      try {
        await editorInstance.current.isReady;
        await editorInstance.current.clear();
        console.log('Editor cleared');
      } catch (error) {
        console.log('[MTEBridge] Error on cleaning:', error);
        await load({ blocks: [] });
      }
    }

    function saveDebounced() {
      const requestId = ++lastSaveRequestId.current;

      if (saveTimer.current) {
        clearTimeout(saveTimer.current);
      }

      saveTimer.current = setTimeout(() => {
        if (requestId !== lastSaveRequestId.current) {
          return;
        }

        save();
      }, DEBOUNCE_MS);
    }

    function setTheme(themeName) {
      const theme = themeName === 'dark' ? 'dark' : 'light';
      document.body.classList.remove('mte-theme-light', 'mte-theme-dark');
      document.body.classList.add(`mte-theme-${theme}`);
    }

    function getStats() {
      const text = document.body.innerText || '';
      const words = text.trim().split(/\s+/).filter(Boolean).length;
      const readingTimeMin = Math.max(1, Math.round(words / 200));

      return {
        words,
        readingTimeMin,
      };
    }

    function getDroppedImageFiles(event) {
      const files = Array.from(event.dataTransfer?.files || []);
      return files.filter(isSupportedMteImageFile);
    }

    function getPastedImageFiles(event) {
      const items = Array.from(event.clipboardData?.items || []);
      return items
        .filter((item) => item.kind === 'file')
        .map((item) => item.getAsFile())
        .filter(Boolean)
        .filter(isSupportedMteImageFile);
    }

    function stopNativeImageEvent(event) {
      event.preventDefault();
      event.stopPropagation();

      if (typeof event.stopImmediatePropagation === 'function') {
        event.stopImmediatePropagation();
      }
    }

    function isInsideMteImageTool(event) {
      return Boolean(event.target?.closest?.('.mte-image-tool'));
    }

    function handleGlobalImageDragOver(event) {
      const files = getDroppedImageFiles(event);

      if (files.length === 0) {
        return;
      }

      stopNativeImageEvent(event);
    }

    async function handleGlobalImageDrop(event) {
      const files = getDroppedImageFiles(event);

      if (files.length === 0) {
        return;
      }

      if (isInsideMteImageTool(event)) {
        return;
      }

      stopNativeImageEvent(event);

      try {
        await editor.isReady;

        for (const file of files) {
          const imageData = await createMteImageDataFromFile(file);
          editor.blocks.insert('image', imageData);
        }

        saveDebounced();
      } catch (error) {
        console.log('[MTEBridge] Error inserting dropped image globally:', error);
        postToHost({
          event: 'editorError',
          data: {
            action: 'globalDropImage',
            message: String(error && error.message ? error.message : error),
          },
        });
      }
    }

    async function handleGlobalImagePaste(event) {
      const files = getPastedImageFiles(event);

      if (files.length === 0) {
        return;
      }

      stopNativeImageEvent(event);

      try {
        await editor.isReady;

        for (const file of files) {
          const imageData = await createMteImageDataFromFile(file);
          editor.blocks.insert('image', imageData);
        }

        saveDebounced();
      } catch (error) {
        console.log('[MTEBridge] Error inserting pasted image globally:', error);
        postToHost({
          event: 'editorError',
          data: {
            action: 'globalPasteImage',
            message: String(error && error.message ? error.message : error),
          },
        });
      }
    }

    window.MTEBridge = {
      version: '2.0.0-react',

      load,
      save: async () => {
        if (saveTimer.current) {
          clearTimeout(saveTimer.current);
        }

        await save();
      },
      saveDebounced,
      clear,
      setTheme,
      getStats,
      insertImage: async (data) => {
        if (!editorInstance.current) {
          return;
        }

        await editorInstance.current.isReady;

        editorInstance.current.blocks.insert('image', {
          url: data?.url || '',
          caption: data?.caption || '',
          width: data?.width || null,
          height: data?.height || null,
          fileName: data?.fileName || data?.caption || '',
          mimeType: data?.mimeType || '',
          size: data?.size || null,
        });

        saveDebounced();
      },
    };

    window.handleSave = () => window.MTEBridge.save();
    window.handleClear = () => window.MTEBridge.clear();
    window.handleLoad = (jsonData) => window.MTEBridge.load(jsonData);

    function handleHostMessage(event) {
      const msg = event.data;

      if (!msg || typeof msg !== 'object') {
        return;
      }

      switch (msg.action) {
        case 'load':
          window.MTEBridge.load(msg.data);
          break;

        case 'save':
          window.MTEBridge.save();
          break;

        case 'saveDebounced':
          window.MTEBridge.saveDebounced();
          break;

        case 'clear':
          window.MTEBridge.clear();
          break;

        case 'setTheme':
          window.MTEBridge.setTheme(msg.data);
          break;

        case 'insertImage':
          window.MTEBridge.insertImage(msg.data);
          break;

        case 'getStats':
          postToHost({
            event: 'stats',
            data: window.MTEBridge.getStats(),
          });
          break;

        default:
          console.warn('[MTEBridge] Unknown action:', msg.action);
          break;
      }
    }

    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.addEventListener('message', handleHostMessage);

      postToHost({
        event: 'bridgeReady',
        data: {
          version: window.MTEBridge.version,
          imageTool: 'MteImageTool',
          imageNormalization: 'mte-custom-image-tool-v1',
        },
      });
    }

    window.addEventListener('dragover', handleGlobalImageDragOver, true);
    window.addEventListener('drop', handleGlobalImageDrop, true);
    window.addEventListener('paste', handleGlobalImagePaste, true);
    document.addEventListener('dragover', handleGlobalImageDragOver, true);
    document.addEventListener('drop', handleGlobalImageDrop, true);
    document.addEventListener('paste', handleGlobalImagePaste, true);

    return () => {
      if (saveTimer.current) {
        clearTimeout(saveTimer.current);
      }

      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.removeEventListener('message', handleHostMessage);
      }

      window.removeEventListener('dragover', handleGlobalImageDragOver, true);
      window.removeEventListener('drop', handleGlobalImageDrop, true);
      window.removeEventListener('paste', handleGlobalImagePaste, true);
      document.removeEventListener('dragover', handleGlobalImageDragOver, true);
      document.removeEventListener('drop', handleGlobalImageDrop, true);
      document.removeEventListener('paste', handleGlobalImagePaste, true);

      delete window.MTEBridge;
      delete window.handleSave;
      delete window.handleClear;
      delete window.handleLoad;
      delete window.__editorInstance;

      editor.isReady
        .then(() => {
          editor.destroy();
        })
        .catch((error) => console.log('Editor.js was not ready', error));
    };
  }, []);

  return (
    <div className="mte-block">
      <div id="editorjs"></div>
    </div>
  );
};

export default EditorJSComponent;
