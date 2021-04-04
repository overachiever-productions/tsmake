
namespace tsmake.Interfaces.Services
{
	public interface IFileManager
	{
		//string LoadFileContents(string path);  
		IIncludedFile LoadIncludedFile(string path);

		// void WriteOutputCode(StringBuilder output);
		// void WriteMarkerFile(?)

		// string GetOutputPath? 
	}
}