
using System;
using System.Collections;
using System.Xml;

namespace Stetic
{
	public class ObjectReader
	{
		FileFormat format;
		IProject proj;
		internal ArrayList GladeChildStack = new ArrayList ();
		
		public ObjectReader (IProject proj, FileFormat format)
		{
			this.format = format;
			this.proj = proj;
		}
		
		public FileFormat Format {
			get { return format; }
		}
		
		public IProject Project {
			get { return proj; }
		}
		
		public virtual ObjectWrapper ReadObject (XmlElement elem, ObjectWrapper root)
		{
			return Stetic.ObjectWrapper.ReadObject (this, elem, root);
		}
		
		public virtual void ReadExistingObject (ObjectWrapper wrapper, XmlElement elem)
		{
			Stetic.ObjectWrapper.ReadExistingObject (this, elem, wrapper);
		}
	}
}
