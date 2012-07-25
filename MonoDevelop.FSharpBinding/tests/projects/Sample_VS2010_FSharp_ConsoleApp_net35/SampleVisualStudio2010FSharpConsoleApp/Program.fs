// This is a sample F# app created in Visual Studio 2010, targeting .NET 3.5

// On Windows, the build should target
//    -r:"C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll"
//    -r:"C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll"
//  and should reference one of these depending on the language version of F# being used
//     Program Files\Reference Assemblies\Microsoft\FSharp\2.0\Runtime\v2.0\FSharp.Core.dll
//     Program Files\Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v2.0\FSharp.Core.dll

module M 

[<EntryPoint>]
let main args = 
    System.Console.WriteLine "Hello world"
    0
