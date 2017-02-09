////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents the set of keyboard commands that can be issued to IntelliSense presenters.
    /// </summary>
    public enum IntellisenseKeyboardCommand
    {
        /// <summary>
        /// The up arrow command.
        /// </summary>
        Up,

        /// <summary>
        /// The down arrow command.
        /// </summary>
        Down,

        /// <summary>
        /// The page up command.
        /// </summary>
        PageUp,

        /// <summary>
        /// The page down command.
        /// </summary>
        PageDown,

        /// <summary>
        /// The go to the top line command
        /// </summary>
        TopLine,

        /// <summary>
        /// The go to the bottom line command.
        /// </summary>
        BottomLine,

        /// <summary>
        /// The home command.
        /// </summary>
        Home,

        /// <summary>
        /// The end command.
        /// </summary>
        End,

        /// <summary>
        /// The enter, or return, command.
        /// </summary>
        Enter,

        /// <summary>
        /// The escape command.
        /// </summary>
        Escape,

        /// <summary>
        /// The increase filter level command. 
        /// </summary>
        /// <remarks>
        ///  This command is most often used in tabbed completion to switch between the completion tabs.
        /// </remarks>
        IncreaseFilterLevel,

        /// <summary>
        /// The decrease filter level command.  
        /// </summary>
        /// <remarks>
        /// This command is most often used in tabbed completion to switch between the completion tabs.
        /// </remarks>
        DecreaseFilterLevel
    }
}
