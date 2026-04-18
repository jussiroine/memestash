(function () {
    'use strict';

    const dropZone = document.getElementById('drop-zone');
    const fileInput = document.getElementById('file-input');
    const browseBtn = document.getElementById('browse-btn');
    const uploadProgress = document.getElementById('upload-progress');
    const progressBar = uploadProgress?.querySelector('.progress-bar');
    const uploadStatus = document.getElementById('upload-status');
    const gallery = document.getElementById('meme-gallery');

    if (!dropZone) return;

    // Extract slug from the current URL path
    const slug = window.location.pathname.replace(/^\//, '').split('/')[0];

    // Browse button
    browseBtn?.addEventListener('click', () => fileInput?.click());
    fileInput?.addEventListener('change', () => {
        if (fileInput.files.length > 0) uploadFiles(fileInput.files);
    });

    // Drag and drop
    ['dragenter', 'dragover'].forEach(evt => {
        dropZone.addEventListener(evt, e => {
            e.preventDefault();
            e.stopPropagation();
            dropZone.classList.add('drag-over');
        });
    });

    ['dragleave', 'drop'].forEach(evt => {
        dropZone.addEventListener(evt, e => {
            e.preventDefault();
            e.stopPropagation();
            dropZone.classList.remove('drag-over');
        });
    });

    dropZone.addEventListener('drop', e => {
        const files = e.dataTransfer?.files;
        if (files && files.length > 0) uploadFiles(files);
    });

    async function uploadFiles(files) {
        const total = files.length;
        let completed = 0;

        uploadProgress?.classList.remove('d-none');
        dropZone.querySelector('.drop-zone-content')?.classList.add('d-none');

        for (const file of files) {
            uploadStatus.textContent = `Uploading ${completed + 1} of ${total}: ${file.name}`;
            const pct = Math.round((completed / total) * 100);
            if (progressBar) progressBar.style.width = pct + '%';

            try {
                const formData = new FormData();
                formData.append('file', file);

                const response = await fetch(`/api/memes/${slug}`, {
                    method: 'POST',
                    body: formData
                });

                if (response.ok) {
                    const meme = await response.json();
                    addMemeToGallery(meme);
                } else {
                    const errorText = await response.text();
                    showToast(`Failed to upload ${file.name}: ${errorText}`, 'danger');
                }
            } catch (err) {
                showToast(`Error uploading ${file.name}: ${err.message}`, 'danger');
            }

            completed++;
        }

        if (progressBar) progressBar.style.width = '100%';
        uploadStatus.textContent = `Done! Uploaded ${completed} file(s).`;

        setTimeout(() => {
            uploadProgress?.classList.add('d-none');
            dropZone.querySelector('.drop-zone-content')?.classList.remove('d-none');
            if (progressBar) progressBar.style.width = '0%';
        }, 1500);

        // Remove the "no memes" placeholder
        const placeholder = document.querySelector('.text-center.text-muted.py-5');
        if (placeholder) placeholder.remove();

        // Reset file input
        if (fileInput) fileInput.value = '';

        // Update count badge
        updateCount();
    }

    function addMemeToGallery(meme) {
        if (!gallery) {
            location.reload();
            return;
        }

        const col = document.createElement('div');
        col.className = 'col';
        col.dataset.blob = meme.blobName;
        col.innerHTML = `
            <div class="card h-100 meme-card">
                <img src="${meme.url}" class="card-img-top meme-img" alt="${meme.blobName}" loading="lazy" />
                <div class="card-body p-2">
                    <div class="d-flex justify-content-between align-items-center">
                        <small class="text-muted text-truncate">${meme.blobName}</small>
                        <div class="btn-group btn-group-sm">
                            <button class="btn btn-outline-warning btn-pin" data-slug="${slug}" data-blob="${meme.blobName}" data-pinned="false" title="Pin">📌</button>
                            <button class="btn btn-outline-primary btn-copy-img" data-url="${meme.url}" title="Copy image">🖼️</button>
                            <button class="btn btn-outline-secondary btn-copy" data-url="${meme.url}" title="Copy link">📋</button>
                            <button class="btn btn-outline-danger btn-delete" data-slug="${slug}" data-blob="${meme.blobName}" title="Delete">🗑️</button>
                        </div>
                    </div>
                </div>
            </div>`;

        gallery.prepend(col);
    }

    // Event delegation for pin, copy-image, copy-link, and delete buttons
    document.addEventListener('click', async e => {
        const pinBtn = e.target.closest('.btn-pin');
        if (pinBtn) {
            const blobSlug = pinBtn.dataset.slug;
            const blobName = pinBtn.dataset.blob;
            const isPinned = pinBtn.dataset.pinned === 'true';
            const method = isPinned ? 'DELETE' : 'PUT';

            try {
                const response = await fetch(`/api/memes/${blobSlug}/${blobName}/pin`, { method });
                if (response.ok) {
                    // Reload page to reflect new pin order
                    location.reload();
                } else {
                    showToast('Failed to update pin.', 'danger');
                }
            } catch (err) {
                showToast(`Error: ${err.message}`, 'danger');
            }
            return;
        }

        const copyImgBtn = e.target.closest('.btn-copy-img');
        if (copyImgBtn) {
            const imgUrl = copyImgBtn.dataset.url;
            try {
                const resp = await fetch(imgUrl);
                const blob = await resp.blob();
                // Clipboard API requires image/png; convert if needed
                const pngBlob = await toPngBlob(blob);
                await navigator.clipboard.write([
                    new ClipboardItem({ 'image/png': pngBlob })
                ]);
                showToast('Image copied to clipboard!', 'success');
            } catch (err) {
                showToast('Could not copy image: ' + err.message, 'danger');
            }
            return;
        }

        const copyBtn = e.target.closest('.btn-copy');
        if (copyBtn) {
            const url = window.location.origin + copyBtn.dataset.url;
            try {
                await navigator.clipboard.writeText(url);
                showToast('Link copied to clipboard!', 'success');
            } catch {
                prompt('Copy this link:', url);
            }
            return;
        }

        const deleteBtn = e.target.closest('.btn-delete');
        if (deleteBtn) {
            const blobSlug = deleteBtn.dataset.slug;
            const blobName = deleteBtn.dataset.blob;

            if (!confirm('Delete this meme?')) return;

            try {
                const response = await fetch(`/api/memes/${blobSlug}/${blobName}`, {
                    method: 'DELETE'
                });

                if (response.ok) {
                    const card = deleteBtn.closest('.col');
                    card?.remove();
                    showToast('Meme deleted.', 'info');
                    updateCount();
                } else {
                    showToast('Failed to delete meme.', 'danger');
                }
            } catch (err) {
                showToast(`Error: ${err.message}`, 'danger');
            }
        }
    });

    function updateCount() {
        const badge = document.querySelector('.badge.bg-secondary');
        if (badge && gallery) {
            const count = gallery.children.length;
            badge.textContent = `${count} meme${count === 1 ? '' : 's'}`;
        }
    }

    function toPngBlob(blob) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = () => {
                const canvas = document.createElement('canvas');
                canvas.width = img.naturalWidth;
                canvas.height = img.naturalHeight;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0);
                canvas.toBlob(pngBlob => {
                    if (pngBlob) resolve(pngBlob);
                    else reject(new Error('Failed to convert image'));
                }, 'image/png');
            };
            img.onerror = () => reject(new Error('Failed to load image'));
            img.crossOrigin = 'anonymous';
            img.src = URL.createObjectURL(blob);
        });
    }

    function showToast(message, type) {
        let container = document.querySelector('.toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'toast-container';
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = `alert alert-${type} alert-dismissible fade show mb-2`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `${message}<button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
        container.appendChild(toast);

        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }
})();
