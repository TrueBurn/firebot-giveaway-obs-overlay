# Firebot Giveaway OBS Overlay

A stylish, animated overlay for Firebot giveaways with Warframe-inspired design.

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
    "FireBotFileFolder": "G:\\Giveaway",    // Directory where Firebot stores giveaway files
    "GiveawayDuration": 60                  // Default giveaway duration in seconds
  }
}
```

### Configuration Options:
- `FireBotFileFolder`: Set this to the directory where Firebot stores your giveaway files
- `GiveawayDuration`: Default duration for giveaways in seconds (can be modified through the Setup page)

## OBS Integration

1. In OBS Studio, add a new Browser Source
2. Set the URL to `http://localhost:5000/giveaway`
3. Recommended dimensions:
   - Width: 800px
   - Height: 200px
4. Important Browser Source settings:
   - Enable "Refresh browser when scene becomes active"
   - Enable "Shutdown source when not visible" for better performance
   - Set custom CSS to `body { background-color: rgba(0, 0, 0, 0); margin: 0px auto; overflow: hidden; }`

## Features

- 🎮 Warframe-inspired design
- ✨ Animated effects and transitions
- 🎯 Real-time entry counter with animations
- 🏆 Celebratory winner announcement
- 🎨 Gradient color schemes
- 📱 Responsive layout

## Development

The project is built using:
- ASP.NET Core 8.0
- Blazor Server
- Bootstrap 5.1

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