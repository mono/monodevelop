using System;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor
{
	[Obsolete ("Old editor")]
	public interface IUnitTestMarker : ITextLineMarker
	{
		UnitTestLocation UnitTest { get; }

		void UpdateState ();
	}

	[Obsolete ("Old editor")]
	public abstract class UnitTestMarkerHost
	{
		public abstract Xwt.Drawing.Image GetStatusIcon (string unitTestIdentifier, string caseId = null);
		public abstract bool IsFailure (string unitTestIdentifier, string caseId = null);
		public abstract string GetMessage (string unitTestIdentifier, string caseId = null);
		public abstract bool HasResult (string unitTestIdentifier, string caseId = null);

		public abstract void PopupContextMenu (UnitTestLocation unitTest, int x, int y);
	}

	[Obsolete ("Old editor")]
	public class UnitTestLocation
	{
		public int Offset { get; set; }
		public bool IsFixture { get; set; }
		public string UnitTestIdentifier { get; set; }
		public bool IsIgnored { get; set; }

		public List<string> TestCases = new List<string> ();

		public UnitTestLocation (int offset)
		{
			Offset = offset;
		}
	}
}

