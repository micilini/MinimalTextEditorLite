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
import SimpleImage from '@editorjs/simple-image';
import Undo from 'editorjs-undo';

import './editorJS.css';

const DEBOUNCE_MS = 400;
const MAX_IMAGE_BYTES = 50 * 1024 * 1024;

const ALLOWED_IMAGE_TYPES = new Set([
  'image/png',
  'image/jpeg',
  'image/gif',
  'image/webp',
  'image/svg+xml',
]);

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

function isSupportedImageFile(file) {
  return file && ALLOWED_IMAGE_TYPES.has(file.type) && file.size <= MAX_IMAGE_BYTES;
}

function readFileAsDataUrl(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = () => resolve(reader.result);
    reader.onerror = () => reject(reader.error || new Error('Could not read image file.'));

    reader.readAsDataURL(file);
  });
}

async function insertImageFile(editor, file) {
  if (!isSupportedImageFile(file)) {
    postToHost({
      event: 'editorError',
      data: {
        action: 'insertImage',
        message: 'Unsupported image file or image is too large.',
      },
    });

    return false;
  }

  const dataUrl = await readFileAsDataUrl(file);

  await editor.isReady;

  editor.blocks.insert('image', {
    url: dataUrl,
    caption: file.name || '',
  });

  return true;
}

function getImageFilesFromDataTransfer(dataTransfer) {
  if (!dataTransfer) {
    return [];
  }

  const files = Array.from(dataTransfer.files || []);
  return files.filter((file) => file.type && file.type.startsWith('image/'));
}

async function blobUrlToDataUrl(url) {
  const response = await fetch(url);
  const blob = await response.blob();

  return await new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result);
    reader.onerror = () => reject(reader.error || new Error('Could not read blob image.'));
    reader.readAsDataURL(blob);
  });
}

async function normalizeImagesBeforePost(document) {
  const normalizedBlocks = [];

  for (const block of document.blocks || []) {
    if (block.type !== 'image') {
      normalizedBlocks.push(block);
      continue;
    }

    const url = block.data?.url || '';

    if (url.startsWith('blob:')) {
      try {
        const dataUrl = await blobUrlToDataUrl(url);
        normalizedBlocks.push({
          ...block,
          data: {
            ...block.data,
            url: dataUrl,
          },
        });
        continue;
      } catch (error) {
        console.log('[MTEBridge] Could not convert blob image:', error);
      }
    }

    normalizedBlocks.push(block);
  }

  return {
    ...document,
    blocks: normalizedBlocks,
  };
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
        image: SimpleImage,
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
        const normalizedOutputData = await normalizeImagesBeforePost(outputData);
        postToHost(normalizedOutputData);
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

    async function handleImageDrop(event) {
      const imageFiles = getImageFilesFromDataTransfer(event.dataTransfer);

      if (imageFiles.length === 0) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      try {
        for (const file of imageFiles) {
          await insertImageFile(editor, file);
        }

        saveDebounced();
      } catch (error) {
        console.log('[MTEBridge] Error inserting dropped image:', error);
        postToHost({
          event: 'editorError',
          data: {
            action: 'dropImage',
            message: String(error && error.message ? error.message : error),
          },
        });
      }
    }

    async function handleImagePaste(event) {
      const items = Array.from(event.clipboardData?.items || []);
      const imageFiles = items
        .filter((item) => item.kind === 'file' && item.type.startsWith('image/'))
        .map((item) => item.getAsFile())
        .filter(Boolean);

      if (imageFiles.length === 0) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      try {
        for (const file of imageFiles) {
          await insertImageFile(editor, file);
        }

        saveDebounced();
      } catch (error) {
        console.log('[MTEBridge] Error inserting pasted image:', error);
        postToHost({
          event: 'editorError',
          data: {
            action: 'pasteImage',
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
        },
      });
    }

    window.addEventListener('drop', handleImageDrop, true);
    window.addEventListener('paste', handleImagePaste, true);

    return () => {
      if (saveTimer.current) {
        clearTimeout(saveTimer.current);
      }

      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.removeEventListener('message', handleHostMessage);
      }

      window.removeEventListener('drop', handleImageDrop, true);
      window.removeEventListener('paste', handleImagePaste, true);

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
