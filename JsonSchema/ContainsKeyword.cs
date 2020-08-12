﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema
{
	[SchemaKeyword(Name)]
	[JsonConverter(typeof(ContainsKeywordJsonConverter))]
	public class ContainsKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "contains";

		public JsonSchema Schema { get; }

		public ContainsKeyword(JsonSchema value)
		{
			Schema = value;
		}

		public ValidationResults Validate(ValidationContext context)
		{
			if (context.Instance.ValueKind != JsonValueKind.Array)
				return null;

			var count = context.Instance.GetArrayLength();
			var subResults = new List<ValidationResults>();
			for (int i = 0; i < count; i++)
			{
				// TODO: shortcut if flag output
				var subContext = ValidationContext.From(context,
					context.InstanceLocation.Combine(PointerSegment.Create($"{i}")),
					context.Instance[i]);
				var subResult = Schema.ValidateSubschema(subContext);
				subResults.Add(subResult);
			}

			var found = subResults.Count(r => r.IsValid);
			context.Annotations[Name] = found;
			var result = found != 0
				? ValidationResults.Success(context)
				: ValidationResults.Fail(context, "Expected array to contain indicated value but it did not");
			result.AddNestedResults(subResults);
			return result;
		}
	}

	public class ContainsKeywordJsonConverter : JsonConverter<ContainsKeyword>
	{
		public override ContainsKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options);

			return new ContainsKeyword(schema);
		}
		public override void Write(Utf8JsonWriter writer, ContainsKeyword value, JsonSerializerOptions options)
		{
			writer.WritePropertyName(ContainsKeyword.Name);
			JsonSerializer.Serialize(writer, value.Schema, options);
		}
	}
}