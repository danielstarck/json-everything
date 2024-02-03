using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Json.More;
using Json.Schema.CodeGeneration.Language;

namespace Json.Schema.CodeGeneration.Tests;

public static class AssertHelpers
{
	private static readonly JsonSerializerOptions _optionsWithReflection =
		new()
		{
			TypeInfoResolverChain = { new DefaultJsonTypeInfoResolver() }
		};

	public static string VerifyCSharp(JsonSchema schema, string expected, EvaluationOptions? options = null)
	{
		var code = schema.GenerateCode(CodeWriters.CSharp, options);

		Console.WriteLine(code);
		Assert.AreEqual(expected, code);

		return code;
	}

	public static void VerifyDeserialization(string code, string json, bool isReflectionAllowed = false)
	{
		var assembly = Compiler.Compile(code);
		Assert.NotNull(assembly, "Could not compile assembly");

		var targetType = assembly!.DefinedTypes.First();
		var model = JsonSerializer.Deserialize(json, targetType, isReflectionAllowed ? _optionsWithReflection : TestEnvironment.SerializerOptions);
		Assert.NotNull(model);

		var node = JsonNode.Parse(json);
		var returnToNode = JsonSerializer.SerializeToNode(model, isReflectionAllowed ? _optionsWithReflection : TestEnvironment.SerializerOptions);

		Console.WriteLine(returnToNode.AsJsonString(TestEnvironment.SerializerOptions));
		Assert.True(node.IsEquivalentTo(returnToNode));
	}

	public static void VerifyFailure(JsonSchema schema, EvaluationOptions? options = null)
	{
		var ex = Assert.Throws<UnsupportedSchemaException>(() =>
		{
			var actual = schema.GenerateCode(CodeWriters.CSharp, options);

			Console.WriteLine(actual);
		});

		Console.WriteLine(ex);
	}
}