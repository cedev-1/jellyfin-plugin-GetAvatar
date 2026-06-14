(function() {
    'use strict';

    const CONFIG = {
        apiBaseUrl: '/GetAvatar',
        modalId: 'getAvatarModal'
    };

    let avatars = [];
    let selectedAvatarId = null;
    let targetUserId = null;

    function createModal() {
        if (document.getElementById(CONFIG.modalId)) return;

        const modal = document.createElement('div');
        modal.id = CONFIG.modalId;
        modal.innerHTML = `
            <div class="dialogContainer" style="display:none;">
                <div class="focuscontainer dialog dialog-fixedSize dialog-medium-tall" style="max-width:900px;max-height:90vh;display:flex;flex-direction:column;">
                    <div class="formDialogHeader">
                        <button is="paper-icon-button-light" class="btnCancel autoSize" title="Close">
                            <span class="material-icons close"></span>
                        </button>
                        <h3 class="formDialogHeaderTitle">Choose Your Avatar</h3>
                    </div>
                    <div class="formDialogContent" style="padding:2em;flex:1;display:flex;flex-direction:column;min-height:0;">
                        <div id="avatarCategoryList" style="display:none;flex-wrap:wrap;gap:0.5em;margin-bottom:1.5em;flex-shrink:0;"></div>
                        <div id="avatarGridContainer" style="display:grid;grid-template-columns:repeat(auto-fill,minmax(100px,1fr));gap:1em;flex:1;overflow-y:auto;-webkit-overflow-scrolling:touch;min-height:0;padding-right:0.5em;"></div>
                        <div style="display:flex;justify-content:flex-end;gap:1em;flex-shrink:0;padding-top:1.5em;border-top:1px solid rgba(255,255,255,0.1);margin-top:1em;">
                            <button is="emby-button" id="cancelAvatarBtn" class="raised button-cancel">Cancel</button>
                            <button is="emby-button" id="applyAvatarBtn" class="raised button-submit" disabled>Set as My Avatar</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(modal);

        modal.querySelector('.btnCancel').onclick = closeModal;
        modal.querySelector('#cancelAvatarBtn').onclick = closeModal;
        modal.querySelector('#applyAvatarBtn').onclick = applyAvatar;
        modal.querySelector('.dialogContainer').onclick = function(e) {
            if (e.target === this) closeModal();
        };
    }

    function openModal() {
        const container = document.querySelector('#' + CONFIG.modalId + ' .dialogContainer');
        if (container) {
            container.style.display = 'flex';
            container.style.position = 'fixed';
            container.style.inset = '0';
            container.style.zIndex = '9999';
            container.style.background = 'rgba(0,0,0,0.7)';
            container.style.alignItems = 'center';
            container.style.justifyContent = 'center';
            
            // Bloquer le scroll du body sur mobile
            document.body.style.overflow = 'hidden';
            document.body.style.position = 'fixed';
            document.body.style.width = '100%';
            
            loadAvatars();
        }
    }

    function closeModal() {
        const container = document.querySelector('#' + CONFIG.modalId + ' .dialogContainer');
        if (container) {
            container.style.display = 'none';
            selectedAvatarId = null;
            
            // FIX phone scroll
            document.body.style.overflow = '';
            document.body.style.position = '';
            document.body.style.width = '';
        }
    }

    async function loadAvatars() {
        const container = document.getElementById('avatarGridContainer');
        container.innerHTML = '<p style="grid-column:1/-1;text-align:center;opacity:0.7;">Loading...</p>';

        try {
            const response = await fetch(ApiClient.getUrl(CONFIG.apiBaseUrl + '/Avatars'), {
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });

            if (!response.ok) {
                throw new Error('HTTP ' + response.status);
            }

            avatars = await response.json();
            console.log('GetAvatar: Loaded avatars', avatars);
            renderAvatars(avatars);
        } catch (error) {
            console.error('GetAvatar: Failed to load avatars', error);
            container.innerHTML = '<p style="grid-column:1/-1;text-align:center;color:#e66;">Failed to load avatars</p>';
        }
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function renderAvatars(list) {
        const container = document.getElementById('avatarGridContainer');
        const categoryList = document.getElementById('avatarCategoryList');

        if (!list || list.length === 0) {
            container.innerHTML = '<p style="grid-column:1/-1;text-align:center;opacity:0.6;">No avatars available. Contact your administrator.</p>';
            if (categoryList) {
                categoryList.style.display = 'none';
                categoryList.innerHTML = '';
            }
            return;
        }

        // Group avatars by category
        const grouped = {};
        list.forEach(function(avatar) {
            const category = avatar.Category || avatar.category || '';
            if (!grouped[category]) grouped[category] = [];
            grouped[category].push(avatar);
        });

        var categories = Object.keys(grouped).sort(function(a, b) {
            if (a === '' && b !== '') return -1;
            if (a !== '' && b === '') return 1;
            return a.localeCompare(b);
        });

        var html = '';
        var hasMultipleCategories = categories.length > 1 || categories[0] !== '';

        if (categoryList) {
            if (hasMultipleCategories) {
                var categoryHtml = '';
                categories.forEach(function(category) {
                    if (!category) return;
                    var categoryId = 'avatar-category-' + category;
                    categoryHtml += '<button class="category-link raised" data-target="' + escapeHtml(categoryId) + '" style="background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.15);border-radius:999px;padding:0.4em 0.9em;color:inherit;cursor:pointer;transition:all 0.2s;font-size:0.85em;white-space:nowrap;">' + escapeHtml(category) + '</button>';
                });
                categoryList.innerHTML = categoryHtml;
                categoryList.style.display = 'flex';

                categoryList.querySelectorAll('.category-link').forEach(function(btn) {
                    btn.addEventListener('click', function() {
                        var targetId = this.getAttribute('data-target');
                        var target = document.getElementById(targetId);
                        if (target) {
                            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                        }
                    });
                });
            } else {
                categoryList.style.display = 'none';
                categoryList.innerHTML = '';
            }
        }

        categories.forEach(function(category) {
            var categoryAvatars = grouped[category];
            var categoryId = 'avatar-category-' + (category || 'uncategorized');

            if (hasMultipleCategories && category) {
                html += '<div id="' + escapeHtml(categoryId) + '" style="grid-column:1/-1;margin-top:1em;margin-bottom:0.3em;padding-bottom:0.3em;border-bottom:1px solid rgba(255,255,255,0.1);">';
                html += '<h3 style="margin:0;font-size:1em;font-weight:600;">' + escapeHtml(category) + '</h3>';
                html += '</div>';
            } else if (hasMultipleCategories) {
                html += '<div id="' + escapeHtml(categoryId) + '" style="grid-column:1/-1;"></div>';
            }

            categoryAvatars.forEach(function(avatar) {
                html += `
            <div class="avatar-option card" data-id="${escapeHtml(avatar.Id)}" style="cursor:pointer;text-align:center;padding:0.5em;border:2px solid transparent;border-radius:8px;">
                <div class="cardBox">
                    <img src="${escapeHtml(ApiClient.getUrl('/GetAvatar/Image/' + avatar.Id))}" style="width:100%;aspect-ratio:1;object-fit:cover;border-radius:4px;" />
                    <div style="font-size:0.8em;margin-top:0.5em;opacity:0.8;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">${escapeHtml(avatar.Name)}</div>
                </div>
            </div>
        `;
            });
        });

        container.innerHTML = html;

        container.querySelectorAll('.avatar-option').forEach(el => {
            el.onclick = function() {
                container.querySelectorAll('.avatar-option').forEach(opt => {
                    opt.style.borderColor = 'transparent';
                    opt.style.background = '';
                });
                this.style.borderColor = '#52B54B';
                this.style.background = 'rgba(82,181,75,0.15)';
                selectedAvatarId = this.dataset.id;
                document.getElementById('applyAvatarBtn').disabled = false;
            };
        });
    }

    async function applyAvatar() {
         if (!selectedAvatarId) return;

         const btn = document.getElementById('applyAvatarBtn');
         btn.disabled = true;
         btn.textContent = 'Applying...';

         try {
             const requestBody = { avatarId: selectedAvatarId };
             if (targetUserId) {
                 requestBody.userId = targetUserId;
             }

             const response = await fetch(ApiClient.getUrl(CONFIG.apiBaseUrl + '/SetAvatar'), {
                 method: 'POST',
                 headers: {
                     'Content-Type': 'application/json',
                     'X-Emby-Token': ApiClient.accessToken()
                 },
                 body: JSON.stringify(requestBody)
             });

            if (!response.ok) {
                throw new Error('HTTP ' + response.status);
            }

            closeModal();

            try { Dashboard.alert({ message: 'Avatar updated!', title: 'Success' }); } catch (e) { console.warn('GetAvatar: Dashboard.alert error (success)', e); }

            try {
                const timestamp = Date.now();
                document.querySelectorAll('img').forEach(img => {
                    const src = img.src || '';
                    if (src.includes('/Users/') && src.includes('/Images/')) {
                        const baseUrl = src.split('?')[0];
                        img.src = baseUrl + '?t=' + timestamp;
                    }
                });
                setTimeout(() => { location.reload(); }, 800);
            } catch (e) {
                console.warn('GetAvatar: image refresh error', e);
            }
        } catch (error) {
            console.error('GetAvatar: Failed to set avatar', error.name + ': ' + error.message, error.stack);
            try { Dashboard.alert({ message: 'Failed to set avatar: ' + error.message, title: 'Error' }); } catch (e) { alert('Failed to set avatar: ' + error.message); }
            btn.disabled = false;
            btn.textContent = 'Set as My Avatar';
        }
    }

    function injectButton() {
         if (!location.hash.includes('userprofile')) return;
         if (document.getElementById('btnChooseGetAvatar')) return;

         const hashParams = new URLSearchParams(location.hash.split('?')[1]);
         targetUserId = hashParams.get('userId') || null;

         const targetSelectors = [
             '.selectImageContainer',
             '.userProfileSettingsPage .detailSection',
             '#btnDeleteImage',
             '.imageEditorContainer'
         ];

         let target = null;
         for (const selector of targetSelectors) {
             target = document.querySelector(selector);
             if (target) break;
         }

         if (!target) return;

         const btn = document.createElement('button');
         btn.id = 'btnChooseGetAvatar';
         btn.setAttribute('is', 'emby-button');
         btn.className = 'raised button-alt block';
         btn.style.marginTop = '1em';
         btn.style.display = 'flex';
         btn.style.alignItems = 'center';
         btn.style.justifyContent = 'center';
         btn.style.gap = '0.5em';
         btn.innerHTML = '<span class="material-icons person" style="margin:0;"></span><span>Choose from Gallery</span>';
         btn.onclick = function(e) {
             e.preventDefault();
             openModal();
         };

         if (target.id === 'btnDeleteImage') {
             target.parentElement.appendChild(btn);
         } else {
             target.appendChild(btn);
         }

        // console.log('GetAvatar: Button injected', targetUserId ? `for user ${targetUserId}` : 'for current user'); - DEBUG
     }

    function init() {
        console.log('GetAvatar: Initializing...');
        createModal();

        const checkPage = () => {
            setTimeout(injectButton, 300);
        };

        window.addEventListener('hashchange', checkPage);
        document.addEventListener('viewshow', checkPage);

        checkPage();
    }

    function waitForApiClient() {
        if (typeof ApiClient !== 'undefined' && typeof Dashboard !== 'undefined') {
            init();
        } else {
            setTimeout(waitForApiClient, 100);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', waitForApiClient);
    } else {
        waitForApiClient();
    }
})();
