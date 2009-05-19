using System;
using System.Collections.Specialized; 
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.WebReferences
{
	/// <summary>Defines the properties and methods for the WebReferenceItemCollection class.</summary>
	public class WebReferenceItemCollection : NameObjectCollectionBase
	{
		#region Properties
		/// <summary>Get the Dictionary Entry for the specified index</summary>
		public WebReferenceItem this [int index]
		{
			get {return((WebReferenceItem) this.BaseGet(index));}
		}

		/// <summary>Get and set the TaskFieldObject for the specified key</summary>
		public WebReferenceItem this [string key]
		{
			get {return((WebReferenceItem) this.BaseGet(key));}
			set {this.BaseSet(key, value);}
		}

		/// <summary>Gets a String array that contains all the keys in the collection</summary>
		public string[] AllKeys  
		{
			get  {return(this.BaseGetAllKeys());}
		}

		/// <summary>Gets a value indicating if the collection contains keys that are not null.</summary>
		public bool HasKeys  
		{
			get {return(this.BaseHasKeys());}
		}
		#endregion

		/// <summary>Initializes a new instance of the WebReferenceItemCollection class</summary>
		public WebReferenceItemCollection() {}

		/// <summary>Initializes a new instance of the WebReferenceItemCollection class by specifying the project</summary>
		public WebReferenceItemCollection(Project project)
		{
			FilePath webRefPath = Library.GetWebReferencePath (project);
			foreach (ProjectFile file in project.Files)
			{
				if (file.FilePath.IsChildPathOf (webRefPath))
				{
					WebReferenceItem item;
					FileInfo fileInfo = new FileInfo(file.FilePath); 
					string refName = fileInfo.Directory.Name;
					
					// Add the item if it does not exist, otherwise get the current item
					if (Contains(refName))
						item = this[refName];
					else
					{
						item = new WebReferenceItem(refName);
						this.Add(item);
					}
					
					// Add the current project file to the web reference item
					if (fileInfo.Extension == ".map")
						item.MapFile = file;
					else
						item.ProxyFile = file;
				}
			}
		}
		
		/// <summary>Checks if the specified key already exists in the collection.</summary>
		/// <param name="proxyFile
		public bool Contains(string key)
		{
			for (int index = 0; index < this.AllKeys.Length; index ++)
				if (this.AllKeys[index] == key)
					return true;
			return false;
		}

		/// <summary>Adds an entry to the collection.</summary>
		/// <param name="key">A string containing the name of the key</param>
		/// <param name="value">A WebReferenceItem containing the value to be added to the collection</param>
		public void Add(string key, WebReferenceItem value)  
		{
			this.BaseAdd(key, value);
		}
		
		/// <summary>Adds an entry to the collection.</summary>
		/// <param name="value">A WebReferenceItem containing the value to be added to the collection</param>
		public void Add(WebReferenceItem value)
		{
			this.Add(value.Name, value);
		}

		/// <summary>Removes an entry with the specified key from the collection</summary>
		/// <param name="key">A string containing the name of the key</param>
		public void Remove(string key )  
		{
			this.BaseRemove(key);
		}

		/// <summary>Removes an entry with the specified key from the collection</summary>
		/// <param name="index">An int containing the index of the key</param>
		public void Remove(int index)  
		{
			this.BaseRemoveAt(index);
		}

		/// <summary>Clears all the elements in the collection</summary>
		public void Clear()  
		{
			this.BaseClear();
		}
	}
	
}
