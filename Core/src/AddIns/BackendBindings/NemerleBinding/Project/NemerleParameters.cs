using System;
using System.Xml;
using System.Diagnostics;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;

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

		[ItemProperty("obcm")]
		public bool obcm = true;

		[ItemProperty("oocm")]
		public bool oocm = true;

		[ItemProperty("oscm")]
		public bool oscm = true;

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
		public bool Obcm
		{
			get { return obcm; }
			set { obcm = value; }
		}
		public bool Oocm
		{
			get { return oocm; }
			set { oocm = value; }
		}
		public bool Oscm
		{
			get { return oscm; }
			set { oscm = value; }
		}
		
		public string Parameters
		{
			get { return parameters; }
			set { parameters = value; }
		}
	}
}
