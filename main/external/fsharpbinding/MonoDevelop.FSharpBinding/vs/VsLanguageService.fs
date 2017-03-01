namespace Microsoft.VisualStudio.FSharp.Editor
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Diagnostics
open Microsoft.CodeAnalysis.Options
open MonoDevelop.FSharp
open ExtCore.Control
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.ItemDescriptionIcons

type FSharpCheckerProvider() =
    member x.Checker = languageService.Checker

type ProjectInfoManager() =
    member x.TryGetOptionsForEditingDocumentOrProject (document:Document) =
        let res, sourceText = document.TryGetText()
        languageService.GetCheckerOptions(document.FilePath, document.Project.FilePath, sourceText.ToString())

namespace Microsoft.FSharp.Compiler.Parser