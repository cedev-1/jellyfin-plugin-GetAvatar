# Jellyfin Plugin GetAvatar

Jellyfin plugin that allows users to choose an avatar from a collection of images.

![banner](./assets/banner.png)

# About

This plugin allows users to choose an avatar from a collection of images. The plugin is installed in the Jellyfin server and provides a button in the user profile to select an avatar.

> [!NOTE]
> Tested with Jellyfin 10.11.0 and 10.11.10.
> Expected to work across Jellyfin 10.11.x.

## Installation

1. You have to open the dashboard of your Jellyfin server. Go to Catalog, click on ⚙️ button.
2. Click to + to add the URL.

```bash
https://raw.githubusercontent.com/cedev-1/Jellyfin-Plugin-GetAvatar/master/manifest.json
```

3. On the Catalog page click on Install.

## Plugin Configuration

You just have to add avatar images, click on "Choose file" and "Upload". The image will be added to the collection of images. You can also remove images by clicking on the cross icon.

<img width="1744" height="1042" alt="image" src="https://github.com/user-attachments/assets/7b076ecf-8415-4d65-b65c-7b248bac2536" />

## User View

User profile page will have a new button "Choose from Gallery".

<img width="3504" height="2092" alt="Screenshot 2026-06-09 at 19-03-44 cedev" src="https://github.com/user-attachments/assets/7f70202e-8f59-4a90-96f6-c0ebeef78615" />

<img width="1742" height="1040" alt="image" src="https://github.com/user-attachments/assets/64dd4f8f-3062-4dd4-bed0-1ff8de069958" />

## Avatar

> [!TIP]
> Now, since version 1.6.4.0, you can directly import avatars with one click from the plugin's configuration interface.

You can import avatar packs from the repository [jellyfin-avatars](https://github.com/cedev-1/jellyfin-avatars) or direct download from [web page](https://cedev-1.github.io/jellyfin-avatars/). 

<img width="436" height="337" alt="image" src="https://github.com/user-attachments/assets/baa16380-6fd1-4d7e-9ef0-026ed04f6572" />

For example, you can import this [Netflix avatar pack](https://imgur.com/gallery/netflix-all-profile-icons-ToZ21Gg).

## Disclaimer

It may have some bugs. If you find any bug, please open an [issue](https://github.com/cedev-1/jellyfin-plugin-GetAvatar/issues).

# Troubleshooting

It is possible that on already active sessions the button is not displayed on the profile. You can clear your browser cache.

## License

[MIT](./LICENSE)
