# Usage Guide

This guide covers all configuration options and daily use of the Firebot Giveaway OBS Overlay.

## Accessing the Application

After starting the application, you can access:

- **Overlay**: `http://localhost:5000/giveaway` - The display for OBS
- **Setup Page**: `http://localhost:5000/setup` - Configuration interface
- **Home**: `http://localhost:5000` - Landing page with navigation

## Setup Page Configuration

### Theme Configuration

The overlay supports 7 preset themes and custom colors.

**Preset Themes:**
| Theme | Description |
|-------|-------------|
| Warframe | Cyan and magenta with dark background (default) |
| Cyberpunk | Yellow and pink neon aesthetic |
| Neon | Bright green and blue |
| Classic | Gold and silver traditional look |
| Ocean | Blue gradient oceanic feel |
| Fire | Orange and red warm tones |
| Purple | Purple and pink gradient |

**Custom Colors:**
When you select "Custom" from the dropdown, three color pickers appear:
- **Primary Color**: Main accent color for text gradients
- **Secondary Color**: Secondary color for gradient effects
- **Timer Expired Color**: Color displayed when countdown reaches zero

Theme changes apply immediately to the overlay without refresh.

### Countdown Timer

Configure the giveaway duration:

- **Enable/Disable Toggle**: Turn the timer on or off
  - When disabled, only entry count is shown
  - File monitoring continues even with timer disabled
- **Hours**: 0-23 hours
- **Minutes**: 0-59 minutes
- **Seconds**: 0-59 seconds
- **Reset Timer Button**: Restart the countdown

**Display Format:**
- Shows `HH:MM:SS` when hours > 0
- Shows `MM:SS` when only minutes and seconds
- Shows `SS` when only seconds

### Firebot File Path

Set the directory where Firebot writes giveaway files:

```
G:\Giveaway
```

Ensure this matches your Firebot configuration. The application monitors this folder for real-time updates.

### Layout Settings

**Prize Section Width**: Controls the horizontal space distribution between the prize display and timer/entries section. Range: 50-90%.

- 50%: Equal split between prize and info
- 75%: Default, more space for prize text
- 90%: Maximum space for long prize names

### Font Sizes

Adjust the text size for each element (in rem units):

| Element | Range | Default |
|---------|-------|---------|
| Prize Font Size | 1.0 - 6.0 rem | 4.5 rem |
| Timer Font Size | 1.0 - 6.0 rem | 3.0 rem |
| Entries Font Size | 1.0 - 6.0 rem | 3.0 rem |

Larger values make text more visible on stream but may cause overflow.

## OBS Integration

### Adding the Browser Source

1. In OBS, click the **+** button in the Sources panel
2. Select **Browser**
3. Name it (e.g., "Giveaway Overlay")
4. Configure:
   - **URL**: `http://localhost:5000/giveaway`
   - **Width**: 1200
   - **Height**: 300

### Recommended OBS Settings

For optimal performance:

- **Refresh browser when scene becomes active**: Enabled
- **Shutdown source when not visible**: Enabled (saves resources)
- **Custom CSS**:
  ```css
  body {
    background-color: rgba(0, 0, 0, 0);
    margin: 0px auto;
    overflow: hidden;
  }
  ```

### Positioning

Place the overlay where it won't obstruct important content:
- Bottom of screen for gameplay streams
- Top corner for chat-focused streams
- Adjust size and position as needed

## Giveaway Workflow

### Starting a Giveaway

1. Ensure the overlay application is running
2. Verify OBS browser source is visible
3. Start the giveaway in Firebot
4. The overlay automatically displays:
   - Prize name
   - Countdown timer (if enabled)
   - Entry count (updates in real-time)

### During the Giveaway

- Entry count updates automatically as viewers enter
- Timer counts down to zero
- Animated effects highlight activity

### Winner Announcement

When a winner is selected in Firebot:
- The overlay transitions to winner display mode
- Winner name appears with celebration styling
- Solid black background for clean presentation

### Ending the Giveaway

After the giveaway:
- Clear the winner file in Firebot or
- Start a new giveaway to reset the display

## Settings Persistence

All settings configured through the Setup page are automatically saved and persist across application restarts.

### How It Works

- Settings are saved to `usersettings.json` in the application directory
- Changes take effect immediately and are saved automatically
- On startup, the app loads your saved settings (or defaults if no saved settings exist)

### Settings Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Default settings shipped with the app |
| `usersettings.json` | Your customized settings (auto-generated) |

### Manual Configuration

You can also edit `usersettings.json` directly:

```json
{
  "fireBotFileFolder": "G:\\Giveaway",
  "countdownTimerEnabled": true,
  "countdownHours": 0,
  "countdownMinutes": 59,
  "countdownSeconds": 59,
  "prizeSectionWidthPercent": 75,
  "prizeFontSizeRem": 4.5,
  "timerFontSizeRem": 3.0,
  "entriesFontSizeRem": 3.0,
  "theme": {
    "name": "Warframe",
    "primaryColor": "#00fff9",
    "secondaryColor": "#ff00c8",
    "backgroundStart": "rgba(0, 0, 0, 0.9)",
    "backgroundEnd": "rgba(15, 25, 35, 0.98)",
    "borderGlowColor": "rgba(0, 255, 255, 0.15)",
    "textColor": "#ffffff",
    "timerExpiredColor": "#ff3333",
    "separatorColor": "rgba(0, 255, 255, 0.5)"
  }
}
```

**Note:** Manual edits require application restart to take effect.

### Resetting Settings

The Setup page includes a **Settings Management** section that shows:

- **Status badge**: Shows "Custom settings active" or "Using defaults"
- **Diff view**: Expandable table comparing your settings to defaults
- **Individual reset**: Click the ↻ button next to any setting to reset just that one
- **Full reset**: "Reset to Defaults" button to restore all settings at once

**To reset individual settings:**
1. Click "Show Details" to expand the diff view
2. Find the setting you want to reset
3. Click the ↻ button in the Action column

**To reset all settings:**
1. Click "Reset to Defaults" button
2. Click "Confirm Reset" to proceed (or "Cancel" to abort)

**Manual reset:** You can also delete `usersettings.json` from the application directory and restart.

## Troubleshooting

### Timer Not Counting Down
- Check that timer is enabled in Setup
- Verify the giveaway has started in Firebot
- Click "Reset Timer" to restart

### Entry Count Not Updating
- Confirm Firebot file path is correct
- Check Firebot is writing to the expected files
- Look for error messages in the console

### Theme Not Applying
- Refresh the overlay page in OBS
- Ensure theme selection was saved
- Try switching to a different theme and back

### Performance Issues
- Enable "Shutdown source when not visible" in OBS
- Close unnecessary browser tabs
- Ensure adequate system resources

## Tips for Streamers

- Test the overlay before going live
- Have a backup plan if technical issues occur
- Consider the overlay size relative to your stream layout
- Use contrasting themes that work with your game/content
- Keep prize names concise for better display
