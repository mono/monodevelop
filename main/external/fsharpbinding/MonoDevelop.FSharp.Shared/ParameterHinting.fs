namespace MonoDevelop.FSharp.Shared
open Microsoft.FSharp.Compiler.SourceCodeServices

module ParameterHinting =
    let getTooltipInformation (symbol: FSharpSymbolUse) =
        match symbol with
        | MemberFunctionOrValue m ->
            let parameters =
                match m.CurriedParameterGroups |> Seq.toList with
                | [single] ->
                    single 
                    |> Seq.map (fun param ->
                                    match param.Name with
                                    | Some n -> n
                                    | _ -> param.DisplayName)
                    |> Array.ofSeq
                | _ -> [||]
            let signature = SymbolTooltips.getFuncSignatureWithFormat symbol.DisplayContext m {Indent=3;Highlight=None}
            let summary = SymbolTooltips.getSummaryFromSymbol m
            ParameterTooltip.ToolTip (signature, summary, parameters)
        | _ -> ParameterTooltip.EmptyTip

    let parameterCount (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as fsm ->
            let cpg = fsm.CurriedParameterGroups
            cpg.[0].Count
        | _ -> 0

    let isParameterListAllowed (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as fsm 
            when fsm.CurriedParameterGroups.Count > 0 ->
                //TODO: How do we handle non tupled arguments?
                let group = fsm.CurriedParameterGroups.[0] 
                if group.Count > 0 then
                    let last = group |> Seq.last
                    last.IsParamArrayArg
                else
                    false
        | _ -> false

    let getParameterName (symbol: FSharpSymbol) i =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as fsm 
            when fsm.CurriedParameterGroups.Count > 0 &&
                 fsm.CurriedParameterGroups.[0].Count > 0 ->
                //TODO: How do we handle non tupled arguments?
                let group = fsm.CurriedParameterGroups.[0]
                let param = group.[i]
                match param.Name with
                | Some n -> n
                | None -> param.DisplayName
        | _ -> ""
