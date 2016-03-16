namespace MonoDevelop.FSharpInteractive

open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

type CompletionData = {
    displayText: string
    category: string
    icon: string
    overloads: CompletionData list
    description: string
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

    let symbolToCompletionData (symbols : FSharpSymbolUse list) =
        let getCompletion displayText symbol = 
            Some  {
                displayText = displayText
                icon = symbolToIcon symbol
                category = ""
                overloads = []
                description = null
            }
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

    let getHashDirectives =
        [
            { displayText = "#r"; category ="keywords"; icon = "md-keyword"; description = "Reference (dynamically load) the given DLL"; overloads = [] }
            { displayText = "#I"; category ="keywords"; icon = "md-keyword"; description = "Add the given search path for referenced DLLs"; overloads = [] }
            { displayText = "#load"; category ="keywords"; icon = "md-keyword"; description = "Load the given file(s) as if compiled and referenced"; overloads = [] }
            { displayText = "#time"; category ="keywords"; icon = "md-keyword"; description = "Toggle timing on/off"; overloads = [] }
            { displayText = "#help"; category ="keywords"; icon = "md-keyword"; description = "Display help"; overloads = [] }
            { displayText = "#quit"; category ="keywords"; icon = "md-keyword"; description = "Exit"; overloads = [] }
        ]

    let getCompletions (fsiSession: FsiEvaluationSession, input:string, column: int) =
        async {
            let parseResults, checkResults, _checkProjectResults = fsiSession.ParseAndCheckInteraction("();;")
            let longName,residue = Parsing.findLongIdentsAndResidue(column, input)
            let! symbols = checkResults.GetDeclarationListSymbols(Some parseResults, 1, column, input, longName, residue, fun (_,_) -> false)
            let results = symbols 
                          |> List.choose symbolToCompletionData
            if longName.Length = 0 && residue.Length = 0 then
                return results
                |> List.append getHashDirectives
            else
                return results
        }
