using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor.Completion;
using NUnit.Framework;
using System;
using System.IO;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	[TestFixture]
	public abstract class SchemaTestFixtureBase
	{
		XmlSchemaCompletionData schemaCompletionData;

		/// <summary>
		/// Gets the <see cref="XmlSchemaCompletionData"/> object generated
		/// by this class.
		/// </summary>
		/// <remarks>This object will be null until the <see cref="FixtureInitBase"/>
		/// has been run.</remarks>
		public XmlSchemaCompletionData SchemaCompletionData {
			get {
				return schemaCompletionData;
			}
		}
		
		/// <summary>
		/// Creates the <see cref="XmlSchemaCompletionData"/> object from 
		/// the derived class's schema.
		/// </summary>
		/// <remarks>Calls <see cref="FixtureInit"/> at the end of the method.
		/// </remarks>
		[TestFixtureSetUp]
		public void FixtureInitBase()
		{
			schemaCompletionData = CreateSchemaCompletionDataObject();
			FixtureInit();
		}
		
		/// <summary>
		/// Method overridden by derived class so it can execute its own
		/// fixture initialisation.
		/// </summary>
		public virtual void FixtureInit()
		{
		}
	
		/// <summary>
		/// Checks whether the specified name exists in the completion data.
		/// </summary>
		public static bool Contains(CompletionDataList items, string name)
		{
			bool Contains = false;
			
			foreach (ICompletionData data in items) {
				if (data.DisplayText == name) {
					Contains = true;
					break;
				}
			}
				
			return Contains;
		}
		
		/// <summary>
		/// Checks whether the completion data specified by name has
		/// the correct description.
		/// </summary>
		public static bool ContainsDescription(CompletionDataList items, string name, string description)
		{
			bool Contains = false;
			
			foreach (ICompletionData data in items) {
				if (data.DisplayText == name) {
					if (data.Description == description) {
						Contains = true;
						break;						
					}
				}
			}
				
			return Contains;
		}		
		
		/// <summary>
		/// Gets a count of the number of occurrences of a particular name
		/// in the completion data.
		/// </summary>
		public static int GetItemCount(CompletionDataList items, string name)
		{
			int count = 0;
			
			foreach (ICompletionData data in items) {
				if (data.DisplayText == name) {
					++count;
				}
			}
			
			return count;
		}
		
		/// <summary>
		/// Returns the schema that will be used in this test fixture.
		/// </summary>
		/// <returns></returns>
		protected virtual string GetSchema()
		{
			return String.Empty;
		}
		
		/// <summary>
		/// Creates an <see cref="XmlSchemaCompletionData"/> object that 
		/// will be used in the test fixture.
		/// </summary>
		protected virtual XmlSchemaCompletionData CreateSchemaCompletionDataObject()
		{
			StringReader reader = new StringReader(GetSchema());
			return new XmlSchemaCompletionData(reader);
		}
	}
}
