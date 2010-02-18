// 
// Afrodite.cs
//  
// Author:
//       Levi Bard <levi.bard@emhartglass.com>
// 
// Copyright (c) 2010 Levi Bard
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using MonoDevelop.Core.Gui;

/// <summary>
/// Wrappers for Afrodite completion library
/// </summary>
namespace MonoDevelop.ValaBinding.Parser.Afrodite
{
	/// <summary>
	/// Afrodite completion engine - interface for queueing source and getting ASTs
	/// </summary>
	internal class CompletionEngine
	{
		public CompletionEngine (string id)
		{
			instance = afrodite_completion_engine_new (id);
		}
		
		/// <summary>
		/// Queue a new source file for parsing
		/// </summary>
		public void QueueSourcefile (string path)
		{
			QueueSourcefile (path, !string.IsNullOrEmpty (path) && path.EndsWith (".vapi", StringComparison.OrdinalIgnoreCase), false);
		}
		
		/// <summary>
		/// Queue a new source file for parsing
		/// </summary>
		public void QueueSourcefile (string path, bool isVapi, bool isGlib)
		{
			afrodite_completion_engine_queue_sourcefile (instance, path, null, isVapi, isGlib);
		}
		
		/// <summary>
		/// Attempt to acquire the current AST
		/// </summary>
		/// <returns>
		/// A <see cref="Ast"/>: null if unable to acquire
		/// </returns>
		public Ast TryAcquireAst ()
		{
			IntPtr astInstance = IntPtr.Zero;
			bool success = afrodite_completion_engine_try_acquire_ast (instance, out astInstance, 10);
			return (success)? new Ast (astInstance, this): null;
		}
		
		/// <summary>
		/// Release the given AST (required for continued parsing)
		/// </summary>
		public void ReleaseAst (Ast ast)
		{
			afrodite_completion_engine_release_ast (instance, ast.Instance);
		}
			
		#region P/Invokes
		
		IntPtr instance;
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_completion_engine_new (string id);
		
		[DllImport("afrodite")]
		static extern void afrodite_completion_engine_queue_sourcefile (IntPtr instance, string path, string content, 
		                                                                bool is_vapi, bool is_glib);
		                                                                
		[DllImport("afrodite")]
		static extern bool afrodite_completion_engine_try_acquire_ast (IntPtr instance, out IntPtr ast, int retry_count);
		
		[DllImport("afrodite")]
		static extern bool afrodite_completion_engine_release_ast (IntPtr instance, IntPtr ast);
		
		#endregion
	}
	
	/// <summary>
	/// Represents a Vala symbol
	/// </summary>
	internal class Symbol
	{
		public Symbol (IntPtr instance)
		{
			this.instance = instance;
		}
		
		/// <summary>
		/// Children of this symbol
		/// </summary>
		public List<Symbol> Children {
			get {
				List<Symbol> list = new List<Symbol> ();
				IntPtr children = afrodite_symbol_get_children (instance);
				
				if (IntPtr.Zero != children) {
					list = new ValaList (children).ToTypedList (delegate (IntPtr item){ return new Symbol (item); });
				}
				
				return list;
			}
		}
		
		/// <summary>
		/// The type of this symbol
		/// </summary>
		public DataType DataType {
			get { return new DataType (afrodite_symbol_get_symbol_type (instance)); }
		}
		
		/// <summary>
		/// The return type of this symbol, if applicable
		/// </summary>
		public DataType ReturnType {
			get{ return new DataType (afrodite_symbol_get_return_type (instance)); }
		}
		
		/// <summary>
		/// The name of this symbol
		/// </summary>
		public string Name {
			get{ return Marshal.PtrToStringAuto (afrodite_symbol_get_display_name (instance)); }
		}
		
		/// <summary>
		/// The fully qualified name of this symbol
		/// </summary>
		public string FullyQualifiedName {
			get { return Marshal.PtrToStringAuto (afrodite_symbol_get_fully_qualified_name (instance)); }
		}
		
		/// <summary>
		/// The parent of this symbol
		/// </summary>
		public Symbol Parent {
			get {
				IntPtr parent = afrodite_symbol_get_parent (instance);
				return (IntPtr.Zero == parent)? null: new Symbol (parent);
			}
		}
		
		/// <summary>
		/// The places where this symbol is declared/defined
		/// </summary>
		public List<SourceReference> SourceReferences {
			get {
				List<SourceReference> list = new List<SourceReference> ();
				IntPtr refs = afrodite_symbol_get_source_references (instance);
				
				if (IntPtr.Zero != refs) {
					list = new ValaList (refs).ToTypedList (delegate (IntPtr item){ return new SourceReference (item); });
				}
				
				return list;
			}
		}
		
		/// <summary>
		/// The symbol type (class, method, ...) of this symbol
		/// </summary>
		public string SymbolType {
			get{ return Marshal.PtrToStringAuto (afrodite_symbol_get_type_name (instance)); }
		}
		
		/// <summary>
		/// The accessibility (public, private, ...) of this symbol
		/// </summary>
		public SymbolAccessibility Accessibility {
			get{ return (SymbolAccessibility)afrodite_symbol_get_access (instance); }
		}
		
		/// <summary>
		/// The parameters this symbol accepts, if applicable
		/// </summary>
		public List<DataType> Parameters {
			get {
				List<DataType> list = new List<DataType> ();
				IntPtr parameters = afrodite_symbol_get_parameters (instance);
				
				if (IntPtr.Zero != parameters) {
					list = new ValaList (parameters).ToTypedList (delegate (IntPtr item){ return new DataType (item); });
				}
				
				return list;
			}
		}
		
		/// <summary>
		/// The icon to be used for this symbol
		/// </summary>
		public string Icon {
			get{ return GetIconForType (SymbolType, Accessibility); }
		}
		
		/// <summary>
		/// Descriptive text for this symbol
		/// </summary>
		public string DisplayText {
			get {
				StringBuilder text = new StringBuilder (Name);
				List<DataType> parameters = Parameters;
				if (0 < parameters.Count) {
					text.AppendFormat ("({0} {1}", parameters[0].TypeName, Parameters[0].Name);
					for (int i = 1; i < parameters.Count; i++) {
						text.AppendFormat (", {0} {1}", parameters[i].TypeName, Parameters[i].Name);
					}
					text.AppendFormat (")");
				}
				if (null != ReturnType && !string.IsNullOrEmpty (ReturnType.TypeName)) {
					text.AppendFormat (": {0}", ReturnType.TypeName);
				}
				
				return text.ToString ();
			}
		}
		
		#region Icons
		
		private static Dictionary<string,string> publicIcons = new Dictionary<string, string> () {
			{ "namespace", Stock.NameSpace },
			{ "class", Stock.Class },
			{ "struct", Stock.Struct },
			{ "enum", Stock.Enum },
			{ "errordomain", Stock.Enum },
			{ "field", Stock.Field },
			{ "method", Stock.Method },
			{ "constructor", Stock.Method },
			{ "creationmethod", Stock.Method },
			{ "property", Stock.Property },
			{ "constant", Stock.Literal },
			{ "enumvalue", Stock.Literal },
			{ "errorcode", Stock.Literal },
			{ "signal", Stock.Event },
			{ "delegate", Stock.Delegate },
			{ "interface", Stock.Interface },
			{ "other", Stock.Delegate }
		};

		private static Dictionary<string,string> privateIcons = new Dictionary<string, string> () {
			{ "namespace", Stock.NameSpace },
			{ "class", Stock.PrivateClass },
			{ "struct", Stock.PrivateStruct },
			{ "enum", Stock.PrivateEnum },
			{ "errordomain", Stock.PrivateEnum },
			{ "field", Stock.PrivateField },
			{ "method", Stock.PrivateMethod },
			{ "constructor", Stock.PrivateMethod },
			{ "creationmethod", Stock.PrivateMethod },
			{ "property", Stock.PrivateProperty },
			{ "constant", Stock.Literal },
			{ "enumvalue", Stock.Literal },
			{ "errorcode", Stock.Literal },
			{ "signal", Stock.PrivateEvent },
			{ "delegate", Stock.PrivateDelegate },
			{ "interface", Stock.PrivateInterface },
			{ "other", Stock.PrivateDelegate }
		};

		private static Dictionary<string,string> protectedIcons = new Dictionary<string, string> () {
			{ "namespace", Stock.NameSpace },
			{ "class", Stock.ProtectedClass },
			{ "struct", Stock.ProtectedStruct },
			{ "enum", Stock.ProtectedEnum },
			{ "errordomain", Stock.ProtectedEnum },
			{ "field", Stock.ProtectedField },
			{ "method", Stock.ProtectedMethod },
			{ "constructor", Stock.ProtectedMethod },
			{ "creationmethod", Stock.ProtectedMethod },
			{ "property", Stock.ProtectedProperty },
			{ "constant", Stock.Literal },
			{ "enumvalue", Stock.Literal },
			{ "errorcode", Stock.Literal },
			{ "signal", Stock.ProtectedEvent },
			{ "delegate", Stock.ProtectedDelegate },
			{ "interface", Stock.ProtectedInterface },
			{ "other", Stock.ProtectedDelegate }
		};

		private static Dictionary<SymbolAccessibility,Dictionary<string,string>> iconTable = new Dictionary<SymbolAccessibility, Dictionary<string, string>> () {
			{ SymbolAccessibility.Public, publicIcons },
			{ SymbolAccessibility.Internal, publicIcons },
			{ SymbolAccessibility.Private, privateIcons },
			{ SymbolAccessibility.Protected, protectedIcons }
		};

		public static string GetIconForType (string nodeType, SymbolAccessibility visibility)
		{
			string icon = null;
			iconTable[visibility].TryGetValue (nodeType.ToLower (), out icon);
			return icon;
		}

		#endregion
		
		#region P/Invokes
		
		IntPtr instance;
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_type_name (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_display_name (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_children (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_parent (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_fully_qualified_name (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_source_references (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern int afrodite_symbol_get_access (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_parameters (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_symbol_type (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_symbol_get_return_type (IntPtr instance);
		
		#endregion
	}
	
	/// <summary>
	/// Represents a Vala AST
	/// </summary>
	/// <remarks>
	/// MUST be disposed for parsing to continue
	/// </remarks>
	internal class Ast: IDisposable
	{
		CompletionEngine engine;
		
		/// <summary>
		/// Create a new AST wrapper
		/// </summary>
		/// <param name="instance">
		/// A <see cref="IntPtr"/>: The native pointer for this AST
		/// </param>
		/// <param name="engine">
		/// A <see cref="CompletionEngine"/>: The completion engine to which this AST belongs
		/// </param>
		public Ast (IntPtr instance, CompletionEngine engine)
		{
			this.instance = instance;
			this.engine = engine;
		}
		
		public QueryResult GetSymbolsForPath (string path)
		{
			return new QueryResult (afrodite_ast_get_symbols_for_path (instance, new QueryOptions ().Instance, path));
		}
		
		/// <summary>
		/// Lookup the symbol at a given location
		/// </summary>
		public Symbol LookupSymbolAt (string filename, int line, int column)
		{
			IntPtr symbol = afrodite_ast_lookup_symbol_at (instance, filename, line, column);
			return (IntPtr.Zero == symbol)? null: new Symbol (symbol);
		}
		
		/// <summary>
		/// Lookup a symbol and its parent by fully qualified name
		/// </summary>
		public Symbol Lookup (string fully_qualified_name, out Symbol parent)
		{
			IntPtr parentInstance = IntPtr.Zero,
			       result = IntPtr.Zero;
			
			result = afrodite_ast_lookup (instance, fully_qualified_name, out parentInstance);
			parent = (IntPtr.Zero == parentInstance)? null: new Symbol (parentInstance);
			return (IntPtr.Zero == result)? null: new Symbol (result);
		}
		
		/// <summary>
		/// Lookup a symbol, given a name and source location
		/// </summary>
		public Symbol GetSymbolForNameAndPath (string name, string path, int line, int column)
		{
			IntPtr result = afrodite_ast_get_symbol_for_name_and_path (instance, QueryOptions.Standard ().Instance,
			                                                           name, path, line, column);
			if (IntPtr.Zero != result) {
				QueryResult qresult = new QueryResult (result);
				if (null != qresult.Children && 0 < qresult.Children.Count)
					return qresult.Children[0].Symbol;
			}
			
			return null;
		}
		
		/// <summary>
		/// Get the source files used to create this AST
		/// </summary>
		public List<SourceFile> SourceFiles {
			get {
				List<SourceFile> files = new List<SourceFile> ();
				IntPtr sourceFiles = afrodite_ast_get_source_files (instance);
				
				if (IntPtr.Zero != sourceFiles) {
					ValaList list = new ValaList (sourceFiles);
					files = list.ToTypedList (delegate (IntPtr item){ return new SourceFile (item); });
				}
				
				return files;
			}
		}
		
		/// <summary>
		/// Lookup a source file by filename
		/// </summary>
		public SourceFile LookupSourceFile (string filename)
		{
			IntPtr sourceFile = afrodite_ast_lookup_source_file (instance, filename);
			return (IntPtr.Zero == sourceFile)? null: new SourceFile (sourceFile);
		}
		
		#region P/Invokes
		
		IntPtr instance;
		
		internal IntPtr Instance {
			get{ return instance; }
		}
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_ast_get_symbols_for_path (IntPtr instance, IntPtr options, string path);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_ast_lookup_symbol_at (IntPtr instance, string filename, int line, int column);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_ast_lookup (IntPtr instance, string fully_qualified_name, out IntPtr parent);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_ast_get_symbol_for_name_and_path (IntPtr instance, IntPtr options,
		                                                                string symbol_qualified_name, string path,
		                                                                int line, int column);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_ast_get_source_files (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_ast_lookup_source_file (IntPtr instance, string filename);
		
		#endregion
		
		#region IDisposable implementation
		
		/// <summary>
		/// Release this AST for reuse
		/// </summary>
		public void Dispose ()
		{
			engine.ReleaseAst (this);
		}
		
		#endregion
	}
	
	/// <summary>
	/// Utility class for dumping an AST to Console.Out
	/// </summary>
	internal class AstDumper
	{
		public AstDumper ()
		{
			instance = afrodite_ast_dumper_new ();
		}
		
		public void Dump (Ast ast, string filterSymbol)
		{
			afrodite_ast_dumper_dump (instance, ast.Instance, filterSymbol);
		}
		
		#region P/Invokes
		
		IntPtr instance;
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_ast_dumper_new ();
		
		[DllImport("afrodite")]
		static extern void afrodite_ast_dumper_dump (IntPtr instance, IntPtr ast, string filterSymbol);
		
		#endregion
	}
	
	/// <summary>
	/// Wrapper class for Afrodite query results
	/// </summary>
	internal class QueryResult
	{
		public QueryResult (IntPtr instance)
		{
			this.instance = instance;
		}
		
		/// <summary>
		/// ResultItems contained in this query result
		/// </summary>
		public List<ResultItem> Children {
			get {
				List<ResultItem> list = new List<ResultItem> ();
				IntPtr children = afrodite_query_result_get_children (instance);
				
				if (IntPtr.Zero != children) {
					list = new ValaList (children).ToTypedList (delegate (IntPtr item){ return new ResultItem (item); });
				}
				
				return list;
			}
		}
		
		#region P/Invokes
		
		IntPtr instance;
		
		internal IntPtr Instance {
			get{ return instance; }
		}
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_query_result_get_children (IntPtr instance);
		
		#endregion
	}
	
	/// <summary>
	/// A single result from a query
	/// </summary>
	internal class ResultItem
	{
		public ResultItem (IntPtr instance)
		{
			this.instance = instance;
		}
		
		public Symbol Symbol {
			get {
				IntPtr symbol = afrodite_result_item_get_symbol (instance);
				return (IntPtr.Zero == symbol)? null: new Symbol (symbol);
			}
		}
		
		#region P/Invokes
		
		IntPtr instance;
		
		internal IntPtr Instance {
			get{ return instance; }
		}
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_result_item_get_symbol (IntPtr instance);
		
		#endregion
	}
	
	/// <summary>
	/// Options for querying an AST
	/// </summary>
	internal class QueryOptions
	{
		public QueryOptions (): this (afrodite_query_options_new ())
		{
		}
		
		public QueryOptions (IntPtr instance)
		{
			this.instance = instance;
		}
		
		public static QueryOptions Standard ()
		{
			return new QueryOptions (afrodite_query_options_standard ());
		}
		
		#region P/Invokes
		
		IntPtr instance;
		
		internal IntPtr Instance {
			get{ return instance; }
		}
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_query_options_new ();
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_query_options_standard ();
		
		#endregion
	}
	
	/// <summary>
	/// IEnumerator wrapper for (Gee|Vala).Iterator
	/// </summary>
	internal class ValaEnumerator: IEnumerator<IntPtr>
	{
		public ValaEnumerator (IntPtr instance)
		{
			this.instance = instance;
		}
		
		#region IDisposable implementation

		public void Dispose ()
		{
		}
		
		#endregion
		
		#region IEnumerator implementation
		
		object IEnumerator.Current {
			get { return ((IEnumerator<IntPtr>)this).Current; }
		}
		
		
		public bool MoveNext ()
		{
			return vala_iterator_next (instance);
		}
		
		
		public void Reset ()
		{
			throw new System.NotImplementedException();
		}
		
		#endregion
		
		#region IEnumerator[System.IntPtr] implementation
		
		IntPtr IEnumerator<IntPtr>.Current {
			get { return vala_iterator_get (instance); }
		}
		
		#endregion
		                                              
		#region P/Invoke
		                                              
		IntPtr instance;
		                                              
		[DllImport("vala")]
		static extern bool vala_iterator_next (IntPtr instance);
		
		[DllImport("vala")]
		static extern IntPtr vala_iterator_get (IntPtr instance);
		
		#endregion
	}
	
	/// <summary>
	/// IList wrapper for (Gee|Vala).List
	/// </summary>
	internal class ValaList: IList<IntPtr>
	{
		public ValaList (IntPtr instance)
		{
			this.instance = instance;
		}
		
		#region ICollection[System.IntPtr] implementation
		
		public void Add (IntPtr item)
		{
			vala_collection_add (instance, item);
		}
		
		
		public void Clear ()
		{
			vala_collection_clear (instance);
		}
		
		
		public bool Contains (IntPtr item)
		{
			return vala_collection_contains (instance, item);
		}
		
		
		public void CopyTo (IntPtr[] array, int arrayIndex)
		{
			if (Count < array.Length - arrayIndex)
				throw new ArgumentException ("Destination array too small", "array");
			for (int i=0; i<Count; ++i)
				array[i+arrayIndex] = this[i];
		}
		
		
		public int Count {
			get { 
				return vala_collection_get_size (instance); 
			}
		}
		
		
		public bool IsReadOnly {
			get { return false; }
		}
		
		
		public bool Remove (IntPtr item)
		{
			return vala_collection_remove (instance, item);
		}
		
		#endregion
		
		#region IEnumerable implementation
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable<IntPtr>)this).GetEnumerator ();
		}
		
		#endregion
		
		#region IList[System.IntPtr] implementation
		
		public int IndexOf (IntPtr item)
		{
			return vala_list_index_of (instance, item);
		}
		
		
		public void Insert (int index, IntPtr item)
		{
			vala_list_insert (instance, index, item);
		}
		
		
		public IntPtr this[int index] {
			get { return vala_list_get (instance, index); }
			set { vala_list_set (instance, index, value); }
		}
		
		
		public void RemoveAt (int index)
		{
			vala_list_remove_at (instance, index);
		}
		
		#endregion
		
		#region IEnumerable[System.IntPtr] implementation
		IEnumerator<IntPtr> IEnumerable<IntPtr>.GetEnumerator ()
		{
			return new ValaEnumerator (vala_iterable_iterator (instance));
		}
		
		#endregion
		
		internal List<T> ToTypedList<T> (Func<IntPtr,T> factory)
		{
			List<T> list = new List<T> (Count);
			foreach (IntPtr item in this) {
				list.Add (factory (item));
			}
			return list;
		}

		#region P/Invoke
		
		IntPtr instance;

		[DllImport("vala")]
		static extern bool vala_collection_add (IntPtr instance, IntPtr item);
		
		[DllImport("vala")]
		static extern void vala_collection_clear (IntPtr instance);
		
		[DllImport("vala")]
		static extern bool vala_collection_contains (IntPtr instance, IntPtr item);
		
		[DllImport("vala")]
		static extern int vala_collection_get_size (IntPtr instance);
		
		[DllImport("vala")]
		static extern bool vala_collection_remove (IntPtr instance, IntPtr item);
		
		[DllImport("vala")]
		static extern IntPtr vala_iterable_iterator (IntPtr instance);
		
		[DllImport("vala")]
		static extern int vala_list_index_of (IntPtr instance, IntPtr item);
		
		[DllImport("vala")]
		static extern void vala_list_insert (IntPtr instance, int index, IntPtr item);
		
		[DllImport("vala")]
		static extern IntPtr vala_list_get (IntPtr instance, int index);
		
		[DllImport("vala")]
		static extern void vala_list_set (IntPtr instance, int index, IntPtr item);
		
		[DllImport("vala")]
		static extern void vala_list_remove_at (IntPtr instance, int index);
		
		#endregion
	}
	
	/// <summary>
	/// Class to represent an AST source file
	/// </summary>
	internal class SourceFile
	{
		public SourceFile (string filename)
		      :this (afrodite_source_file_new (filename))
		{
		}
		
		public SourceFile (IntPtr instance)
		{
			this.instance = instance;
		}
		
		/// <summary>
		/// Symbols declared in this source file
		/// </summary>
		public List<Symbol> Symbols {
			get {
				List<Symbol> list = new List<Symbol> ();
				IntPtr symbols = afrodite_source_file_get_symbols (instance);
				
				if (IntPtr.Zero != symbols) {
					list = new ValaList (symbols).ToTypedList (delegate (IntPtr item){ return new Symbol (item); });
				}
				
				return list;
			}
		}
		
		/// <summary>
		/// Using directives in this source file
		/// </summary>
		public List<Symbol> UsingDirectives {
			get {
				List<Symbol> list = new List<Symbol> ();
				IntPtr symbols = afrodite_source_file_get_using_directives (instance);
				
				if (IntPtr.Zero != symbols) {
					list = new ValaList (symbols).ToTypedList (delegate (IntPtr item){ return new Symbol (item); });
				}
				
				return list;
			}
		}
		
		/// <summary>
		/// The name of this source file
		/// </summary>
		public string Name {
			get{ return Marshal.PtrToStringAuto (afrodite_source_file_get_filename (instance)); }
		}
		
		#region P/Invoke
		                                              
		IntPtr instance;
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_source_file_new (string filename);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_source_file_get_filename (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_source_file_get_symbols (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_source_file_get_using_directives (IntPtr instance);
		
		
		#endregion
	}
	
	/// <summary>
	/// Represents an Afrodite symbol data type
	/// </summary>
	internal class DataType
	{
		public DataType (IntPtr instance)
		{
			this.instance = instance;
		}
		
		/// <summary>
		/// Get the raw name of this datatype
		/// </summary>
		public string Name {
			get{ return Marshal.PtrToStringAuto (afrodite_data_type_get_name (instance)); }
		}
		
		/// <summary>
		/// Get the descriptive type name (ref Gee.List<string>[]?) for this datatype
		/// </summary>
		public string TypeName {
			get {
				StringBuilder text = new StringBuilder ();
				
				// prefix out/ref
				if (IsOut) {
					text.Append ("out ");
				} else if (IsRef) {
					text.Append ("ref ");
				}
				
				text.Append (Marshal.PtrToStringAuto (afrodite_data_type_get_type_name (instance)));
				
				if (IsGeneric) {
					text.Append ("<");
					List<DataType> parameters = GenericTypes;
					text.Append (parameters[0].TypeName);
					for (int i = 0; i < parameters.Count; i++) {
						text.AppendFormat (",{0}", parameters[i].TypeName);
					}
					text.Append (">");
				}
				
				if (IsArray) { text.Append ("[]"); }
				if (IsNullable){ text.Append ("?"); }
				if (IsPointer){ text.Append ("*"); }
				
				return text.ToString ();
			}
		}
		
		/// <summary>
		/// Get the symbol for this datatype
		/// </summary>
		public Symbol Symbol {
			get {
				IntPtr symbol = afrodite_data_type_get_symbol (instance);
				return (IntPtr.Zero == symbol)? null: new Symbol (symbol);
			}
		}
		
		/// <summary>
		/// Whether this datatype is an array
		/// </summary>
		public bool IsArray {
			get{ return afrodite_data_type_get_is_array (instance); }
		}
		
		/// <summary>
		/// Whether this datatype is a pointer
		/// </summary>
		public bool IsPointer {
			get{ return afrodite_data_type_get_is_pointer (instance); }
		}
		
		/// <summary>
		/// Whether this datatype is nullable
		/// </summary>
		public bool IsNullable {
			get{ return afrodite_data_type_get_is_nullable (instance); }
		}
		
		/// <summary>
		/// Whether this is an out datatype
		/// </summary>
		public bool IsOut {
			get{ return afrodite_data_type_get_is_out (instance); }
		}
		
		/// <summary>
		/// Whether this is a ref datatype
		/// </summary>
		public bool IsRef {
			get{ return afrodite_data_type_get_is_ref (instance); }
		}
		
		/// <summary>
		/// Whether this datatype is generic
		/// </summary>
		public bool IsGeneric {
			get{ return afrodite_data_type_get_is_generic (instance); }
		}
		
		/// <summary>
		/// Type list for generic datatypes (e.g. HashMap<KeyType,ValueType>)
		/// </summary>
		public List<DataType> GenericTypes {
			get {
				List<DataType> list = new List<DataType> ();
				IntPtr types = afrodite_data_type_get_generic_types (instance);
				
				if (IntPtr.Zero != types) {
					list = new ValaList (types).ToTypedList (delegate (IntPtr item){ return new DataType (item); });
				}
				
				return list;
			}
		}
		
		#region P/Invoke
		                                              
		IntPtr instance;
		                                              
		[DllImport("afrodite")]
		static extern IntPtr afrodite_data_type_get_type_name (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_data_type_get_name (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_data_type_get_symbol (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_data_type_get_generic_types (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern bool afrodite_data_type_get_is_array (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern bool afrodite_data_type_get_is_pointer (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern bool afrodite_data_type_get_is_nullable (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern bool afrodite_data_type_get_is_out (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern bool afrodite_data_type_get_is_ref (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern bool afrodite_data_type_get_is_generic (IntPtr instance);
		
		
		#endregion
	}
	
	/// <summary>
	/// Class to represent a reference area in a source file
	/// </summary>
	internal class SourceReference
	{
		public SourceReference (IntPtr instance)
		{
			this.instance = instance;
		}
		
		public string File {
			get { 
				IntPtr sourcefile = afrodite_source_reference_get_file (instance);
				return (IntPtr.Zero == sourcefile)? string.Empty: new SourceFile (sourcefile).Name;
			}
		}
		
		public int FirstLine {
			get{ return afrodite_source_reference_get_first_line (instance); }
		}
		
		public int LastLine {
			get{ return afrodite_source_reference_get_last_line (instance); }
		}
		
		public int FirstColumn {
			get{ return afrodite_source_reference_get_first_column (instance); }
		}
		
		public int LastColumn {
			get{ return afrodite_source_reference_get_last_column (instance); }
		}
		
		#region P/Invoke
		                                              
		IntPtr instance;
		                                              
		[DllImport("afrodite")]
		static extern IntPtr afrodite_source_reference_get_file (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern int afrodite_source_reference_get_first_line (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern int afrodite_source_reference_get_last_line (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern int afrodite_source_reference_get_first_column (IntPtr instance);
		
		[DllImport("afrodite")]
		static extern int afrodite_source_reference_get_last_column (IntPtr instance);
		
		
		#endregion
	}
	
	// From afrodite.vapi
	public enum SymbolAccessibility {
		Private = 0x1,
		Internal = 0x2,
		Protected = 0x4,
		Public = 0x8,
		Any = 0x10
	}
	
	/// <summary>
	/// Wrapper class for Afrodite.Utils namespace
	/// </summary>
	internal static class Utils
	{
		/// <summary>
		/// Get a list of vapi files for a given package
		/// </summary>
		public static List<string> GetPackagePaths (string package)
		{
			List<string> list = new List<string> ();
			IntPtr paths = afrodite_utils_get_package_paths (package, IntPtr.Zero, null);
			if (IntPtr.Zero != paths)
				list = new ValaList (paths).ToTypedList (delegate(IntPtr item){ return Marshal.PtrToStringAuto (item); });
				
			return list;
		}
		
		[DllImport("afrodite")]
		static extern IntPtr afrodite_utils_get_package_paths (string package, IntPtr codeContext, string[] vapiDirs);
		
	}
}

