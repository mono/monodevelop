namespace MonoDevelop.FSharp

open Microsoft.FSharp.Compiler
open MonoDevelop
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Debugger

type FSharpDebuggerExpressionResolver() =
    inherit TextEditorExtension()

    interface IDebuggerExpressionResolver with
        member x.ResolveExpressionAsync (doc, context, offset, cancellationToken) =
            async {
                let ast = context.TryGetAst()
                let location =
                    match ast with
                    | None -> None
                    | Some pcr ->
                        let location = doc.OffsetToLocation(offset)
                        let line = doc.GetLine location.Line
                        let lineTxt = doc.GetTextAt (line.Offset, line.Length)
                        let symbol = pcr.GetSymbolAtLocation (location.Line, location.Column, lineTxt) |> Async.RunSynchronously
                        match symbol with
                        | Some symbolUse when not symbolUse.IsFromDefinition ->
                            match symbolUse with
                            | SymbolUse.ActivePatternCase apc ->
                                Some (apc.DeclarationLocation, apc.DisplayName)
                            | SymbolUse.Entity _ent -> None
                            | SymbolUse.Field _field ->
                                let loc = symbolUse.RangeAlternate
                                Some (loc, lineTxt.[loc.StartColumn..loc.EndColumn-1])
                            | SymbolUse.GenericParameter gp ->
                                Some (gp.DeclarationLocation, gp.DisplayName)
                            | SymbolUse.Parameter p ->
                                Some (p.DeclarationLocation, p.DisplayName)
                            | SymbolUse.StaticParameter sp ->
                                Some (sp.DeclarationLocation, sp.DisplayName)
                            | SymbolUse.UnionCase _uc -> None
                            | SymbolUse.Class _c -> None
                            | SymbolUse.ClosureOrNestedFunction _cl -> None
                            | SymbolUse.Constructor _ctor -> None
                            | SymbolUse.Delegate _del -> None
                            | SymbolUse.Enum enum ->
                                Some (enum.DeclarationLocation, enum.DisplayName)
                            | SymbolUse.Event _ev -> None
                            | SymbolUse.Function _f -> None
                            | SymbolUse.Interface _i -> None
                            | SymbolUse.Module _m -> None
                            | SymbolUse.Namespace _ns -> None
                            | SymbolUse.Operator _op -> None
                            | SymbolUse.Pattern _p -> None
                            | SymbolUse.Property _pr ->
                                let loc = symbolUse.RangeAlternate
                                let partialName = QuickParse.GetPartialLongNameEx(lineTxt, loc.EndColumn-1)
                                let longName = (partialName.QualifyingIdents @ [partialName.PartialIdent]) |> String.concat "."
                                Some (loc, longName)
                            | SymbolUse.Record r ->
                                let loc = r.DeclarationLocation
                                Some (loc, r.DisplayName)
                            | SymbolUse.TypeAbbreviation _ta -> None
                            | SymbolUse.Union _un -> None
                            | SymbolUse.Val v ->
                                let loc = v.DeclarationLocation
                                Some (loc, v.DisplayName)
                            | SymbolUse.ValueType _vt -> None
                            | _ -> None
                        | _ -> None
                match location with
                | None -> return DebugDataTipInfo()
                | Some (range, name) ->
                    let ts = Symbols.getTextSpan range doc
                    return DebugDataTipInfo(ts, name)}

            |> StartAsyncAsTask cancellationToken

