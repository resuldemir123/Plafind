/**
 * İşletme Karşılaştırma Sistemi - Local Storage Yönetimi
 */

(function () {
    'use strict';

    const STORAGE_KEY = 'plafind_comparison_list';
    const MAX_COMPARISON_ITEMS = 4;

    /**
     * Local Storage'dan karşılaştırma listesini alır
     */
    function getComparisonList() {
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            if (!stored) {
                console.log('Local Storage boş, yeni liste oluşturuluyor');
                return [];
            }

            const ids = JSON.parse(stored);
            const filteredIds = Array.isArray(ids) ? ids.filter(id => typeof id === 'number' && id > 0) : [];
            console.log('Local Storage\'dan alınan liste:', filteredIds);
            return filteredIds;
        } catch (error) {
            console.error('getComparisonList error:', error);
            console.error('Hatalı JSON:', localStorage.getItem(STORAGE_KEY));
            return [];
        }
    }

    /**
     * Local Storage'a karşılaştırma listesini kaydeder
     */
    function saveComparisonList(ids) {
        try {
            const uniqueIds = [...new Set(ids)].slice(0, MAX_COMPARISON_ITEMS);
            const jsonString = JSON.stringify(uniqueIds);
            localStorage.setItem(STORAGE_KEY, jsonString);
            console.log('Local Storage kaydedildi:', jsonString, 'Key:', STORAGE_KEY);
            updateComparisonBadge();
            return true;
        } catch (error) {
            console.error('saveComparisonList error:', error);
            return false;
        }
    }

    /**
     * İşletmeyi karşılaştırmaya ekler
     */
    function addToComparison(businessId) {
        console.log('addToComparison çağrıldı:', businessId);
        
        if (!businessId || businessId <= 0) {
            console.error('Geçersiz işletme ID:', businessId);
            return { success: false, message: 'Geçersiz işletme ID\'si' };
        }

        const currentList = getComparisonList();
        console.log('Mevcut liste:', currentList);

        if (currentList.includes(businessId)) {
            console.log('İşletme zaten listede');
            return { success: false, message: 'Bu işletme zaten karşılaştırma listesinde' };
        }

        if (currentList.length >= MAX_COMPARISON_ITEMS) {
            console.log('Maksimum limit aşıldı');
            return { success: false, message: `Maksimum ${MAX_COMPARISON_ITEMS} işletme karşılaştırılabilir` };
        }

        currentList.push(businessId);
        console.log('Yeni liste:', currentList);
        
        const saveResult = saveComparisonList(currentList);
        console.log('Kaydetme sonucu:', saveResult);
        
        // Kaydetme sonrası kontrol
        const savedList = getComparisonList();
        console.log('Kaydedilen liste kontrolü:', savedList);

        return { success: true, message: 'İşletme karşılaştırmaya eklendi', count: currentList.length };
    }

    /**
     * İşletmeyi karşılaştırmadan çıkarır
     */
    function removeFromComparison(businessId) {
        if (!businessId || businessId <= 0) {
            return { success: false, message: 'Geçersiz işletme ID\'si' };
        }

        const currentList = getComparisonList();
        const filteredList = currentList.filter(id => id !== businessId);

        if (filteredList.length === currentList.length) {
            return { success: false, message: 'Bu işletme karşılaştırma listesinde değil' };
        }

        saveComparisonList(filteredList);

        return { success: true, message: 'İşletme karşılaştırmadan çıkarıldı', count: filteredList.length };
    }

    /**
     * İşletmenin karşılaştırma listesinde olup olmadığını kontrol eder
     */
    function isInComparison(businessId) {
        const currentList = getComparisonList();
        return currentList.includes(businessId);
    }

    /**
     * Karşılaştırma listesini temizler
     */
    function clearComparison() {
        try {
            localStorage.removeItem(STORAGE_KEY);
            updateComparisonBadge();
            return { success: true, message: 'Karşılaştırma listesi temizlendi' };
        } catch (error) {
            console.error('clearComparison error:', error);
            return { success: false, message: 'Liste temizlenirken hata oluştu' };
        }
    }

    /**
     * Karşılaştırma badge'ini günceller
     */
    function updateComparisonBadge() {
        const count = getComparisonList().length;
        const badges = document.querySelectorAll('.comparison-badge-count');
        
        badges.forEach(badge => {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'inline-block' : 'none';
        });

        // Karşılaştır butonunu göster/gizle
        const compareButton = document.getElementById('compare-button');
        if (compareButton) {
            compareButton.style.display = count > 0 ? 'inline-block' : 'none';
        }
    }

    /**
     * Karşılaştırma sayfasına yönlendirir
     */
    function goToComparison() {
        const ids = getComparisonList();
        if (ids.length === 0) {
            alert('Karşılaştırma için en az bir işletme seçmelisiniz.');
            return;
        }

        const idsParam = ids.join(',');
        window.location.href = `/Comparison/Index?ids=${idsParam}`;
    }

    /**
     * AJAX ile karşılaştırma verilerini yükler
     */
    async function loadComparisonData(ids) {
        try {
            const response = await fetch(`/Comparison/GetComparisonData?ids=${ids.join(',')}`);
            const data = await response.json();
            return data;
        } catch (error) {
            console.error('loadComparisonData error:', error);
            return { success: false, message: 'Veri yüklenirken hata oluştu' };
        }
    }

    /**
     * Karşılaştırma tablosunu render eder
     */
    function renderComparisonTable(data) {
        // Bu fonksiyon Comparison/Index.cshtml sayfasında kullanılacak
        // Şimdilik placeholder
        console.log('renderComparisonTable:', data);
    }

    /**
     * Sadece farklı satırları göster/gizle
     */
    function toggleDifferencesOnly(showOnly) {
        const rows = document.querySelectorAll('.comparison-table tbody tr');
        rows.forEach(row => {
            if (showOnly) {
                if (!row.classList.contains('highlight-diff')) {
                    row.style.display = 'none';
                } else {
                    row.style.display = '';
                }
            } else {
                row.style.display = '';
            }
        });
    }

    // Global fonksiyonları export et
    window.ComparisonManager = {
        addToComparison: addToComparison,
        removeFromComparison: removeFromComparison,
        isInComparison: isInComparison,
        clearComparison: clearComparison,
        getComparisonList: getComparisonList,
        goToComparison: goToComparison,
        loadComparisonData: loadComparisonData,
        renderComparisonTable: renderComparisonTable,
        toggleDifferencesOnly: toggleDifferencesOnly,
        updateComparisonBadge: updateComparisonBadge
    };

    // Sayfa yüklendiğinde badge'i güncelle
    function initializeComparison() {
        function doInitialize() {
            updateComparisonBadge();
            console.log('ComparisonManager yüklendi ve hazır');
            console.log('Mevcut karşılaştırma listesi:', getComparisonList());
        }

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', doInitialize);
        } else {
            // DOM zaten yüklü, hemen çalıştır
            setTimeout(doInitialize, 100);
        }
    }

    // Hemen başlat
    initializeComparison();

    // Global olarak erişilebilir olduğunu doğrula
    console.log('ComparisonManager export edildi:', typeof window.ComparisonManager !== 'undefined');

})();

