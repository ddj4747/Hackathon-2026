# Desktop releases

## Setup
Push the Unity project to GitHub, including Assets and their .meta files, Packages, ProjectSettings, and `.github/workflows/release.yml`.

Add these repository secrets under Settings > Secrets and variables > Actions:

| Secret | Value |
| --- | --- |
| UNITY_EMAIL | Unity account email |
| UNITY_PASSWORD | Unity account password |
| UNITY_LICENSE | Personal license .ulf file contents |
| UNITY_SERIAL | Pro serial, instead of UNITY_LICENSE if using Pro |

Use only the license method appropriate to your account. Follow [GameCI activation guidance](https://game.ci/docs/github/activation/); Personal license activation support depends on Unity licensing. Never commit credentials or license files. GitHub provides the release token automatically; no extra GitHub PAT is needed.

## Publish
Commit and push the workflow before creating the tag:

```sh
git tag v1.0.0
git push origin v1.0.0
```

Stable tags `vMAJOR.MINOR.PATCH` trigger the workflow. Prerelease tags are rejected. Watch Actions > Build and release.

## Behavior and dependencies
- GameCI unity-builder v4 reads the exact Unity version from `ProjectSettings/ProjectVersion.txt` and builds enabled scenes from project build settings. Matching GameCI editor images/modules are required; availability for Unity 6000.5.0f1 was not verified locally.
- Windows x64, Linux x64, and macOS are cross-built on Ubuntu using the existing desktop Mono backend. Switching to IL2CPP requires revisiting the runner/toolchain matrix. macOS architecture follows Unity project/default settings; a universal binary is not guaranteed.
- Builds run sequentially to avoid concurrent activation of a single license. The player version comes from the tag; the output filename is Hackathon2026.
- Only after all builds succeed does the workflow upload Windows ZIP, Linux/macOS tarballs, and SHA256SUMS.txt to a GitHub release. Tarballs preserve executable permissions and bundle symlinks.
- New releases stay draft until uploads complete. Rerunning replaces same-named release assets. Other release assets are retained.
- Only the release job has `contents: write`. The build jobs use read access.
- Windows/macOS binaries are unsigned; macOS builds are not notarized.

## Verification and status
The workflow has not run on GitHub. Local checks cover YAML structure and platform/archive mappings, not Unity compilation or runtime behavior. Bash syntax validation was attempted but Git Bash could not start (Win32 error 5 creating a signal pipe). Download and smoke-test each player on its target OS after the first successful run.

The intended module note `.agents/release-builds.md` could not be created because that directory is protected in this session. This document contains its setup and behavior information until the note can be placed there.
