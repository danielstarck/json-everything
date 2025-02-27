﻿using System.Linq;

namespace Json.Schema.DataGeneration.Requirements;

internal class NumberRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchema schema)
	{
		var supportsNumbers = false;

		var range = NumberRangeSet.Full;
		var minimum = schema.Keywords?.OfType<MinimumKeyword>().FirstOrDefault()?.Value;
		if (minimum != null)
		{
			range = range.Floor(minimum.Value);
			supportsNumbers = true;
		}
		var maximum = schema.Keywords?.OfType<MaximumKeyword>().FirstOrDefault()?.Value;
		if (maximum != null)
		{
			range = range.Ceiling(maximum.Value);
			supportsNumbers = true;
		}
		if (range != NumberRangeSet.Full)
		{
			if (context.NumberRanges != null)
				context.NumberRanges *= range;
			else
				context.NumberRanges = range;
			supportsNumbers = true;
		}

		var multipleOf = schema.Keywords?.OfType<MultipleOfKeyword>().FirstOrDefault()?.Value;
		if (multipleOf != null)
		{
			if (context.Multiples != null)
				context.Multiples?.Add(multipleOf.Value);
			else
				context.Multiples = [multipleOf.Value];
			supportsNumbers = true;
		}

		if (supportsNumbers)
			context.InferredType |= SchemaValueType.Number;
	}
}