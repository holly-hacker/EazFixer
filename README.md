# EazFixer
A deobfuscation tool for Eazfuscator.

## Description
EazFixer is a deobfuscation tool for [Eazfuscator](https://www.gapotchenko.com/eazfuscator.net), a commercial .NET obfuscator. For a list of features, see the list below.

### Implemented features:
* String encryption
* Resource encryption
* Assembly embedding

### Considered features:
* Code and data virtualization
* Entrypoint obfuscation
* Useless code obfuscation (may only be present in Eazfuscator binary itself)

### Not considered:
* Symbol renaming (usually the symbol names are unrecoverable)
* Automatic code optimization (not an anti-feature!)
* Code control flow obfuscation (I didn't have any problems with my samples in dnSpy)
* Assemblies merging (doesn't seem probable, especially with symbol renaming)

## Usage
For now, just call it from the commandline and give it your obfsucated file as parameter. Or, if you are scared of typing in commands, 
just drag your obfuscated on the exe and let it run.

If your assembly is protected with control-flow obfuscation, run it through [de4dot](https://github.com/0xd4d/de4dot) with the
`--only-cflow-deob` flag first.

## Building
Clone the repository recursively and use the latest version of Visual Studio (2017, at the time of writing) to build.

## Support
EazFixer is (and will always be) targeted at the latest version of Eazfuscator. If your version is not supported, try a more universal 
deobfuscator like [de4dot](https://github.com/0xd4d/de4dot). If your version is newer than what this tool supports, create an issue only 
**after** verifying with the latest version of Eazfuscator.

Also, I will not help you use this program. Consider it for advanced users only. If you do run into a problem and are sure it is a bug, 
feel free to submit an issue but I cannot guarantee I will fix it.

## Credits
This tool uses the following (open source) software:
* [dnlib](https://github.com/0xd4d/dnlib) by [0xd4d](https://github.com/0xd4d), license under the MIT license, for reading/writing assemblies.
* a fork of [Harmony](https://github.com/hcoona/Harmony) by [hcoona](https://github.com/hcoona), licensed under the MIT license, to patch runtime methods.  
The original [Harmony](https://github.com/pardeike/Harmony) is by [Andreas Pardeike](https://github.com/pardeike), but does not unprotect the memory pages it writes to, making the program crash.
