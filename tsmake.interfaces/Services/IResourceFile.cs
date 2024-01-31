using System;
using System.Collections.Generic;

namespace tsmake.Interfaces.Services
{
	public interface IResourceFile
	{
		public string FullPath { get; }
		public string Fullname { get; }
		//public DateTime FileAdded { get; }

		public string FileContents { get; }  // I could use an IStream here (it's in an odd namespace) and Unit test against MemoryStreams: https://stackoverflow.com/a/30212374/11191  .. but, ultimately, we're just playing with strings.

		//public List<Exception> Exceptions { get; }
		//public void AddException(Exception added);
	}
}