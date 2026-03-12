# Code Signing Guide — SignPath.io (Free for OSS)

This guide covers setting up free code signing for the Firebot Giveaway OBS Overlay using [SignPath.io](https://signpath.io/), which eliminates the Windows SmartScreen "not trusted" warning for users.

## Why Code Signing?

Windows SmartScreen flags unsigned executables with a "Windows protected your PC" warning. Code signing cryptographically verifies the publisher identity and guarantees the binary hasn't been tampered with. SignPath.io offers this for free to open-source projects.

## Prerequisites

- **Public GitHub repository** with an OSI-approved open-source license
- **Actively maintained** project with existing releases
- No proprietary code (system libraries are fine)
- Team members must use multi-factor authentication

## Step 1: Apply to SignPath Foundation

1. Go to [signpath.org](https://signpath.org/)
2. Fill out the OSS application form
3. Submit via email
4. Wait for approval (review of project eligibility)

## Step 2: Initial Setup (After Approval)

### Create SignPath Project
1. Log in to [SignPath](https://app.signpath.io/)
2. Create a new project with slug: `FirebotGiveawayOverlay`
3. Link the GitHub repository as a **trusted build system** (use predefined "GitHub.com")

### Install GitHub App
1. Install the [SignPath GitHub App](https://github.com/apps/signpath) on your repository
2. Grant it access to the overlay repo

### Configure Secrets and Variables
Add to your GitHub repository:
- **Secret**: `SIGNPATH_API_TOKEN` — generate from SignPath console
- **Variable**: `SIGNPATH_ORGANIZATION_ID` — found in SignPath console under organization settings

## Step 3: Create Artifact Configuration

Create `.signpath/artifact-configuration.xml` in the repo root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<artifact-configuration xmlns="http://signpath.io/artifact-configuration/v1">
  <zip-file>
    <pe-file path="FirebotGiveawayObsOverlay.WebApp.exe">
      <authenticode-sign/>
    </pe-file>
  </zip-file>
</artifact-configuration>
```

This tells SignPath to open the ZIP artifact and Authenticode-sign the .exe inside it.

## Step 4: Update GitHub Actions Workflow

Modify `.github/workflows/release.yml` to add signing between the build and release jobs.

### Add Signing to the Build Job

After the existing "Upload artifact" step, add:

```yaml
      - name: Upload artifact for signing
        uses: actions/upload-artifact@v4
        id: upload-unsigned
        with:
          name: unsigned-${{ matrix.runtime }}
          path: FirebotGiveawayOverlay-${{ needs.check-version.outputs.version }}-${{ matrix.runtime }}.zip

      - name: Sign with SignPath
        uses: signpath/github-action-submit-signing-request@v2
        with:
          api-token: '${{ secrets.SIGNPATH_API_TOKEN }}'
          organization-id: '${{ vars.SIGNPATH_ORGANIZATION_ID }}'
          project-slug: 'FirebotGiveawayOverlay'
          signing-policy-slug: 'release-signing'
          github-artifact-id: '${{ steps.upload-unsigned.outputs.artifact-id }}'
          wait-for-completion: true
          output-artifact-directory: './signed'
```

### Update Release Job

Change the release job to download and use the signed artifacts instead of the unsigned ones.

### Full Workflow Reference

See the [SignPath GitHub Actions Demo](https://github.com/SignPath/github-actions-demo) for a complete working example.

## Step 5: Publish Code Signing Policy

SignPath Foundation requires a published code signing policy. Add a section to your README or create a dedicated page documenting:
- Who has signing authority (team roles)
- What gets signed (release builds only)
- How the signing pipeline works (GitHub Actions → SignPath)

## Step 6: Verify

After the first signed release:

1. Download the ZIP from GitHub Releases
2. Extract the .exe
3. Right-click the .exe → **Properties** → **Digital Signatures** tab
4. You should see a valid Authenticode signature
5. Running the .exe should **not** trigger SmartScreen

## Resources

- [SignPath Foundation (OSS applications)](https://signpath.org/)
- [SignPath Documentation](https://docs.signpath.io/)
- [GitHub Integration Docs](https://docs.signpath.io/trusted-build-systems/github)
- [SignPath GitHub Action](https://github.com/marketplace/actions/submit-a-signing-request)
- [SignPath GitHub Actions Demo](https://github.com/SignPath/github-actions-demo)
- [DB Browser for SQLite — SignPath case study](https://sqlitebrowser.org/blog/signing-windows-executables-our-journey-with-signpath/)
