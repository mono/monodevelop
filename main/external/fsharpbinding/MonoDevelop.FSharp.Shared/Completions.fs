namespace MonoDevelop.FSharp.Shared

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell
open System
open System.IO

type CompletionData = {
    displayText: string
    completionText: string
    category: string
    icon: string
    overloads: CompletionData list
    description: string
}

type PathCompletion = {
    paths: string seq
    residue: string
}

module Completion =
    let rec allBaseTypes (entity:FSharpEntity) =
        seq {
            match entity.TryFullName with
            | Some _ ->
                match entity.BaseType with
                | Some t ->
                    yield t
                    if t.HasTypeDefinition then
                        yield! allBaseTypes t.TypeDefinition
                | _ -> ()
            | _ -> ()
        }

    let isAttribute (symbolUse: FSharpSymbolUse) =
        match symbolUse.Symbol with
        | :? FSharpEntity as ent ->
            allBaseTypes ent
            |> Seq.exists (fun t -> if t.HasTypeDefinition then
                                        match t.TypeDefinition.TryFullName with
                                        | Some name -> name = "System.Attribute"
                                        | _ -> false
                                    else
                                        false)
        | _ -> false

    let symbolToIcon (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | SymbolUse.ActivePatternCase _ -> "ActivePatternCase"
        | SymbolUse.Field _ -> "Field"
        | SymbolUse.UnionCase _ -> "UnionCase"
        | SymbolUse.Class _ -> "Class"
        | SymbolUse.Delegate _ -> "Delegate"
        | SymbolUse.Constructor _  -> "Constructor"
        | SymbolUse.Event _ -> "Event"
        | SymbolUse.Property _ -> "Property"
        | SymbolUse.Function f ->
            if f.IsExtensionMember then "ExtensionMethod"
            elif f.IsMember then "Method"
            else "Field"
        | SymbolUse.Operator _ -> "Operator"
        | SymbolUse.ClosureOrNestedFunction _ -> "ClosureOrNestedFunction"
        | SymbolUse.Val _ -> "Val"
        | SymbolUse.Enum _ -> "Enum"
        | SymbolUse.Interface _ -> "Interface"
        | SymbolUse.Module _ -> "Module"
        | SymbolUse.Namespace _ -> "Namespace"
        | SymbolUse.Record _ -> "Record"
        | SymbolUse.Union _ -> "Union"
        | SymbolUse.ValueType _ -> "ValueType"
        | SymbolUse.Entity _ -> "Entity"
        | _ -> "Event"

    let symbolToCompletionData (symbols : FSharpSymbolUse list) =
        let getCompletion displayText symbol = 
            Some  ({
                    displayText = displayText
                    completionText = PrettyNaming.QuoteIdentifierIfNeeded displayText
                    icon = symbolToIcon symbol
                    category = ""
                    overloads = []
                    description = null
                }, symbol)
        match symbols with
        | head :: tail ->
            let completion =
                if (*isInsideAttribute*) false then
                    if isAttribute head then
                        let name = head.Symbol.DisplayName
                        let name =
                            if name.EndsWith("Attribute") then
                                name.Remove(name.Length - 9)
                            else
                                name
                        getCompletion name head    

                    else
                        None
                else
                    getCompletion head.Symbol.DisplayName head

            completion
        | _ -> None

    let hashDirectives =
        [
            { displayText = "#r"; completionText ="#r"; category ="keywords"; icon = "md-keyword"; description = "Reference (dynamically load) the given DLL"; overloads = [] }
            { displayText = "#I"; completionText = "#I"; category ="keywords"; icon = "md-keyword"; description = "Add the given search path for referenced DLLs"; overloads = [] }
            { displayText = "#load"; completionText = "#load"; category ="keywords"; icon = "md-keyword"; description = "Load the given file(s) as if compiled and referenced"; overloads = [] }
            { displayText = "#time"; completionText = "#time"; category ="keywords"; icon = "md-keyword"; description = "Toggle timing on/off"; overloads = [] }
            { displayText = "#help"; completionText = "#help"; category ="keywords"; icon = "md-keyword"; description = "Display help"; overloads = [] }
            { displayText = "#quit"; completionText = "#quit"; category ="keywords"; icon = "md-keyword"; description = "Exit"; overloads = [] }
        ]
         
    let mutable symbolList = List.empty

    let getPaths directive (path: string) =
        seq {
            // absolute paths
            if (Directory.Exists path) then
                yield! Directory.EnumerateDirectories path

                if directive = "load" then
                    yield! Directory.EnumerateFiles(path, "*.fsx")

                if directive = "r" then
                    yield! Directory.EnumerateFiles(path, "*.dll")
        }

    let getPathCompletion (workingFolder: string option) directive (path: string) =
        let separatorIndex = path.LastIndexOf Path.DirectorySeparatorChar
        let pathToSeparator, residue, offset = 
            if separatorIndex > -1 then
                path.[0..separatorIndex], path.[separatorIndex..], 0
            else
                "", path, 1

        let paths = seq {
            if Path.IsPathRooted pathToSeparator then
                yield! getPaths directive pathToSeparator
                       |> Seq.map(fun path -> path.[separatorIndex..])
            // relative to working folder
            elif workingFolder.IsSome then
                let relpath = Path.Combine (workingFolder.Value, pathToSeparator)
                yield! getPaths directive relpath
                       |> Seq.map(fun path -> path.[separatorIndex+workingFolder.Value.Length+1+offset..])
        } 

        { residue=residue; paths=paths  }

    let getCompletions (fsiSession: FsiEvaluationSession, input:string, column: int) =
        async {
            let! parseResults, checkResults, _checkProjectResults = fsiSession.ParseAndCheckInteraction(input)
            let longName,residue = Parsing.findLongIdentsAndResidue(column, input)
            if residue.Length > 0 && residue.[0] = '#' then
                return hashDirectives |> Array.ofList
            else
                let partialName = QuickParse.GetPartialLongNameEx(input, column-1)

                let! symbols = checkResults.GetDeclarationListSymbols(Some parseResults, 1, input, partialName, fun() -> [])
                let results = symbols
                              |> List.choose symbolToCompletionData

                let completions, symbols = results |> List.unzip
                symbolList <- symbols
                return completions |> Array.ofList
        }

    let getCompletionTooltip filter =
        async {
            let symbol =
                symbolList 
                |> List.tryFind (fun sym -> sym.Symbol.DisplayName = filter)

            return
                match symbol with 
                | Some symbol' ->
                    match SymbolTooltips.getTooltipFromSymbolUse symbol' with
                    | Some tooltip -> ToolTip tooltip
                    | None -> EmptyTip
                | None ->
                    EmptyTip
        }

    

    let getParameterHints (fsiSession: FsiEvaluationSession, input:string, column: int) =
        async {
            let! _parseResults, checkResults, _checkProjectResults = fsiSession.ParseAndCheckInteraction("();;")

            let lineToCaret = input.[0..column-1]
            let column = lineToCaret |> Seq.tryFindIndexBack (fun c -> c <> '(' && c <> ' ')
            match column with
            | Some col ->
                match Parsing.findIdents (col-1) lineToCaret SymbolLookupKind.ByLongIdent with
                | None -> return []
                | Some(colu, identIsland) ->
                    let! symbols = 
                        checkResults.GetMethodsAsSymbols(1, colu, lineToCaret, identIsland)

                    match symbols with
                    | Some symbols' when symbols'.Length > 0 ->
                        return
                            symbols'
                            |> List.map ParameterHinting.getTooltipInformation
                    | _ -> return []
            | _ -> return []
        }
