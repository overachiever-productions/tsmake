using System;
using Moq;
using NUnit.Framework;
using tsmake.Interfaces.Configuration;
using tsmake.Interfaces.Core;
using tsmake.Pipeline;

namespace tsmake.Tests.UnitTests.Services.Pipeline
{
	[TestFixture]
	public class BuildVersionTests
	{
		//[Test]
		//public void Year_ThrowsOn_Invalid_Input()
		//{
		//	//	https://stackoverflow.com/questions/51536197/how-do-i-test-my-c-sharp-constructor-throws-an-exception-with-nunit-3
		//	//	@JohnMorsley The duplicates do show you how to do it, even though it's not obvious. Just use Assert.Throws<ArgumentOutOfRangeException>(() => new Year(-1)); – Andrew T Finnell Jul 26 '18 at 12:37
		//		  //public Year(int value)
		//		  //{
		//		  //	if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), "Cannot be less than 1.");
		//		  //	if (value > 9999) throw new ArgumentOutOfRangeException(nameof(value), "Cannot be greater than 9999.");
		//		  //}	
		//	Assert.Throws<ArgumentOutOfRangeException>(() => new Year(-1));
		//}

		[Test]
		public void BuildVersion_Ctor_Throws_On_Negative_Major()
		{
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new BuildVersion(VersionScheme.FourPart, -1, 3, string.Empty));

			StringAssert.Contains("less than 0", ex.Message);
			StringAssert.Contains("major", ex.Message);
		}

		[Test]
		public void BuildVersion_Ctor_Throws_On_Negative_Minor()
		{
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new BuildVersion(VersionScheme.FourPart, 2, -3, string.Empty));

			StringAssert.Contains("less than 0", ex.Message);
			StringAssert.Contains("minor", ex.Message);
		}
		
		[Test]
		public void BuildVersion_Ctor_Throws_On_versionSummary_IsNullOrEmpty()
		{
			var ex = Assert.Throws<ArgumentException>(() => new BuildVersion(VersionScheme.FourPart, 1, 0, string.Empty));

			StringAssert.Contains("null or empty", ex.Message);
			StringAssert.Contains("versionSummary", ex.Message);
		}

		[Test]
		public void BuildVersion_AssignsMajorInput_To_Major()
		{
			BuildVersion sut = new BuildVersion(VersionScheme.FourPart, 5, 3, "This is a summary");

			Assert.AreEqual(5, sut.Major);
		}

		[Test]
		public void BuildVersion_AssignsMinorInput_To_Minor()
		{
			BuildVersion sut = new BuildVersion(VersionScheme.SemVer, 5, 3, "This is a summary");

			Assert.AreEqual(3, sut.Minor);
		}

		[Test]
		public void BuildVersion_BuildNumber_Defers_To_Explicit_Build_Parameter()
		{
			BuildVersion sut = new BuildVersion(VersionScheme.FourPart, 5, 3, "This is a summary", 5);

			Assert.AreEqual(5, sut.Build);
		}

	}
}