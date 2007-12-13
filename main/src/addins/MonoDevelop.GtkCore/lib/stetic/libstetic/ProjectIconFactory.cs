
using System;
using System.CodeDom;
using System.Xml;
using System.Collections;

namespace Stetic
{
	public class ProjectIconFactory
	{
		ProjectIconSetCollection icons = new ProjectIconSetCollection ();
		
		public ProjectIconFactory()
		{
		}
		
		public ProjectIconSetCollection Icons {
			get { return icons; }
		}
		
		public ProjectIconSet GetIcon (string name)
		{
			foreach (ProjectIconSet icon in icons)
				if (icon.Name == name)
					return icon;
			return null;
		}
		
		public XmlElement Write (XmlDocument doc)
		{
			XmlElement elem = doc.CreateElement ("icon-factory");
			foreach (ProjectIconSet icon in icons)
				elem.AppendChild (icon.Write (doc));
			return elem;
		}
		
		public void Read (IProject project, XmlElement elem)
		{
			icons.Clear ();
			foreach (XmlElement child in elem.SelectNodes ("icon-set")) {
				ProjectIconSet icon = new ProjectIconSet ();
				icon.Read (project, child);
				icons.Add (icon);
			}
		}
		
		public void GenerateBuildCode (GeneratorContext ctx)
		{
			string varName = ctx.NewId ();
			CodeVariableDeclarationStatement varDec = new CodeVariableDeclarationStatement (typeof(Gtk.IconFactory), varName);
			varDec.InitExpression = new CodeObjectCreateExpression (typeof(Gtk.IconFactory));
			ctx.Statements.Add (varDec);
			
			CodeVariableReferenceExpression var = new CodeVariableReferenceExpression (varName);
			foreach (ProjectIconSet icon in icons) {
				
				CodeExpression exp = new CodeMethodInvokeExpression (
					var,
					"Add",
					new CodePrimitiveExpression (icon.Name),
					icon.GenerateObjectBuild (ctx)
				);
				ctx.Statements.Add (exp);
			}
			
			CodeExpression addd = new CodeMethodInvokeExpression (
				var,
				"AddDefault"
			);
			ctx.Statements.Add (addd);
		}
		
		public Gdk.Pixbuf RenderIcon (IProject project, string name, Gtk.IconSize size)
		{
			ProjectIconSet icon = GetIcon (name);
			if (icon == null)
				return null;
				
			foreach (ProjectIconSource src in icon.Sources) {
				if (src.SizeWildcarded || src.Size == size)
					return src.Image.GetScaledImage (project, size);
			}
			
			return icon.Sources [0].Image.GetScaledImage (project, size);
		}
		
		public Gdk.Pixbuf RenderIcon (IProject project, string name, int size)
		{
			ProjectIconSet icon = GetIcon (name);
			if (icon == null)
				return null;
				
			return icon.Sources [0].Image.GetScaledImage (project, size, size);
		}
	}
	
	public class ProjectIconSet
	{
		ProjectIconSourceCollection sources = new ProjectIconSourceCollection ();
		string name;
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public ProjectIconSourceCollection Sources {
			get { return sources; }
		}
		
		public XmlElement Write (XmlDocument doc)
		{
			XmlElement elem = doc.CreateElement ("icon-set");
			elem.SetAttribute ("id", name);
			foreach (ProjectIconSource src in sources)
				elem.AppendChild (src.Write (doc));
			return elem;
		}
		
		public void Read (IProject project, XmlElement elem)
		{
			sources.Clear ();
			name = elem.GetAttribute ("id");
			if (name.Length == 0)
				throw new InvalidOperationException ("Name attribute not found");

			foreach (XmlElement child in elem.SelectNodes ("source")) {
				ProjectIconSource src = new ProjectIconSource ();
				src.Read (project, child);
				sources.Add (src);
			}
		}
		
		internal CodeExpression GenerateObjectBuild (GeneratorContext ctx)
		{
			string varName = ctx.NewId ();
			CodeVariableDeclarationStatement varDec = new CodeVariableDeclarationStatement (typeof(Gtk.IconSet), varName);
			ctx.Statements.Add (varDec);
			
			CodeVariableReferenceExpression var = new CodeVariableReferenceExpression (varName);
			
			if (sources.Count == 1 && sources[0].AllWildcarded) {
				varDec.InitExpression = new CodeObjectCreateExpression (
					typeof(Gtk.IconSet),
					sources[0].Image.ToCodeExpression (ctx)
				);
			} else {
				varDec.InitExpression = new CodeObjectCreateExpression (typeof(Gtk.IconSet));
				foreach (ProjectIconSource src in sources) {
					CodeExpression exp = new CodeMethodInvokeExpression (
						var,
						"AddSource",
						src.GenerateObjectBuild (ctx)
					);
					ctx.Statements.Add (exp);
				}
			}
			return var;
		}
	}
	
	public class ProjectIconSource: Gtk.IconSource
	{
		ImageInfo imageInfo;
		
		public ImageInfo Image {
			get { return imageInfo; }
			set { imageInfo = value; }
		}
		
		public bool AllWildcarded {
			get {
				return DirectionWildcarded && SizeWildcarded && StateWildcarded;
			}
			set {
				DirectionWildcarded = SizeWildcarded = StateWildcarded = true;
			}
		}
		
		public XmlElement Write (XmlDocument doc)
		{
			XmlElement elem = doc.CreateElement ("source");
			
			XmlElement prop = doc.CreateElement ("property");
			prop.SetAttribute ("name", "Image");
			prop.InnerText = imageInfo.ToString ();
			elem.AppendChild (prop);
			
			if (!SizeWildcarded) {
				prop = doc.CreateElement ("property");
				prop.SetAttribute ("name", "Size");
				prop.InnerText = Size.ToString ();
				elem.AppendChild (prop);
			}
			
			if (!StateWildcarded) {
				prop = doc.CreateElement ("property");
				prop.SetAttribute ("name", "State");
				prop.InnerText = State.ToString ();
				elem.AppendChild (prop);
			}
			
			if (!DirectionWildcarded) {
				prop = doc.CreateElement ("property");
				prop.SetAttribute ("name", "Direction");
				prop.InnerText = Direction.ToString ();
				elem.AppendChild (prop);
			}
			
			return elem;
		}
		
		public void Read (IProject project, XmlElement elem)
		{
			XmlElement prop = elem.SelectSingleNode ("property[@name='Image']") as XmlElement;
			if (prop != null)
				imageInfo = ImageInfo.FromString (prop.InnerText);

			prop = elem.SelectSingleNode ("property[@name='Size']") as XmlElement;
			if (prop != null && prop.InnerText != "*") {
				SizeWildcarded = false;
				Size = (Gtk.IconSize) Enum.Parse (typeof(Gtk.IconSize), prop.InnerText);
			} else
				SizeWildcarded = true;

			prop = elem.SelectSingleNode ("property[@name='State']") as XmlElement;
			if (prop != null && prop.InnerText != "*") {
				StateWildcarded = false;
				State = (Gtk.StateType) Enum.Parse (typeof(Gtk.StateType), prop.InnerText);
			} else
				StateWildcarded = true;

			prop = elem.SelectSingleNode ("property[@name='Direction']") as XmlElement;
			if (prop != null && prop.InnerText != "*") {
				DirectionWildcarded = false;
				Direction = (Gtk.TextDirection) Enum.Parse (typeof(Gtk.TextDirection), prop.InnerText);
			} else
				DirectionWildcarded = true;
		}
		
		internal CodeExpression GenerateObjectBuild (GeneratorContext ctx)
		{
			string varName = ctx.NewId ();
			CodeVariableDeclarationStatement varDec = new CodeVariableDeclarationStatement (typeof(Gtk.IconSource), varName);
			varDec.InitExpression = new CodeObjectCreateExpression (typeof(Gtk.IconSource));
			ctx.Statements.Add (varDec);
			
			CodeVariableReferenceExpression var = new CodeVariableReferenceExpression (varName);
			
			ctx.Statements.Add (new CodeAssignStatement (
				new CodePropertyReferenceExpression (var, "Pixbuf"),
				imageInfo.ToCodeExpression (ctx)
			));
			
			if (!SizeWildcarded) {
				ctx.Statements.Add (new CodeAssignStatement (
					new CodePropertyReferenceExpression (var, "SizeWildcarded"),
					new CodePrimitiveExpression (false)
				));
				ctx.Statements.Add (new CodeAssignStatement (
					new CodePropertyReferenceExpression (var, "Size"),
					new CodeFieldReferenceExpression (
						new CodeTypeReferenceExpression ("Gtk.IconSize"),
						Size.ToString ()
					)
				));
			}
			
			if (!StateWildcarded) {
				ctx.Statements.Add (new CodeAssignStatement (
					new CodePropertyReferenceExpression (var, "StateWildcarded"),
					new CodePrimitiveExpression (false)
				));
				ctx.Statements.Add (new CodeAssignStatement (
					new CodePropertyReferenceExpression (var, "State"),
					new CodeFieldReferenceExpression (
						new CodeTypeReferenceExpression ("Gtk.StateType"),
						State.ToString ()
					)
				));
			}
			
			if (!DirectionWildcarded) {
				ctx.Statements.Add (new CodeAssignStatement (
					new CodePropertyReferenceExpression (var, "DirectionWildcarded"),
					new CodePrimitiveExpression (false)
				));
				ctx.Statements.Add (new CodeAssignStatement (
					new CodePropertyReferenceExpression (var, "Direction"),
					new CodeFieldReferenceExpression (
						new CodeTypeReferenceExpression ("Gtk.TextDirection"),
						Direction.ToString ()
					)
				));
			}
			
			return var;
		}
	}
	
	public class ProjectIconSetCollection: CollectionBase
	{
		public ProjectIconSet this [int n] {
			get { return (ProjectIconSet) List [n]; }
		}
		
		public void Add (ProjectIconSet icon)
		{
			List.Add (icon);
		}
		
		public void Remove (ProjectIconSet icon)
		{
			List.Remove (icon);
		}
	}
	
	public class ProjectIconSourceCollection: CollectionBase
	{
		public void AddRange (ICollection c)
		{
			foreach (ProjectIconSource s in c)
				List.Add (s);
		}
		
		public ProjectIconSource this [int n] {
			get { return (ProjectIconSource) List [n]; }
		}
		
		public void Add (ProjectIconSource source)
		{
			List.Add (source);
		}
	}
}
