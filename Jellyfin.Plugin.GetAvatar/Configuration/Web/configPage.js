const GetAvatarConfig = {
  pluginUniqueId: "88accc81-d913-44b3-b1d3-2abfa457dd2d",
};

const getAvatarCss = `
.avatar-list-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 1em;
}
.avatar-list-header h3 { margin: 0; font-size: 1em; }
.avatar-count { font-size: 0.85em; opacity: 0.5; }
.avatar-links-section {
    margin-bottom: 1.5em;
    background: rgba(255,255,255,0.06);
    border: 1px solid rgba(255,255,255,0.1);
    border-radius: 8px;
    padding: 1em 1.2em;
}
.avatar-links-text {
    margin: 0 0 0.8em;
    opacity: 0.85;
    font-size: 0.95em;
}
.avatar-links {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75em;
}
.avatar-link-button {
    display: inline-flex;
    align-items: center;
    gap: 0.4em;
    text-decoration: none;
    color: inherit;
    padding: 0.4em 0.8em;
    border-radius: 4px;
    transition: opacity 0.15s, background 0.15s;
}
.avatar-link-button:hover {
    opacity: 0.85;
    background: rgba(255,255,255,0.07);
}
.avatar-link-button:active {
    opacity: 0.65;
}
.avatar-link-button .material-icons {
    font-size: 1.1em;
}
.empty-state, .loading-state {
    text-align: center;
    padding: 2em;
    opacity: 0.5;
    font-size: 0.9em;
}
.avatar-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 1em;
}
.avatar-card {
    position: relative;
    text-align: center;
    width: auto;
}
.avatar-image-container {
    width: 100px;
    height: 100px;
    margin: 0 auto;
    overflow: hidden;
    border-radius: 4px;
}
.avatar-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
}
.delete-button {
    position: absolute;
    top: 5px;
    right: 5px;
    background: #000;
    color: #fff;
    border: none;
    border-radius: 50%;
    width: 24px;
    height: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    font-size: 16px;
    line-height: 1;
    z-index: 10;
    padding: 0;
}
.delete-button:hover {
    background: #333;
}
.category-section {
    margin-bottom: 2em;
}
.category-header {
    display: flex;
    align-items: center;
    gap: 1em;
    margin-bottom: 0.8em;
    padding-bottom: 0.4em;
    border-bottom: 1px solid rgba(255,255,255,0.1);
}
.category-title {
    margin: 0;
    font-size: 1em;
    font-weight: 600;
}
.category-count {
    font-size: 0.8em;
    opacity: 0.5;
}
.delete-category-button {
    background: none;
    border: 1px solid rgba(229, 57, 53, 0.3);
    color: #e57373;
    cursor: pointer;
    padding: 0.2em 0.6em;
    font-size: 0.8em;
    border-radius: 4px;
    margin-left: auto;
    transition: all 0.2s;
}
.delete-category-button:hover {
    background: rgba(229, 57, 53, 0.1);
    border-color: #e53935;
    color: #e53935;
}
.category-details {
    margin-top: 0.6em;
    display: inline-block;
}
.category-details[open] .category-summary {
    margin-bottom: 0.4em;
}
.category-summary {
    list-style: none;
    cursor: pointer;
    font-size: 0.85em;
    color: rgba(255,255,255,0.75);
    display: flex;
    align-items: center;
    gap: 0.3em;
    user-select: none;
}
.category-summary::-webkit-details-marker { display: none; }
.category-summary::before {
    content: "▶";
    font-size: 0.6em;
    transition: transform 0.15s;
    display: inline-block;
}
.category-details[open] .category-summary::before {
    transform: rotate(90deg);
}
.category-info-icon {
    font-size: 0.95em;
    cursor: help;
    margin-left: 0.2em;
    position: relative;
}
.category-tooltip {
    display: none;
    position: absolute;
    left: 1.5em;
    top: 50%;
    transform: translateY(-50%);
    background: #1a1a1a;
    border: 1px solid rgba(255,255,255,0.15);
    border-radius: 6px;
    padding: 0.75em 1em;
    font-size: 0.9em;
    font-weight: normal;
    white-space: normal;
    width: 260px;
    z-index: 100;
    line-height: 1.5;
    box-shadow: 0 4px 12px rgba(0,0,0,0.4);
    pointer-events: none;
}
.category-info-icon:hover .category-tooltip {
    display: block;
}
.category-field {
    display: flex;
    align-items: center;
    gap: 0.5em;
    margin-top: 0.3em;
}
.category-field input {
    flex: 1;
    max-width: 250px;
}
.upload-label {
    cursor: pointer;
    padding: 0.4em 0.8em;
    transition: opacity 0.15s, background 0.15s;
}
.upload-label:hover {
    opacity: 0.85;
    background: rgba(255,255,255,0.07);
    border-radius: 4px;
}
.upload-label:active {
    opacity: 0.65;
}
.feature-settings {
    margin-top: 1.5em;
    margin-bottom: 0.5em;
}
.feature-toggle-row {
    display: flex;
    align-items: center;
    gap: 0.75em;
    margin-bottom: 0.75em;
}
.feature-toggle-label-text {
    font-size: 0.95em;
    font-weight: 500;
}
.switch {
    position: relative;
    display: inline-block;
    width: 42px;
    height: 24px;
    flex-shrink: 0;
}
.switch input { display: none; }
.switch-slider {
    position: absolute;
    inset: 0;
    background: rgba(255,255,255,0.15);
    border-radius: 24px;
    cursor: pointer;
    transition: background 0.2s;
}
.switch-slider::before {
    content: "";
    position: absolute;
    width: 18px;
    height: 18px;
    left: 3px;
    top: 3px;
    background: #fff;
    border-radius: 50%;
    transition: transform 0.2s;
}
.switch input:checked + .switch-slider {
    background: #00a4dc;
}
.switch input:checked + .switch-slider::before {
    transform: translateX(18px);
}
.feature-info-icon {
    font-size: 0.9em;
    cursor: help;
    position: relative;
}
.feature-tooltip {
    display: none;
    position: absolute;
    left: 1.5em;
    top: 50%;
    transform: translateY(-50%);
    background: #1a1a1a;
    border: 1px solid rgba(255,255,255,0.15);
    border-radius: 6px;
    padding: 0.75em 1em;
    font-size: 0.85em;
    font-weight: normal;
    white-space: normal;
    width: 280px;
    z-index: 100;
    line-height: 1.5;
    box-shadow: 0 4px 12px rgba(0,0,0,0.4);
    pointer-events: none;
}
.feature-info-icon:hover .feature-tooltip {
    display: block;
}
.upload-buttons-row {
    display: flex;
    flex-direction: row;
    gap: 0.5em;
    align-items: center;
}
.upload-submit-row {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 1em;
}
#avatarFileInput,
#avatarFolderInput {
    display: none;
}
.download-online-pack-button {
    background: transparent !important;
    box-shadow: none !important;
    border: none !important;
    color: inherit !important;
}
.download-online-pack-button:hover {
    opacity: 0.85;
    background: rgba(255,255,255,0.07) !important;
}
.download-online-pack-button:active {
    opacity: 0.65;
}
.online-pack-list {
    display: flex;
    flex-direction: column;
    gap: 0.5em;
}
.online-pack-item {
    display: flex;
    align-items: center;
    gap: 0.75em;
    padding: 0.7em 0.8em;
    background: rgba(255,255,255,0.05);
    border-radius: 6px;
    cursor: pointer;
    transition: background 0.15s;
}
.online-pack-item:hover {
    background: rgba(255,255,255,0.08);
}
.online-pack-item input[type="checkbox"] {
    margin: 0;
    width: 18px;
    height: 18px;
    flex-shrink: 0;
}
.online-pack-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
}
.online-pack-name {
    font-weight: 500;
    font-size: 0.95em;
}
.online-pack-size {
    font-size: 0.8em;
    opacity: 0.6;
}
.online-pack-empty,
.online-pack-error {
    text-align: center;
    padding: 2em;
    opacity: 0.6;
}
.online-pack-error {
    color: #e57373;
    opacity: 1;
}
#onlinePackDialog .dialogContainer {
    animation: none;
}
`;

function escapeHtml(str) {
  return String(str)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

export default function (view) {
  const styleId = 'GetAvatarPluginStyles';
  if (!document.getElementById(styleId)) {
    const style = document.createElement('style');
    style.id = styleId;
    style.textContent = getAvatarCss;
    document.head.appendChild(style);
  }

  const avatarListContainer = view.querySelector("#avatarList");
  const avatarCountEl = view.querySelector("#avatarCount");
  const fileInput = view.querySelector("#avatarFileInput");
  const folderInput = view.querySelector("#avatarFolderInput");
  const categoryInput = view.querySelector("#categoryInput");
  const uploadButton = view.querySelector("#uploadButton");
  const selectedFileDiv = view.querySelector("#selectedFile");
  const fileNameSpan = view.querySelector("#fileName");
  const enableAutoAssignSwitch = view.querySelector("#enableAutoAssign");

  let selectedFiles = [];

  function loadSettings() {
    ApiClient.fetch({
      url: ApiClient.getUrl("/GetAvatar/Settings"),
      type: "GET",
      dataType: "json",
    })
      .then(function (settings) {
        enableAutoAssignSwitch.checked = settings.enableAutoAssign;
      })
      .catch(function (error) {
        console.error("GetAvatar: Failed to load settings", error);
      });
  }

  function saveSettings() {
    ApiClient.fetch({
      url: ApiClient.getUrl("/GetAvatar/Settings"),
      type: "POST",
      contentType: "application/json",
      data: JSON.stringify({ enableAutoAssign: enableAutoAssignSwitch.checked }),
    })
      .then(function () {
        Dashboard.alert({ message: "Settings saved.", title: "Success" });
      })
      .catch(function (error) {
        console.error("GetAvatar: Failed to save settings", error);
        Dashboard.alert({ message: "Failed to save settings.", title: "Error" });
      });
  }

  function loadAvatars() {
    avatarListContainer.innerHTML =
      '<div class="loading-state"><p>Loading avatars...</p></div>';
    avatarCountEl.textContent = "";

    ApiClient.fetch({
      url: ApiClient.getUrl("/GetAvatar/Avatars"),
      type: "GET",
      dataType: "json",
    })
      .then(function (avatars) {
        renderAvatars(avatars);
      })
      .catch(function (error) {
        console.error("Failed to load avatars:", error);
        avatarListContainer.innerHTML =
          '<div class="empty-state"><p style="color:#e57373;">Failed to load avatars.</p></div>';
      });
  }

  function renderAvatars(avatars) {
    if (!avatars || avatars.length === 0) {
      avatarCountEl.textContent = "0 avatars";
      avatarListContainer.innerHTML = `
                <div class="empty-state">
                    <p>No avatars yet</p>
                    <p style="font-size:0.9em;">Upload your first avatar to get started.</p>
                </div>
            `;
      return;
    }

    avatarCountEl.textContent =
      avatars.length + " avatar" + (avatars.length > 1 ? "s" : "");

    // Group avatars by category
    const grouped = {};
    avatars.forEach(function (avatar) {
      const category = avatar.Category || avatar.category || "";
      if (!grouped[category]) {
        grouped[category] = [];
      }
      grouped[category].push(avatar);
    });

    const categories = Object.keys(grouped).sort(function (a, b) {
      if (a === "" && b !== "") return -1;
      if (a !== "" && b === "") return 1;
      return a.localeCompare(b);
    });

    let html = "";

    categories.forEach(function (category) {
      const categoryAvatars = grouped[category];
      const categoryLabel = category || "Uncategorized";
      const hasMultipleCategories = categories.length > 1 || category !== "";

      if (hasMultipleCategories) {
        html += `<div class="category-section">`;
        html += `<div class="category-header">
          <h3 class="category-title">${escapeHtml(categoryLabel)}</h3>
          <span class="category-count">${categoryAvatars.length} avatar${categoryAvatars.length > 1 ? "s" : ""}</span>
          ${category ? `<button class="delete-category-button" data-category="${escapeHtml(category)}" title="Delete all avatars in this category">Delete category</button>` : ""}
        </div>`;
      }

      html += '<div class="avatar-grid">';

      categoryAvatars.forEach(function (avatar) {
        const id = avatar.Id || avatar.id;
        const name = avatar.Name || avatar.name;
        const url = ApiClient.getUrl("/GetAvatar/Image/" + id);

        html += `
                <div class="avatar-card" data-avatar-id="${escapeHtml(id)}">
                    <button class="delete-button" data-avatar-id="${escapeHtml(id)}" title="Delete">\u00d7</button>
                    <div class="avatar-image-container">
                        <img src="${escapeHtml(url)}" alt="${escapeHtml(name)}" class="avatar-image" loading="lazy" width="100" height="100" />
                    </div>
                </div>
            `;
      });

      html += "</div>";

      if (hasMultipleCategories) {
        html += "</div>";
      }
    });

    avatarListContainer.innerHTML = html;

    avatarListContainer
      .querySelectorAll(".delete-button")
      .forEach(function (btn) {
        btn.addEventListener("click", function (e) {
          e.preventDefault();
          e.stopPropagation();
          deleteAvatar(this.getAttribute("data-avatar-id"));
        });
      });

    avatarListContainer
      .querySelectorAll(".delete-category-button")
      .forEach(function (btn) {
        btn.addEventListener("click", function (e) {
          e.preventDefault();
          e.stopPropagation();
          const category = this.getAttribute("data-category");
          deleteCategory(category, avatars);
        });
      });
  }

  function deleteCategory(category, avatars) {
    const categoryAvatars = avatars.filter(function (a) {
      return (a.Category || a.category || "") === category;
    });
    if (!confirm("Delete all " + categoryAvatars.length + " avatars in \"" + category + "\"?")) {
      return;
    }

    Dashboard.showLoadingMsg();

    const deletePromises = categoryAvatars.map(function (avatar) {
      const id = avatar.Id || avatar.id;
      return ApiClient.fetch({
        url: ApiClient.getUrl("/GetAvatar/Delete/" + id),
        type: "DELETE",
        dataType: "json",
      });
    });

    Promise.all(deletePromises)
      .then(function () {
        Dashboard.hideLoadingMsg();
        loadAvatars();
      })
      .catch(function (error) {
        console.error("Failed to delete category:", error);
        Dashboard.hideLoadingMsg();
        loadAvatars();
      });
  }

  function deleteAvatar(avatarId) {
    if (!confirm("Delete this avatar?")) {
      return;
    }

    Dashboard.showLoadingMsg();

    ApiClient.fetch({
      url: ApiClient.getUrl("/GetAvatar/Delete/" + avatarId),
      type: "DELETE",
      dataType: "json",
    })
      .then(function () {
        Dashboard.hideLoadingMsg();
        loadAvatars();
      })
      .catch(function (error) {
        console.error("Failed to delete avatar:", error);
        Dashboard.hideLoadingMsg();
        Dashboard.alert({
          message: "Failed to delete avatar.",
          title: "Error",
        });
      });
  }

  async function uploadAvatar() {
    if (!selectedFiles || selectedFiles.length === 0) {
      Dashboard.alert({
        message: "Please select at least one file first.",
        title: "No File",
      });
      return;
    }

    Dashboard.showLoadingMsg();

    let successCount = 0;
    let failCount = 0;
    const errors = [];

    const category = categoryInput ? categoryInput.value.trim() : "";

    for (let i = 0; i < selectedFiles.length; i++) {
      const file = selectedFiles[i];
      const formData = new FormData();
      formData.append("file", file);

      const uploadUrl = category
        ? ApiClient.getUrl("/GetAvatar/Upload") + "?category=" + encodeURIComponent(category)
        : ApiClient.getUrl("/GetAvatar/Upload");

      try {
        const response = await fetch(uploadUrl, {
          method: "POST",
          headers: { "X-Emby-Token": ApiClient.accessToken() },
          body: formData,
        });

        if (!response.ok) {
          const text = await response.text();
          throw new Error(text || "Upload failed");
        }

        await response.json();
        successCount++;
      } catch (error) {
        console.error("Failed to upload avatar:", file.name, error);
        failCount++;
        errors.push(file.name + ": " + error.message);
      }
    }

    Dashboard.hideLoadingMsg();

    fileInput.value = "";
    if (folderInput) folderInput.value = "";
    if (categoryInput) categoryInput.value = "";
    selectedFiles = [];
    selectedFileDiv.classList.remove("visible");
    uploadButton.classList.remove("visible");

    loadAvatars();

    if (failCount === 0) {
      Dashboard.alert({
        message: successCount + " avatar(s) uploaded successfully!",
        title: "Success",
      });
    } else if (successCount > 0) {
      Dashboard.alert({
        message:
          successCount +
          " avatar(s) uploaded, " +
          failCount +
          " failed.\n\n" +
          errors.join("\n"),
        title: "Partial Success",
      });
    } else {
      Dashboard.alert({
        message: "All uploads failed.\n\n" + errors.join("\n"),
        title: "Error",
      });
    }
  }

  function handleFileSelection(files, fromFolder) {
    if (!files || files.length === 0) return;

    const fileArray = Array.from(files);

    const allowedTypes = [
      "image/jpeg",
      "image/png",
      "image/webp",
      "image/gif"
    ];
    const validFiles = [];
    const validationErrors = [];

    for (let i = 0; i < fileArray.length; i++) {
      const file = fileArray[i];

      if (file.size > 5 * 1024 * 1024) {
        validationErrors.push(file.name + " exceeds 5 MB limit");
        continue;
      }

      if (!allowedTypes.includes(file.type)) {
        validationErrors.push(file.name + " has invalid file type");
        continue;
      }

      validFiles.push(file);
    }

    if (validationErrors.length > 0) {
      Dashboard.alert({
        message:
          "Some files were rejected:\n\n" +
          validationErrors.join("\n") +
          "\n\nValid files: " +
          validFiles.length,
        title: "File Validation",
      });
    }

    if (validFiles.length === 0) {
      fileInput.value = "";
      if (folderInput) folderInput.value = "";
      selectedFiles = [];
      selectedFileDiv.classList.remove("visible");
      uploadButton.classList.remove("visible");
      return;
    }

    // Auto-detect category from folder name
    if (fromFolder && categoryInput && validFiles.length > 0) {
      const firstPath = fileArray[0].webkitRelativePath || "";
      const folderName = firstPath.split("/")[0] || "";
      if (folderName && !categoryInput.value.trim()) {
        categoryInput.value = folderName;
      }
    }

    selectedFiles = validFiles;
    fileNameSpan.textContent =
      validFiles.length === 1
        ? validFiles[0].name
        : validFiles.length + " files selected";
    selectedFileDiv.classList.add("visible");
    uploadButton.classList.add("visible");
  }

  fileInput.addEventListener("change", function () {
    if (this.files && this.files.length > 0) {
      handleFileSelection(this.files, false);
    } else {
      selectedFiles = [];
      selectedFileDiv.classList.remove("visible");
      uploadButton.classList.remove("visible");
    }
  });

  if (folderInput) {
    folderInput.addEventListener("change", function () {
      if (this.files && this.files.length > 0) {
        handleFileSelection(this.files, true);
      }
    });
  }

  uploadButton.addEventListener("click", uploadAvatar);
  enableAutoAssignSwitch.addEventListener("change", saveSettings);

  const downloadOnlinePackButton = view.querySelector("#downloadOnlinePackButton");
  const onlinePackDialog = view.querySelector("#onlinePackDialog");
  const onlinePackList = view.querySelector("#onlinePackList");
  const onlinePackResult = view.querySelector("#onlinePackResult");
  const importOnlinePacksButton = view.querySelector("#importOnlinePacksButton");
  const importAllOnlinePacksButton = view.querySelector("#importAllOnlinePacksButton");
  const closeOnlinePackDialogButton = view.querySelector("#closeOnlinePackDialog");
  const cancelOnlinePackDialog = view.querySelector("#cancelOnlinePackDialog");

  let onlinePacks = [];

  function formatBytes(bytes) {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
  }

  function openOnlinePackDialog() {
    if (onlinePackDialog) {
      onlinePackDialog.style.display = "block";
    }
    if (onlinePackResult) {
      onlinePackResult.style.display = "none";
      onlinePackResult.textContent = "";
    }
    loadOnlinePacks();
  }

  function closeOnlinePackDialogFn() {
    if (onlinePackDialog) {
      onlinePackDialog.style.display = "none";
    }
    onlinePacks = [];
    if (onlinePackList) {
      onlinePackList.innerHTML = "";
    }
    if (importOnlinePacksButton) {
      importOnlinePacksButton.disabled = true;
      importOnlinePacksButton.textContent = "Import Selected";
    }
  }

  function loadOnlinePacks() {
    if (!onlinePackList || !importOnlinePacksButton) return;

    onlinePackList.innerHTML = '<div class="loading-state">Loading packs...</div>';
    importOnlinePacksButton.disabled = true;

    ApiClient.fetch({
      url: ApiClient.getUrl("/GetAvatar/OnlinePacks"),
      type: "GET",
      dataType: "json",
    })
      .then(function (packs) {
        onlinePacks = packs || [];
        renderOnlinePacks();
      })
      .catch(function (error) {
        console.error("GetAvatar: Failed to load online packs", error);
        onlinePackList.innerHTML = '<div class="online-pack-error">Failed to load online packs. Please try again later.</div>';
      });
  }

  function renderOnlinePacks() {
    if (!onlinePackList) return;

    if (!onlinePacks || onlinePacks.length === 0) {
      onlinePackList.innerHTML = '<div class="online-pack-empty">No online packs available.</div>';
      return;
    }

    let html = '<div class="online-pack-list">';
    onlinePacks.forEach(function (pack) {
      const id = pack.Id || pack.id || "";
      const name = pack.Name || pack.name || "";
      const size = formatBytes(pack.Size || pack.size || 0);
      html += `
        <label class="online-pack-item">
          <input type="checkbox" value="${escapeHtml(id)}" />
          <div class="online-pack-info">
            <span class="online-pack-name">${escapeHtml(name)}</span>
            <span class="online-pack-size">${escapeHtml(size)}</span>
          </div>
        </label>
      `;
    });
    html += '</div>';

    onlinePackList.innerHTML = html;
    updateImportButton();

    onlinePackList.querySelectorAll('input[type="checkbox"]').forEach(function (checkbox) {
      checkbox.addEventListener("change", updateImportButton);
    });
  }

  function updateImportButton() {
    if (!onlinePackList || !importOnlinePacksButton) return;
    const checked = onlinePackList.querySelectorAll('input[type="checkbox"]:checked');
    importOnlinePacksButton.disabled = checked.length === 0;
  }

  async function importOnlinePacks() {
    if (!onlinePackList || !importOnlinePacksButton || !onlinePackResult) return;

    const checked = onlinePackList.querySelectorAll('input[type="checkbox"]:checked');
    const packIds = Array.from(checked).map(function (cb) { return cb.value; });

    if (packIds.length === 0) return;

    importOnlinePacksButton.disabled = true;
    importOnlinePacksButton.textContent = "Importing...";

    try {
      const response = await fetch(ApiClient.getUrl("/GetAvatar/ImportOnlinePacks"), {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Emby-Token": ApiClient.accessToken(),
        },
        body: JSON.stringify({ packs: packIds }),
      });

      if (!response.ok) {
        const text = await response.text();
        throw new Error(text || "Import failed");
      }

      const result = await response.json();
      const imported = result.importedCount || 0;
      const total = result.totalImages || 0;
      const duplicates = result.duplicateCount || 0;

      onlinePackResult.style.display = "block";

      if (imported > 0) {
        onlinePackResult.style.background = "rgba(82, 181, 75, 0.1)";
        onlinePackResult.textContent = "Successfully imported " + imported + " avatar(s).";
      } else if (total > 0 && duplicates === total) {
        onlinePackResult.style.background = "rgba(255, 255, 255, 0.1)";
        onlinePackResult.textContent = "All " + total + " avatar(s) are already imported.";
      } else {
        onlinePackResult.style.background = "rgba(255, 193, 7, 0.1)";
        onlinePackResult.textContent = "No new avatars to import.";
      }

      loadAvatars();

      setTimeout(function () {
        closeOnlinePackDialogFn();
      }, 1500);
    } catch (error) {
      console.error("GetAvatar: Failed to import online packs", error);
      onlinePackResult.style.display = "block";
      onlinePackResult.style.background = "rgba(229, 57, 53, 0.1)";
      onlinePackResult.textContent = "Import failed: " + error.message;
    } finally {
      importOnlinePacksButton.disabled = false;
      importOnlinePacksButton.textContent = "Import Selected";
      updateImportButton();
    }
  }

  async function importAllOnlinePacks() {
    if (!onlinePackList || !importOnlinePacksButton) return;

    const checkboxes = onlinePackList.querySelectorAll('input[type="checkbox"]');
    checkboxes.forEach(function (cb) {
      cb.checked = true;
    });
    updateImportButton();

    await importOnlinePacks();
  }

  if (downloadOnlinePackButton) {
    downloadOnlinePackButton.addEventListener("click", function(e) {
      e.preventDefault();
      e.stopPropagation();
      openOnlinePackDialog();
    });
  }

  // Fallback: delegate click in case Jellyfin replaces the button element
  view.addEventListener("click", function(e) {
    var btn = e.target.closest("#downloadOnlinePackButton");
    if (btn) {
      e.preventDefault();
      e.stopPropagation();
      openOnlinePackDialog();
    }
  });
  if (closeOnlinePackDialogButton) {
    closeOnlinePackDialogButton.addEventListener("click", closeOnlinePackDialogFn);
  }
  if (cancelOnlinePackDialog) {
    cancelOnlinePackDialog.addEventListener("click", closeOnlinePackDialogFn);
  }
  if (importOnlinePacksButton) {
    importOnlinePacksButton.addEventListener("click", importOnlinePacks);
  }
  if (importAllOnlinePacksButton) {
    importAllOnlinePacksButton.addEventListener("click", function(e) {
      e.preventDefault();
      e.stopPropagation();
      importAllOnlinePacks();
    });
  }

  view.addEventListener("viewshow", function () {
    loadSettings();
    loadAvatars();
  });
}
