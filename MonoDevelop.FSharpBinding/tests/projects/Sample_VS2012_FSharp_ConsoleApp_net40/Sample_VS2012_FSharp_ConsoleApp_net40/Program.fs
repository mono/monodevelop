// This is a sample F# app created in Visual Studio 2012, targeting .NET 4.0

// On Windows, the build should reference
//    -r:"C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\mscorlib.dll"
//    -r:"C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.dll"
//    -r:"C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Core.dll"
//    -r:"C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Numerics.dll"
//  and should reference one of these depending on the language version of F# being used
//    -r:"C:Program Files\Reference Assemblies\Microsoft\FSharp\2.0\Runtime\v4.0\FSharp.Core.dll"
//    -r:"C:Program Files\Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v4.0\FSharp.Core.dll"
module M 


[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    0 // return an integer exit code
