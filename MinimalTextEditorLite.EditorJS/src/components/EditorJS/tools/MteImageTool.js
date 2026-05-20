import './mteImageTool.css';

const MAX_IMAGE_BYTES = 50 * 1024 * 1024;

const IMAGE_MIME_BY_EXTENSION = {
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.webp': 'image/webp',
  '.svg': 'image/svg+xml',
};

const ALLOWED_IMAGE_TYPES = new Set(Object.values(IMAGE_MIME_BY_EXTENSION));

function getFileExtension(fileName) {
  if (!fileName || typeof fileName !== 'string') {
    return '';
  }

  const lastDotIndex = fileName.lastIndexOf('.');

  if (lastDotIndex < 0) {
    return '';
  }

  return fileName.slice(lastDotIndex).toLowerCase();
}

function getMimeTypeFromFileName(fileName) {
  return IMAGE_MIME_BY_EXTENSION[getFileExtension(fileName)] || '';
}

function getMimeTypeFromFile(file) {
  if (!file) {
    return '';
  }

  if (file.type && ALLOWED_IMAGE_TYPES.has(file.type)) {
    return file.type;
  }

  return getMimeTypeFromFileName(file.name);
}

function isSupportedImageFile(file) {
  const mimeType = getMimeTypeFromFile(file);
  return Boolean(file && mimeType && file.size <= MAX_IMAGE_BYTES);
}

function arrayBufferToBase64(buffer) {
  const bytes = new Uint8Array(buffer);
  const chunkSize = 0x8000;
  let binary = '';

  for (let index = 0; index < bytes.length; index += chunkSize) {
    const chunk = bytes.subarray(index, index + chunkSize);
    binary += String.fromCharCode.apply(null, chunk);
  }

  return btoa(binary);
}

function readFileAsDataUrl(file) {
  return new Promise((resolve, reject) => {
    const mimeType = getMimeTypeFromFile(file);

    if (!mimeType) {
      reject(new Error('Unsupported image type.'));
      return;
    }

    const reader = new FileReader();

    reader.onload = () => {
      try {
        const base64 = arrayBufferToBase64(reader.result);
        resolve(`data:${mimeType};base64,${base64}`);
      } catch (error) {
        reject(error);
      }
    };

    reader.onerror = () => reject(reader.error || new Error('Could not read image file.'));
    reader.readAsArrayBuffer(file);
  });
}

function getImageDimensions(url) {
  return new Promise((resolve) => {
    const image = new Image();

    image.onload = () => {
      resolve({
        width: image.naturalWidth || image.width || null,
        height: image.naturalHeight || image.height || null,
      });
    };

    image.onerror = () => {
      resolve({
        width: null,
        height: null,
      });
    };

    image.src = url;
  });
}

function isAllowedPersistedUrl(url) {
  if (!url || typeof url !== 'string') {
    return false;
  }

  return (
    url.startsWith('data:image/png;base64,') ||
    url.startsWith('data:image/jpeg;base64,') ||
    url.startsWith('data:image/gif;base64,') ||
    url.startsWith('data:image/webp;base64,') ||
    url.startsWith('data:image/svg+xml;base64,') ||
    url.startsWith('http://') ||
    url.startsWith('https://')
  );
}

function sanitizeCaption(value) {
  if (!value || typeof value !== 'string') {
    return '';
  }

  return value.trim();
}

function normalizeData(data) {
  return {
    url: typeof data?.url === 'string' ? data.url : '',
    caption: typeof data?.caption === 'string' ? data.caption : '',
    width: Number.isFinite(data?.width) ? data.width : null,
    height: Number.isFinite(data?.height) ? data.height : null,
    fileName: typeof data?.fileName === 'string' ? data.fileName : '',
    mimeType: typeof data?.mimeType === 'string' ? data.mimeType : '',
    size: Number.isFinite(data?.size) ? data.size : null,
  };
}

export default class MteImageTool {
  static get toolbox() {
    return {
      title: 'Image',
      icon: '<svg width="17" height="15" viewBox="0 0 17 15" xmlns="http://www.w3.org/2000/svg"><path d="M15.25 0H1.75C.78 0 0 .78 0 1.75v11.5C0 14.22.78 15 1.75 15h13.5c.97 0 1.75-.78 1.75-1.75V1.75C17 .78 16.22 0 15.25 0ZM1.5 1.75c0-.14.11-.25.25-.25h13.5c.14 0 .25.11.25.25v8.49l-3.1-3.1a1.25 1.25 0 0 0-1.77 0L7.9 9.87 6.63 8.6a1.25 1.25 0 0 0-1.77 0L1.5 11.96V1.75Zm.25 11.75a.25.25 0 0 1-.25-.25v-.17l4.24-4.24 4.66 4.66H1.75Zm13.5 0h-2.73L8.96 9.94l2.55-2.55 3.99 3.99v1.87c0 .14-.11.25-.25.25ZM5.25 5.5A1.75 1.75 0 1 1 5.25 2a1.75 1.75 0 0 1 0 3.5Z"/></svg>',
    };
  }

  static get pasteConfig() {
    return {
      files: {
        mimeTypes: [
          'image/png',
          'image/jpeg',
          'image/gif',
          'image/webp',
          'image/svg+xml',
        ],
        extensions: [
          'png',
          'jpg',
          'jpeg',
          'gif',
          'webp',
          'svg',
        ],
      },
      tags: [
        {
          img: {
            src: true,
            alt: true,
          },
        },
      ],
      patterns: {
        image: /https?:\/\/\S+\.(png|jpe?g|gif|webp|svg)(\?\S*)?$/i,
      },
    };
  }

  static get sanitize() {
    return {
      url: false,
      caption: true,
      width: false,
      height: false,
      fileName: true,
      mimeType: false,
      size: false,
    };
  }

  static get isReadOnlySupported() {
    return true;
  }

  static async createDataFromFile(file) {
    if (!isSupportedImageFile(file)) {
      throw new Error('Unsupported image file or image is too large.');
    }

    const url = await readFileAsDataUrl(file);
    const dimensions = await getImageDimensions(url);
    const mimeType = getMimeTypeFromFile(file);

    return {
      url,
      caption: file.name || '',
      width: dimensions.width,
      height: dimensions.height,
      fileName: file.name || '',
      mimeType,
      size: file.size || null,
    };
  }

  constructor({ data, readOnly }) {
    this.data = normalizeData(data);
    this.readOnly = readOnly;
    this.wrapper = null;
    this.fileInput = null;
    this.captionInput = null;
  }

  render() {
    this.wrapper = document.createElement('div');
    this.wrapper.className = 'mte-image-tool';

    this.renderContent();

    return this.wrapper;
  }

  save() {
    const caption = this.captionInput
      ? this.captionInput.value
      : this.data.caption;

    return {
      url: this.data.url,
      caption: sanitizeCaption(caption),
      width: this.data.width,
      height: this.data.height,
      fileName: this.data.fileName,
      mimeType: this.data.mimeType,
      size: this.data.size,
    };
  }

  validate(savedData) {
    return isAllowedPersistedUrl(savedData?.url);
  }

  async onPaste(event) {
    const { type, file, data, key } = event.detail || {};

    if (type === 'file' && file) {
      await this.setFile(file);
      return;
    }

    if (type === 'tag' && data?.tagName === 'IMG') {
      const src = data.getAttribute('src') || '';
      const alt = data.getAttribute('alt') || '';

      if (isAllowedPersistedUrl(src)) {
        await this.setUrl(src, alt);
      }

      return;
    }

    if (type === 'pattern' && key === 'image') {
      await this.setUrl(data, '');
    }
  }

  renderContent() {
    if (!this.wrapper) {
      return;
    }

    this.wrapper.innerHTML = '';

    if (this.data.url && isAllowedPersistedUrl(this.data.url)) {
      this.renderPreview();
      return;
    }

    this.renderEmptyState();
  }

  renderEmptyState() {
    const dropZone = document.createElement('div');
    dropZone.className = 'mte-image-tool__dropzone';

    const title = document.createElement('div');
    title.className = 'mte-image-tool__title';
    title.textContent = 'Drop an image here';

    const description = document.createElement('div');
    description.className = 'mte-image-tool__description';
    description.textContent = 'PNG, JPG, GIF, WEBP or SVG up to 50 MB';

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'mte-image-tool__button';
    button.textContent = 'Choose image';
    button.disabled = this.readOnly;

    this.fileInput = document.createElement('input');
    this.fileInput.type = 'file';
    this.fileInput.accept = 'image/png,image/jpeg,image/gif,image/webp,image/svg+xml';
    this.fileInput.className = 'mte-image-tool__input';
    this.fileInput.disabled = this.readOnly;

    button.addEventListener('click', () => {
      if (!this.readOnly) {
        this.fileInput.click();
      }
    });

    this.fileInput.addEventListener('change', async () => {
      const file = this.fileInput.files?.[0];

      if (file) {
        await this.setFile(file);
      }
    });

    dropZone.addEventListener('dragover', (event) => {
      if (this.readOnly) {
        return;
      }

      event.preventDefault();
      dropZone.classList.add('mte-image-tool__dropzone--active');
    });

    dropZone.addEventListener('dragleave', () => {
      dropZone.classList.remove('mte-image-tool__dropzone--active');
    });

    dropZone.addEventListener('drop', async (event) => {
      if (this.readOnly) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      dropZone.classList.remove('mte-image-tool__dropzone--active');

      const file = Array.from(event.dataTransfer?.files || [])
        .find(isSupportedImageFile);

      if (file) {
        await this.setFile(file);
      }
    });

    dropZone.appendChild(title);
    dropZone.appendChild(description);
    dropZone.appendChild(button);
    dropZone.appendChild(this.fileInput);
    this.wrapper.appendChild(dropZone);
  }

  renderPreview() {
    const figure = document.createElement('figure');
    figure.className = 'mte-image-tool__figure';

    const image = document.createElement('img');
    image.className = 'mte-image-tool__image';
    image.src = this.data.url;
    image.alt = this.data.caption || this.data.fileName || '';

    const meta = document.createElement('div');
    meta.className = 'mte-image-tool__meta';

    const dimensions = this.data.width && this.data.height
      ? `${this.data.width} x ${this.data.height}`
      : '';

    const fileName = this.data.fileName || this.data.caption || '';
    meta.textContent = [fileName, dimensions].filter(Boolean).join(' - ');

    this.captionInput = document.createElement('input');
    this.captionInput.className = 'mte-image-tool__caption';
    this.captionInput.placeholder = 'Caption';
    this.captionInput.value = this.data.caption || '';
    this.captionInput.disabled = this.readOnly;

    const actions = document.createElement('div');
    actions.className = 'mte-image-tool__actions';

    const replaceButton = document.createElement('button');
    replaceButton.type = 'button';
    replaceButton.className = 'mte-image-tool__action';
    replaceButton.textContent = 'Replace';
    replaceButton.disabled = this.readOnly;

    const removeButton = document.createElement('button');
    removeButton.type = 'button';
    removeButton.className = 'mte-image-tool__action mte-image-tool__action--danger';
    removeButton.textContent = 'Remove';
    removeButton.disabled = this.readOnly;

    this.fileInput = document.createElement('input');
    this.fileInput.type = 'file';
    this.fileInput.accept = 'image/png,image/jpeg,image/gif,image/webp,image/svg+xml';
    this.fileInput.className = 'mte-image-tool__input';
    this.fileInput.disabled = this.readOnly;

    replaceButton.addEventListener('click', () => {
      if (!this.readOnly) {
        this.fileInput.click();
      }
    });

    removeButton.addEventListener('click', () => {
      if (!this.readOnly) {
        this.data = normalizeData({});
        this.renderContent();
      }
    });

    this.fileInput.addEventListener('change', async () => {
      const file = this.fileInput.files?.[0];

      if (file) {
        await this.setFile(file);
      }
    });

    actions.appendChild(replaceButton);
    actions.appendChild(removeButton);
    actions.appendChild(this.fileInput);

    figure.appendChild(image);
    figure.appendChild(meta);
    figure.appendChild(this.captionInput);
    figure.appendChild(actions);

    this.wrapper.appendChild(figure);
  }

  async setFile(file) {
    try {
      this.data = normalizeData(await MteImageTool.createDataFromFile(file));
      this.renderContent();
    } catch (error) {
      this.renderError(error);
    }
  }

  async setUrl(url, caption) {
    if (!isAllowedPersistedUrl(url)) {
      this.renderError(new Error('Unsupported image URL.'));
      return;
    }

    const dimensions = await getImageDimensions(url);

    this.data = normalizeData({
      url,
      caption,
      width: dimensions.width,
      height: dimensions.height,
      fileName: caption || '',
      mimeType: '',
      size: null,
    });

    this.renderContent();
  }

  renderError(error) {
    if (!this.wrapper) {
      return;
    }

    const message = document.createElement('div');
    message.className = 'mte-image-tool__error';
    message.textContent = error?.message || 'Could not insert image.';

    this.wrapper.appendChild(message);

    setTimeout(() => {
      if (message.parentNode) {
        message.parentNode.removeChild(message);
      }
    }, 5000);
  }
}
