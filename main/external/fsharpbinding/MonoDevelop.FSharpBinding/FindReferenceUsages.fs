namespace MonoDevelop.FSharp

open System
open System.Reflection
open System.Threading.Tasks
open MonoDevelop.Ide.FindInFiles
open MonoDevelop.Refactoring

type FSharpFindReferenceUsagesProvider() =
    inherit FindReferenceUsagesProvider()

    override this.FindReferences(reference, monitor) =
        async {

            let assemblyName = AssemblyName(reference.Reference).Name
            let! symbols = Search.getAllProjectSymbols reference.Project

            symbols
            |> Seq.filter(fun symbol ->
                symbol.Symbol.Assembly.SimpleName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
            |> Seq.map (fun symbol -> Symbols.getOffsetsTrimmed symbol.Symbol.DisplayName symbol)
            |> Seq.distinct
            |> Seq.iter (fun (filename, startOffset, endOffset) ->
                let result = SearchResult (FileProvider (filename), startOffset, endOffset-startOffset)
                monitor.ReportResult result)
        } |> Async.StartAsTask :> Task
