(function() {
    'use strict';

    const CONFIG = {
        apiBaseUrl: '/GetAvatar',
        modalId: 'getAvatarModal'
    };

    let avatars = [];
    let selectedAvatarId = null;

    function createModal() {
        if (document.getElementById(CONFIG.modalId)) return;

        const modal = document.createElement('div');
        modal.id = CONFIG.modalId;
        modal.innerHTML = `
            <div class="dialogContainer" style="display:none;">
                <div class="focuscontainer dialog dialog-fixedSize dialog-medium-tall" style="max-width:900px;">
                    <div class="formDialogHeader">
                        <button is="paper-icon-button-light" class="btnCancel autoSize" title="Close">
                            <span class="material-icons close"></span>
                        </button>
                        <h3 class="formDialogHeaderTitle">Choose Your Avatar</h3>
                    </div>
                    <div class="formDialogContent scrollY" style="padding:2em;">
                        <div id="avatarGridContainer" style="display:grid;grid-template-columns:repeat(auto-fill,minmax(100px,1fr));gap:1em;"></div>
                        <div style="margin-top:2em;display:flex;justify-content:flex-end;gap:1em;">
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
            loadAvatars();
        }
    }

    function closeModal() {
        const container = document.querySelector('#' + CONFIG.modalId + ' .dialogContainer');
        if (container) {
            container.style.display = 'none';
            selectedAvatarId = null;
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

    function renderAvatars(list) {
        const container = document.getElementById('avatarGridContainer');

        if (!list || list.length === 0) {
            container.innerHTML = '<p style="grid-column:1/-1;text-align:center;opacity:0.6;">No avatars available. Contact your administrator.</p>';
            return;
        }

        container.innerHTML = list.map(avatar => `
            <div class="avatar-option card" data-id="${avatar.Id}" style="cursor:pointer;text-align:center;padding:0.5em;border:2px solid transparent;border-radius:8px;">
                <div class="cardBox">
                    <img src="${avatar.Url}" style="width:100%;aspect-ratio:1;object-fit:cover;border-radius:4px;" />
                    <div style="font-size:0.8em;margin-top:0.5em;opacity:0.8;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">${avatar.Name}</div>
                </div>
            </div>
        `).join('');

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
            const response = await fetch(ApiClient.getUrl(CONFIG.apiBaseUrl + '/SetAvatar'), {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Emby-Token': ApiClient.accessToken()
                },
                body: JSON.stringify({ avatarId: selectedAvatarId })
            });

            if (!response.ok) {
                throw new Error('HTTP ' + response.status);
            }

            closeModal();
            Dashboard.alert({ message: 'Avatar updated!', title: 'Success' });

            const timestamp = Date.now();
            document.querySelectorAll('img').forEach(img => {
                const src = img.src || '';
                if (src.includes('/Users/') && src.includes('/Images/')) {
                    const baseUrl = src.split('?')[0];
                    img.src = baseUrl + '?t=' + timestamp;
                }
            });

            setTimeout(() => {
                const url = new URL(location.href);
                url.searchParams.set('_refresh', timestamp);
                location.href = url.toString();
            }, 800);
        } catch (error) {
            console.error('GetAvatar: Failed to set avatar', error);
            Dashboard.alert({ message: 'Failed to set avatar', title: 'Error' });
            btn.disabled = false;
            btn.textContent = 'Set as My Avatar';
        }
    }

    function injectButton() {
        if (!location.hash.includes('userprofile')) return;
        if (document.getElementById('btnChooseGetAvatar')) return;

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

        console.log('GetAvatar: Button injected');
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
