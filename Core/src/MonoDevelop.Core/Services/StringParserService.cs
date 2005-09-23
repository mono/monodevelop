// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.Services
{
	/// <summary>
	/// this class parses internal ${xyz} tags of sd.
	/// All environment variables are avaible under the name env.[NAME]
	/// where [NAME] represents the string under which it is avaiable in
	/// the environment.
	/// </summary>
	public class StringParserService : AbstractService
	{
		PropertyDictionary properties         = new PropertyDictionary();
		Hashtable          stringTagProviders = new Hashtable();
		
		public PropertyDictionary Properties {
			get {
				return properties;
			}
		}
		
		public StringParserService()
		{
			IDictionary variables = Environment.GetEnvironmentVariables();
			foreach (string name in variables.Keys) {
				properties.Add("env:" + name, (string)variables[name]);
			}
		}
		
		public string Parse(string input)
		{
			return Parse(input, null);
		}
		
		/// <summary>
		/// Parses an array and replaces the elements
		/// </summary>
		public void Parse(ref string[] inputs)
		{
			for (int i = inputs.GetLowerBound(0); i <= inputs.GetUpperBound(0); ++i) {
				inputs[i] = Parse(inputs[i], null);
			}
		}
		
		public void RegisterStringTagProvider(IStringTagProvider tagProvider)
		{
			foreach (string str in tagProvider.Tags) {
				stringTagProviders[str.ToUpper()] = tagProvider;
			}
		}
		
		string Replace (string[,] customTags, string propertyName)
		{
			string propertyValue = null;
			switch (propertyName.ToUpper()) {
				case "DATE": // current date
					propertyValue = DateTime.Today.ToShortDateString();
					break;
				case "TIME": // current time
					propertyValue = DateTime.Now.ToShortTimeString();
					break;
				default:
					propertyValue = null;
					if (customTags != null) {
						for (int j = 0; j < customTags.GetLength(0); ++j) {
							if (propertyName.ToUpper() == customTags[j, 0].ToUpper()) {
								propertyValue = customTags[j, 1];
								break;
							}
						}
					}
					
					if (propertyValue == null) {
						propertyValue = properties[propertyName.ToUpper()];
					}
					
					if (propertyValue == null) {
						IStringTagProvider stringTagProvider = stringTagProviders[propertyName.ToUpper()] as IStringTagProvider;
						if (stringTagProvider != null) {
							propertyValue = stringTagProvider.Convert(propertyName.ToUpper());
						}
					}
					
					if (propertyValue == null) {
						int k = propertyName.IndexOf(':');
						if (k > 0) {
							switch (propertyName.Substring(0, k).ToUpper()) {
								case "RES":
									throw new Exception ("This syntax is deprecated and needs to be removed from the offending consumer");
								case "PROPERTY":
									PropertyService propertyService = (PropertyService)ServiceManager.GetService(typeof(PropertyService));
									propertyValue = propertyService.GetProperty(propertyName.Substring(k + 1)).ToString();
									break;
							}
						}
					}
					break;
			}
			
			return propertyValue;
		}
			
		/// <summary>
		/// Expands ${xyz} style property values.
		/// </summary>
		public string Parse(string input, string [,] customTags)
		{
			StringBuilder sb = new StringBuilder (input.Length);
			for (int i = 0; i < input.Length; i++) {
				if (input [i] != '$') {
					sb.Append (input [i]);
					continue;
				}
				
				int start = i;
				
				if (++i >= input.Length)
					break;
				
				if (input [i] != '{') {
					sb.Append (input [i]);
					continue;
				}
				
				int end;
				for (end = ++i; end < input.Length; end++) {
					if (input [end] == '}')
						break;
				}
				
				string replacement;
				if (end == input.Length || (replacement = Replace (customTags, input.Substring (i, end - i))) == null) {
					sb.Append (input.Substring (start, end - start));
					break;
				}
				
				sb.Append (replacement);
				i = end;
			}
			
			sb.Replace (@"\&", "||!|");
			sb.Replace ("&", "_");
			sb.Replace ("||!|", "&");
			
			return sb.ToString ();
		}
	}
	
	public class PropertyDictionary : DictionaryBase
	{
		/// <summary>
		/// Maintains a list of the property names that are readonly.
		/// </summary>
		StringCollection readOnlyProperties = new StringCollection();
		
		/// <summary>
		/// Adds a property that cannot be changed.
		/// </summary>
		/// <remarks>
		/// Properties added with this method can never be changed.  Note that
		/// they are removed if the <c>Clear</c> method is called.
		/// </remarks>
		/// <param name="name">Name of property</param>
		/// <param name="value">Value of property</param>
		public void AddReadOnly(string name, string value) 
		{
			if (!readOnlyProperties.Contains(name)) {
				readOnlyProperties.Add(name);
				Dictionary.Add(name, value);
			}
		}
		
		/// <summary>
		/// Adds a property to the collection.
		/// </summary>
		/// <param name="name">Name of property</param>
		/// <param name="value">Value of property</param>
		public void Add(string name, string value) 
		{
			if (!readOnlyProperties.Contains(name)) {
				Dictionary.Add(name, value);
			}
		}
		
		public string this[string name] {
			get { 
				return (string)Dictionary[(object)name.ToUpper()];
			}
			set {
				Dictionary[name.ToUpper()] = value;
			}
		}
		
		protected override void OnClear() 
		{
			readOnlyProperties.Clear();
		}
	}
}
