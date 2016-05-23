namespace MonoDevelop.FSharp

open System
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler
open System.Globalization
open MonoDevelop.Core

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
                let res = text.Split '.' |> List.ofArray |> List.rev
                if lineStr.[col - 1] = '.' then res |> List.rev, ""
                else
                    match res with
                    | head :: tail -> tail |> List.rev, head
                    | [] -> [], ""
        | _ -> [], ""

    let findResidue (col, lineStr:string) =
        // scan backwards until we find the start of the current symbol
        let rec loop index =
            if index = 0 then
                0
            elif lineStr.[index - 1] = '.' || lineStr.[index - 1] = ' ' then
                index
            else
                loop (index - 1)

        let index = loop col
        lineStr.[index..]