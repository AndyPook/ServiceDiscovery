using System.Collections.Generic;

namespace Pook.Net
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Append<T>(this IEnumerable<T> orig, IEnumerable<T> additional)
		{
			foreach (var item in orig)
				yield return item;
			if (additional == null)
				yield break;
			foreach (var item in additional)
				yield return item;
		}
	}
}