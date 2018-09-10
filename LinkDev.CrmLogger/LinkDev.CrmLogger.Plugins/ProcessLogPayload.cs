#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using LinkDev.Libraries.Common;
using Microsoft.Xrm.Sdk;

#endregion

namespace LinkDev.CrmLogger.Plugins
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
		public ProcessLogPayloadLogic() : base(null, PluginStage.All)
		{ }

		protected override bool IsContextValid()
		{
			if (context.MessageName != "Create")
			{
				throw new InvalidPluginExecutionException(
					$"Step registered on wrong message: {context.MessageName},"
						+ $"expected: Create.");
			}

			if (!context.InputParameters.Contains("Target"))
			{
				throw new InvalidPluginExecutionException($"Context is missing input parameter: Target.");
			}

			if (context.PrimaryEntityName != "ldv_log")
			{
				throw new InvalidPluginExecutionException(
					$"Step registered on wrong entity: {context.PrimaryEntityName},"
						+ $"expected: ldv_log");
			}

			return true;
		}

		protected override void ExecuteLogic()
		{
			// get the triggering record
			var target = (Entity)context.InputParameters["Target"];

			var rawSerialisedEntries = target.GetAttributeValue<string>("ldv_serialisedlogentries");

			if (string.IsNullOrEmpty(rawSerialisedEntries))
			{
				tracingService.Trace("rawSerialisedEntries is empty.");
				return;
			}

			tracingService.Trace("Decompress log entries.");
			var serialisedEntries =
				rawSerialisedEntries
					.Decompress()
					.Split(new[] { "<|||>" }, StringSplitOptions.RemoveEmptyEntries);

			tracingService.Trace("Checking if there is a 'parent' log added to the compressed payload ...");
			var updatedParent = serialisedEntries
				.Where(s => s.Contains("<PARENT>"))
				.Select(s => EntitySerializer.DeserializeObject(s.Replace("<PARENT>", "")))
				.FirstOrDefault();
			tracingService.Trace(updatedParent == null ? "Doesn't exist" : "Exists");
			var exceptionLogRef = updatedParent?.GetAttributeValue<EntityReference>("ldv_exceptionlogentry");

			tracingService.Trace("Deserialising the log entries returning them to 'Entity' type ...");
			var logEntries = serialisedEntries
				.Where(s => s?.Contains("<PARENT>") == false)
				.Select(EntitySerializer.DeserializeObject)
				.Where(l => l.LogicalName == "ldv_logentry").ToList();

			var guidMap = new Dictionary<Guid, Guid>();

			tracingService.Trace("Clearing all previously generated GUIDs and creating entries ...");
			foreach (var logEntry in logEntries)
			{
				var oldId = logEntry.Id;
				// clear entry ID because CRM generates sequential ones, which is faster to retrieve from SQL later
				logEntry.Id = Guid.Empty;

				var parentRef = logEntry.GetAttributeValue<EntityReference>("ldv_parentlogentryid");

				// no parent, so can't be created!
				if (parentRef != null)
				{
					// get the parent entry ID from the previously created entries
					guidMap.TryGetValue(parentRef.Id, out var newParentId);

					// set in this entry
					if (newParentId != Guid.Empty)
					{
						parentRef.Id = newParentId;
					}
				}

				var newId = service.Create(logEntry);
				guidMap[oldId] = newId;
			}

			tracingService.Trace("Clearing the compressed payload from the log ...");
			if (updatedParent == null)
			{
				updatedParent =
					new Entity(target.LogicalName)
					{
						Id = target.Id
					};
			}
			else if (exceptionLogRef != null)
			{
				guidMap.TryGetValue(exceptionLogRef.Id, out var newExceptionId);
				exceptionLogRef.Id = newExceptionId;
			}

			updatedParent["ldv_serialisedlogentries"] = null;

			service.Update(updatedParent);
		}
	}
}
