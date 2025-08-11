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
 "config":{
  "cacheDir":"/var/www/wow.tools/"
 }
}
```

## License

This project is licensed under:

- MIT license ([LICENSE](LICENSE) or <http://opensource.org/licenses/MIT>)

## Thanks

- Marlamin ❤️
- WoWDev wiki authors
- Blizzard
