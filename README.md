This repository contains source code for https://sharplab.io.

SharpLab is a .NET code playground that shows intermediate steps and results of code compilation.
Some language features are thin wrappers on top of other features -- e.g. `using()` becomes `try/catch`.
SharpLab allows you to see the code as compiler sees it, and get a better understanding of .NET languages.

Recent versions include experimental support for running code, with some limitations.

### Languages

SharpLab supports three source languages:

1. C#
2. Visual Basic
3. F#

Due to complexity of F#'s compiler library, some features might not be available for F#.

### Decompilation/Disassembly

There are currently four targets for decompilation/disassembly:

1. C#
2. Visual Basic
3. IL
4. JIT Asm (Native Asm Code)

Note that C#=>VB or VB=>C# disassembly shouldn't be used to convert between languages, as the produced code is intentionally overly verbose.

### Execution

You can use "Run" target to execute your code and see the output.  
Execution enables a few nice features such as flow arrows — see here:  
https://twitter.com/ashmind/status/894058159223955456.

Execution is intentionally limited, however the limits are continuously improved and corrected.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).