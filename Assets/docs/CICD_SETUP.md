# CI/CD Setup Guide for Walk Game

This guide will help you set up automated builds and testing for both Android and iOS.

## Overview

The CI/CD pipeline uses **GitHub Actions** with **GameCI** to:

- ✅ Run unit tests on every push/PR
- 🤖 Build Android APK automatically
- 🍎 Build iOS Xcode project automatically
- 📦 Create releases with downloadable builds

## Prerequisites

- GitHub repository with your Unity project
- Unity account (free tier works!)
- For iOS deployment: Mac with Xcode, Apple Developer account

---

## Step 1: Get Your Unity License File

GameCI needs your Unity license to build projects. Here's how to get it:

### Option A: Using the Activation Workflow (Recommended)

1. Push the workflow files to your repository
2. Go to **Actions** tab in GitHub
3. Select **"Acquire Unity License"** workflow
4. Click **"Run workflow"** → **"Run workflow"**
5. Wait for it to complete
6. Download the `Unity_Activation_File` artifact
7. Extract the `.alf` file

### Option B: Get License from Local Unity Installation

Your license file location:
- **Windows:** `C:\ProgramData\Unity\Unity_lic.ulf`
- **Mac:** `/Library/Application Support/Unity/Unity_lic.ulf`
- **Linux:** `~/.local/share/unity3d/Unity/Unity_lic.ulf`

---

## Step 2: Activate the License (if using Option A)

1. Go to [Unity Manual Activation](https://license.unity3d.com/manual)
2. Upload your `.alf` file
3. Select license type (Personal/Plus/Pro)
4. Download the resulting `.ulf` file

---

## Step 3: Add GitHub Secrets

Go to your repository: **Settings** → **Secrets and variables** → **Actions**

Add these secrets:

### Required Secrets

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `UNITY_LICENSE` | Contents of your `.ulf` file | Open the `.ulf` file in a text editor, copy ALL contents |
| `UNITY_EMAIL` | Your Unity account email | The email you use to log into Unity |
| `UNITY_PASSWORD` | Your Unity account password | Your Unity account password |

### Optional: Android Signing Secrets

For signed APKs (required for Play Store):

| Secret Name | Description |
|-------------|-------------|
| `ANDROID_KEYSTORE_BASE64` | Base64-encoded keystore file |
| `ANDROID_KEYSTORE_PASS` | Keystore password |
| `ANDROID_KEYALIAS_NAME` | Key alias name |
| `ANDROID_KEYALIAS_PASS` | Key alias password |

To create base64 keystore:
```bash
base64 -i your-keystore.keystore | pbcopy  # Mac
base64 your-keystore.keystore | clip       # Windows
base64 your-keystore.keystore              # Linux (copy output)
```

### Optional: iOS Signing Secrets

For signed IPA builds:

| Secret Name | Description |
|-------------|-------------|
| `APPLE_CERTIFICATE_BASE64` | Base64-encoded .p12 certificate |
| `APPLE_CERTIFICATE_PASSWORD` | Certificate password |
| `APPLE_PROVISIONING_PROFILE_BASE64` | Base64-encoded provisioning profile |

---

## Step 4: Configure Unity Project

### Required Project Settings

1. **Add at least one scene** to Build Settings
   - File → Build Settings → Add Open Scenes

2. **Set correct bundle identifier**
   - Edit → Project Settings → Player → Android/iOS → Other Settings
   - Example: `com.yourcompany.walkgame`

3. **Set minimum API levels**
   - Android: API Level 29+ (for step counter permission)
   - iOS: 12.0+

4. **Enable required capabilities** (iOS)
   - The post-processor script handles this automatically

### Recommended Settings

1. **IL2CPP backend** (for better performance)
   - Project Settings → Player → Other Settings → Scripting Backend

2. **ARM64 architecture**
   - Project Settings → Player → Other Settings → Target Architectures

---

## Step 5: Workflow Files

Copy these files to your repository:

```
.github/
└── workflows/
    ├── activation.yml    # One-time license activation
    ├── build.yml         # Main CI/CD workflow
    └── release.yml       # Release creation workflow
```

---

## Using the CI/CD Pipeline

### Automatic Builds

Builds trigger automatically on:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`

### Manual Builds

1. Go to **Actions** tab
2. Select **"Build & Test"** workflow
3. Click **"Run workflow"**
4. Choose options (build Android, iOS, run tests)
5. Click **"Run workflow"**

### Creating a Release

1. Update version in Unity
2. Commit changes
3. Create a tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
4. The release workflow will:
   - Build Android APK
   - Build iOS Xcode project
   - Create GitHub Release with downloads

### Downloading Builds

1. Go to **Actions** tab
2. Click on the workflow run
3. Scroll to **Artifacts** section
4. Download the build for your platform

---

## Installing Test Builds

### Android

1. Download the `.apk` file to your phone
2. Open the file
3. If prompted, enable "Install from unknown sources"
4. Install and run

### iOS (Requires Mac)

1. Download the Xcode project zip
2. Extract on your Mac
3. Open `.xcodeproj` in Xcode
4. Select your development team
5. Connect your iPhone
6. Select your device as build target
7. Click **Run** (⌘R)

---

## Troubleshooting

### "Unity license is invalid"

- Make sure you copied the ENTIRE contents of the `.ulf` file
- The file should start with `<?xml` and end with `</root>`
- Check there are no extra spaces or line breaks

### "Build failed: No scenes in build"

- Open Unity
- Go to File → Build Settings
- Add your scenes to the build

### "Android SDK not found"

- GameCI handles this automatically
- If issues persist, check Unity version matches workflow

### "iOS build requires macOS"

- The Unity → Xcode project build runs on Linux
- The Xcode → IPA build requires macOS runner
- For testing, download Xcode project and build locally

### Build takes too long

- First builds are slow (20-40 min) due to library caching
- Subsequent builds are faster (10-20 min)
- Consider self-hosted runners for faster builds

---

## Cost Considerations

### GitHub Actions Free Tier

- **Public repos:** Unlimited free minutes
- **Private repos:** 2,000 minutes/month free

### Estimated Build Times

| Platform | First Build | Cached Build |
|----------|-------------|--------------|
| Android  | ~30 min     | ~15 min      |
| iOS      | ~35 min     | ~20 min      |
| Tests    | ~10 min     | ~5 min       |

### Tips to Save Minutes

1. Use caching (already configured)
2. Skip builds with `[skip ci]` in commit message
3. Use manual triggers for expensive builds
4. Consider self-hosted runners

---

## Advanced: Self-Hosted Runners

For faster builds and lower costs, you can set up self-hosted runners:

1. Go to **Settings** → **Actions** → **Runners**
2. Click **"New self-hosted runner"**
3. Follow instructions for your OS
4. Modify workflow files: change `runs-on: ubuntu-latest` to `runs-on: self-hosted`

---

## Next Steps

1. ✅ Set up secrets
2. ✅ Push workflow files
3. ✅ Run activation workflow
4. ✅ Trigger first build
5. ✅ Download and test on device
6. 🎉 Iterate!

For questions or issues, check:
- [GameCI Documentation](https://game.ci/docs)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
