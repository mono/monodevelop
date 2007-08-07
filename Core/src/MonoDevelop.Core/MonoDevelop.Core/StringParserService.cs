// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core
{
	/// <summary>
	/// this class parses internal ${xyz} tags of sd.
	/// All environment variables are avaible under the name env.[NAME]
	/// where [NAME] represents the string under which it is avaiable in
	/// the environment.
	/// </summary>
	public class StringParserService : AbstractService
	{
		Dictionary<string, string> properties = new Dictionary<string, string> ();
		Dictionary<string, IStringTagProvider> stringTagProviders = new Dictionary<string, IStringTagProvider> ();
		
		public Dictionary<string, string> Properties {
			get {
				return properties;
			}
		}
		
		public StringParserService()
		{
			System.Collections.IDictionary variables = Environment.GetEnvironmentVariables();
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
			switch (propertyName.ToUpper (CultureInfo.InvariantCulture)) {
				case "DATE": // current date
					propertyValue = DateTime.Today.ToShortDateString();
					break;
				case "YEAR": // current year
					propertyValue = DateTime.Today.Year.ToString ();
					break;
				case "MONTH": // current month
					propertyValue = DateTime.Today.Month.ToString ();
					break;
				case "DAY": // current day
					propertyValue = DateTime.Today.Day.ToString ();
					break;
				
				case "TIME": // current time
					propertyValue = DateTime.Now.ToShortTimeString();
					break;
				case "HOUR": // current hour
					propertyValue = DateTime.Now.Hour.ToString ();
					break;
				case "MINUTE": // current minute
					propertyValue = DateTime.Now.Minute.ToString ();
					break;
				case "SECOND": // current second
					propertyValue = DateTime.Now.Second.ToString ();
					break;
				
				case "USER": // current time
					propertyValue = Environment.UserName;
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
					
					if (propertyValue == null && properties.ContainsKey (propertyName.ToUpper())) {
						propertyValue = properties [propertyName.ToUpper()];
					}
					
					if (propertyValue == null) {
						IStringTagProvider stringTagProvider;
						if (stringTagProviders.TryGetValue (propertyName.ToUpper (), out stringTagProvider))
							propertyValue = stringTagProvider.Convert(propertyName.ToUpper());
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
					sb.Append ('$');
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
					sb.Append (input.Substring (start, end - start + 1));
					i = end;
					continue;
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
}
