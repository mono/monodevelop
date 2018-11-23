/* 
 * PropertyPad.cs: The pad that holds the MD property grid. Can also 
 * hold custom grid widgets.
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if MAC

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.PropertyEditing;
using Xamarin.PropertyEditing.Drawing;

namespace MonoDevelop.DesignerSupport
{
	class MockResourceProvider
		: IResourceProvider
	{
		public bool CanCreateResources => true;

		public Task<ResourceCreateError> CheckNameErrorsAsync (object target, ResourceSource source, string name)
		{
			ResourceCreateError error = null;

			if (resources.Any (s => s.ResourceSource == source && s.Resource.Any (j => j.Name == name))) {
				error = new ResourceCreateError ("Name in use", isWarning: false);
			} else {
				var order = new List<ResourceSourceType> {
					ResourceSourceType.Document,
					ResourceSourceType.ResourceDictionary,
					ResourceSourceType.Application,
					ResourceSourceType.System,
				};

				// Simplistic example of hierarchy override checking
				for (int i = order.IndexOf (source.Type) + 1; i < order.Count; i++) {
					if (resources.Any (ig => ig.ResourceSource.Type == order [i] && ig.ResourceSource.Name == name )) {
						error = new ResourceCreateError ("Resource would override another resource", isWarning: true);
						break;
					}
				}
			}

			return Task.FromResult (error);
		}

		public Task<Resource> CreateResourceAsync<T> (ResourceSource source, string name, T value)
		{
			var r = new Resource<T> (source, name, value);
			//((ObservableLookup<ResourceSource, Resource>)this.resources).Add (source, r);
			return Task.FromResult<Resource> (r);
		}

		public Task<IReadOnlyList<Resource>> GetResourcesAsync (object target, CancellationToken cancelToken)
		{
			var result = new List<Resource> ();
			foreach (var item in resources.Where (r => !(r.ResourceSource is ObjectResourceSource ors) || ReferenceEquals (target, ors.Target))
			.Select (s => s.Resource)) {
				result.AddRange (item);
			}
			return Task.FromResult<IReadOnlyList<Resource>> (result);
		}

		public Task<IReadOnlyList<Resource>> GetResourcesAsync (object target, IPropertyInfo property, CancellationToken cancelToken)
		{
			var result = new List<Resource> ();
			foreach (var item in resources.Where (r => property.Type.IsAssignableFrom (r.ResourceSource.GetType ().GetGenericArguments () [0]) && (!(r.ResourceSource is ObjectResourceSource ors) || ReferenceEquals (target, ors.Target)))
			.Select (s => s.Resource)) {
				result.AddRange (item);
			}
			return Task.FromResult<IReadOnlyList<Resource>> (result);


			//return Task.FromResult<IReadOnlyList<Resource>> (this.resources.SelectMany (g => g)
				//.Where (r => property.Type.IsAssignableFrom (r.GetType ().GetGenericArguments () [0]) && (!(r.Source is ObjectResourceSource ors) || ReferenceEquals (target, ors.Target)))
				//.ToList ());
		}

		Task<IReadOnlyList<ResourceSource>> IResourceProvider.GetResourceSourcesAsync (object target)
		{
			return GetResourceSourcesAsync (target);
		}

		public Task<IReadOnlyList<ResourceSource>> GetResourceSourcesAsync (object target, IPropertyInfo property)
		{
			return GetResourceSourcesAsync (target);
		}

		public static Task<IReadOnlyList<ResourceSource>> GetResourceSourcesAsync (object target)
		{
			return Task.FromResult<IReadOnlyList<ResourceSource>> (new [] { SystemResourcesSource, ApplicationResourcesSource, Resources, Window, new ObjectResourceSource (target, target.GetType ().Name, ResourceSourceType.Document) });
		}

		public Task<string> SuggestResourceNameAsync (IReadOnlyCollection<object> targets, IPropertyInfo property)
		{
			return SuggestResourceNameAsync (targets, property.RealType);
		}

		public Task<string> SuggestResourceNameAsync (IReadOnlyCollection<object> targets, ITypeInfo resourceType)
		{
			int i = 1;
			string key;
			do {
				key = resourceType.Name + i++;

			} while (resources.Any (s => s.ResourceSource == ApplicationResourcesSource && s.Resource.Any (j => j.Name == key)));

			return Task.FromResult (key);
		}

		private class ObjectResourceSource
			: ResourceSource
		{
			public ObjectResourceSource (object target, string name, ResourceSourceType type)
				: base (name, type)
			{
				if (target == null)
					throw new ArgumentNullException (nameof (target));

				this.target = target;
			}

			public object Target => this.target;

			public override int GetHashCode ()
			{
				int hashCode = base.GetHashCode ();
				unchecked {
					hashCode = (hashCode * 397) ^ this.target.GetHashCode ();
				}

				return hashCode;
			}

			public override bool Equals (ResourceSource other)
			{
				if (!base.Equals (other))
					return false;

				return (other is ObjectResourceSource ors && ReferenceEquals (ors.target, this.target));
			}

			private readonly object target;
		}

		internal static readonly ResourceSource SystemResourcesSource = new ResourceSource ("System Resources", ResourceSourceType.System);
		internal static readonly ResourceSource ApplicationResourcesSource = new ResourceSource ("App resources", ResourceSourceType.Application);
		static readonly ResourceSource Resources = new ResourceSource ("Resources.xaml", ResourceSourceType.ResourceDictionary);
		static readonly ResourceSource Window = new ResourceSource ("Window: <no name>", ResourceSourceType.Document);

		class LookUp
		{
			public LookUp (ResourceSource resourceSource, List<Resource> Resources)
			{
				ResourceSource = resourceSource;
				Resource = Resources;
			}

			public ResourceSource ResourceSource { get; set; }
			public List<Resource> Resource { get; set; }
		}

		readonly List<LookUp> resources = new List<LookUp> {
			new LookUp (SystemResourcesSource,
				new List<Resource> () {
						new Resource<CommonSolidBrush> (SystemResourcesSource, "ControlTextBrush", new CommonSolidBrush (0, 0, 0)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "HighlightBrush", new CommonSolidBrush (51, 153, 255)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "TransparentBrush", new CommonSolidBrush (0, 0, 0, 0)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "ATextBrush", new CommonSolidBrush (0, 0, 0)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "ATransparentBrush", new CommonSolidBrush (51, 153, 255)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "AHighlightBrush", new CommonSolidBrush (0, 0, 0, 0)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "BTextBrush", new CommonSolidBrush (0, 0, 0)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "BHighlightBrush", new CommonSolidBrush (51, 153, 255)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "BTransparentBrush", new CommonSolidBrush (0, 0, 0, 0)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "CTextBrush", new CommonSolidBrush (0, 0, 0)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "CHighlightBrush", new CommonSolidBrush (51, 153, 255)),
 						new Resource<CommonSolidBrush> (SystemResourcesSource, "CTransparentBrush", new CommonSolidBrush (0, 0, 0, 0)),
 						new Resource<CommonColor> (SystemResourcesSource, "ControlTextColor", new CommonColor (0, 0, 0)),
 						new Resource<CommonColor> (SystemResourcesSource, "HighlightColor", new CommonColor (51, 153, 255))
				})
		};
		};
	}

[Serializable]
public sealed class CommonSolidBrush : CommonBrush, IEquatable<CommonSolidBrush>
{
	public CommonSolidBrush (CommonColor color, string colorSpace = null, double opacity = 1.0)
		: base (opacity)
	{
		Color = color;
		ColorSpace = colorSpace;
	}

	public CommonSolidBrush (byte r, byte g, byte b, byte a = 255, string colorSpace = null, double opacity = 1.0)
		: this (new CommonColor (r, g, b, a), colorSpace, opacity) { }

	/// <summary>
	/// The color of the brush.
	/// </summary>
	public CommonColor Color { get; }

	/// <summary>
	/// The color space the brush is defined in.
	/// </summary>
	public string ColorSpace { get; }

	public override bool Equals (object obj)
	{
		var brush = obj as CommonSolidBrush;
		if (brush == null) return false;
		return Equals (brush);
	}

	public bool Equals (CommonSolidBrush other)
	{
		return other != null &&
			   Color.Equals (other.Color) &&
			   ColorSpace == other.ColorSpace &&
			   Opacity == other.Opacity;
	}

	public static bool operator == (CommonSolidBrush left, CommonSolidBrush right) => Equals (left, right);
	public static bool operator != (CommonSolidBrush left, CommonSolidBrush right) => !Equals (left, right);

	public override int GetHashCode ()
	{
		var hashCode = base.GetHashCode ();
		unchecked {
			hashCode = hashCode * -1521134295 + Color.GetHashCode ();
			if (ColorSpace != null)
				hashCode = hashCode * -1521134295 + ColorSpace.GetHashCode ();
		}
		return hashCode;
	}
}

#endif