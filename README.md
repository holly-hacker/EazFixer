# EazFixer
A deobfuscation tool for Eazfuscator.

## Description
EazFixer is a deobfuscation tool for [Eazfuscator](https://www.gapotchenko.com/eazfuscator.net), a commercial .NET obfuscator. For a list of features, see the list below.

### Implemented fixes:
* String encryption
* Resource encryption
* Assembly embedding

### Not implemented, may be added in the future:
* Entrypoint obfuscation
* Data virtualization

### Out of scope:
* Code virtualization (separate project)
* Symbol renaming (symbol names are either unrecoverable or encrypted. For symbol decryption in case of a known key, see [EazDecode](https://github.com/HoLLy-HaCKeR/EazDecode))
* Automatic code optimization (not an anti-feature!)
* Code control flow obfuscation (I didn't have any problems with my samples in dnSpy)
* Assemblies merging (doesn't seem probable, especially with symbol renaming)
* Control flow obfuscation (use de4dot)

## Usage
Call from the command line or drag and drop the file on and let it run or use the command line flag `--file`.

If your assembly is protected with control-flow obfuscation, run it through [de4dot](https://github.com/0xd4d/de4dot) with the
`--only-cflow-deob` flag first.

* --file path
* --keep-types
* --virt-fix

The flag `--file` is used for the input file.
The flag `--keep-types` is similar to the de4dot flag, Keeps obfuscator types and assemblies.
The flag `--virt-fix` keeps certain parts obfuscated to stay working with [virtualized](https://help.gapotchenko.com/eazfuscator.net/30/virtualization) assemblies.

example: `EazFixer.exe --file test.exe --keep-types`

## Building
Clone the repository and use the latest version of Visual Studio (2019, at the time of writing).

## Support
EazFixer is (and will always be) targeted at the latest version of Eazfuscator. If your version is not supported, try a more universal 
deobfuscator like [de4dot](https://github.com/0xd4d/de4dot). If your version is newer than what this tool supports, create an issue only 
**after** verifying with the latest version of Eazfuscator.

Also, I will not help you use this program. Consider it for advanced users only. If you do run into a problem and are sure it is a bug, 
feel free to submit an issue but I cannot guarantee I will fix it.

## Related projects
- [EazDecode](https://github.com/HoLLy-HaCKeR/EazDecode), for decrypting encrypted symbol names in case of a known encryption key.
- [eazdevirt](https://github.com/saneki/eazdevirt), a tool for devirtualizing older version of EazFuscator.
- [eazdevirt fork](https://github.com/HoLLy-HaCKeR/eazdevirt), my abandoned fork of eazdevirt, may work slightly better on newer samples.

## Credits
This tool uses the following (open source) software:
* [dnlib](https://github.com/0xd4d/dnlib) by [0xd4d](https://github.com/0xd4d), license under the MIT license, for reading/writing assemblies.
* a fork of [Harmony](https://github.com/hcoona/Harmony) by [hcoona](https://github.com/hcoona), licensed under the MIT license, to patch runtime methods.  
The original [Harmony](https://github.com/pardeike/Harmony) is by [Andreas Pardeike](https://github.com/pardeike), but does not unprotect the memory pages it writes to, making the program crash.
