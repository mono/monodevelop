//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace MonoDevelop.SourceEditor.Braces
{
	using System;
	using Microsoft.VisualStudio.Text.Editor;
	using MonoDevelop.Components;
	using MonoDevelop.Ide.Editor;
	using MonoDevelop.Ide.Editor.Highlighting;
	using Microsoft.VisualStudio.Text;

	/// <summary>
	/// A service for displaying an adornment under the inner most closing brace.
	/// </summary>
	internal class BraceCompletionAdornmentService : IBraceCompletionAdornmentService
	{
		#region Private Members

		private ITrackingPoint _trackingPoint;
		private ITextView _view;
		private TextEditor _textEditor;
		private IGenericTextSegmentMarker _marker;

		#endregion

		#region Constructors

		public BraceCompletionAdornmentService (ITextView textView)
		{
			if (textView == null)
				throw new ArgumentNullException ("textView");

			_view = textView;
		}

		#endregion

		#region IBraceCompletionAdornmentService

		public ITrackingPoint Point {
			get {
				return _trackingPoint;
			}

			set {
				if (_trackingPoint != null) {
					TextEditor.RemoveMarker (_marker);

					_trackingPoint.TextBuffer.Changed -= OnTextBufferChanged;
					_marker = null;
				}

				if (value != null) {
					value.TextBuffer.Changed += OnTextBufferChanged;

					int newPosition = value.GetPosition (value.TextBuffer.CurrentSnapshot) - 1;

					EditorTheme colorScheme = SyntaxHighlightingService.GetEditorTheme (MonoDevelop.Ide.IdeApp.Preferences.ColorScheme);
					HslColor color = SyntaxHighlightingService.GetColor (colorScheme, EditorThemeColors.Selection);

					_marker = TextMarkerFactory.CreateGenericTextSegmentMarker (TextEditor, TextSegmentMarkerEffect.Underline, color, newPosition, 1);
					TextEditor.AddMarker (_marker);
				}

				_trackingPoint = value;
			}
		}

		#endregion

		#region Private Helpers

		private void OnTextBufferChanged (object sender, TextContentChangedEventArgs e)
		{
			int newPosition = _trackingPoint.GetPosition (_trackingPoint.TextBuffer.CurrentSnapshot) - 1;

			if (newPosition != _marker.Offset) {
				TextEditor.RemoveMarker (_marker);

				_marker = TextMarkerFactory.CreateGenericTextSegmentMarker (TextEditor, TextSegmentMarkerEffect.Underline, _marker.Color, newPosition, 1);

				TextEditor.AddMarker (_marker);
			}
		}

		private TextEditor TextEditor {
			get {
				if (_textEditor == null) {
					_textEditor = _view.Properties.GetProperty<TextEditor> (typeof (TextEditor));
				}

				return _textEditor;
			}
		}

		#endregion
	}
}
