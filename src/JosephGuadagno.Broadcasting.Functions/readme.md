# Broadcasting Functions

This library provides a set of functions for broadcasting messages to multiple recipients in a distributed system. It includes features for message routing, message delivery, and message acknowledgment.


## Setup

Most of these run through Aspire and Azure Functions.  To test the event grid topics locally, you will need to install the [AzureEventGridSimulator](https://github.com/pm7y/AzureEventGridSimulator). The installation instructions are in the readme.  

Install as a .NET global tool:

```powershell
dotnet tool install -g AzureEventGridSimulator
```

The simulator is configured with a custom appsettings.json file, [event-grid-simulator-config.json](event-grid-simulator-config.json).  You will need to create a "topic" configuration for each topic you want to test.

## Running the Simulator to Test

First, you will need to edit the event-grid-simulator-config.json file to include the topics you want to test. All the topics are listed in the file, but you can add or remove them as needed.

Second, you will need to edit the ports to match the post that the Aspire is using for the Functions app.

```json
{
    "name": "BlueskyProcessSpeakingEngagementDataFired",
    "endpoint": "http://localhost:59862/runtime/webhooks/EventGrid?functionName=BlueskyProcessSpeakingEngagementDataFired", "disableValidation": true
}
```

Third, you will need to make sure that the topics you want to simulate are enabled. Check the `disabled` property in the topic's configuration.

Lastly, run the simulator:

```powershell
azure-eventgrid-simulator --ConfigFile=event-grid-simulator-config.json
```

Once started, you should be able to send messages to the topics and view the dashboard at [https://localhost:60101/dashboard](https://localhost:60101/dashboard)


## References

- [Azure Event Grid](https://docs.microsoft.com/en-us/azure/event-grid/)
- [AzureEventGridSimulator](https://github.com/pm7y/AzureEventGridSimulator)
- [AzureEventGridSimulator Wiki](https://github.com/pm7y/AzureEventGridSimulator/wiki/Configuration)
