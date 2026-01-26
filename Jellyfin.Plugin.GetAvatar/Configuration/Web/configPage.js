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
.avatar-thumb {
    width: 35px;
    height: 35px;
    border-radius: 4px;
    object-fit: cover;
}
.delete-btn {
    background: none;
    border: none;
    color: #999;
    cursor: pointer;
    padding: 0.3em 0.5em;
    font-size: 1.1em;
    border-radius: 4px;
    opacity: 0.5;
    transition: all 0.2s;
}
.delete-btn:hover {
    color: #e53935;
    background: rgba(229, 57, 53, 0.1);
    opacity: 1;
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
`;

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
  const uploadButton = view.querySelector("#uploadButton");
  const selectedFileDiv = view.querySelector("#selectedFile");
  const fileNameSpan = view.querySelector("#fileName");

  let selectedFile = null;

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

    let html = '<div class="avatar-grid">';

    avatars.forEach(function (avatar) {
      const date = new Date(
        avatar.DateAdded || avatar.dateAdded,
      ).toLocaleDateString();
      const id = avatar.Id || avatar.id;
      const name = avatar.Name || avatar.name;
      const url = avatar.Url || avatar.url;

      html += `
                <div class="avatar-card" data-avatar-id="${id}">
                    <button class="delete-button" data-avatar-id="${id}" title="Delete">×</button>
                    <div class="avatar-image-container">
                        <img src="${url}" alt="${name}" class="avatar-image" loading="lazy" width="100" height="100" />
                    </div>
                </div>
            `;
    });

    html += "</div>";
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

  function uploadAvatar() {
    if (!selectedFile) {
      Dashboard.alert({
        message: "Please select a file first.",
        title: "No File",
      });
      return;
    }

    const formData = new FormData();
    formData.append("file", selectedFile);

    Dashboard.showLoadingMsg();

    fetch(ApiClient.getUrl("/GetAvatar/Upload"), {
      method: "POST",
      headers: { "X-Emby-Token": ApiClient.accessToken() },
      body: formData,
    })
      .then(function (response) {
        if (!response.ok) {
          return response.text().then(function (text) {
            throw new Error(text || "Upload failed");
          });
        }
        return response.json();
      })
      .then(function () {
        Dashboard.hideLoadingMsg();

        fileInput.value = "";
        selectedFile = null;
        selectedFileDiv.classList.remove("visible");
        uploadButton.classList.remove("visible");

        loadAvatars();
      })
      .catch(function (error) {
        console.error("Failed to upload avatar:", error);
        Dashboard.hideLoadingMsg();
        Dashboard.alert({
          message: error.message || "Failed to upload avatar.",
          title: "Error",
        });
      });
  }

  fileInput.addEventListener("change", function () {
    if (this.files && this.files.length > 0) {
      selectedFile = this.files[0];

      if (selectedFile.size > 5 * 1024 * 1024) {
        Dashboard.alert({
          message: "File exceeds 5 MB limit.",
          title: "File Too Large",
        });
        this.value = "";
        selectedFile = null;
        selectedFileDiv.classList.remove("visible");
        uploadButton.classList.remove("visible");
        return;
      }

      const allowedTypes = [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
      ];
      if (!allowedTypes.includes(selectedFile.type)) {
        Dashboard.alert({ message: "Invalid file type.", title: "Error" });
        this.value = "";
        selectedFile = null;
        selectedFileDiv.classList.remove("visible");
        uploadButton.classList.remove("visible");
        return;
      }

      fileNameSpan.textContent = selectedFile.name;
      selectedFileDiv.classList.add("visible");
      uploadButton.classList.add("visible");
    } else {
      selectedFile = null;
      selectedFileDiv.classList.remove("visible");
      uploadButton.classList.remove("visible");
    }
  });

  uploadButton.addEventListener("click", uploadAvatar);

  view.addEventListener("viewshow", function () {
    loadAvatars();
  });
}
