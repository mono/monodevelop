//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// a focusable grid, with no focusvisualstyle and whenever it gets keyboard focus, attempts to move focus to the next focusable item.
    /// </summary>
    internal sealed class FocusMovingGrid : Grid
    {
        public FocusMovingGrid()
        {
            this.Focusable = true;
            this.FocusVisualStyle = null;
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if (e.NewFocus == this)
            {
                // HOT POTATO!  whenever this grid gets focus, try to immediately move to something else inside
                this.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }
}
