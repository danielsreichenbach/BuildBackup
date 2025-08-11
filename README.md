# BuildBackup

**Maintained Fork Notice**: This is a maintained fork of the original
[BuildBackup by Marlamin](https://github.com/Marlamin/BuildBackup). This fork
serves as a stop-gap measure to keep the tool functional while the
[WoW Emulation project](https://github.com/wowemulation-dev) develops a comprehensive
replacement for Blizzard's NGDP systems.

<div align="center">

[![Discord](https://img.shields.io/discord/1394228766414471219?logo=discord&style=flat-square)](https://discord.gg/Q44pPMvGEd)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

</div>

## Fork Status & Future Plans

This maintained fork includes:

- Updated dependencies and .NET 9.0 compatibility
- Enhanced error handling and stability improvements
- Additional CDN endpoints for improved reliability
- Support for newer game versions (WoW 11.1+)
- Expanded debugging and analysis tools

**Long-term Vision**: The WoW Emulation project is actively working on building
a full solution to replace and replicate Blizzard's NGDP (Next Generation Distribution
Pipeline) systems. Once their comprehensive solution is ready, this fork will be
deprecated in favor of their more complete implementation.

## Enhanced Features

This maintained fork includes several key improvements over the original:

✨ **Resume Interrupted Downloads** - Never lose progress again! Downloads automatically resume from where they left off if interrupted.

⚡ **Parallel Downloads** - Configurable concurrent downloads for faster backup operations (default: 4, configurable via `maxParallelDownloads` in config.json).

🔄 **Enhanced CDN Failover** - Additional archive endpoints including:
- cdn.arctium.tools (Arctium Launcher archive)
- tact.mirror.reliquaryhq.com (ReliquaryHQ archive)
- Improved failover logic with detailed logging

🛠️ **30+ Debugging Commands** - Comprehensive CASC analysis tools for developers and researchers.

📊 **Detailed Logging** - Enhanced HTTP request logging and error handling throughout the application.

🔧 **Modern Dependencies** - Updated to .NET 9.0 with latest NuGet packages for improved stability and performance.

## Description

BuildBackup was created to back up [CASC](https://wowdev.wiki/CASC) data from
Blizzard's CDN. Blizzard often removes data for older builds from their CDN, making
them unavailable for install. The goal is to have it back up all data needed to
install a specific version for a specific application.

In addition to the backup functionality there are also several utilities implemented
to dump information/extract data from the CASC filesystem.

## Supported products

Basic data for all [product codes](https://wowdev.wiki/CASC#NGDP_Program_Codes)
is supported (when available on CDN), but BuildBackup currently supports full
data backups for the following applications:

- World of Warcraft
- Battle.net Agent
- Battle.net App

## Configuration

Files will be saved in the path specified in a ```config.json``` file like :

```json
{
  "config": {
    "cacheDir": "/var/www/wow.tools/",
    "checkProducts": [ "wow", "wow_classic", "wow_classic_era" ],
    "backupProducts": [ "wow_classic_era" ],
    "useRibbit": false,
    "downloadPatchFiles": true,
    "maxParallelDownloads": 4
  }
}
```

### Configuration Options

- `cacheDir` - Directory where downloaded files will be stored
- `checkProducts` - Array of product codes to check for updates
- `backupProducts` - Array of product codes to fully backup
- `useRibbit` - Whether to use Ribbit API for version info (default: false)
- `downloadPatchFiles` - Whether to download patch files (default: true)
- `maxParallelDownloads` - Number of concurrent downloads (default: 4)

## How to Download Specific Builds

You can use [wago.tools](https://wago.tools) to find specific builds and download them:

### Step 1: Find Build Information on Wago.tools

1. Visit https://wago.tools/builds
2. Select your product (e.g., World of Warcraft, WoW Classic, etc.)
3. Pick the build version you want to download
4. Copy the **Build Config** and **CDN Config** hashes (long hexadecimal strings)

### Step 2: Download the Build

Use one of these commands to download your chosen build:

```bash
# Method 1: Download specific build data
dotnet run downloadbuild <product> <buildconfig>

# Method 2: Force complete build backup  
dotnet run forcebuild <product> <buildconfig> <cdnconfig>

# Examples:
dotnet run downloadbuild wow a1b2c3d4e5f67890abcdef1234567890abcdef12
dotnet run forcebuild wow a1b2c3d4e5f67890abcdef1234567890abcdef12 1234567890abcdef1234567890abcdef12345678
```

### Step 3: Extract Files (Optional)

```bash
# Extract all files from a build to a directory
dotnet run extractfiles <product> <buildconfig> <cdnconfig> <output_directory>

# Example:
dotnet run extractfiles wow a1b2c3d4e5f67890abcdef1234567890abcdef12 1234567890abcdef1234567890abcdef12345678 ./extracted_files
```

### Pro Tips

💡 **Faster Downloads**: Use the `partialdl` flag for quicker partial downloads:
```bash
dotnet run partialdl downloadbuild wow <buildconfig>
```

💡 **Optimize Speed**: Configure `maxParallelDownloads` in your config.json for optimal download speed based on your connection.

💡 **Resume Support**: Downloads automatically resume if interrupted - no need to restart from scratch!

💡 **CDN Reliability**: The tool automatically handles CDN failover using multiple endpoints for maximum reliability.

## Usage Guidelines

⚠️ **Important**: Please use this tool responsibly and considerately:

### Recommended Usage
✅ **Download Your Favorite Versions** - Focus on specific builds you need or want to preserve (e.g., favorite expansion versions, classic releases, etc.)

✅ **Personal Archives** - Download builds you want archived on your personal NAS, backup drives, or local storage

### Full Mirror Considerations
⚖️ **Full Mirrors** should only be downloaded if you:
- Plan to provide a **long-term public mirror** to benefit the community
- Have the infrastructure and commitment to maintain reliable public access
- Want comprehensive archives for research, development, or preservation purposes

### Why This Matters
- CDN bandwidth and storage costs are significant
- Multiple unnecessary full mirrors create redundant load
- Community benefits more from reliable, maintained public mirrors than many private copies
- Selective downloading helps preserve resources while still protecting important game versions

💡 **Tip**: Start with specific versions you care about, then consider broader archival goals if you have the resources and commitment to maintain them long-term.

## License

This project is licensed under:

- MIT license ([LICENSE](LICENSE) or <http://opensource.org/licenses/MIT>)

## Thanks

- Marlamin ❤️
- WoWDev wiki authors
- Blizzard
