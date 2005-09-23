

namespace BooBinding.Properties

import System

import Gtk
import Gdk
import Pango

import MonoDevelop.Gui.Widgets
import MonoDevelop.Core.Services
import MonoDevelop.Core.Properties
import MonoDevelop.Services

class ShellProperties:
	private propertyService = cast (PropertyService, ServiceManager.GetService (typeof(PropertyService)))
	private properties = cast (IProperties, propertyService.GetProperty (PropertyName, DefaultProperties()))

	public abstract PropertyName as string:
		get:
			pass

	public InternalProperties as IProperties:
		get:
			return properties
	
	FontName as string:
		get:
			return properties.GetProperty ("Font", "__default_monospace")
		set:
			properties.SetProperty ("Font", value)
	
	Font as FontDescription:
		get:
			if FontName == "__default_monospace":
				return FontDescription.FromString (GConf.Client ().Get ("/desktop/gnome/interface/monospace_font_name"))
			else:
				return FontDescription.FromString (FontName)

	
	AutoIndentBlocks as bool:
		get:
			return properties.GetProperty ("AutoIndentBlocks", true)
		set:
			properties.SetProperty ("AutoIndentBlocks", value)
	
	ResetClearsScrollback as bool:
		get:
			return properties.GetProperty ("ResetClearsScrollback", true)
		set:
			properties.SetProperty ("ResetClearsScrollback", value)

	ResetClearsHistory as bool:
		get:
			return properties.GetProperty ("ResetClearsHistory", true)
		set:
			properties.SetProperty ("ResetClearsHistory", value)

	LoadAssemblyAfterBuild as bool:
		get:
			return properties.GetProperty ("LoadAssemblyAfterBuild", true)
		set:
			properties.SetProperty ("LoadAssemblyAfterBuild", value)
