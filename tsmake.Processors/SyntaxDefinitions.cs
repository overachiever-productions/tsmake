using System.Diagnostics.CodeAnalysis;using System.Security.Principal;
using System.Text.RegularExpressions;

[ExcludeFromCodeCoverage]
public static class COMMAND
{
	public static class OUTPUT
	{
		public const string Syntax = "--##OUTPUT:";
		public const string RegularExpression = @"--\s*##OUTPUT:.*\n{1}";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}

	public static class INCLUDE
	{
		public const string Syntax = "--##INCLUDE:";
		public const string RegularExpression = @"--\s*##INCLUDE:.*\n{1}";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}

	public static class CONDITIONAL_INCLUDE
	{
		public const string Syntax = "xx";
		public const string RegularExpression = @"";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}

	public static class NOTE
	{
		public const string Syntax = "--##NOTE";
		public const string RegularExpression = @"--\s*##NOTE:.*\n{1}";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}

	public static class CONDITIONAL_SUPPORT
	{
		public const string Syntax = "xx";
		public const string RegularExpression = @"";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}

	public static class CONDITIONAL_VERSION
	{
		public const string Syntax = "xx";
		public const string RegularExpression = @"";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}
}

[ExcludeFromCodeCoverage]
public static class TOKEN
{
	public static class COPYRIGHT
	{
		public const string Syntax = "{{##COPYRIGHT}}";
		public const string RegularExpression = @"\{\{##COPYRIGHT\}\}";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}

	public static class INFO
	{
		public const string Syntax = "{{##INFO}}";
		public const string RegularExpression = @"\{\{##INFO\}\}";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}

	public static class VERSION
	{
		public const string Syntax = "{{##VERSION}}";
		public const string RegularExpression = @"\{\{##VERSION\}\}";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}

	public static class VERSION_SUMMARY
	{
		public const string Syntax = "{{##SUMMARY}}";
		public const string RegularExpression = @"\{\{##SUMMARY\}\}";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}
}

[ExcludeFromCodeCoverage]
public static class OPERATOR
{
	public static class HEADER_COMMENT_REMOVER
	{
		public const string Syntax = "xx";
		public const string RegularExpression = @"";
		public const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
	}
}