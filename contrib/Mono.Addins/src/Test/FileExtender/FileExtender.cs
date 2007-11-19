using FileContentExtension;
using Mono.Addins;

namespace FileExtender
{
	[Extension]
	class ExtraFileContent: IExtraFileContent
	{
		public string Content {
			get { return "extended content"; }
		}

	}
}
