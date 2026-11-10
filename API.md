# GetAvatar client API

This page describes the HTTP routes that Jellyfin clients can use to show the avatar gallery and set a user's profile picture. The Jellyfin web UI uses the same routes. It is written for third-party clients such as [Moonfin](https://github.com/Moonfin-Client/Moonfin-Core).

Paths are relative to the Jellyfin server address, including its base URL when one is configured (for example `https://jellyfin.example.com/jellyfin/GetAvatar/Avatars`).

## Authentication

Routes marked **user** accept the access token of any Jellyfin user. Send it in the `Authorization` header:

```http
Authorization: MediaBrowser Token="<access token>"
```

The same header can also carry the usual client fields (`MediaBrowser Client="...", Device="...", DeviceId="...", Version="...", Token="..."`). Do not rely on the `X-Emby-Token` header or the `api_key` query parameter: since Jellyfin 12, the server ignores them unless an administrator turns legacy authorization back on (`EnableLegacyAuthorization`, see [#15](https://github.com/cedev-1/jellyfin-plugin-GetAvatar/issues/15)).

`SetAvatar` and `Gravatar` read the caller's user ID from the session and answer `401` when there is none, for example with a server API key. Use a user access token for them.

Routes marked **none** need no token.

## Detecting the plugin

`GET /Plugins` is reserved to administrators since Jellyfin 10.9, so a client signed in as a regular user detects GetAvatar through its own routes:

1. `GET /GetAvatar/Avatars`: `200` means the plugin is installed (the list can be empty). `404` means it is not.
2. `GET /GetAvatar/Features`:
   - `200` with `apiVersion`: compare `apiVersion` with the versions your client supports (see [Versioning policy](#versioning-policy-proposal)).
   - `200` without `apiVersion`: plugin 1.7.0.0 to 1.7.5.0. The routes on this page behave as in API version 1, but `Features` only returns `gravatarEnabled`.
   - `404`: plugin older than 1.7.0.0. `Avatars`, `Image` and `SetAvatar` are available, `Features` and `Gravatar` are not. These versions can differ from this page: `Category` exists since 1.6.1.0, the `userId` field of `SetAvatar` since 1.5.3.0, and a non-administrator who targets another user gets a server error instead of `403`.

## Routes

| Route | Auth | Purpose |
| --- | --- | --- |
| `GET /GetAvatar/Avatars` | user | List the gallery. |
| `GET /GetAvatar/Image/{avatarId}` | none | Get an avatar image. |
| `POST /GetAvatar/SetAvatar` | user | Set a gallery avatar as a user's profile image. |
| `GET /GetAvatar/Features` | user | Read the optional features and the API version. |
| `POST /GetAvatar/Gravatar` | user | Set a user's Gravatar as their profile image. |

### GET /GetAvatar/Avatars

Returns a JSON array with one object per avatar in the gallery.

| Field | Type | Description |
| --- | --- | --- |
| `Id` | string | Avatar ID. Treat it as opaque. |
| `Name` | string | Display name (the source file name without its extension). |
| `FileName` | string | Name of the stored file. |
| `DateAdded` | string | Date the avatar was added, ISO 8601 in UTC. |
| `Category` | string | Category (set at upload, or the Immich album or avatar pack name). `""` when there is none. |
| `Url` | string | Path of the image: `/GetAvatar/Image/{Id}`. It does not include the server base URL. |

Field names are PascalCase. A client that sends `Accept: application/json; profile="CamelCase"` gets them in camelCase (`id`, `name`, ...).

```json
[
  {
    "Id": "6f1c1b0e-2d5a-4a4e-9b8e-6f0f3c7d2a11",
    "Name": "fox",
    "FileName": "6f1c1b0e-2d5a-4a4e-9b8e-6f0f3c7d2a11.png",
    "DateAdded": "2026-10-11T08:15:30.1234567Z",
    "Category": "Animals",
    "Url": "/GetAvatar/Image/6f1c1b0e-2d5a-4a4e-9b8e-6f0f3c7d2a11"
  }
]
```

Status codes: `200`, `500`.

### GET /GetAvatar/Image/{avatarId}

Returns the image file of an avatar, so it can be used directly as an image source. The `Content-Type` is `image/jpeg`, `image/png`, `image/webp` or `image/gif` (`application/octet-stream` for any other extension). The image behind an ID never changes, so clients can cache it by ID.

Status codes: `200`, `404` (unknown ID or missing file), `500`.

### POST /GetAvatar/SetAvatar

Sets a gallery avatar as a user's profile image. Request body (`Content-Type: application/json`):

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `avatarId` | string | yes | An `Id` returned by `Avatars`. |
| `userId` | string | no | Jellyfin user ID (GUID, with or without dashes). Defaults to the caller. Only administrators can target another user. |

```json
{ "avatarId": "6f1c1b0e-2d5a-4a4e-9b8e-6f0f3c7d2a11" }
```

Response `200`:

```json
{ "message": "Avatar set successfully" }
```

| Status | When |
| --- | --- |
| `400` | Body missing or not JSON, `avatarId` missing or empty, `userId` not a GUID, unknown user, unknown avatar. |
| `401` | No user in the session. |
| `403` | `userId` is another user and the caller is not an administrator. |
| `415` | `Content-Type` header missing or not JSON (for example `text/plain`). |
| `500` | Unexpected error. |

The avatar is written as the user's standard Jellyfin profile image, the one `POST /UserImage` sets. The user's `PrimaryImageTag` changes, and every client gets the new picture from `GET /UserImage?userId={userId}`. See also [Recorded avatar](#recorded-avatar).

### GET /GetAvatar/Features

Returns the optional features enabled by the administrator, and the versions.

| Field | Type | Description |
| --- | --- | --- |
| `gravatarEnabled` | boolean | `true` when users can import their Gravatar with `POST /GetAvatar/Gravatar`. |
| `apiVersion` | integer | Version of the API described on this page. Absent in plugin 1.7.5.0 and older. |
| `version` | string | Plugin version, for example `1.8.0.0`. Empty when unknown. Informational: base feature checks on `apiVersion` and the feature flags. |

These names are camelCase whatever the `Accept` profile.

```json
{ "gravatarEnabled": false, "apiVersion": 1, "version": "1.8.0.0" }
```

Status codes: `200`.

### POST /GetAvatar/Gravatar

Downloads the Gravatar of an email address and sets it as a user's profile image. Only available when `Features` returns `"gravatarEnabled": true`. Request body (`Content-Type: application/json`):

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `email` | string | yes | Bare email address (`user@example.com`, not `Name <user@example.com>`). Only used to compute the Gravatar hash, never stored or logged. |
| `userId` | string | no | Same as for `SetAvatar`. |

Response `200`:

```json
{ "message": "Gravatar imported successfully" }
```

| Status | When |
| --- | --- |
| `400` | Body missing or not JSON, invalid email address, `userId` not a GUID, unknown user. |
| `401` | No user in the session. |
| `403` | Gravatar import is disabled, or `userId` is another user and the caller is not an administrator. |
| `404` | The address has no Gravatar. |
| `415` | `Content-Type` header missing or not JSON (for example `text/plain`). |
| `502` | Gravatar could not be reached, timed out, answered with an error, or returned an unsupported or too large (over 5 MB) image. |
| `500` | Unexpected error. |

Like `SetAvatar`, it writes the standard Jellyfin profile image. It also clears the [recorded avatar](#recorded-avatar).

## Recorded avatar

`SetAvatar` also records, in the plugin configuration, which gallery avatar the user chose. At each server start, and when an administrator runs the avatar validation, the plugin sets that avatar again for a user whose profile image file is missing. When the administrator enabled automatic assignment, a server start assigns a random avatar instead.

As a result, deleting the profile image with Jellyfin's `DELETE /UserImage` does not remove it for good: an avatar comes back at the next server start. Uploading an image with `POST /UserImage` keeps the record too, but the uploaded image stays as long as its file exists. The record is cleared by `Gravatar`, by the Immich profile picture sync, by the administrator route `RemoveUserAvatar`, and when the avatar is deleted from the gallery.

## Errors

Branch on the HTTP status code. Messages are short English texts meant for logs and may change.

- Errors returned by the plugin carry the message as a JSON string, for example `"Invalid user ID format"`.
- Errors raised by ASP.NET Core before the plugin code runs (empty or malformed body, a required field sent as `null`, missing or wrong `Content-Type`) and the `404` of `Image/{avatarId}` carry a problem details object (`application/problem+json`) with `type`, `title`, `status` and, for validation errors, `errors`.
- `401` and `403` answered by Jellyfin's authentication (missing or invalid token, disabled user) carry no plugin message.

## OpenAPI

Jellyfin lists these routes, under the `Avatar` tag, in its own API documentation: `/api-docs/swagger` (Swagger UI), `/api-docs/redoc`, and the OpenAPI document at `/api-docs/openapi.json`. `Features` has a typed schema (`GetAvatarFeaturesResponse`). The other routes list their status codes. Jellyfin does not load plugin XML documentation, so route descriptions are missing there. It also shows a lock on `Image/{avatarId}` although that route needs no token, and labels `400` responses as `ProblemDetails`, although the plugin's own `400` messages are JSON strings (see [Errors](#errors)). This page is the reference.

## Versioning policy (proposal)

- `apiVersion` starts at `1` and covers the routes on this page.
- It is incremented only for a breaking change to these routes: a route or field removed or renamed, a field type or casing changed, a request field made required, or a status code given another meaning.
- Additive changes keep the same `apiVersion`: new routes, new response fields, new optional request fields. Clients must ignore fields they do not know.
- Message texts, administrator routes and the routes under [Not covered](#not-covered) are outside this policy.

## Not covered

- `GET /GetAvatar/UserAvatar/{userId}` returns the gallery image of the [recorded avatar](#recorded-avatar). It can differ from the user's real profile image: it answers `404` after a Gravatar import and keeps returning the old avatar after an upload through Jellyfin. Use `GET /UserImage?userId={userId}`.
- `GET /GetAvatar/ClientScript` is the script injected into the Jellyfin web UI.
- Administrator routes: `Upload`, `Delete/{avatarId}`, `RemoveUserAvatar`, `ValidateAvatars`, `AssignMissing`, `CleanupOrphans`, `AvatarStatus`, `Settings`, `ImmichTest`, `ImmichAlbums`, `ImportImmichAlbum`, `SyncImmichAlbums`, `ImmichUsers`, `ImmichUserMappings`, `SyncImmichProfiles`, `OnlinePacks`, `ImportOnlinePacks`.
