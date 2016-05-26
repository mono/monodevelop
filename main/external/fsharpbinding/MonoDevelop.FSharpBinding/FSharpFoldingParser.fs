namespace MonoDevelop.FSharp
open System
open MonoDevelop.Ide.TypeSystem

/// The folding parser is used for generating a preliminary parsed document that does not
/// contain a full dom - only some basic lexical constructs such as comments or pre processor directives.
/// As we dont currently fold comments or compiler directives this is an empty DefaultParsedDocument
type FSharpFoldingParser() =
    interface IFoldingParser with
        member x.Parse(fileName, _content) = 
            DefaultParsedDocument (fileName) :> _