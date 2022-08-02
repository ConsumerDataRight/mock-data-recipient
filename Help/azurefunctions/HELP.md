<h2>Azure Functions</h2>
<div style="margin-left:18px;">
The Mock Data Recipient solution contains azure function projects.<br />
The DiscoverDataHolders function is used to get the list of Data Holder Brands<br />
from the Mock Register and update the Mock Data Recipient repository DataHolderBrands table.<br />
The DCR function is used to register the software product included in the Mock Data Recipient<br />
with the newly discovered Data Holder Brands from the Mock Register.<br />
</div>

<h2>To Run and Debug Azure Functions</h2>
<div style="margin-left:18px;">
	The following procedures can be used to run the functions in a local development environment for evaluation of the functions.
<br />

<div style="margin-top:6px;">
1) Start the Mock Register, Mock Data Holder, Mock Data Holder Energy and Mock Data Recipient solutions.
</div>
<div style="margin-left:18px;">
	Noting that the Mock Data Recipient must be running as it is required for the https://localhost:9001/jwks<br />
	endpoint that is used for the Access Token.<br />
</div>

<div style="margin-top:6px;">
2) Start the Azure Storage Emulator (Azurite):
</div>
<div style="margin-left:18px;margin-bottom:6px;">
	using a MS Windows command prompt:<br />
</div>

```
md C:\azurite
cd "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator"
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

<div style="margin-left:18px;">
	Noting this is only required to be performed once, it will then be listening on ports - 10000, 10001 and 10002<br />
	when debugging is started from MS Visual Studio by selecting CDR.DiscoverDataHolder or CDR.DCR as the startup project<br />
	(by starting a debug instance using F5 or Debug > Start Debugging)
	<br />
</div>
<div style="margin-left:18px;margin-bottom:6px;">
	or by using a MS Windows command prompt:<br />
</div>

```
navigate to .\mock-data-holder-energy\Source\CDR.GetDataRecipients<br />
func start --verbose<br />
```

<div style="margin-left:18px;">
    To reset the message queue, uncomment the following line of code and start the debug instance as indicated above, placing a<br /> breakpoint after this code;<br />
	await DeleteAllMessagesAsync(log, qConnString, qName);<br />
</div>

<div style="margin-top:6px;">
3) Open two instances of Mock Data Recipient in MS Visual Studio,
</div>
<div style="margin-left:18px;">
	(select CDR.DCR as the startup project in one and CDR.DiscoverDataHolder as the startup project in the other)
</div>

<div style="margin-top:6px;">
4) Start each debug instances (F5 or Debug > Start Debugging), this will simulate the discovery of Data Holder brands and the
</div>
<div style="margin-left:18px;">
	processing of the messages added to the queue.
</div>

<div style="margin-left:18px;margin-top:12px;margin-bottom:6px;">
	Noting the below sql script is used to clear out the registrations as the seed data is not a good reflection on reality as the<br /> infosecBaseUri values are either invalid or duplicated, so running this script resets the data so the registration process can<br />
	occur for the processed message queue item.
</div>

```
DECLARE @i INT = 1;
WHILE (@i <= 3600000)
BEGIN
WAITFOR DELAY '00:00:01'
	DELETE FROM [cdr-mdr].[dbo].[Registration]
	DELETE FROM [cdr-idsvr].[dbo].[Clients]
	DELETE FROM [cdr-idsvre].[dbo].[Clients]
SET  @i = @i + 1;
END


Observing the following tables shows the above functions in operation from the database perspective:
SELECT * FROM [cdr-mdr].[dbo].[DcrMessage]
SELECT * FROM [cdr-mdr].[dbo].[LogEvents_DCRService]
```
