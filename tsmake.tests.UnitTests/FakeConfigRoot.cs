using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace tsmake.Tests.UnitTests
{
	[ExcludeFromCodeCoverage]
	public class FakeConfigRoot : IConfigurationRoot
	{
		private Dictionary<string, string> _overrides;
		private Dictionary<string, string> _defaults;

		public IConfigurationSection GetSection(string key)
		{
			throw new System.NotImplementedException();
		}

		public IEnumerable<IConfigurationSection> GetChildren()
		{
			throw new System.NotImplementedException();
		}

		public IChangeToken GetReloadToken()
		{
			throw new System.NotImplementedException();
		}

		public FakeConfigRoot(Dictionary<string, string> overrides)
		{
			this._overrides = overrides;

			this._defaults = new Dictionary<string, string>();
			this._defaults.Add("VersionScheme", "SemVer");
			this._defaults.Add("VersionCodeDate", "2017-01-12");
			this._defaults.Add("BuildMarkerTemplatePath", @"D:\Repositories\my-repo\some-path\template.md");
			this._defaults.Add("BuildOutputPath", @"D:\Repositories\my-repo\deployment\xxx_latest.sql");
			this._defaults.Add("BuildRoot", @"D:\Repositories\my-repo");
			this._defaults.Add("CopyrightText", "Copyright 2012+ by so and so... ");
			this._defaults.Add("ProjectInfoText", "https://www.somesite.com/latest");
		}

		public string this[string key]
		{
			get
			{
				if (_overrides.ContainsKey(key))
					return _overrides[key];

				return _defaults[key];
			}

			set => throw new System.NotImplementedException();
		}

		public void Reload()
		{
			throw new System.NotImplementedException();
		}

		public IEnumerable<IConfigurationProvider> Providers { get; }
	}
}