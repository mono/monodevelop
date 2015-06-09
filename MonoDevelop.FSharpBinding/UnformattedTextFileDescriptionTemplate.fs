namespace MonoDevelop.FSharp
open System
open System.Collections.Generic
open System.IO
open MonoDevelop.Ide.Templates
open MonoDevelop.Core
open MonoDevelop.Projects
open MonoDevelop.Ide.StandardHeader
open System.Text
open MonoDevelop.Ide.Gui.Content
open ExtCore

type UnformattedTextFileDescriptionTemplate() =
    inherit TextFileDescriptionTemplate()

    let getDefaultNs (potential:string) =
        let sb = StringBuilder potential.Length

        for c in potential do
            match c with
            | c when Char.IsLetter c || c = '_' || c = '.' ->
                sb.Append c |> ignore
            | c when Char.IsDigit c && sb.LastCharacterIs ((=) '.') || sb.Length = 0 ->
                sb.Append '_' |> ignore
                sb.Append c |> ignore
            | c when Char.IsDigit c ->
                sb.Append c |> ignore
            | _ ->
                sb.Append '_' |> ignore

        if sb.LastCharacterIs ((=) '.') then sb.Remove (sb.Length - 1, 1) |> ignore
        sb.ToString ()

    override x.ModifyTags (policyParent, project, language, identifier, fileName, tags) =
        base.ModifyTags (policyParent, project, language, identifier, fileName, &tags)

        let ns = project |> function null -> "Application" | project -> getDefaultNs project.Name
        tags.["Namespace"] <- ns

    override x.CreateFileContent(policyParent, project, language, fileName, identifier) =
        let tags = new Dictionary<_, _> ()
        x.ModifyTags (policyParent, project, language, identifier, fileName, ref tags)
                       
        let ms = new MemoryStream ()

        let bom = Encoding.UTF8.GetPreamble ()
        ms.Write (bom, 0, bom.Length)

        if x.AddStandardHeader then
            let header = StandardHeaderService.GetHeader (policyParent, fileName, true)
            let data = System.Text.Encoding.UTF8.GetBytes header
            ms.Write (data, 0, data.Length)

        let doc =
            let content = x.CreateContent (project, tags, language)
            let content = StringParserService.Parse (content, tags)
            new Mono.TextEditor.TextDocument (Text = content)
            
        let textPolicy =
            match policyParent with
            | null -> Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> "text/plain"
            | p -> p.Policies.Get<TextStylePolicy> "text/plain"
             
        let eolMarker = TextStylePolicy.GetEolMarker textPolicy.EolMarker
        let eolMarkerBytes = Encoding.UTF8.GetBytes eolMarker
                        
           
        for line in doc.Lines do
            let lineText =
                let lt = doc.GetTextAt (line.Offset, line.Length)
                if textPolicy.TabsToSpaces then
                    let tab = String.replicate textPolicy.TabWidth " "
                    lt.Replace ("\t", tab)
                else lt

            let data = Encoding.UTF8.GetBytes lineText
            ms.Write (data, 0, data.Length)
            ms.Write (eolMarkerBytes, 0, eolMarkerBytes.Length)
            
        ms.Position <- 0L
        ms :> _