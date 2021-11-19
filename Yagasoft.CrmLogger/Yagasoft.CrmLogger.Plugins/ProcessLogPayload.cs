#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using Yagasoft.Libraries.Common;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.CrmLogger.Plugins
{
	/// <summary>
	///     This plugin creates log entries using the compressed value in the 'serialised' field.<br />
	///     Version: 1.1.1
	/// </summary>
	public class ProcessLogPayload : IPlugin
	{
		public void Execute(IServiceProvider serviceProvider)
		{
			new ProcessLogPayloadLogic().Execute(this, serviceProvider, false);
		}
	}

	internal class ProcessLogPayloadLogic : PluginLogic<ProcessLogPayload>
	{
		public ProcessLogPayloadLogic()
			: base("Create", PluginStage.PostOperation, Plugins.Log.EntityLogicalName)
		{ }

		protected override void ExecuteLogic()
		{
			// get the triggering record
			var target = (Entity)Context.InputParameters["Target"];

			var rawSerialisedEntries = target.GetAttributeValue<string>("ldv_serialisedlogentries");

			if (rawSerialisedEntries.IsEmpty())
			{
				TracingService.Trace("rawSerialisedEntries is empty.");
				return;
			}

			TracingService.Trace("Decompressing log entries ...");
			var serialisedEntries =
				rawSerialisedEntries
					.Decompress()
					.Split(new[] { "<|||>" }, StringSplitOptions.RemoveEmptyEntries);

			TracingService.Trace("Deserialising the log entries returning them to 'Entity' type ...");
			var logEntries = serialisedEntries
				.Select(EntitySerializer.DeserializeObject)
				.ToEntity<LogEntry>()
				.Where(l => l.LogicalName == LogEntry.EntityLogicalName).ToList();

			var guidMap = new Dictionary<Guid, Guid>();

			TracingService.Trace("Clearing all previously generated GUIDs and creating entries ...");

			foreach (var logEntry in logEntries)
			{
				var oldId = logEntry.Id;
				// clear entry ID because CRM generates sequential ones, which is faster to retrieve from SQL later
				logEntry.Id = Guid.Empty;

				logEntry.ParentLog = target.Id;

				var parentRef = logEntry.ParentLogEntry;

				// no parent, so can't be created!
				if (parentRef.HasValue)
				{
					// get the parent entry ID from the previously created entries
					guidMap.TryGetValue(parentRef.Value, out var newParentId);

					// set in this entry
					if (newParentId != Guid.Empty)
					{
						logEntry.ParentLogEntry = newParentId;
					}
				}

				guidMap[oldId] = logEntry.Id = Service.Create(logEntry);
			}

			TracingService.Trace("Clearing the compressed payload from the log ...");

			var exceptionEntry = logEntries.AsEnumerable().Reverse().FirstOrDefault(l => l.ExceptionThrown == true);

			Service.Update(
				new Log
				{
					Id = target.Id,
					ExceptionLogEntry = exceptionEntry?.Id,
					ExceptionMessage = $"{exceptionEntry?.Message}"
						+ $"{(exceptionEntry?.InnerExceptionMessage.IsFilled() == true ? $" | {exceptionEntry.InnerExceptionMessage}" : "")}",
					SerialisedLogEntries = null
				});
		}
	}
}
