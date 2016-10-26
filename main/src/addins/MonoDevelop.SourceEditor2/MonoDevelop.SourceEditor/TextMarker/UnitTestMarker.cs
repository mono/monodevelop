using System;
using MonoDevelop.Ide.Editor;
using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.SourceEditor.Wrappers;

namespace MonoDevelop.SourceEditor
{
	class UnitTestMarker : MarginMarker, IUnitTestMarker
	{
		IDocumentLine ITextLineMarker.Line {
			get {
				return LineSegment;
			}
		}

		readonly UnitTestMarkerHost host;
		readonly UnitTestLocation unitTest;
		readonly ExtensibleTextEditor textEditor;

		UnitTestLocation IUnitTestMarker.UnitTest {
			get {
				return unitTest;
			}
		}

		void IUnitTestMarker.UpdateState ()
		{
			var line = LineSegment;
			if (line == null)
				return;
			textEditor.RedrawMarginLine (textEditor.ActionMargin, line.LineNumber); 
		}

		public UnitTestMarker (ExtensibleTextEditor textEditor, UnitTestMarkerHost host, UnitTestLocation unitTest)
		{
			if (textEditor == null)
				throw new ArgumentNullException ("textEditor");
			if (host == null)
				throw new ArgumentNullException ("host");
			this.textEditor = textEditor;
			this.host = host;
			this.unitTest = unitTest;
		}

		public override bool CanDrawForeground (Margin margin)
		{
			return margin is ActionMargin;
		}

		public override void InformMouseHover (Mono.TextEditor.MonoTextEditor editor, Margin margin, MarginMouseEventArgs args)
		{
			if (!(margin is ActionMargin))
				return;
			string toolTip;
			if (unitTest.IsFixture) {
				if (isFailed) {
					toolTip = GettextCatalog.GetString ("NUnit Fixture failed (click to run)");
					if (!string.IsNullOrEmpty (failMessage))
						toolTip += Environment.NewLine + failMessage.TrimEnd ();
				} else {
					toolTip = GettextCatalog.GetString ("NUnit Fixture (click to run)");
				}
			} else {
				if (isFailed) {
					toolTip = GettextCatalog.GetString ("NUnit Test failed (click to run)");
					if (!string.IsNullOrEmpty (failMessage))
						toolTip += Environment.NewLine + failMessage.TrimEnd ();
					foreach (var id in unitTest.TestCases) {
						if (host.IsFailure (unitTest.UnitTestIdentifier, id)) {
							var msg = host.GetMessage (unitTest.UnitTestIdentifier, id);
							if (!string.IsNullOrEmpty (msg)) {
								toolTip += Environment.NewLine + "Test" + id + ":";
								toolTip += Environment.NewLine + msg.TrimEnd ();
							}
						}
					}
				} else {
					toolTip = GettextCatalog.GetString ("NUnit Test (click to run)");
				}

			}
			editor.TooltipText = toolTip;
		}

//		static Menu menu;

		public override void InformMousePress (Mono.TextEditor.MonoTextEditor editor, Margin margin, MarginMouseEventArgs args)
		{
			if (!(margin is ActionMargin))
				return;
			host.PopupContextMenu (unitTest, (int)(args.X + margin.XOffset), (int)args.Y);
			editor.TextArea.ResetMouseState (); 
		}

		bool isFailed;
		string failMessage;

		public override void DrawForeground (Mono.TextEditor.MonoTextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
		{
			isFailed = false;
			bool searchCases = false;

			Xwt.Drawing.Image icon = host.GetStatusIcon (unitTest.UnitTestIdentifier);
			if (icon != null) {
				if (host.HasResult (unitTest.UnitTestIdentifier)) {
					searchCases = true;
				} else if (host.IsFailure (unitTest.UnitTestIdentifier)) {
					failMessage = host.GetMessage (unitTest.UnitTestIdentifier);
					isFailed = true;
				}
			} else {
				searchCases = true;
			}

			if (searchCases) {
				foreach (var caseId in unitTest.TestCases) {
					icon = host.GetStatusIcon (unitTest.UnitTestIdentifier, caseId);
					if (host.IsFailure (unitTest.UnitTestIdentifier, caseId)) {
						failMessage = host.GetMessage (unitTest.UnitTestIdentifier, caseId);
						isFailed = true;
						break;
					} 
				}
			}

			if (icon != null) {
				if (icon.Width > metrics.Width || icon.Height > metrics.Height)
					icon = icon.WithBoxSize (metrics.Width, metrics.Height);
				cr.DrawImage (editor, icon, Math.Truncate (metrics.X + metrics.Width / 2 - icon.Width / 2), Math.Truncate (metrics.Y + metrics.Height / 2 - icon.Height / 2));
			}
		}
	}
}

