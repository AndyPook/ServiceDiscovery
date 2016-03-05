using System;
using System.Text;

namespace Pook.Net
{
	public static class ExceptionExtensions
	{
		public static string MessageAggregator(this Exception exception)
		{
			var messages = new StringBuilder();
			var aggEx = exception as AggregateException;
			if (aggEx != null)
			{
				foreach (var e in aggEx.InnerExceptions)
					messages.AppendLine(e.MessageAggregator());
			}
			else
			{
				while (exception != null)
				{
					messages.AppendLine(exception.ToString());
					messages.AppendLine();
					exception = exception.InnerException;
				}
			}

			return messages.ToString().TrimEnd('\n', '\r');
		}
	}
}