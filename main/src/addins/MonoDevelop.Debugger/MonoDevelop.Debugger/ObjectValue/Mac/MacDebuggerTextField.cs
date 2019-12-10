//
// MacCenteredTextFieldCell.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;

using AppKit;
using Foundation;

namespace MonoDevelop.Debugger
{
	class MacDebuggerTextField : NSTextField
	{
		MacDebuggerObjectCellViewBase cellView;
		string oldValue, newValue;
		bool editing;

		public MacDebuggerTextField (MacDebuggerObjectCellViewBase cellView)
		{
			this.cellView = cellView;
		}

		public override bool AcceptsFirstResponder ()
		{
			if (!base.AcceptsFirstResponder ())
				return false;

			// Note: The MacDebuggerObjectNameView sets the PlaceholderAttributedString property
			// so that it can control the font color and the baseline offset. Unfortunately, this
			// breaks once the NSTextField is in "edit" mode because the placeholder text ends up
			// being rendered as black instead of gray. By reverting to using the basic
			// PlaceholderString property once we enter "edit" mode, it fixes the text color.
			var placeholder = PlaceholderAttributedString;

			if (placeholder != null)
				PlaceholderString = placeholder.Value;

			TextColor = NSColor.ControlText;

			return true;
		}

		public override bool BecomeFirstResponder ()
		{
			if (cellView is MacDebuggerObjectNameView nameView) {
				nameView.Edit ();
				return true;
			}

			return base.BecomeFirstResponder ();
		}

		public override void DidBeginEditing (NSNotification notification)
		{
			base.DidBeginEditing (notification);
			cellView.TreeView.OnStartEditing ();
			oldValue = newValue = StringValue.Trim ();
			editing = true;
		}

		public override void DidChange (NSNotification notification)
		{
			newValue = StringValue.Trim ();
			base.DidChange (notification);
		}

		public override void DidEndEditing (NSNotification notification)
		{
			base.DidEndEditing (notification);

			if (!editing)
				return;

			editing = false;

			cellView.TreeView.OnEndEditing ();

			if (cellView is MacDebuggerObjectNameView) {
				if (cellView.Node is AddNewExpressionObjectValueNode) {
					if (newValue.Length > 0)
						cellView.TreeView.OnExpressionAdded (newValue);
				} else if (newValue != oldValue) {
					cellView.TreeView.OnExpressionEdited (cellView.Node, newValue);
				}
			} else if (cellView is MacDebuggerObjectValueView) {
				if (newValue != oldValue && cellView.TreeView.GetEditValue (cellView.Node, newValue)) {
					var metadata = new Dictionary<string, object> ();
					metadata["UIElementName"] = cellView.TreeView.UIElementName;
					metadata["ObjectValue.Type"] = cellView.Node.TypeName;

					Counters.EditedValue.Inc (1, null, metadata);
					cellView.Refresh ();
				}
			}

			oldValue = newValue = null;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
				cellView = null;

			base.Dispose (disposing);
		}
	}
}
