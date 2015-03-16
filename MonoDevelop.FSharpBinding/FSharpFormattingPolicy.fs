namespace MonoDevelop.FSharp

open System
open System.Text
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Core
open MonoDevelop.Core.Serialization
open System.ComponentModel
open MonoDevelop.Projects.Policies

type FSharpFormattingSettings() = 
    [<ItemProperty>]
    [<LocalizedCategory ("Layout")>]
    [<LocalizedDisplayName ("Indent on try/with")>]
    member val IndentOnTryWith = false with get, set

    [<ItemProperty>]
    [<LocalizedCategory ("Refactoring")>]
    [<LocalizedDisplayName ("Reorder open declaration")>]
    member val ReorderOpenDeclaration = false with get, set

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space after comma")>]
    member val SpaceAfterComma = true with get, set

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space after semicolon")>]
    member val SpaceAfterSemicolon = true with get, set

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space around delimiter")>]
    member val SpaceAroundDelimiter = true with get, set

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space before argument")>]
    member val SpaceBeforeArgument = true with get, set

    [<ItemProperty>]
    [<LocalizedCategory ("Spacing")>]
    [<LocalizedDisplayName ("Space before colon")>]
    member val SpaceBeforeColon = true with get, set

//    [<ItemProperty>]
//    [<LocalizedCategory ("Syntax")>]
//    [<LocalizedDisplayName ("Semicolon at End of Line")>]
//    member val SemicolonAtEndOfLine = false with get, set

    member x.Clone() =
        x.MemberwiseClone() :?> FSharpFormattingSettings

[<AllowNullLiteral>]
[<PolicyType ("F# formatting")>]
type FSharpFormattingPolicy() =
    [<ItemProperty>]
    member val Formats = ResizeArray<FSharpFormattingSettings>() with get, set
                
    [<ItemProperty>]
    member val DefaultFormat = FSharpFormattingSettings() with get, set

    member x.Clone() =
        let clone = FSharpFormattingPolicy()
        clone.DefaultFormat <- x.DefaultFormat.Clone()
        for f in x.Formats do
            clone.Formats.Add (f.Clone())
        clone

    interface IEquatable<FSharpFormattingPolicy> with
        member this.Equals(other) = 
            this.DefaultFormat = other.DefaultFormat 
            && Seq.forall (fun f -> Seq.exists (fun f' -> f' = f) other.Formats) this.Formats