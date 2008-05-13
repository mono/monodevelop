
using System;
using System.IO;

namespace Stetic
{
	public interface IResourceProvider
	{
		ResourceInfo[] GetResources ();
		Stream GetResourceStream (string resourceName);
		ResourceInfo AddResource (string fileName);
		void RemoveResource (string resourceName);
	}
	
	[Serializable]
	public class ResourceInfo
	{
		string name;
		string fileName;
		string mimeType;
		
		public ResourceInfo (string name, string fileName): this (name, fileName, null)
		{
		}
		
		public ResourceInfo (string name, string fileName, string mimeType)
		{
			this.name = name;
			this.fileName = fileName;
			this.mimeType = mimeType;
		}
		
		public string Name {
			get { return name; }
		}

		public string FileName {
			get { return fileName; }
		}
		
		public string MimeType {
			get { 
				if (mimeType == null) {
					if (File.Exists (fileName)) {
						mimeType = Gnome.Vfs.MimeType.GetMimeTypeForUri (fileName);
					} else {
						// Guess the mime type creating a temp file with the same extension
						string fn = Path.GetTempFileName ();
						string ext = Path.GetExtension (fileName);
						int n=0;
						while (File.Exists (fn + n + ext))
							n++;
						string tname = fn + n + ext;
						File.Move (fn, tname);
						mimeType = Gnome.Vfs.MimeType.GetMimeTypeForUri (tname);
						File.Delete (tname);
					}
					if (mimeType == null || mimeType == "")
						mimeType = "text";
				}
				return mimeType; 
			}
		}
	}
}
