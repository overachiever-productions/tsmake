
	using System;

	public class ConfigurationException : Exception
	{
	public ConfigurationException(string message) : base(message) { }
	public ConfigurationException(string message, Exception ex) : base(message, ex) { }
}
