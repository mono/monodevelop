namespace Sharpen
{
	using System;

	public interface FilenameFilter
	{
		bool Accept (FilePath dir, string name);
	}
}
