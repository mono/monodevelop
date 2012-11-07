#nowarn "62" // This construct is for ML compatibility.

//-------------------------------------------------------------------------------------------------
// Public types

namespace Microsoft.FSharp.Compiler.CodeDom

open System
open System.IO
open System.Text
open System.Collections
open System.CodeDom
open System.CodeDom.Compiler

open Microsoft.FSharp.Compiler.CodeDom.Internal

type FSharpCodeProvider() = 
    inherit CodeDomProvider()
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")>]
    override this.FileExtension = "fs";
    
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")>]
    override this.CreateCompiler() = 
        raise (NotSupportedException("Compilation not supported."))

    [<System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")>]            
    override this.CreateGenerator() =
        let usingStringWriter f =
            let sb = new StringBuilder()
            use sw = new StringWriter(sb)
            f (sw :> TextWriter);
            let res = sb.ToString();
            res
      
        { new ICodeGenerator with
            // Identifier related functions            
            member this.CreateEscapedIdentifier (value:string) : string =
              Generator.makeEscapedIdentifier value
            member this.CreateValidIdentifier (value:string) : string =
              Generator.makeValidIdentifier value
            member this.IsValidIdentifier (value:string) : bool =
              Generator.isValidIdentifier value; 
            member this.ValidateIdentifier (value:string) : unit =
              if (not (Generator.isValidIdentifier value)) then 
                raise (ArgumentException(sprintf "'%s' is not a valid F# identifier!" value))
  
            // Implementations of code generation related functions
            member this.GenerateCodeFromCompileUnit(compileUnit, textWriter, options) =
                Generator.createContext textWriter options Generator.AdditionalOptions.None
                |> Generator.generateCompileUnit compileUnit 
                |> ignore
            
            member this.GenerateCodeFromExpression(codeExpr, textWriter, options) : unit =
                Generator.createContext textWriter options Generator.AdditionalOptions.None
                |> Generator.generateExpression codeExpr
                |> ignore
            
            member this.GenerateCodeFromNamespace(codeNamespace, textWriter, options) : unit =
                (Generator.createContext textWriter options Generator.AdditionalOptions.None) 
                |> (Generator.generateNamespace codeNamespace) 
                |> ignore
            
            member this.GenerateCodeFromStatement(codeStatement, textWriter, options) : unit =
                (Generator.createContext textWriter options Generator.AdditionalOptions.None) 
                |> (Generator.generateStatement codeStatement) 
                |> ignore
                
            member this.GenerateCodeFromType(codeTypeDecl, textWriter, options) : unit =
                (Generator.createContext textWriter options Generator.AdditionalOptions.None) 
                |> (Generator.generateTypeDeclOnly codeTypeDecl) 
                |> ignore
            member this.GetTypeOutput (t:CodeTypeReference) : string =
                usingStringWriter (fun sw ->
                  (Generator.createContext sw (CodeGeneratorOptions()) Generator.AdditionalOptions.None) 
                  |> (Generator.generateTypeRef t) |> ignore)

            member this.Supports (supports:GeneratorSupport) : bool =
              (supports &&&  (GeneratorSupport.ReturnTypeAttributes ||| 
                              GeneratorSupport.ParameterAttributes ||| 
                              GeneratorSupport.AssemblyAttributes ||| 
                              GeneratorSupport.StaticConstructors ||| 
                              GeneratorSupport.NestedTypes ||| 
                              GeneratorSupport.EntryPointMethod |||
                              GeneratorSupport.GotoStatements ||| 
                              GeneratorSupport.MultipleInterfaceMembers |||
                              GeneratorSupport.ChainedConstructorArguments
                              ) = enum 0) }
