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

## Fork Status and Future Plans

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

**Resume Interrupted Downloads** - Never lose progress again! Downloads automatically resume
from where they left off if interrupted.

**Parallel Downloads** - Configurable concurrent downloads for faster backup operations
(default: 4, configurable via `maxParallelDownloads` in config.json).

**Enhanced CDN Failover** - Multiple CDN endpoints with automatic failover:

- Official CDN hosts from Ribbit API (dynamically added at runtime)
- cdn.arctium.tools (Arctium Launcher archive)
- casc.wago.tools (wago.tools CDN)
- archive.wow.tools (wow.tools archive)

**30+ Debugging Commands** - Comprehensive CASC analysis tools for developers and researchers.

**Detailed Logging** - Enhanced HTTP request logging and error handling throughout the
application.

**Modern Dependencies** - Updated to .NET 9.0 with latest NuGet packages for improved stability
and performance.

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

## Setup

### Requirements

- .NET 9.0 or later
- Internet connection for CDN access

### Installation

1. Clone this repository:

   ```bash
   git clone https://github.com/danielsreichenbach/BuildBackup.git
   cd BuildBackup
   ```

2. Build the project:

   ```bash
   dotnet build
   ```

3. Create your configuration file (see Configuration section below)

## Configuration

Files will be saved in the path specified in a `config.json` file like:

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

## Basic Usage

### Default Operation

When run without arguments, the application will check products listed in the `checkProducts`
configuration and backup products listed in `backupProducts`:

```bash
dotnet build
dotnet run
```

### Available Commands

The tool supports the following operations:

#### Build Operations

```bash
# Force backup of specific build
dotnet run forcebuild <product> <buildconfig> <cdnconfig>

# Force backup of specific product
dotnet run forceprogram <product>

# Enable partial download mode
dotnet run partialdl
```

#### File Extraction

```bash
# Extract file by content hash
dotnet run extractfilebycontenthash <product> <buildconfig> <cdnconfig> <contenthash> <outname>

# Extract raw file by content hash
dotnet run extractrawfilebycontenthash <product> <buildconfig> <cdnconfig> <contenthash> <outname>

# Extract file by encoding key
dotnet run extractfilebyencodingkey <product> <cdnconfig> <contenthash> <outname>

# Extract files from list
dotnet run extractfilesbylist <buildconfig> <cdnconfig> <basedir> <list>

# Extract files by filename list
dotnet run extractfilesbyfnamelist <buildconfig> <cdnconfig> <basedir> <list> [product]

# Extract files by FileDataID list
dotnet run extractfilesbyfdidlist <buildconfig> <cdnconfig> <basedir> <list> [product]
```

#### Data Analysis and Debugging

```bash
# Dump build information
dotnet run dumpinfo <product> <buildconfig> <cdnconfig>

# Dump root file contents (multiple variants)
dotnet run dumproot <root>
dotnet run dumproot2 <root> [product]
dotnet run dumproot3 <root>
dotnet run dumproot4 <product> <root>

# Dump various file types
dotnet run dumpinstall <product> <install>
dotnet run dumpdownload <product> <download>
dotnet run dumpencoding <product> <encoding>
dotnet run dumpconfig <product> <hash>

# WoW-specific commands
dotnet run dumpencrypted <product> <buildconfig>
dotnet run dumpsizes <product> <buildconfig>

# Utility commands
dotnet run calchash <string>
dotnet run calchashlistfile <file>
dotnet run cachebuild <buildconfig> <cdnconfig> <basedir>
```

## Troubleshooting

### Common Issues

#### Config not found or configuration errors

- Ensure `config.json` exists in the project root directory
- Verify JSON syntax is valid (use a JSON validator if needed)
- Check that `cacheDir` path is accessible and has write permissions

#### Build errors

- Verify you have .NET 9.0 or later installed: `dotnet --version`
- Try cleaning and rebuilding: `dotnet clean && dotnet build`

#### Download failures or CDN errors

- The application automatically tries multiple CDN endpoints
- Check your internet connection
- Some old builds may no longer be available on any CDN

#### Invalid buildConfig or cdnConfig errors

- Verify the build config and CDN config hashes are correct
- Use tools like wago.tools to find valid hash combinations
- Some very old or very new builds may not be supported

#### Permission denied errors

- Ensure the `cacheDir` directory has write permissions
- On Unix systems, you may need to create the directory first: `mkdir -p /path/to/cache`

#### Memory or disk space issues

- Game backups can be very large (50GB+ for full builds)
- Ensure you have sufficient disk space in your `cacheDir`
- Consider using `partialdl` mode for testing or smaller downloads

### Generated Files

The application creates several files during operation:

- `errors.txt` - Error log file (check this for detailed error information)
- `archiveSizes.txt` - Archive size cache for resume functionality
- Cache directory structure under your configured `cacheDir`

## Usage Guidelines

**Important**: Please use this tool responsibly and considerately:

### Recommended Usage

**Download Your Favorite Versions** - Focus on specific builds you need or want to preserve
(e.g., favorite expansion versions, classic releases, etc.)

**Personal Archives** - Download builds you want archived on your personal NAS, backup drives,
or local storage

### Full Mirror Considerations

**Full Mirrors** should only be downloaded if you:

- Plan to provide a **long-term public mirror** to benefit the community
- Have the infrastructure and commitment to maintain reliable public access
- Want comprehensive archives for research, development, or preservation purposes

### Why This Matters

- CDN bandwidth and storage costs are significant
- Multiple unnecessary full mirrors create redundant load
- Community benefits more from reliable, maintained public mirrors than many private copies
- Selective downloading helps preserve resources while still protecting important game versions

**Tip**: Start with specific versions you care about, then consider broader archival goals if
you have the resources and commitment to maintain them long-term.

## License

This project is licensed under:

- MIT license ([LICENSE](LICENSE) or <http://opensource.org/licenses/MIT>)

## Thanks

- Marlamin
- WoWDev wiki authors
- Blizzard
