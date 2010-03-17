

namespace BooBinding.Properties

import System
import Pango
import MonoDevelop.Core

class ShellProperties:
	//FIXME: BOO COMPILER CAN'T RESOLVE OVERLOADS OF GENERIC METHODS
	//private properties = PropertyService.Get [of Properties] (PropertyName)
	private properties = PropertyService.Get [of Properties] (PropertyName, Properties())
	//END FIXME
	
	public abstract PropertyName as string:
		get:
			pass

	public InternalProperties as Properties:
		get:
			return properties
	
	FontName as string:
		get:
			return properties.Get [of string] ("Font", "__default_monospace")
		set:
			properties.Set ("Font", value)
	
	Font as FontDescription:
		get:
			if FontName == "__default_monospace":
				return FontDescription.FromString (GConf.Client ().Get ("/desktop/gnome/interface/monospace_font_name"))
			else:
				return FontDescription.FromString (FontName)

	
	AutoIndentBlocks as bool:
		get:
			return properties.Get [of bool] ("AutoIndentBlocks", true)
		set:
			properties.Set ("AutoIndentBlocks", value)
	
	ResetClearsScrollback as bool:
		get:
			return properties.Get [of bool] ("ResetClearsScrollback", true)
		set:
			properties.Set ("ResetClearsScrollback", value)

	ResetClearsHistory as bool:
		get:
			return properties.Get [of bool] ("ResetClearsHistory", true)
		set:
			properties.Set ("ResetClearsHistory", value)

	LoadAssemblyAfterBuild as bool:
		get:
			return properties.Get [of bool] ("LoadAssemblyAfterBuild", true)
		set:
			properties.Set ("LoadAssemblyAfterBuild", value)
