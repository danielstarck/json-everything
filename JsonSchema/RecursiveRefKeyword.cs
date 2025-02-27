﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `$recursiveRef`.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[Vocabulary(Vocabularies.Core201909Id)]
[JsonConverter(typeof(RecursiveRefKeywordJsonConverter))]
public class RecursiveRefKeyword : IJsonSchemaKeyword
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "$recursiveRef";

	/// <summary>
	/// The URI reference.
	/// </summary>
	public Uri Reference { get; }

	/// <summary>
	/// Creates a new <see cref="RecursiveRefKeyword"/>.
	/// </summary>
	/// <param name="value">The URI.</param>
	public RecursiveRefKeyword(Uri value)
	{
		Reference = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Builds a constraint object for a keyword.
	/// </summary>
	/// <param name="schemaConstraint">The <see cref="SchemaConstraint"/> for the schema object that houses this keyword.</param>
	/// <param name="localConstraints">
	/// The set of other <see cref="KeywordConstraint"/>s that have been processed prior to this one.
	/// Will contain the constraints for keyword dependencies.
	/// </param>
	/// <param name="context">The <see cref="EvaluationContext"/>.</param>
	/// <returns>A constraint object.</returns>
	public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, IReadOnlyList<KeywordConstraint> localConstraints, EvaluationContext context)
	{
		var newUri = new Uri(context.Scope.LocalScope, Reference);
		var newBaseUri = new Uri(newUri.GetLeftPart(UriPartial.Query));

		JsonSchema? targetSchema = null;
		var targetBase = context.Options.SchemaRegistry.Get(newBaseUri) ??
						 throw new JsonSchemaException($"Cannot resolve base schema from `{newUri}`");

		foreach (var uri in context.Scope.Reverse())
		{
			var scopeRoot = context.Options.SchemaRegistry.Get(uri);
			if (scopeRoot == null)
				throw new Exception("This shouldn't happen");

			if (scopeRoot is not JsonSchema schemaRoot)
				throw new Exception("Does OpenAPI use anchors?");

			if (schemaRoot.RecursiveAnchor == null) continue;

			if (targetBase is JsonSchema { RecursiveAnchor: null }) break;

			targetSchema = schemaRoot.RecursiveAnchor;
			break;
		}

		if (targetSchema == null)
		{
			if (JsonPointer.TryParse(newUri.Fragment, out var pointerFragment))
			{
				if (targetBase == null)
					throw new JsonSchemaException($"Cannot resolve base schema from `{newUri}`");

				targetSchema = targetBase.FindSubschema(pointerFragment!, context.Options);
			}
			else
			{
				var anchorFragment = newUri.Fragment[1..];
				if (!AnchorKeyword.AnchorPattern.IsMatch(anchorFragment))
					throw new JsonSchemaException($"Unrecognized fragment type `{newUri}`");

				if (targetBase is JsonSchema targetBaseSchema &&
					targetBaseSchema.Anchors.TryGetValue(anchorFragment, out var anchorDefinition))
					targetSchema = anchorDefinition.Schema;
			}

			if (targetSchema == null)
				throw new JsonSchemaException($"Cannot resolve schema `{newUri}`");
		}

		return new KeywordConstraint(Name, (e, c) => Evaluator(e, c, targetSchema));
	}

	private static void Evaluator(KeywordEvaluation evaluation, EvaluationContext context, JsonSchema target)
	{
		var childEvaluation = target
			.GetConstraint(JsonPointer.Create(Name), evaluation.Results.InstanceLocation, JsonPointer.Empty, context)
			.BuildEvaluation(evaluation.LocalInstance, evaluation.Results.InstanceLocation, evaluation.Results.EvaluationPath.Combine(Name), context.Options);
		evaluation.ChildEvaluations = [childEvaluation];

		childEvaluation.Evaluate(context);

		if (!childEvaluation.Results.IsValid)
			evaluation.Results.Fail();
	}
}

/// <summary>
/// JSON converter for <see cref="RecursiveRefKeyword"/>.
/// </summary>
public sealed class RecursiveRefKeywordJsonConverter : WeaklyTypedJsonConverter<RecursiveRefKeyword>
{
	/// <summary>Reads and converts the JSON to type <see cref="RecursiveRefKeyword"/>.</summary>
	/// <param name="reader">The reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">An object that specifies serialization options to use.</param>
	/// <returns>The converted value.</returns>
	public override RecursiveRefKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var uri = reader.GetString()!;
		return new RecursiveRefKeyword(new Uri(uri, UriKind.RelativeOrAbsolute));


	}

	/// <summary>Writes a specified value as JSON.</summary>
	/// <param name="writer">The writer to write to.</param>
	/// <param name="value">The value to convert to JSON.</param>
	/// <param name="options">An object that specifies serialization options to use.</param>
	public override void Write(Utf8JsonWriter writer, RecursiveRefKeyword value, JsonSerializerOptions options)
	{
		options.Write(writer, value.Reference, JsonSchemaSerializerContext.Default.Uri);
	}
}