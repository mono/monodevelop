using System;
using System.Xml;
using System.Diagnostics;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace NemerleBinding
{
	public class NemerleParameters: ICloneable
	{
		[ItemProperty("nostdmacros")]
		public bool nostdmacros = false;

		[ItemProperty("nostdlib")]
		public bool nostdlib = false;

		[ItemProperty("ot")]
		public bool ot = false;

		[ItemProperty("greedy")]
		public bool greedy = true;

		[ItemProperty("pedantic")]
		public bool pedantic = true;

		[ItemProperty("parameters")]
		public string parameters = String.Empty;
		
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		public bool Nostdmacros
		{
			get { return nostdmacros; }
			set { nostdmacros = value; }
		}
		public bool Nostdlib
		{
			get { return nostdlib; }
			set { nostdlib = value; }
		}
		public bool Ot
		{
			get { return ot; }
			set { ot = value; }
		}
		public bool Greedy
		{
			get { return greedy; }
			set { greedy = value; }
		}
		public bool Pedantic
		{
			get { return pedantic; }
			set { pedantic = value; }
		}
		
		public string Parameters
		{
			get { return parameters; }
			set { parameters = value; }
		}
	}
}
