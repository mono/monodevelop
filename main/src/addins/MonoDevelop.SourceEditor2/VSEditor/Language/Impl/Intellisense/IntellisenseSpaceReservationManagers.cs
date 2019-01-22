////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.SourceEditor
{
    internal class IntellisenseSpaceReservationManagers
    {
        [Export]
        [Name(CurrentLineSpaceReservationAgent.CurrentLineSRManagerName)]
        [Order(Before = IntellisenseSpaceReservationManagerNames.SmartTagSpaceReservationManagerName)]
        internal MDSpaceReservationManagerDefinition currentLineManager;

        [Export]
        [Name(IntellisenseSpaceReservationManagerNames.SmartTagSpaceReservationManagerName)]
        [Order(Before = IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName)]
        internal MDSpaceReservationManagerDefinition smartTagManager;

        [Export]
        [Name(IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName)]
        [Order(Before = IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName)]
        internal MDSpaceReservationManagerDefinition quickInfoManager;

        [Export]
        [Name(IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName)]
        [Order(Before = IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName)]
        internal MDSpaceReservationManagerDefinition signatureHelpManager;

        [Export]
        [Name(IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName)]
        [Order()]
        internal MDSpaceReservationManagerDefinition completionManager;
    }
}
