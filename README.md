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

- .NET 9.0 compatibility
- Additional CDN endpoints for failover
- Support for newer game versions (WoW 11.1+)

**Long-term Vision**: The WoW Emulation project is working on a replacement for
Blizzard's NGDP (Next Generation Distribution Pipeline) systems. This fork will be
deprecated when that implementation is ready.

## Features

**Resume Interrupted Downloads** - Downloads resume from where they stopped.

**Parallel Downloads** - Configurable concurrent downloads (default: 4, set via
`maxParallelDownloads` in config.json).

**CDN Failover** - Multiple CDN endpoints with automatic failover:

- Official CDN hosts from Blizzard V2 API (added at runtime)
- cdn.arctium.tools
- casc.wago.tools
- archive.wow.tools
- tact.mirror.reliquaryhq.com

**30+ Commands** - CASC analysis and extraction tools.

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
    "downloadPatchFiles": true,
    "maxParallelDownloads": 4
  }
}
```

### Configuration Options

- `cacheDir` - Directory where downloaded files will be stored
- `checkProducts` - Array of product codes to check for updates
- `backupProducts` - Array of product codes to fully backup
- `downloadPatchFiles` - Whether to download patch files (default: true)
- `maxParallelDownloads` - Number of concurrent downloads (default: 4)

## Basic Usage

### Default Operation

When run without arguments, the application checks products listed in `checkProducts`
and backs up products listed in `backupProducts`:

```bash
dotnet build
dotnet run --project BuildBackup
```

### Available Commands

#### Build Operations

```bash
# Force backup of specific build
dotnet run --project BuildBackup -- forcebuild <product> <buildconfig> <cdnconfig>

# Force backup of specific product
dotnet run --project BuildBackup -- forceprogram <product>

# Enable partial download mode
dotnet run --project BuildBackup -- partialdl
```

#### File Extraction

```bash
# Extract file by content hash
dotnet run --project BuildBackup -- extractfilebycontenthash <product> <buildconfig> <cdnconfig> <contenthash> <outname>

# Extract raw file by content hash
dotnet run --project BuildBackup -- extractrawfilebycontenthash <product> <buildconfig> <cdnconfig> <contenthash> <outname>

# Extract file by encoding key
dotnet run --project BuildBackup -- extractfilebyencodingkey <product> <cdnconfig> <contenthash> <outname>

# Extract files from list
dotnet run --project BuildBackup -- extractfilesbylist <buildconfig> <cdnconfig> <basedir> <list>

# Extract files by filename list
dotnet run --project BuildBackup -- extractfilesbyfnamelist <buildconfig> <cdnconfig> <basedir> <list> [product]

# Extract files by FileDataID list
dotnet run --project BuildBackup -- extractfilesbyfdidlist <buildconfig> <cdnconfig> <basedir> <list> [product]
```

#### Data Analysis and Debugging

```bash
# Dump build information
dotnet run --project BuildBackup -- dumpinfo <product> <buildconfig> <cdnconfig>

# Dump root file contents (multiple variants)
dotnet run --project BuildBackup -- dumproot <root>
dotnet run --project BuildBackup -- dumproot2 <root> [product]
dotnet run --project BuildBackup -- dumproot3 <root>
dotnet run --project BuildBackup -- dumproot4 <product> <root>

# Dump various file types
dotnet run --project BuildBackup -- dumpinstall <product> <install>
dotnet run --project BuildBackup -- dumpdownload <product> <download>
dotnet run --project BuildBackup -- dumpencoding <product> <encoding>
dotnet run --project BuildBackup -- dumpconfig <product> <hash>

# WoW-specific commands
dotnet run --project BuildBackup -- dumpencrypted <product> <buildconfig>
dotnet run --project BuildBackup -- dumpsizes <product> <buildconfig>

# Utility commands
dotnet run --project BuildBackup -- calchash <string>
dotnet run --project BuildBackup -- calchashlistfile <file>
dotnet run --project BuildBackup -- cachebuild <buildconfig> <cdnconfig> <basedir>
```

## Troubleshooting

### Common Issues

#### Config not found or configuration errors

- Ensure `config.json` exists in the solution root directory (where BuildBackup.sln is located)
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
