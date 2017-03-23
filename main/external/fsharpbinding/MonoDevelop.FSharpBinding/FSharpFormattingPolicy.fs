namespace MonoDevelop.FSharp

open MonoDevelop.Core
open MonoDevelop.Core.Serialization
open MonoDevelop.Projects.Policies

[<CLIMutable>]
type FSharpFormattingSettings = {
    [<ItemProperty>]
    [<LocalizedCategory ("Layout")>]
    [<LocalizedDisplayName ("Indent on try/with")>]
    mutable IndentOnTryWith : bool;

    [<ItemProperty>]
    [<LocalizedCategory ("Refactoring")>]
    [<LocalizedDisplayName ("Reorder open declaration")>]
    mutable ReorderOpenDeclaration : bool

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space after comma")>]
    mutable SpaceAfterComma : bool

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space after semicolon")>]
    mutable SpaceAfterSemicolon : bool

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space around delimiter")>]
    mutable SpaceAroundDelimiter : bool

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space before argument")>]
    mutable SpaceBeforeArgument : bool

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space before colon")>]
    mutable SpaceBeforeColon : bool
}

[<CLIMutable>]
[<PolicyType ("F# formatting")>]
type FSharpFormattingPolicy = {
    [<ItemProperty>]
    mutable Formats : ResizeArray<FSharpFormattingSettings>

    [<ItemProperty>]
    mutable DefaultFormat : FSharpFormattingSettings
}

module DefaultFSharpFormatting =
    let settings = 
        { IndentOnTryWith=false
          ReorderOpenDeclaration=false
          SpaceAfterComma=true
          SpaceAfterSemicolon=true
          SpaceAroundDelimiter=true
          SpaceBeforeArgument=true
          SpaceBeforeColon=true }

    let policy = { Formats = ResizeArray<FSharpFormattingSettings>(); DefaultFormat=settings }
