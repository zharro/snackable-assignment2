# Practical assignment

## Solution overview
* Presentation API - `FilesController.cs`
* Processing API gateway - `IProcessingApiClient.cs`
* Ingestion procedure - `AllFilesSynchronizer.cs`
* Polling procedure - `ProcessingFilesSynchronizer.cs`
* Swagger document - `/swagger/index.html`

## To be done
The current solution endlessly checks the failed task to complete.

## Processing API suggestions
* Add status to /details

## Other considerations
* Presentation API fully relies on the cache (in-memory DB) which can lead to stale/irrelevant relevant responses.
* Processing service can notify Presentation service about file status changes during its processing (e.g. via RabbitMQ/Kafka). In this case, the Presentation service would store its up-to-date projection of files.
* If the file is uploaded via Presentation API - it can track all files under processing. There is no need in looking for Processing files in a whole bunch of files in this case.