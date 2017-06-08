using System;
using System.Collections.Generic;
using System.Linq;

namespace AsyncGenerator.Core.FileConfiguration.Internal
{
	internal class XmlValidationResult
	{
		private readonly List<Exception> _errors = new List<Exception>();
		private readonly List<Exception> _warnings = new List<Exception>();

		public bool IsValid => !Errors.Any();

		public IEnumerable<Exception> Errors => _errors;

		public IEnumerable<Exception> Warnings => _warnings;

		internal void AddError(Exception exception)
		{
			_errors.Add(exception);
		}

		internal void AddWarning(Exception exception)
		{
			_warnings.Add(exception);
		}

		public override string ToString()
		{
			var str = $"XmlValidationResult contains '{_errors.Count}' errors and '{_warnings.Count}' warnings.{Environment.NewLine}";
			if (_errors.Any())
			{
				str += string.Join(Environment.NewLine, _errors.Select(o => string.Format("Error: " + o.Message)));
			}
			if (_warnings.Any())
			{
				str += string.Join(Environment.NewLine, _warnings.Select(o => string.Format("Warning: " + o.Message)));
			}
			return str;
		}

		public static implicit operator string(XmlValidationResult mesasge)
		{
			return mesasge.ToString();
		}
	}
}
