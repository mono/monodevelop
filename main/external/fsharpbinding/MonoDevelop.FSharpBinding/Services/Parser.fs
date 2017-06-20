namespace MonoDevelop.FSharp

open System

// --------------------------------------------------------------------------------------
/// Parsing utilities for IntelliSense (e.g. parse identifier on the left-hand side
/// of the current cursor location etc.)
module Parsing =
    let inline private tryGetLexerSymbolIslands sym =
        match sym.Text with "" -> None | _ -> Some (sym.RightColumn, sym.Text.Split '.' |> Array.toList)
        
    // Parsing - find the identifier around the current location
    // (we look for full identifier in the backward direction, but only
    // for a short identifier forward - this means that when you hover
    // 'B' in 'A.B.C', you will get intellisense for 'A.B' module)
    let findIdents col lineStr lookupType =
        if lineStr = "" then None
        else
            Lexer.getSymbol lineStr 0 col lineStr lookupType [||] Lexer.singleLineQueryLexState
            |> Option.bind tryGetLexerSymbolIslands
    
    let findLongIdentsAndResidue (col, lineStr:string) =
        let lineStr = lineStr.Substring(0, col)
    
        match Lexer.getSymbol lineStr 0 col lineStr SymbolLookupKind.ByLongIdent [||] Lexer.singleLineQueryLexState with
        | Some sym ->
            match sym.Text with
            | "" -> [], ""
            | text ->
                let res = text.Split '.' |> List.ofArray |> List.filter (String.isEmpty >> not) |> List.rev
                if lineStr.[col - 1] = '.' then res |> List.rev, ""
                else
                    match res with
                    | head :: tail -> tail |> List.rev, head
                    | [] -> [], ""
        | _ -> [], ""
