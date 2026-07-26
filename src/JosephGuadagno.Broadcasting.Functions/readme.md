# Broadcasting Functions

This project hosts the Azure Functions used by the broadcasting system.
Collectors load source content, publishers write platform-specific queue messages,
and supporting functions handle token refresh, health checks, and maintenance.

## Local development

Most of the Functions app runs through Aspire and Azurite.
Local publisher dispatch now writes directly to storage queues, so the Azure Event
Grid simulator is no longer required.

To run locally:

1. Start the AppHost project.
2. Confirm the Functions app and Azurite queue service are healthy.
3. Use the Web UI to manage per-user publisher mappings and random post settings.
4. Inspect queued publish requests with Azure Storage Explorer when needed.

## References

- [Azure Functions](https://learn.microsoft.com/azure/azure-functions/)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
