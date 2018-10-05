using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Ide.Gui;
using Microsoft.CodeAnalysis;
using System;

namespace MonoDevelop.Components.MainToolbar
{
	partial class RoslynSearchCategory
	{
		class DeclaredSymbolInfoResult : SearchResult
		{
			readonly INavigateToSearchResult result;

			public override SearchResultType SearchResultType { get { return SearchResultType.Type; } }

			public override string File {
				get { return result.NavigableItem.Document.FilePath; }
			}

			public override Xwt.Drawing.Image Icon {
				get {
					return ImageService.GetIcon (result.GetStockIconForNavigableItem (), IconSize.Menu);
				}
			}

			public override int Offset {
				get { return result.NavigableItem.SourceSpan.Start; }
			}

			public override int Length {
				get { return result.NavigableItem.SourceSpan.Length; }
			}

			public override string PlainText {
				get {
					return result.Name;
				}
			}

			public override string AccessibilityMessage {
				get {
					switch (result.Kind) {
					case NavigateToItemKind.Class:
						return GettextCatalog.GetString ("Class {0}", result.Name);

					case NavigateToItemKind.Delegate:
						return GettextCatalog.GetString ("Delegate {0}", result.Name);

					case NavigateToItemKind.Event:
						return GettextCatalog.GetString ("Event {0}", result.Name);

					case NavigateToItemKind.Enum:
						return GettextCatalog.GetString ("Enumeration {0}", result.Name);

					case NavigateToItemKind.Constant:
						return GettextCatalog.GetString ("Constant {0}", result.Name);

					case NavigateToItemKind.Field:
						return GettextCatalog.GetString ("Field {0}", result.Name);

					case NavigateToItemKind.EnumItem:
						return GettextCatalog.GetString ("Enumeration member {0}", result.Name);

					case NavigateToItemKind.Interface:
						return GettextCatalog.GetString ("Interface {0}", result.Name);

					case NavigateToItemKind.Method:
						return GettextCatalog.GetString ("Method {0}", result.Name);

					case NavigateToItemKind.Property:
						return GettextCatalog.GetString ("Property {0}", result.Name);

					case NavigateToItemKind.Structure:
						return GettextCatalog.GetString ("Structure {0}", result.Name);
					default:
						return result.Name;
					}


				}
			}


			public override Task<TooltipInformation> GetTooltipInformation (CancellationToken token)
			{
				return Task.Run (async () => {
					var document = result.NavigableItem.Document;
					var span = result.NavigableItem.SourceSpan;

					var root = await document.GetSyntaxRootAsync (token).ConfigureAwait (false);
					var node = root.FindNode (span);
					var semanticModel = await document.GetSemanticModelAsync (token).ConfigureAwait (false);
					var symbol = semanticModel.GetDeclaredSymbol (node, token);
					return await Ambience.GetTooltip (token, symbol);
				});
			}

			public override string Description {
				get {
					string loc = GettextCatalog.GetString ("file {0}", File);
					return result.GetDisplayStringForNavigableItem (loc);
				}
			}

			public override string GetMarkupText (bool selected)
			{
				// use tagged markers
				return HighlightMatch (result.Name, match, selected);
			}

			public DeclaredSymbolInfoResult (string match, string matchedString, int rank, INavigateToSearchResult result) : base (match, matchedString, rank)
			{
				this.result = result;
			}

			public override bool CanActivate {
				get {
					return result.NavigableItem.Document != null;
				}
			}

			public override async void Activate ()
			{
				var filePath = result.NavigableItem.Document.FilePath;
				var offset = result.NavigableItem.SourceSpan.Start;

				var proj = TypeSystemService.GetMonoProject (result.NavigableItem.Document.Project);
				if (proj?.ParentSolution != null) {
					string projectedName;
					int projectedOffset;
					if (TypeSystemService.GetWorkspace (proj.ParentSolution).TryGetOriginalFileFromProjection (filePath, offset, out projectedName, out projectedOffset)) {
						filePath = projectedName;
						offset = projectedOffset;
					}
				}

				await IdeApp.Workbench.OpenDocument (new FileOpenInformation (filePath, proj) {
					Offset = offset
				});
			}
		}
	}

	static class INavigateToSearchResultExtensions
	{
		static readonly IconId Class = "md-class";
		static readonly IconId Enum = "md-enum";
		static readonly IconId Event = "md-event";
		static readonly IconId Field = "md-field";
		static readonly IconId Interface = "md-interface";
		static readonly IconId Method = "md-method";
		static readonly IconId Property = "md-property";
		static readonly IconId Struct = "md-struct";
		static readonly IconId Delegate = "md-delegate";
		// static readonly IconId Constant = "md-literal";
		public static readonly IconId Namespace = "md-name-space";

		internal static IconId GetStockIconForNavigableItem (this INavigateToSearchResult item)
		{
			switch (item.Kind) {
			case NavigateToItemKind.Class:
				return Class;

			case NavigateToItemKind.Delegate:
				return Delegate;

			case NavigateToItemKind.Event:
				return Event;

			case NavigateToItemKind.Enum:
				return Enum;

			case NavigateToItemKind.Constant:
			case NavigateToItemKind.Field:
			case NavigateToItemKind.EnumItem:
				return Field;

			case NavigateToItemKind.Interface:
				return Interface;

			case NavigateToItemKind.Method:
			case NavigateToItemKind.Module:
				return Method;

			case NavigateToItemKind.Property:
				return Property;

			case NavigateToItemKind.Structure:
				return Struct;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		internal static string GetDisplayStringForNavigableItem (this INavigateToSearchResult item, string loc)
		{
			switch (item.Kind) {
			case NavigateToItemKind.Class:
				return GettextCatalog.GetString ("class ({0})", loc);

			case NavigateToItemKind.Delegate:
				return GettextCatalog.GetString ("delegate ({0})", loc);

			case NavigateToItemKind.Enum:
				return GettextCatalog.GetString ("enumeration ({0})", loc);

			case NavigateToItemKind.Event:
				return GettextCatalog.GetString ("event ({0})", loc);


			case NavigateToItemKind.Constant:
			case NavigateToItemKind.Field:
				return GettextCatalog.GetString ("field ({0})", loc);

			case NavigateToItemKind.EnumItem:
				return GettextCatalog.GetString ("enum member ({0})", loc);

			case NavigateToItemKind.Interface:
				return GettextCatalog.GetString ("interface ({0})", loc);

			case NavigateToItemKind.Method:
			case NavigateToItemKind.Module:
				return GettextCatalog.GetString ("method ({0})", loc);

			case NavigateToItemKind.Property:
				return GettextCatalog.GetString ("property ({0})", loc);

			case NavigateToItemKind.Structure:
				return GettextCatalog.GetString ("struct ({0})", loc);
			default:
				return GettextCatalog.GetString ("symbol ({0})", loc);
			}
		}
	}
}
