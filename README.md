# Firebot Giveaway OBS Overlay

A stylish, animated overlay for Firebot giveaways with customizable themes and Warframe-inspired design.

## Usage

1. Start the application
2. Add a Browser Source in OBS Studio pointing to `http://localhost:5243/giveaway`
3. Configure the overlay settings through the Setup page
4. Start your giveaway in Firebot
5. The overlay will automatically update based on the giveaway status

## Configuration

The application can be configured through the `appsettings.json` file:

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
      "SecondaryColor": "#ff00c8",
      "BackgroundStart": "rgba(0, 0, 0, 0.9)",
      "BackgroundEnd": "rgba(15, 25, 35, 0.98)",
      "BorderGlowColor": "rgba(0, 255, 255, 0.15)",
      "TextColor": "#ffffff",
      "TimerExpiredColor": "#ff3333",
      "SeparatorColor": "rgba(0, 255, 255, 0.5)"
    }
  }
}
```

### Configuration Options:
- `FireBotFileFolder`: Directory where Firebot stores your giveaway files
- `CountdownTimerEnabled`: Enable/disable the countdown timer display
- `CountdownHours`, `CountdownMinutes`, `CountdownSeconds`: Default duration for giveaways
- `PrizeSectionWidthPercent`: Controls the layout width distribution (50-90%)
- `PrizeFontSizeRem`, `TimerFontSizeRem`, `EntriesFontSizeRem`: Font size settings
- `Theme`: Color scheme configuration (see Theme Configuration below)

### Theme Configuration
You can use a preset theme by name or customize individual colors:

**Preset Themes**: Warframe, Cyberpunk, Neon, Classic, Ocean, Fire, Purple

To use a preset, just set the `Name` field. To customize, modify the color values:
- `PrimaryColor`: Main accent color for text gradients
- `SecondaryColor`: Secondary color for gradients and effects
- `BackgroundStart`/`BackgroundEnd`: Container background gradient
- `BorderGlowColor`: Border glow effect color
- `TextColor`: General text color
- `TimerExpiredColor`: Color when timer reaches zero
- `SeparatorColor`: Divider line color

## OBS Integration

1. In OBS Studio, add a new Browser Source
2. Set the URL to `http://localhost:5243/giveaway`
3. Recommended dimensions:
   - Width: 1200px
   - Height: 300px
4. Important Browser Source settings:
   - Enable "Refresh browser when scene becomes active"
   - Enable "Shutdown source when not visible" for better performance
   - Set custom CSS to `body { background-color: rgba(0, 0, 0, 0); margin: 0px auto; overflow: hidden; }`

## Features

- 🎮 Customizable themes with 7 presets (Warframe, Cyberpunk, Neon, Classic, Ocean, Fire, Purple)
- 🎨 Custom color picker for personalized themes
- ✨ Animated effects and transitions
- 🎯 Real-time entry counter with animations
- 🏆 Celebratory winner announcement overlay
- 🌈 Gradient color schemes with animated borders
- 📱 Responsive layout with customizable dimensions
- ⏱️ Configurable countdown timer (hours, minutes, seconds) with enable/disable option
- 🔧 Easy setup page for all customization options with live preview

## Setup Page Options

The setup page allows you to configure:
- **Theme**: Select from preset themes or customize colors (primary, secondary, timer expired)
- **Countdown Timer**: Duration (hours/minutes/seconds) with enable/disable toggle
- **Firebot File Path**: Directory where Firebot stores giveaway files
- **Layout Proportions**: Prize section width percentage
- **Font Sizes**: Individual sizing for prize, timer, and entries text

## Development

The project is built using:
- ASP.NET Core 8.0
- Blazor Server
- Bootstrap 5.1
- CSS animations

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## Acknowledgments

- Built for integration with [Firebot](https://firebot.app/)
- Inspired by the Warframe community and streaming aesthetics
- Special thanks to all contributors and testers