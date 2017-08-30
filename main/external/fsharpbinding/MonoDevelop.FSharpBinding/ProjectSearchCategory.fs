namespace MonoDevelop.FSharp
open System.Collections.Generic
open System.Threading.Tasks
open MonoDevelop.Core
open MonoDevelop.Core.Text
open MonoDevelop.Components.MainToolbar
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Projects
open Microsoft.FSharp.Compiler.SourceCodeServices
open Symbols

[<AutoOpen>]
module Accessibility =

    let inline getImage name = ImageService.GetIcon( name, Gtk.IconSize.Menu)

    let inline getImageFromAccessibility pub inter priv typeWithAccessibility =
        let accessibility = (^a : (member Accessibility : FSharpAccessibility) typeWithAccessibility)
        if accessibility.IsPublic then getImage pub
        elif accessibility.IsInternal then getImage inter
        else getImage priv

module Search =

    let inline private is expr s =
        match expr s with Some _ -> true | None -> false

    let private filter tag (s:FSharpSymbolUse seq) =
        match tag with
        | "type" | "t" | "c" -> s |> Seq.filter (is (|Class|_|))
        | "mod" -> s |> Seq.filter (is (|Module|_|))
        | "s" -> s |> Seq.filter (is (|ValueType|_|))
        | "i" -> s |> Seq.filter (is (|Interface|_|))
        | "e" -> s |> Seq.filter (is (|Enum|_|))
        | "d" -> s |> Seq.filter (is (|Delegate|_|))
        | "u" -> s |> Seq.filter (is (|Union|_|))
        | "r" -> s |> Seq.filter (is (|Record|_|))
        | "member" | "m" -> s |> Seq.filter (is (|Method|_|))
        | "p" -> s |> Seq.filter (is (|Property|_|))
        | "f" -> s |> Seq.filter (is (|Field|_|))
        | "ap" -> s |> Seq.filter (is (|ActivePattern|_|))
        | "op" -> s |> Seq.filter (is (|Operator|_|))
        | _ -> s

    let byTag tag (items: FSharpSymbolUse seq) =
        let filtered = items |> filter tag
        filtered

    let getAllFSharpProjects() =
      seq { for p in IdeApp.Workspace.GetAllProjects() do
                if p.SupportedLanguages |> Array.contains "F#" then 
                    yield p }

    let getAllProjectSymbols project =
        async {
            try
                let checkResult = languageService.GetCachedProjectCheckResult project
                match checkResult with
                | Some v -> let! allSymbols =  v.GetAllUsesOfAllSymbols()
                            return allSymbols |> Array.toSeq
                | None -> return Seq.empty
            with ex ->
                LoggingService.LogError("Global Search (F#) error", ex)

                return Seq.empty }
    

    let getAllSymbolsInAllProjects() =
        asyncSeq {
            for projectFile in getAllFSharpProjects() do
                let! symbols = getAllProjectSymbols(projectFile)
                for symbol in symbols do
                    yield symbol
        }

    /// constructors have a display name of ( .ctor ) use the enclosing entities display name
    let correctDisplayName (symbol:FSharpSymbolUse) =
        match symbol with
        | SymbolUse.Constructor c ->
            match c.EnclosingEntity with
            | Some ent -> ent.DisplayName
            | _ -> LoggingService.LogError(sprintf "Constructor with no EnclosingEntity: %s" c.DisplayName)
                   c.DisplayName
        | _ -> symbol.Symbol.DisplayName

    let byPattern (cache:Dictionary<_,_>) pattern symbols =

        let matchName (matcher:StringMatcher) (name:string) =
            if name = null then (false, -1)
            else
                match cache.TryGetValue(name) with
                | true, v -> v
                | false, _ ->
                    let doesMatch, rank = matcher.CalcMatchRank (name)
                    let savedMatch = (doesMatch, rank)
                    cache.Add(name, savedMatch)
                    savedMatch

        let matcher = StringMatcher.GetMatcher (pattern, false)

        symbols
        |> Seq.choose (fun s -> let doesMatch, rank = matchName matcher (correctDisplayName s)
                                if doesMatch then Some(s, rank)
                                else None)

type SymbolSearchResult(match', matchedString, rank, symbol:FSharpSymbolUse) =
    inherit SearchResult(match', matchedString, rank)

    let simpleName = Search.correctDisplayName symbol
    let offsetAndLength = lazy Symbols.getOffsetAndLength simpleName symbol

    override x.SearchResultType =
        match symbol with
        | SymbolUse.Record _ | SymbolUse.Module _ | SymbolUse.ValueType _ | SymbolUse.Delegate _ | SymbolUse.Union _  | SymbolUse.Class _
        | SymbolUse.Namespace _ | SymbolUse.Interface _ | SymbolUse.Enum _ | SymbolUse.ActivePattern _ -> SearchResultType.Type

        | SymbolUse.ActivePatternCase _ | SymbolUse.Field _ | SymbolUse.UnionCase _ | SymbolUse.Property _
        | SymbolUse.Event _ | SymbolUse.Operator _ | SymbolUse.Constructor _ | SymbolUse.Function _ | SymbolUse.Val _-> SearchResultType.Member
        | _ -> SearchResultType.Unknown

    override x.Description =
        let cat =
            match symbol with
            | SymbolUse.Record _ -> "record"
            | SymbolUse.Module _ -> "module"
            | SymbolUse.ValueType _ -> "struct"
            | SymbolUse.Delegate _ -> "delegate"
            | SymbolUse.Union _ -> "union"
            | SymbolUse.Class c -> if c.IsFSharp then "type" else "class"
            | SymbolUse.Namespace _ -> "namespace"
            | SymbolUse.Interface _ -> "interface"
            | SymbolUse.Enum _ -> "enum"
            | SymbolUse.ActivePattern _ -> "active pattern"
            | SymbolUse.Field _ -> "field"
            | SymbolUse.UnionCase _ -> "union case"
            | SymbolUse.Property _ -> "property"
            | SymbolUse.Event _ -> "event"
            | SymbolUse.Operator _ -> "operator"
            | SymbolUse.Constructor _ -> "constructor"
            | SymbolUse.Method _ -> "method"
            | SymbolUse.Function _ -> "function"
            | SymbolUse.Val _ -> "val"
            | _ -> "symbol"
        sprintf "%s (file %s)" cat symbol.RangeAlternate.FileName

    override x.PlainText = simpleName

    override x.File = symbol.RangeAlternate.FileName
    override x.Icon =
        match symbol with
        | SymbolUse.Record _ -> getImage "md-type"
        | SymbolUse.Module _ -> getImage "md-module"
        | SymbolUse.ValueType s -> s |> getImageFromAccessibility Stock.Struct.Name Stock.InternalStruct.Name Stock.PrivateStruct.Name
        | SymbolUse.Delegate d -> d |> getImageFromAccessibility Stock.Delegate.Name Stock.InternalDelegate.Name Stock.PrivateDelegate.Name
        | SymbolUse.Union _ -> getImage "md-type"
        | SymbolUse.Class c -> if c.IsFSharp then getImage "md-type" else c |> getImageFromAccessibility Stock.Class.Name Stock.InternalClass.Name Stock.PrivateClass.Name
        | SymbolUse.Namespace _ -> getImage Stock.NameSpace.Name
        | SymbolUse.Interface i -> i |> getImageFromAccessibility Stock.Interface.Name Stock.InternalInterface.Name Stock.PrivateInterface.Name
        | SymbolUse.Enum e -> e |> getImageFromAccessibility Stock.Enum.Name Stock.InternalEnum.Name Stock.PrivateEnum.Name
        | SymbolUse.ActivePattern _ -> getImage "md-type"
        | SymbolUse.Field f ->f |> getImageFromAccessibility Stock.Field.Name Stock.InternalField.Name Stock.PrivateField.Name
        | SymbolUse.UnionCase _ -> getImage "md-type"
        | SymbolUse.Property p -> p |> getImageFromAccessibility Stock.Property.Name Stock.InternalProperty.Name Stock.PrivateProperty.Name
        | SymbolUse.Event e -> e |> getImageFromAccessibility Stock.Event.Name Stock.InternalEvent.Name Stock.PrivateEvent.Name
        | SymbolUse.Operator _ -> getImage "md-fs-field"
        | SymbolUse.Constructor c -> c |> getImageFromAccessibility Stock.Method.Name Stock.InternalMethod.Name Stock.PrivateMethod.Name
        | SymbolUse.Function mfv ->
            if mfv.IsExtensionMember then mfv |> getImageFromAccessibility "md-extensionmethod" "md-internal-extensionmethod" "md-private-extensionmethod"
            elif mfv.IsMember then mfv |> getImageFromAccessibility Stock.Method.Name Stock.InternalMethod.Name Stock.PrivateMethod.Name
            else getImage "md-fs-field"
        | SymbolUse.Val _ -> getImage "md-fs-field" //NOTE: Maybe make this a normal field icon?
        | _ -> getImage Stock.Event.Name

    override x.GetTooltipInformation(_token) = 
        SymbolTooltips.getTooltipInformation symbol |> Async.StartAsTask
        
    override x.Offset = fst (offsetAndLength.Force())
    override x.Length = snd (offsetAndLength.Force())

type ProjectSearchCategory() =
    inherit SearchCategory(GettextCatalog.GetString ("Solution"), sortOrder = SearchCategory.FirstCategory)

    //type, module, struct, interface, enum, delegate, union, record
    let typeTags = ["type"; "t"; "c"; "mod"; "s"; "i"; "e"; "d"; "u"; "r" ]

    //member, property, field, event, active pattern, operator
    let memberTags = ["member"; "m"; "p"; "f"; "evt"; "ap"; "op"]
    let tags = lazy (List.concat [typeTags; memberTags] |> List.toArray)

    override x.get_Tags() = tags.Force()

    override x.IsValidTag tag =
        typeTags |> List.contains tag || memberTags |> List.contains tag

    override x.GetResults(callback, pattern, token) =
        let cachingSearch = Search.byPattern (Dictionary<_,_>())
        Task.Run(
            (fun () -> async {
                for projFile in Search.getAllFSharpProjects() do
                    try
                        //LoggingService.LogInfo(sprintf "F# Global Search: Getting all project symbols for %s" shortName )
                        let! allProjectSymbols = Search.getAllProjectSymbols projFile
            
                        //LoggingService.LogInfo(sprintf "F# Global Search: Filtering %i project symbols from %s, for definitions" (allProjectSymbols |> Seq.length) shortName )
                        let definitions = allProjectSymbols |> Seq.filter (fun s -> s.IsFromDefinition)
            
                        //LoggingService.LogInfo(sprintf "F# Global Search: Filtering %i matching tag %s for %s" (definitions |> Seq.length) pattern.Tag shortName )
                        let tagFiltered = definitions |> Search.byTag pattern.Tag
            
                        //LoggingService.LogInfo(sprintf "F# Global Search: Caching search on %i typeFilteredSymbols for matching pattern %s on %s" (tagFiltered |> Seq.length) pattern.Pattern shortName )
                        let matchedSymbols = tagFiltered |> cachingSearch pattern.Pattern
            
                        //LoggingService.LogInfo(sprintf "F# Global Search: Matched %i symbols from %s" (matchedSymbols |> Seq.length) shortName )
                        for symbol:FSharpSymbolUse, rank in matchedSymbols do
                            let sr = SymbolSearchResult(pattern.Pattern, symbol.Symbol.DisplayName, rank, symbol)
                            callback.ReportResult sr
            
                    with ex ->
                        LoggingService.LogError("F# Global Serach error", ex) } |> Async.StartImmediate) , token )
