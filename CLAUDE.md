# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Tasks

### Building the Project
```bash
dotnet build
```

### Running the Application
```bash
dotnet run
```

### Key Command Line Arguments

**Main Operations**
- `backup` - Runs the main backup process for configured products
- `dumpinfo <product> <buildconfig> <cdnconfig>` - Dumps information about a specific build
- `downloadbuild <product> <buildconfig>` - Downloads a specific build
- `forcebuild <product> <buildconfig> <cdnconfig>` - Forces backup of specific build
- `forceprogram <product>` - Forces backup of specific product
- `partialdl` - Enable partial download mode

**File Extraction**
- `extractfiles <product> <buildconfig> <cdnconfig> <outdir>` - Extracts files from CASC storage
- `extractfilebycontenthash <product> <buildconfig> <cdnconfig> <contenthash> <outname>` - Extract file by content hash
- `extractrawfilebycontenthash <product> <buildconfig> <cdnconfig> <contenthash> <outname>` - Extract raw file by content hash
- `extractfilebyencodingkey <product> <cdnconfig> <contenthash> <outname>` - Extract file by encoding key
- `extractfilesbylist <buildconfig> <cdnconfig> <basedir> <list>` - Extract files from list
- `extractfilesbyfnamelist <buildconfig> <cdnconfig> <basedir> <list> [product]` - Extract files by filename list
- `extractfilesbyfdidlist <buildconfig> <cdnconfig> <basedir> <list> [product]` - Extract files by FileDataID list

**Data Analysis & Debugging**
- `dumproot <root>` - Dumps root file contents
- `dumproot2 <root> [product]` - Alternative root dump with product support
- `dumproot3 <root>` - Third root dump variant
- `dumproot4 <product> <root>` - Product-specific root dump
- `dumpinstall <product> <install>` - Dumps install file contents
- `dumpdownload <product> <download>` - Dumps download file contents
- `dumpencoding <product> <encoding>` - Dumps encoding file contents
- `dumpdecodedencoding <product> <encoding>` - Dumps decoded encoding file
- `dumpconfig <product> <hash>` - Dumps configuration file
- `dumpencrypted <product> <buildconfig>` - Lists encrypted files (WoW only)
- `dumpbadlyencrypted <product> <buildconfig>` - Lists badly encrypted files (WoW only)
- `dumpsizes <product> <buildconfig>` - Shows file sizes (WoW only)
- `dumprawfile <path> [numbytes]` - Dumps raw BLTE file contents
- `dumprawfiletofile <input> [output]` - Converts BLTE file to regular file
- `dumprawinstall <install>` - Dumps raw install file
- `dumpindex <product> <hash> [folder]` - Dumps index file contents
- `dumparchive <indexfile>` - Dumps archive index contents

**Utilities**
- `calchash <string>` - Calculates Jenkins96 hash of string
- `calchashlistfile <file>` - Calculates hashes for file list
- `checkarchive <file>` - Checks a specific archive file
- `fixarchive <file>` - Fixes missing files in an archive
- `downloadinstallfiles <product> <buildconfig> <cdnconfig>` - Downloads install files
- `cachebuild <buildconfig> <cdnconfig> <basedir>` - Caches build data
- `dumpencrypteddirtodir <src> <dest>` - Decrypts directory contents
- `dumpencryptedfiletofile <input> <output>` - Decrypts single file

## High-Level Architecture

### Core Components

**CASC Data Handling**
- `BLTE.cs` - Implements BLTE (Block Table Encoded) file format parsing for compressed game data
- `CDN.cs` - Manages CDN operations including file downloads and caching with failover support across multiple CDN endpoints
- `Structs.cs` - Defines data structures for CASC files (versions, cdns, buildconfig, cdnconfig, encoding, root, install, download, patch)

**Main Application Flow**
1. **Configuration Loading** - Reads `config.json` to determine cache directory and products to check/backup
2. **Product Discovery** - Uses Ribbit API or manual HTTP requests to get version information for configured products
3. **Data Download** - Downloads CASC data files (buildconfig, cdnconfig, encoding, root, install, download) from CDN
4. **Archive Processing** - Downloads and verifies archive files containing game assets
5. **Index Management** - Builds and maintains indexes for efficient file lookups

**Key Utilities**
- `Utils/KeyService.cs` - Manages encryption keys for protected game content
- `Utils/Salsa20.cs` - Implements Salsa20 encryption for decrypting protected files
- `Utils/Jenkins96.cs` - Hash function implementation for CASC lookups
- `Logger.cs` - Simple logging utility for tracking operations

### Recent Improvements
- **Enhanced CDN Support** - Added archive.wow.tools CDN endpoint for improved reliability
- **Better Error Handling** - Comprehensive error handling improvements throughout the application (~100 lines of additional error handling code)
- **WoW 11.1 Root Reading** - Added support for reading WoW 11.1 root files with improved parsing
- **Fake Buildconfig Support** - Added capability to handle fake/mock buildconfig files for testing and debugging
- **Expanded Command Set** - Over 30 different command-line operations for comprehensive CASC data analysis

### Important Notes
- Built for .NET 9.0 with unsafe code blocks enabled
- Supports full data backups for World of Warcraft, Battle.net Agent, and Battle.net App
- Uses multiple CDN endpoints with automatic failover for reliability:
  - blzddist1-a.akamaihd.net (Akamai - primary)
  - level3.blizzard.com (Level3)
  - us.cdn.blizzard.com (Official US CDN)
  - eu.cdn.blizzard.com (Official EU CDN)
  - cdn.arctium.tools (Arctium Launcher archive)
  - tact.mirror.reliquaryhq.com (ReliquaryHQ archive)
- Requires a `config.json` file with cacheDir path configuration
- Extensive debugging and analysis capabilities for CASC file formats