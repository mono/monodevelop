﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Configuration
open System.Diagnostics
open Microsoft.CodeAnalysis.Classification

[<RequireQualifiedAccess>]
module internal FSharpCommonConstants =
    [<Literal>]
    let packageGuidString = "871D2A70-12A2-4e42-9440-425DD92A4116"
    [<Literal>]
    let languageServiceGuidString = "BC6DD5A5-D4D6-4dab-A00D-A51242DBAF1B"
    [<Literal>]
    let editorFactoryGuidString = "4EB7CCB7-4336-4FFD-B12B-396E9FD079A9"
    [<Literal>]
    let svsSettingsPersistenceManagerGuidString = "9B164E40-C3A2-4363-9BC5-EB4039DEF653"
    [<Literal>]
    let FSharpLanguageName = "F#"
    [<Literal>]
    let FSharpContentTypeName = "F#"
    [<Literal>]
    let FSharpSignatureHelpContentTypeName = "F# Signature Help"
    [<Literal>]
    let FSharpLanguageServiceCallbackName = "F# Language Service"
    [<Literal>]
    let FSharpLanguageLongName = "FSharp"