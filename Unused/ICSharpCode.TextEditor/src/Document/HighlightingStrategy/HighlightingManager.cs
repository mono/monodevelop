// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

namespace MonoDevelop.TextEditor.Document
{
	public class HighlightingManager
	{
		ArrayList syntaxModeFileProviders = new ArrayList();
		static HighlightingManager highlightingManager;
		
		Hashtable highlightingDefs = new Hashtable();
		internal Hashtable extensionsToName = new Hashtable();
		
		public Hashtable HighlightingDefinitions {
			get {
				return highlightingDefs;
			}
		}
		
		public static HighlightingManager Manager {
			get {
				return highlightingManager;		
			}
		}
		
		static HighlightingManager()
		{
			highlightingManager = new HighlightingManager();
			highlightingManager.AddSyntaxModeFileProvider(new ResourceSyntaxModeProvider());
		}
		
		public HighlightingManager()
		{
			CreateDefaultHighlightingStrategy();
		}
		
		public void AddSyntaxModeFileProvider(ISyntaxModeFileProvider syntaxModeFileProvider)
		{
			foreach (SyntaxMode syntaxMode in syntaxModeFileProvider.SyntaxModes) {
				highlightingDefs[syntaxMode.Name] = new DictionaryEntry(syntaxMode, syntaxModeFileProvider);
				foreach (string extension in syntaxMode.Extensions) {
					extensionsToName[extension.ToUpper()] = syntaxMode.Name;
				}
			}
		}
		
		void CreateDefaultHighlightingStrategy()
		{
			DefaultHighlightingStrategy defaultHighlightingStrategy = new DefaultHighlightingStrategy();
			defaultHighlightingStrategy.Extensions = new string[] {};
			defaultHighlightingStrategy.Rules.Add(new HighlightRuleSet());
			highlightingDefs["Default"] = defaultHighlightingStrategy;
		}
		
		IHighlightingStrategy LoadDefinition(DictionaryEntry entry)
		{
			SyntaxMode              syntaxMode             = (SyntaxMode)entry.Key;
			ISyntaxModeFileProvider syntaxModeFileProvider = (ISyntaxModeFileProvider)entry.Value;
			
			DefaultHighlightingStrategy highlightingStrategy = HighlightingDefinitionParser.Parse(syntaxMode, syntaxModeFileProvider.GetSyntaxModeFile(syntaxMode));
			highlightingDefs[syntaxMode.Name] = highlightingStrategy;
			highlightingStrategy.ResolveReferences();
			
			return highlightingStrategy;
		}
		
		public IHighlightingStrategy FindHighlighter(string name)
		{
			object def = highlightingDefs[name];
			if (def is DictionaryEntry) {
				return LoadDefinition((DictionaryEntry)def);
			}
			return (IHighlightingStrategy)(def == null ? highlightingDefs["Default"] : def);
		}
		
		public IHighlightingStrategy FindHighlighterForFile(string fileName)
		{
			string highlighterName = (string)extensionsToName[Path.GetExtension(fileName).ToUpper()];
			if (highlighterName != null) {
				object def = highlightingDefs[highlighterName];
				if (def is DictionaryEntry) {
					return LoadDefinition((DictionaryEntry)def);
				}
				return (IHighlightingStrategy)(def == null ? highlightingDefs["Default"] : def);
			} else {
				return (IHighlightingStrategy)highlightingDefs["Default"];
			}
		}
	}
}
