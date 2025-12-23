// Admin Businesses Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    const categoryFilter = document.getElementById('categoryFilter');
    const searchInput = document.getElementById('searchInput');
    const businessTable = document.getElementById('businessTable');
    const businessCount = document.getElementById('businessCount');
    const selectAllCheckbox = document.getElementById('selectAll');
    const selectAllHeaderCheckbox = document.getElementById('selectAllHeader');
    const bulkActionsPanel = document.getElementById('bulkActionsPanel');
    const selectedCount = document.getElementById('selectedCount');
    const bulkActionSelect = document.getElementById('bulkActionSelect');
    const bulkCategorySelect = document.getElementById('bulkCategorySelect');

    // Filter functionality
    function filterTable() {
        const categoryId = categoryFilter.value;
        const search = searchInput.value.toLowerCase();

        const rows = businessTable.querySelectorAll('tbody tr');
        let visibleCount = 0;

        rows.forEach(row => {
            const rowCategoryId = row.dataset.categoryId || '0';
            const rowSearch = row.dataset.search || '';

            const matchCategory = !categoryId || rowCategoryId === categoryId;
            const matchSearch = !search || rowSearch.includes(search);

            if (matchCategory && matchSearch) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        businessCount.textContent = visibleCount;
    }

    if (categoryFilter) {
        categoryFilter.addEventListener('change', filterTable);
    }
    if (searchInput) {
        searchInput.addEventListener('input', filterTable);
    }

    // Bulk selection
    function toggleSelectAll() {
        const checked = selectAllCheckbox?.checked || selectAllHeaderCheckbox?.checked;
        const checkboxes = document.querySelectorAll('.business-checkbox');
        checkboxes.forEach(cb => {
            if (cb.closest('tr').style.display !== 'none') {
                cb.checked = checked;
            }
        });
        updateBulkActions();
    }

    function updateBulkActions() {
        const checkedBoxes = document.querySelectorAll('.business-checkbox:checked');
        const count = checkedBoxes.length;

        if (count > 0) {
            bulkActionsPanel.style.display = 'block';
            selectedCount.textContent = count;
        } else {
            bulkActionsPanel.style.display = 'none';
        }

        // Show/hide category select based on action
        if (bulkActionSelect.value === 'changeCategory') {
            bulkCategorySelect.style.display = 'inline-block';
        } else {
            bulkCategorySelect.style.display = 'none';
        }
    }

    function clearSelection() {
        document.querySelectorAll('.business-checkbox').forEach(cb => cb.checked = false);
        selectAllCheckbox.checked = false;
        selectAllHeaderCheckbox.checked = false;
        updateBulkActions();
    }

    function executeBulkAction() {
        const checkedBoxes = document.querySelectorAll('.business-checkbox:checked');
        const businessIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));
        const action = bulkActionSelect.value;

        if (!action) {
            alert('Lütfen bir işlem seçin');
            return;
        }

        if (businessIds.length === 0) {
            alert('Lütfen en az bir işletme seçin');
            return;
        }

        if (action === 'changeCategory') {
            const categoryId = bulkCategorySelect.value;
            if (!categoryId) {
                alert('Lütfen bir kategori seçin');
                return;
            }
        }

        if (confirm(`${businessIds.length} işletme için "${action}" işlemini uygulamak istediğinizden emin misiniz?`)) {
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = '/Admin/BulkAction';

            // Anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) {
                form.appendChild(token.cloneNode(true));
            }

            // Action
            const actionInput = document.createElement('input');
            actionInput.type = 'hidden';
            actionInput.name = 'action';
            actionInput.value = action;
            form.appendChild(actionInput);

            // Business IDs
            businessIds.forEach(id => {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'businessIds';
                input.value = id;
                form.appendChild(input);
            });

            // Category ID if needed
            if (action === 'changeCategory') {
                const categoryInput = document.createElement('input');
                categoryInput.type = 'hidden';
                categoryInput.name = 'categoryId';
                categoryInput.value = bulkCategorySelect.value;
                form.appendChild(categoryInput);
            }

            document.body.appendChild(form);
            form.submit();
        }
    }

    // Reject modal
    window.showRejectModal = function(businessId, businessName) {
        document.getElementById('rejectBusinessId').value = businessId;
        const modal = new bootstrap.Modal(document.getElementById('rejectModal'));
        modal.show();
    };

    // Event listeners
    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', toggleSelectAll);
    }
    if (selectAllHeaderCheckbox) {
        selectAllHeaderCheckbox.addEventListener('change', toggleSelectAll);
    }
    if (bulkActionSelect) {
        bulkActionSelect.addEventListener('change', updateBulkActions);
    }

    document.querySelectorAll('.business-checkbox').forEach(cb => {
        cb.addEventListener('change', updateBulkActions);
    });

    window.clearFilters = function() {
        categoryFilter.value = '';
        searchInput.value = '';
        filterTable();
    };

    window.updateBulkActions = updateBulkActions;
    window.clearSelection = clearSelection;
    window.executeBulkAction = executeBulkAction;
});

