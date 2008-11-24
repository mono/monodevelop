
using System;
using System.Xml;
using Stetic.Wrapper;

namespace Stetic
{
	public class ObjectWriter
	{
		XmlDocument doc;
		FileFormat format;
		bool createUndo;
		
		public ObjectWriter (XmlDocument doc, FileFormat format)
		{
			this.doc = doc;
			this.format = format;
		}
		
		public FileFormat Format {
			get { return format; }
		}
		
		public XmlDocument XmlDocument {
			get { return doc; }
		}
		
		public bool CreateUndoInfo {
			get { return createUndo; }
			set { createUndo = value; }
		}
		
		public virtual XmlElement WriteObject (ObjectWrapper wrapper)
		{
			return wrapper.Write (this);
		}
	}
}
