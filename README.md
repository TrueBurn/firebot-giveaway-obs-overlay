# Firebot Giveaway OBS Overlay

[![Release](https://img.shields.io/github/v/release/trueburn/firebot-giveaway-obs-overlay)](https://github.com/trueburn/firebot-giveaway-obs-overlay/releases)
[![License](https://img.shields.io/github/license/trueburn/firebot-giveaway-obs-overlay)](LICENSE)

A stylish, animated overlay for Firebot giveaways with customizable themes and Warframe-inspired design.

## Quick Start

1. **Download** the latest release for your system from [Releases](https://github.com/trueburn/firebot-giveaway-obs-overlay/releases)
2. **Extract** and run `FirebotGiveawayObsOverlay.WebApp.exe`
3. **Add browser source** in OBS: `http://localhost:5000/giveaway`
4. **Configure** at: `http://localhost:5000/setup`

For detailed instructions, see [Getting Started](docs/getting-started.md).

## System Requirements

- Windows 10/11 (x86 or x64)
- Firebot with file-based giveaway output
- OBS Studio (or compatible streaming software)

## Downloads

Download the latest version from the [Releases](https://github.com/trueburn/firebot-giveaway-obs-overlay/releases) page:

| File | Description |
|------|-------------|
| `FirebotGiveawayOverlay-x.x.x-win-x64.zip` | 64-bit Windows (recommended) |
| `FirebotGiveawayOverlay-x.x.x-win-x86.zip` | 32-bit Windows |

## Features

- 7 preset themes: Warframe, Cyberpunk, Neon, Classic, Ocean, Fire, Purple
- Custom color picker for personalized themes
- Animated effects and transitions
- Real-time entry counter with animations
- Celebratory winner announcement overlay
- Gradient color schemes with animated borders
- Responsive layout with customizable dimensions
- Configurable countdown timer (hours, minutes, seconds) with enable/disable option
- Easy setup page with live preview
- Version display for easy troubleshooting

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and first run
- [Usage Guide](docs/usage.md) - Configuration and daily use
- [Architecture](docs/architecture.md) - Technical documentation

## Configuration

Configure through the Setup page or `appsettings.json`:

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

## OBS Integration

1. Add a new Browser Source in OBS Studio
2. Set URL to `http://localhost:5000/giveaway`
3. Recommended dimensions: 1200px x 300px
4. Custom CSS for transparency:
   ```css
   body { background-color: rgba(0, 0, 0, 0); margin: 0px auto; overflow: hidden; }
   ```

## Development

Built with ASP.NET Core 8.0, Blazor Server, and Bootstrap 5.1.

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet run
```

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
