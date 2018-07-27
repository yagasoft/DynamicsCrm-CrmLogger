# DynamicsCrm-CrmLogger
### Version: 1.1
---

A CRM solution that provides a lot of details when logging from plugins, web-services, and console apps. It provides a tree view of function calls as well.

### Features

  + Log each whole execution in its own record
	+ Log works in plugins, custom steps, console apps, and web services
  + Defers log to the end of the execution for improved performance
  + Create parent log container and defers creating entries to the async job for performance
  + Option for logging function start and end (manually)
	+ For automatic logging, use CrmLogger.Fody from NuGet
  + Automatically parses the exception details on passing an Exception object to the log
  + Logs to CRM, CSV file, or console
	+ File logging can be used as a fallback
  + Log files overflow to new files on date or size threshold

### Guide

The following is a sample of a plugin log. I will post a complete guide soon.

```csharp
public void Execute(IServiceProvider serviceProvider)
{
	var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
	
	try
	{
		log.SetRegarding(context.PrimaryEntityName, context.PrimaryEntityId);
		log.LogExecutionStart();

		log.LogDebug("Starting plugin logic ...");

		log.Log("Some info.");
		Test(log);
		log.LogWarning("Some warning.");

		log.LogDebug("Finished plugin logic.");
	}
	catch (Exception e)
	{
		log.ExecutionFailed();
		log.Log(e, context);

		throw new InvalidPluginExecutionException(e.Message, e);
	}
	finally
	{
		log.LogExecutionEnd();
	}
}

private void Test(CrmLog log)
{
	log.LogFunctionStart();

	try
	{
		log.Log("Some info.");
	}
	catch (Exception e)
	{
		log.Log(e);
		throw;
	}
	finally
	{
		log.LogFunctionEnd();
	}
}
```

### Dependencies

  + Generic Base solution

---
**Copyright &copy; by Ahmed el-Sawalhy ([YagaSoft](http://yagasoft.com))** -- _GPL v3 Licence_
