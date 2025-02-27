﻿using System.Linq;

namespace Json.Schema.DataGeneration.Requirements;

internal class ConditionalRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchema schema)
	{
		var ifKeyword = schema.Keywords?.OfType<IfKeyword>().FirstOrDefault();
		var thenKeyword = schema.Keywords?.OfType<ThenKeyword>().FirstOrDefault();
		var elseKeyword = schema.Keywords?.OfType<ElseKeyword>().FirstOrDefault();

		if (ifKeyword != null)
		{
			RequirementsContext? ifthen = null;
			if (thenKeyword != null)
			{
				ifthen = ifKeyword.Schema.GetRequirements();
				ifthen.And(thenKeyword.Schema.GetRequirements());
			}

			RequirementsContext? ifelse = null;
			if (elseKeyword != null)
			{
				ifelse = ifKeyword.Schema.GetRequirements().Break();
				ifelse.And(elseKeyword.Schema.GetRequirements());
			}

			if (ifthen == null && ifelse == null) return;
			if (ifthen == null)
				context.And(ifelse!);
			else if (ifelse == null)
				context.And(ifthen);
			else
			{
				if (context.Options != null)
				{
					context.Options.Add(ifthen);
					context.Options.Add(ifelse);
				}
				else
					context.Options = [ifthen, ifelse];
			}
		}
	}
}