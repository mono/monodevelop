
using System;
using System.Collections.Specialized;
using System.Xml.Serialization;

namespace Stetic
{
	public class RecentFiles
	{
		[XmlArrayItem ("File")]
		public StringCollection Files = new StringCollection ();
	}
}
