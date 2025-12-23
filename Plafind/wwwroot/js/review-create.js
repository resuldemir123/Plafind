// Review Create Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    const imageInput = document.getElementById('imageInput');
    const imagePreviewContainer = document.getElementById('imagePreviewContainer');
    const uploadPlaceholder = document.querySelector('.upload-placeholder');
    const maxImages = 5;
    let selectedImages = [];

    // File input change event
    if (imageInput) {
        imageInput.addEventListener('change', function(e) {
            handleImageSelection(e.target.files);
        });

        // Drag and drop
        const uploadArea = document.getElementById('imageUploadArea');
        
        uploadArea.addEventListener('dragover', function(e) {
            e.preventDefault();
            uploadPlaceholder.style.borderColor = 'var(--color-primary)';
            uploadPlaceholder.style.background = 'var(--color-primary-soft)';
        });

        uploadArea.addEventListener('dragleave', function(e) {
            e.preventDefault();
            uploadPlaceholder.style.borderColor = 'var(--border-color)';
            uploadPlaceholder.style.background = 'var(--bg-body)';
        });

        uploadArea.addEventListener('drop', function(e) {
            e.preventDefault();
            uploadPlaceholder.style.borderColor = 'var(--border-color)';
            uploadPlaceholder.style.background = 'var(--bg-body)';
            
            const files = Array.from(e.dataTransfer.files).filter(file => file.type.startsWith('image/'));
            handleImageSelection(files);
        });
    }

    function handleImageSelection(files) {
        const remainingSlots = maxImages - selectedImages.length;
        const filesToAdd = Array.from(files).slice(0, remainingSlots);

        filesToAdd.forEach(file => {
            if (file.size > 5 * 1024 * 1024) {
                alert(`${file.name} dosyası çok büyük (maks. 5MB)`);
                return;
            }

            const reader = new FileReader();
            reader.onload = function(e) {
                const imageData = {
                    file: file,
                    preview: e.target.result
                };
                selectedImages.push(imageData);
                updateImagePreviews();
                updateFileInput();
            };
            reader.readAsDataURL(file);
        });

        if (files.length > remainingSlots) {
            alert(`Maksimum ${maxImages} fotoğraf ekleyebilirsiniz.`);
        }
    }

    function updateImagePreviews() {
        imagePreviewContainer.innerHTML = '';
        
        if (selectedImages.length === 0) {
            uploadPlaceholder.style.display = 'block';
            return;
        }

        uploadPlaceholder.style.display = selectedImages.length >= maxImages ? 'none' : 'block';

        selectedImages.forEach((imageData, index) => {
            const previewItem = document.createElement('div');
            previewItem.className = 'image-preview-item';
            previewItem.innerHTML = `
                <img src="${imageData.preview}" alt="Preview ${index + 1}" />
                <button type="button" class="remove-btn" onclick="removeImage(${index})" aria-label="Fotoğrafı kaldır">
                    <i class="fas fa-times"></i>
                </button>
            `;
            imagePreviewContainer.appendChild(previewItem);
        });
    }

    function updateFileInput() {
        // Create new FileList with selected images
        const dataTransfer = new DataTransfer();
        selectedImages.forEach(imageData => {
            dataTransfer.items.add(imageData.file);
        });
        imageInput.files = dataTransfer.files;
    }

    window.removeImage = function(index) {
        selectedImages.splice(index, 1);
        updateImagePreviews();
        updateFileInput();
    };

    // Rating radio button visual feedback
    document.querySelectorAll('input[name="Rating"]').forEach(radio => {
        radio.addEventListener('change', function() {
            document.querySelectorAll('.rating-display').forEach(display => {
                display.style.borderColor = 'var(--border-color)';
                display.style.background = 'var(--bg-body)';
            });
            
            const selectedDisplay = this.nextElementSibling;
            if (selectedDisplay) {
                selectedDisplay.style.borderColor = 'var(--color-primary)';
                selectedDisplay.style.background = 'var(--color-primary-soft)';
            }
        });
    });

    // Set initial rating display
    const defaultRating = document.querySelector('input[name="Rating"]:checked');
    if (defaultRating) {
        defaultRating.dispatchEvent(new Event('change'));
    }
});

