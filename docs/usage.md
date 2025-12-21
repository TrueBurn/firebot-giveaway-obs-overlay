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

## Configuration File

Settings can also be configured via `appsettings.json`:

```json
{
  "AppSettings": {
    "FireBotFileFolder": "G:\\Giveaway",
    "CountdownTimerEnabled": true,
    "CountdownHours": 0,
    "CountdownMinutes": 59,
    "CountdownSeconds": 59,
    "PrizeSectionWidthPercent": 75,
    "PrizeFontSizeRem": 4.5,
    "TimerFontSizeRem": 3.0,
    "EntriesFontSizeRem": 3.0,
    "Theme": {
      "Name": "Warframe",
      "PrimaryColor": "#00fff9",
      "SecondaryColor": "#ff00c8"
    }
  }
}
```

Changes to this file require application restart.

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
