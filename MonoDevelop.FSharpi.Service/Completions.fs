namespace MonoDevelop.FSharpInteractive

open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

type CompletionData = {
    displayText: string
    category: string
    icon: string
    overloads: CompletionData list
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
                            Some 
                                {
                                displayText = name
                                icon = symbolToIcon head
                                category = ""
                                overloads = []
                                }

                        else
                            None
                    else
                        Some 
                            {
                            displayText = head.Symbol.DisplayName
                            icon = symbolToIcon head
                            category = ""
                            overloads = []
                            }

                //match tryGetCategory head, completion with
                //| Some (id, ent), Some comp -> 
                //    let category = getOrAddCategory ent id
                //    comp.CompletionCategory <- category
                //| _, _ -> ()

                completion
            | _ -> None

    let getCompletions (fsiSession: FsiEvaluationSession, input:string, column: int) =
        async {
            let parseResults, checkResults, _checkProjectResults = fsiSession.ParseAndCheckInteraction("();;")
            let longName,residue = Parsing.findLongIdentsAndResidue(column, input)
            let! results = checkResults.GetDeclarationListSymbols(Some parseResults, 1, column, input, longName, residue, fun (_,_) -> false)
            return results 
                   |> List.choose symbolToCompletionData
        }
