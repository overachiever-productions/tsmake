using tsmake.Interfaces.Services;

namespace tsmake.FileManagement
{
	public class ResourceFile : IResourceFile
	{
		public string FullPath { get; }
		public string Fullname { get; }
		public string FileContents { get; }

		public ResourceFile(string path, string contents)
		{
			this.FullPath = path;
			this.FileContents = contents;

			// TODO: think my idea was that I'd be able to get the 'name' of the kind of code or whatever OUT of the file itself... doesn't seem like the greatest idea now that I'm in here looking at building this... 

		}
	}
}