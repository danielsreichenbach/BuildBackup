# Changelog

All notable changes to BuildBackup are documented in this file.

This project does not use semantic versioning tags. Changes are organized chronologically
by development period.

## 2026-01-21

### Added

- GitHub Actions workflow for native binary builds on 6 platforms
- Parallel download support with configurable concurrency (maxParallelDownloads)
- Human-readable file size formatting in CDN operations
- Editor configuration (.editorconfig) for consistent formatting
- Git attributes (.gitattributes) for line ending normalization
- Markdown linting configuration (.markdownlint.jsonc)
- Thread-safe locking for Armadillo key loading
- Generic decryptionKeyName field on CDN class

### Changed

- Upgraded to .NET 9.0 with updated dependencies
- Replaced Travis CI with GitHub Actions
- CDN list now uses community mirrors as primary sources
- Comprehensive .gitignore with patterns for IDEs, builds, and generated files
- README rewritten with detailed usage documentation
- Switched to HTTPS V2 patch service URLs (us.version.battle.net)
- Removed Ribbit.NET dependency, simplified to HTTP-only version fetching
- Removed useRibbit configuration option

### Fixed

- Config path handling for buildconfig and cdnconfig file types
- Path concatenation issues causing malformed CDN URLs

## 2025-08-11 - 2025-08-12

### Added

- Human-readable file sizes in size check output
- Incremental saving of archive sizes during size checking
- Comprehensive usage guide and responsible usage guidelines

### Changed

- Enhanced CDN handling with improved failover logic
- Simplified file size checking output for terminal readability

### Fixed

- PowerShell compatibility and trimming issues in CI workflow
- Cached file logging clarity in parallel download system
- Misleading HTTP HEAD messages in file size checking

## 2024-12-20

### Added

- Fake buildconfig support
- 11.1 root reading support
- archive.wow.tools CDN endpoint

## 2024-07-26

### Added

- Two new command-line tools
- Improved error handling

### Fixed

- CDNs loading order to fix keyring downloads
- Updated CDN list

## 2023-12-13

### Added

- wowxptr product support
- Updated CDN list

## 2023-08-21 - 2023-08-25

### Added

- Gryphon support
- 10.1.7 root support
- Optional patch downloading configuration
- Improved encrypted product support

### Fixed

- Null reference exception

## 2023-01-15 - 2023-04-18

### Changed

- Bumped Newtonsoft.Json dependency
- Encrypted/multi-product fixes
- Updated listfile URL

## 2022-11-23

### Changed

- Upgraded to .NET 7

## 2022-04-02 - 2022-10-20

### Added

- Quick and dirty build cache functionality
- New buildconfig keys support
- Armadillo support (untested)
- Listing of files flagged as encrypted which aren't
- dumpbadlyencrypted performance improvements

### Changed

- Upgraded to .NET 6
- Replaced BitConverter.ToString with Convert.ToHexString
- Stopped checking MD5 hashes for performance

### Fixed

- Files not being found in index during extraction
- Download parsing
- Empty file creation on bad response
- Minimal cdnconfigs handling

## 2021-09-07 - 2021-10-29

### Added

- Multiple eKey reading support
- Build-signature support
- Classic era products

### Changed

- Improved file-index reading
- Removed duplicate code
- Moved configuration items to settings
- Removed gameblob support

### Fixed

- Duplicate index entry handling

## 2021-02-18 - 2021-06-01

### Added

- wowlivetest support
- Classic era products

### Changed

- Updated contentflags
- Let CDN class handle pathing

### Fixed

- Extraction issues

## 2020-09-06

### Added

- Asynchronous downloading
- Incomplete archive download checks

### Changed

- Upgraded to .NET Core 3.1

## 2020-01-12 - 2020-03-25

### Added

- Mode for downloading only required files
- wow_classic_ptr support
- Various utilities
- Warcraft 3 backup support

### Changed

- CDN reordering
- Updated encoding parsing

### Fixed

- Speed improvements on large extracts
- Extraction crash handling

## 2019-11-01 - 2019-11-21

### Added

- Shadowlands encryption key
- 8.3 DK encryption key
- Baby alpaca encryption key

## 2019-06-16 - 2019-10-03

### Added

- wow_classic support
- wow_classic_beta support
- Extraction by fdid;fname list
- Multiple encryption keys per file support
- Size dumping utility

### Changed

- CDN priority reordering
- Experimental CDN failover retrying

### Fixed

- HTTP timeout handling
- Encoding error handling
- Failed download logging

## 2019-05-01 - 2019-06-07

### Added

- 8.2 root handling changes

### Changed

- Ribbit protocol as primary version source
- Upgraded to .NET Core 2.2 with config file

### Fixed

- Root reading
- Empty Ribbit response handling

## 2018-06-24 - 2018-11-29

### Added

- Partial encryption support for file extraction
- wowdemo to backup programs
- Multiple TactKey additions (121, 123, 114, 115, 119)
- Patch manifest/index reading
- Unarchived patch file downloading
- wowz product

### Changed

- File retrieval consolidation
- Index reading to dictionary

### Fixed

- FileDataID display in diff output
- CDN directory handling

## 2018-01-04 - 2018-05-23

### Added

- catalogs program
- wowdev support
- Encrypted config backup

### Changed

- Removed catalog backup (only contains configs)

### Fixed

- 404 productconfigs handling
- Unknown ngdp variables handling
- File extraction performance

## 2017-10-14 - 2017-12-28

### Added

- Encrypted FileDataID dumping
- 7.3.2 key placeholders
- BlizzCon 2017 mount encryption key
- New TACT keys (98, 101)
- TOM_RUS's chunk decryption implementation
- Encoding table B parsing

### Changed

- Upgraded to .NET Core 2.0
- Always retrieve all buildconfigs

### Fixed

- Decompression on decrypted chunks
- Corrupt encoding file handling

## 2017-04-28 - 2017-09-15

### Added

- Travis CI configuration
- LICENSE file
- SC1: Remastered program codes
- Catalog program support
- Lockfile for single instance enforcement
- Raw BLTE file extraction
- Armadillo key support (backup only)
- w3t product

### Changed

- Ported to .NET Core 1.1
- List extraction speed improvements
- Read-only file access for extraction modes

### Fixed

- 7.3 compatibility
- Multiple buildconfig download/install keys handling

## 2017-01-02 - 2017-04-27

### Added

- Install reading and root reading
- Root diffing functionality
- File extraction by content hash
- Unarchived file extraction
- Content hash to root dumping
- Listfile dump
- Patch file downloading
- Multiple product support

### Changed

- Performance improvements for file operations
- HTTP error handling
- Config overriding support

### Fixed

- Root dumping functionality
- Install tag parsing

## 2016-03-04 - 2016-12-29

### Added

- Initial BuildBackup implementation
- Multiple program support
- Progress indication
- Archive groups support
- Local file support
- Unarchived file dumping
