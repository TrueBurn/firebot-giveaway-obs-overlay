# Getting Started

This guide will help you download, install, and configure the Firebot Giveaway OBS Overlay for your first use.

## Prerequisites

Before you begin, make sure you have:

- **Windows 10/11** (x86 or x64)
- **Firebot** installed and configured with file-based giveaway output
- **OBS Studio** (or compatible streaming software with browser source support)

## Download

1. Go to the [GitHub Releases](https://github.com/trueburn/firebot-giveaway-obs-overlay/releases) page
2. Download the appropriate ZIP file for your system:
   - `FirebotGiveawayOverlay-x.x.x-win-x64.zip` - For 64-bit Windows (recommended)
   - `FirebotGiveawayOverlay-x.x.x-win-x86.zip` - For 32-bit Windows
3. Extract the ZIP to your desired location (e.g., `C:\Tools\FirebotOverlay`)

## Quick Start

1. **Run the application**
   - Double-click `FirebotGiveawayObsOverlay.WebApp.exe`
   - The overlay will automatically open in your browser

2. **Configure the overlay**
   - Navigate to `http://localhost:5000/setup`
   - Set your Firebot file path
   - Choose a theme
   - Adjust timer and layout settings

3. **Add to OBS**
   - Add a new Browser Source in OBS
   - Set URL to `http://localhost:5000/giveaway`
   - Set dimensions to 1200 x 300

4. **Start your giveaway**
   - Begin the giveaway in Firebot
   - The overlay updates automatically

## Firebot Configuration

The overlay monitors files created by Firebot during giveaways. Ensure Firebot is configured to output giveaway data to files:

1. In Firebot, configure your giveaway command to write to files
2. Note the directory where files are saved (e.g., `G:\Giveaway`)
3. Enter this path in the overlay's Setup page

### Expected Files

The overlay looks for these files in the configured directory:
- Prize information file
- Entry count file
- Winner announcement file

## First Run Checklist

- [ ] Application starts without errors
- [ ] Setup page loads at `http://localhost:5000/setup`
- [ ] Firebot file path is set correctly
- [ ] Timer settings are configured
- [ ] Theme is selected
- [ ] OBS browser source displays the overlay
- [ ] Test giveaway shows entries updating

## Troubleshooting

### Application won't start
- Ensure no other application is using port 5000
- Try running as administrator
- Check Windows Defender/antivirus isn't blocking the executable

### Overlay not updating
- Verify the Firebot file path is correct
- Check that Firebot is writing files to the expected location
- Refresh the browser source in OBS

### OBS shows blank/white screen
- Wait a few seconds for the page to load
- Check the URL is exactly `http://localhost:5000/giveaway`
- Ensure the application is running

## Next Steps

- Read the [Usage Guide](usage.md) for detailed configuration options
- Check out the [Architecture](architecture.md) documentation for technical details
