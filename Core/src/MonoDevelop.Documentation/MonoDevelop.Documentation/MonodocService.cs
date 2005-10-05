using System;
using System.Xml;

using Monodoc;

using MonoDevelop.Core;

namespace MonoDevelop.Documentation
{

	public class MonodocService : AbstractService
	{

		RootTree helpTree;

		public override void InitializeService ()
		{
			helpTree = RootTree.LoadTree ();
		}
		
		public RootTree HelpTree {
			get { return helpTree; }
		}

		public XmlDocument GetHelpXml (string type) {
			return helpTree.GetHelpXml ("T:" + type);
		}
	}
}
