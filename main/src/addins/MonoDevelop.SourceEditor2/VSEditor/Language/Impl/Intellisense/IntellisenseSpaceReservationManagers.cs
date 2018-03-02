////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal class IntellisenseSpaceReservationManagers
    {
        [Export]
        [Name(CurrentLineSpaceReservationAgent.CurrentLineSRManagerName)]
        [Order(Before = IntellisenseSpaceReservationManagerNames.SmartTagSpaceReservationManagerName)]
        internal SpaceReservationManagerDefinition currentLineManager;

        [Export]
        [Name(IntellisenseSpaceReservationManagerNames.SmartTagSpaceReservationManagerName)]
        [Order(Before = IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName)]
        internal SpaceReservationManagerDefinition smartTagManager;

        [Export]
        [Name(IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName)]
        [Order(Before = IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName)]
        internal SpaceReservationManagerDefinition quickInfoManager;

        [Export]
        [Name(IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName)]
        [Order(Before = IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName)]
        internal SpaceReservationManagerDefinition signatureHelpManager;

        [Export]
        [Name(IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName)]
        [Order()]
        internal SpaceReservationManagerDefinition completionManager;
    }
}
