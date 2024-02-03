using System.Text.Encodings.Web;
using System.Text.Json;
using NUnit.Framework;

namespace Json.Schema.Tests;

[SetUpFixture]
public class TestEnvironment
{
	public static readonly JsonSerializerOptions SerializerOptions =
		new()
		{
			TypeInfoResolverChain = { TestSerializerContext.Default },
		};

	public static readonly JsonSerializerOptions TestOutputSerializerOptions =
		new()
		{
			TypeInfoResolverChain = { TestSerializerContext.Default },
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

	public static readonly JsonSerializerOptions TestSuiteSerializationOptions =
		new()
		{
			TypeInfoResolverChain = { TestSerializerContext.Default },
			PropertyNameCaseInsensitive = true
		};


	[OneTimeSetUp]
	public void Setup()
	{
	}
}
