# DynamicsCrm-CrmLogger

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-CrmLogger](https://badges.gitter.im/yagasoft/DynamicsCrm-CrmLogger.svg)](https://gitter.im/yagasoft/DynamicsCrm-CrmLogger?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

---

A CRM solution that provides a lot of details when logging from plugins. It provides a tree view of function calls as well.

### Features

  + Log each whole execution in its own record
	+ Log works in plugins and custom steps
  + Defers log to the end of the execution for improved performance
  + Create parent log container and defers creating entries to the async job for performance
  + Option for logging function start and end (manually)
	+ For automatic logging, use CrmLogger.Fody from NuGet
  + Automatically parses the exception details on passing an Exception object to the log

### Install

 + Import Common solution ([Dynamics365-YsCommonSolution](https://github.com/yagasoft/Dynamics365-YsCommonSolution))
 + PluginLogger class can be found in Common.cs in the [DynamicsCrm-Libraries](https://github.com/yagasoft/DynamicsCrm-Libraries) repository.

### Guide

  + Set the log level in the Generic Configuration entity, on the 'Logging' form
  + Go to the Log entity view to view the logs

I will post a complete guide soon.

The following is a sample of how to log in a plugin.

```csharp
public void Execute(IServiceProvider serviceProvider)
{
	var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
	var log = new PluginLogger(serviceProvider);

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
		log.Log(e);

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

## Changes
+ Check Releases page for the later changes

---
**Copyright &copy; by Ahmed Elsawalhy ([Yagasoft](https://yagasoft.com))** -- _GPL v3 Licence_
