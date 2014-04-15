
namespace MonoDevelop.FSharp

open System
open System.Collections.Generic

open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeFormatting
open MonoDevelop.Ide.Gui
open MonoDevelop.Projects.Policies
open Mono.TextEditor

/// As you can see, there's not much happening here. This is implemented because 
/// MonoDevelop throws an exception when inserting a template when there's not a 
/// Formatter available where 'SupportsOnTheFlyFormatting' returns true

type FSharpFormatter() =
    inherit AbstractAdvancedFormatter()

    override x.SupportsOnTheFlyFormatting = true
    override x.SupportsCorrectingIndent = true

    override x.CorrectIndenting(policyParent:PolicyContainer, mimeTypeChain: string seq, data:TextEditorData, line:int) = ()

    override x.OnTheFlyFormat(doc:Document, startOffset, endOffset) = () 
    override x.FormatText(policyParent:PolicyContainer, mimeTypeChain:string seq, input, startOffset, endOffset) = 
         input
