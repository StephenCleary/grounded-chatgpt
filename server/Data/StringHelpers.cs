using System.Text.RegularExpressions;

namespace server.Data;

public static class StringHelpers
{
	public static string Template(this string format, object values)
	{
		var result = format;
		var props = values.GetType().GetProperties();
		foreach (var prop in props)
		{
			var replacement = $"{{{prop.Name}}}";
			var newResult = result.Replace(replacement, prop.GetValue(values)?.ToString());
			if (newResult == result)
				throw new InvalidOperationException($"Replacement {replacement} not found in {format}");
			result = newResult;
		}

		var matches = _templateRegex.Matches(result);
		if (matches.Count > 0)
			throw new InvalidOperationException($"Replacement {matches[0].Value} not substituted in {format}");
		return result.Replace("\r\n", "\n");
	}

	private static readonly Regex _templateRegex = new(@"{[a-zA-Z0-9_]+}");
}
