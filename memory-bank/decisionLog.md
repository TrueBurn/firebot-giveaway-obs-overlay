# Decision Log

## Architectural Decisions

### [2025-04-14] Winner Overlay Visual Design
**Decision**: Changed winner overlay from semi-transparent to solid black background and removed trophy emojis
**Rationale**: 
- Semi-transparent overlay caused visual distractions with content showing through
- Trophy emojis created visual clutter that detracted from winner name prominence
- Solid black provides clean, professional appearance that focuses attention on winner
**Implications**: 
- Improved readability and visual clarity
- More professional streaming appearance
- Easier integration with various stream backgrounds

### [Previous] File-Based Integration Approach
**Decision**: Use file system monitoring instead of direct Firebot API integration
**Rationale**:
- Firebot already outputs giveaway data to files
- No need to modify Firebot or create API dependencies
- Simpler integration path with existing Firebot workflow
- More reliable than potential API connectivity issues
**Implications**:
- Application depends on file system access to Firebot directory
- Real-time updates require efficient file monitoring
- Limited to data that Firebot outputs to files

### [Previous] ASP.NET Core with Blazor Server Architecture
**Decision**: Use Blazor Server instead of client-side Blazor or other web frameworks
**Rationale**:
- Real-time updates work naturally with SignalR
- Server-side processing for file monitoring
- Smaller client-side footprint for OBS browser source
- Familiar development experience for .NET developers
**Implications**:
- Requires local server running for overlay to function
- Network dependency for real-time updates
- Better suited for local development/streaming setup

### [Previous] Static Configuration Management
**Decision**: Use static helper class for configuration instead of dependency injection
**Rationale**:
- Simple access pattern for configuration values
- Avoids dependency injection complexity in Razor components
- Allows runtime configuration changes without restart
- Centralized configuration management
**Implications**:
- Global state management approach
- Thread safety considerations for configuration updates
- Easy access but less testable than DI approach

### [Previous] Warframe-Inspired Visual Theme
**Decision**: Adopt Warframe game aesthetic for overlay styling
**Rationale**:
- Appeals to gaming audience typical of Twitch streams
- Distinctive visual identity that stands out
- Rich color palette and animation opportunities
- Professional gaming aesthetic
**Implications**:
- Specific target audience appeal
- Requires custom CSS and animation work
- Theme consistency across all UI elements

[2025-01-26 - Initial decision log documentation with historical decisions]