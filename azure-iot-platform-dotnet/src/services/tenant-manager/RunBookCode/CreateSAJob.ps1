<#
    .DESCRIPTION
        A run book for creating a new Stream Analytics Job per Tenant
    .NOTES
        AUTHOR: Radha Mohanty
#>
param
(
    [Parameter (Mandatory = $false)]
    [object] $WebhookData
)

# Make sure this runbook was triggered by a webhook
if (-Not $WebhookData) {
    # Error
    Write-Error "This runbook is meant to be started from an Azure alert webhook only."
}

# Retrieve the data from the Webhook request body
$data = (ConvertFrom-Json -InputObject $WebhookData.RequestBody)

# Authenticate with the service principal
$connectionName = "AzureRunAsConnection"
try
{
    $servicePrincipalConnection = Get-AutomationConnection -Name $connectionName

    "Logging in to Azure..."
    Connect-AzAccount -Tenant $servicePrincipalConnection.TenantID `
                             -ApplicationId $servicePrincipalConnection.ApplicationID   `
                             -CertificateThumbprint $servicePrincipalConnection.CertificateThumbprint `
                             -ServicePrincipal `
                             -SubscriptionId $data.subscriptionId
    "Successfully Logged in using ServicePrincipal"
}
catch {
    if (!$servicePrincipalConnection)
    {
        $ErrorMessage = "Connection $connectionName not found."
        throw $ErrorMessage
    } else{
        Write-Error -Message $_.Exception
        throw $_.Exception
    }
}

# Get data
$location = $data.location
$resourceGroup = $data.resourceGroup
$saJobName = $data.saJobName
$storageAccountName = $data.storageAccountName
$storageAccountKey = $data.storageAccountKey
$eventHubNamespaceName = $data.eventHubNamespaceName
$eventHubAccessPolicyKey = $data.eventHubAccessPolicyKey
$iotHubName = $data.iotHubName
$iotHubAccessKey = $data.iotHubAccessKey
$cosmosDbAccountName = $data.cosmosDbAccountName
$cosmosDbDatabaseId = $data.cosmosDbDatabaseId
$cosmosDbAccountKey = $data.cosmosDbAccountKey
$tenantId = $data.tenantId

# check if some of the values are coming as blank
if ([string]::IsNullOrEmpty($cosmosDbAccountKey)){
    Write-Output "cosmosDbAccountKey is empty"
}

if ([string]::IsNullOrEmpty($iotHubAccessKey)){
    Write-Output "IothubConnectionKey is empty"
}

$SAjobDefinition = @"
{
  "location":"$($location)",
  "properties":{
    "sku":{
      "name":"Standard"
    },
    "eventsOutOfOrderPolicy":"Adjust",
    "outputErrorPolicy": "Stop",
    "eventsOutOfOrderMaxDelayInSeconds":10,
    "eventsLateArrivalMaxDelayInSeconds":5,
    "compatibilityLevel": 1.1
  }
}
"@

$SAJobInputDefinition = @"
{
	"value": [

		{
			"name": "DeviceGroups",
			"type": "Microsoft.StreamAnalytics/streamingjobs/inputs",
			"properties": {
				"type": "Reference",
				"datasource": {
					"type": "Microsoft.Storage/Blob",
					"properties": {
						"blobName": "$($tenantId)",
						"storageAccounts": [{
							"accountName": "$($storageAccountName)",
							"accountKey": "$($storageAccountKey)"
						}],
						"container": "referenceinput",
						"pathPattern": "{date}/{time}/devicegroups.csv",
						"dateFormat": "yyyy/MM/dd",
						"timeFormat": "HH-mm"
					}
				},
				"compression": {
					"type": "None"
				},
				"serialization": {
					"type": "Csv",
					"properties": {
						"fieldDelimiter": ",",
						"encoding": "UTF8"
					}
				}
			}
		},
		{
			"name": "DeviceTelemetry",
			"type": "Microsoft.StreamAnalytics/streamingjobs/inputs",
			"properties": {
				"type": "Stream",
				"datasource": {
					"type": "Microsoft.Devices/IotHubs",
					"properties": {
						"iotHubNamespace": "$($iotHubName)",
						"sharedAccessPolicyName": "iothubowner",
						"sharedAccessPolicyKey": "$($iotHubAccessKey)",
						"endpoint": "messages/events",
						"consumerGroupName": "sajobconsumergroup"
					}
				},
				"compression": {
					"type": "None"
				},
				"serialization": {
					"type": "Json",
					"properties": {
						"encoding": "UTF8"
					}
				}
			}
		},
		{
			"name": "Rules",
			"type": "Microsoft.StreamAnalytics/streamingjobs/inputs",
			"properties": {
				"type": "Reference",
				"datasource": {
					"type": "Microsoft.Storage/Blob",
					"properties": {
						"storageAccounts": [{
							"accountName": "$($storageAccountName)",
							"accountKey": "$($storageAccountKey)"
						}],
						"container": "referenceinput",
						"pathPattern": "{date}/{time}/rules.json",
						"dateFormat": "yyyy-MM-dd",
						"timeFormat": "HH-mm"
					}
				},
				"compression": {
					"type": "None"
				},
				"serialization": {
					"type": "Json",
					"properties": {
						"encoding": "UTF8"
					}
				}
			}
		}
	]
}
"@

$SAJobOutputDefinition = @"
{
  "value": [
        {
    "name": "Actions",
    "type": "Microsoft.StreamAnalytics/streamingjobs/outputs",
    "properties": {
        "datasource": {
            "type": "Microsoft.ServiceBus/EventHub",
            "properties": {
                "eventHubName": "actions-eventhub",
                "serviceBusNamespace": "$($eventHubNamespaceName)",
                "sharedAccessPolicyName": "RootManageSharedAccessKey",  
                "sharedAccessPolicyKey": "$($eventHubAccessPolicyKey)"  
                }
             },
             "serialization": {
             "type": "Json",
             "properties": {
             "encoding": "UTF8",
             "format": "LineSeparated"
             }
         }
     }
 },
 {
    "name": "Alarms",
    "type": "Microsoft.StreamAnalytics/streamingjobs/outputs",
      "properties": {
        "datasource": {
          "type": "Microsoft.Storage/DocumentDB",
          "properties": {
            "accountId": "$($cosmosDbAccountName)",
            "accountKey": "$($cosmosDbAccountKey)",
            "database": "$($cosmosDbDatabaseId)",
            "collectionNamePattern": "alarms-$($tenantId)"
          }
        }
     }
   }
 ]
}
"@

$SAJobQuery = @"
{
    "name":"MyTransformation",
    "type":"Microsoft.StreamAnalytics/streamingjobs/transformations",
    "properties":{
        "streamingUnits":1,
        "script":null,
        "query":"WITH TelemetryAndRules AS
        (
            SELECT
                T.IotHub.ConnectionDeviceId as __deviceid,
                T.PartitionId,
                T.EventEnqueuedUtcTime as __receivedtime,
                R.Id as __ruleid,
                R.AggregationWindow,
                Fields.ArrayValue as MeasurementName,
                GetRecordPropertyValue(T, Fields.ArrayValue) as MeasurementValue
            FROM
                DeviceTelemetry T PARTITION BY PartitionId TIMESTAMP BY T.EventEnqueuedUtcTime
                JOIN DeviceGroups G ON T.IoTHub.ConnectionDeviceId = G.DeviceId
                JOIN Rules R ON R.GroupId = G.GroupId
                CROSS APPLY GetArrayElements(R.Fields) AS Fields
        ),
        AggregateMultipleWindows AS (
            SELECT
                TR.__deviceid,
                TR.__ruleid,
                TR.PartitionId,
                TR.MeasurementName,
                AVG(TR.MeasurementValue),
                MAX(TR.MeasurementValue),
                MIN(TR.MeasurementValue),
                COUNT(TR.MeasurementValue),
                MAX(DATEDIFF(millisecond, '1970-01-01T00:00:00Z', TR.__receivedtime)) as __lastReceivedTime
            FROM
                TelemetryAndRules TR PARTITION BY PartitionId
            WHERE
                TR.AggregationWindow = 'tumblingwindow1minutes'
            GROUP BY
                TR.__deviceid,
                TR.__ruleid,
                TR.PartitionId,
                TR.MeasurementName,
                TumblingWindow(minute, 1)
        
            UNION
        
            SELECT
                TR.__deviceid,
                TR.__ruleid,
                TR.PartitionId,
                TR.MeasurementName,
                AVG(TR.MeasurementValue),
                MAX(TR.MeasurementValue),
                MIN(TR.MeasurementValue),
                COUNT(TR.MeasurementValue),
                MAX(DATEDIFF(millisecond, '1970-01-01T00:00:00Z', TR.__receivedtime)) as __lastReceivedTime
            FROM
                TelemetryAndRules TR PARTITION BY PartitionId
            WHERE
                TR.AggregationWindow = 'tumblingwindow5minutes'
            GROUP BY
                TR.__deviceid,
                TR.__ruleid,
                TR.PartitionId,
                TR.MeasurementName,
                TumblingWindow(minute, 5)
        
            UNION
        
            SELECT
                TR.__deviceid,
                TR.__ruleid,
                TR.PartitionId,
                TR.MeasurementName,
                AVG(TR.MeasurementValue),
                MAX(TR.MeasurementValue),
                MIN(TR.MeasurementValue),
                COUNT(TR.MeasurementValue),
                MAX(DATEDIFF(millisecond, '1970-01-01T00:00:00Z', TR.__receivedtime)) as __lastReceivedTime
            FROM
                TelemetryAndRules TR PARTITION BY PartitionId
            WHERE
                TR.AggregationWindow = 'tumblingwindow10minutes'
            GROUP BY
                TR.__deviceid,
                TR.__ruleid,
                TR.PartitionId,
                TR.MeasurementName,
                TumblingWindow(minute, 10)
        ),
        GroupAggregatedMeasurements AS (
            SELECT
                AM.__deviceid,
                AM.__ruleid,
                AM.PartitionId,
                AM.__lastReceivedTime,
                Collect() AS Measurements
            FROM
                AggregateMultipleWindows AM PARTITION BY PartitionId
            GROUP BY
                AM.__deviceid,
                AM.__ruleid,
                AM.PartitionId,
                AM.__lastReceivedTime,
                System.Timestamp
        ),
        FlatAggregatedMeasurements AS (
            SELECT
                GA.__deviceid,
                GA.__ruleid,
                GA.__lastReceivedTime,
                udf.flattenMeasurements(GA) AS __aggregates
            FROM
                GroupAggregatedMeasurements GA PARTITION BY PartitionId
        ),
        CombineAggregatedMeasurementsAndRules AS (
            SELECT
                FA.__deviceid,
                FA.__ruleid,
                FA.__aggregates,
                FA.__lastReceivedTime,
                R.Description as __description,
                R.Severity as __severity,
                R.Actions as __actions,
                R.__rulefilterjs as __rulefilterjs
            FROM
                FlatAggregatedMeasurements FA PARTITION BY PartitionId
                JOIN Rules R ON FA.__ruleid = R.Id
        ),
        ApplyAggregatedRuleFilters AS
        (
            SELECT
                CMR.*
            FROM
                CombineAggregatedMeasurementsAndRules CMR PARTITION BY PartitionId
            WHERE TRY_CAST(udf.applyRuleFilter(CMR) AS bigint) = 1
        ),
        GroupInstantMeasurements AS (
            SELECT
                TR.__deviceid,
                TR.__ruleid,
                TR.PartitionId,
                TR.__receivedTime,
                Collect() AS Measurements
            FROM
                TelemetryAndRules TR PARTITION BY PartitionId
            WHERE
                TR.AggregationWindow = 'instant'
            GROUP BY
                TR.__deviceid,
                TR.__ruleid,
                TR.PartitionId,
                TR.__receivedTime,
                System.Timestamp
        ),
        FlatInstantMeasurements AS (
            SELECT
                GI.__deviceid,
                GI.__ruleid,
                GI.__receivedTime,
                udf.flattenMeasurements(GI) AS __aggregates
            FROM
                GroupInstantMeasurements GI PARTITION BY PartitionId
        ),
        CombineInstantMeasurementsAndRules as
        (
            SELECT
                FI.__deviceid,
                FI.__ruleid,
                FI.__receivedtime,
                FI.__aggregates,
                R.Description as __description,
                R.Severity as __severity,
                R.Actions as __actions,
                R.__rulefilterjs as __rulefilterjs
            FROM
                FlatInstantMeasurements FI PARTITION BY PartitionId
                JOIN Rules R ON FI.__ruleid = R.Id
        ),
        ApplyInstantRuleFilters as
        (
            SELECT
                CI.*
            FROM
                CombineInstantMeasurementsAndRules CI PARTITION BY PartitionId
            WHERE TRY_CAST(udf.applyRuleFilter(CI) AS bigint) = 1
        ),
        CombineAlarms as
        (
            SELECT
                1 as [doc.schemaVersion],
                'alarm' as [doc.schema],
                'open' as [status],
                '1Rule-1Device-NMessage' as [logic],
                DATEDIFF(millisecond, '1970-01-01T00:00:00Z', System.Timestamp) as created,
                DATEDIFF(millisecond, '1970-01-01T00:00:00Z', System.Timestamp) as modified,
                AA.__description as [rule.description],
                AA.__severity as [rule.severity],
                AA.__actions as [rule.actions],
                AA.__ruleid as [rule.id],
                AA.__deviceId as [device.id],
                AA.__aggregates,
                AA.__lastReceivedTime as [device.msg.received]
            FROM
                ApplyAggregatedRuleFilters AA PARTITION BY PartitionId
        
            UNION
        
            SELECT
                1 as [doc.schemaVersion],
                'alarm' as [doc.schema],
                'open' as [status],
                '1Rule-1Device-1Message' as [logic],
                DATEDIFF(millisecond, '1970-01-01T00:00:00Z', System.Timestamp) as created,
                DATEDIFF(millisecond, '1970-01-01T00:00:00Z', System.Timestamp) as modified,
                AI.__description as [rule.description],
                AI.__severity as [rule.severity],
                AI.__actions as [rule.actions],
                AI.__ruleid as [rule.id],
                AI.__deviceId as [device.id],
                AI.__aggregates,
                DATEDIFF(millisecond, '1970-01-01T00:00:00Z', AI.__receivedTime) as [device.msg.received]
            FROM
                ApplyInstantRuleFilters AI PARTITION BY PartitionId
        )
        
        SELECT
            CA.[doc.schemaVersion],
            CA.[doc.schema],
            CA.[status],
            CA.[logic],
            CA.[created],
            CA.[modified],
            CA.[rule.description],
            CA.[rule.severity],
            CA.[rule.id],
            CA.[device.id],
            CA.[device.msg.received]
        INTO
            Alarms
        FROM
            CombineAlarms CA PARTITION BY PartitionId
        
        SELECT
            CA.[created],
            CA.[modified],
            CA.[rule.description],
            CA.[rule.severity],
            CA.[rule.id],
            CA.[rule.actions],
            CA.[device.id],
            CA.[device.msg.received]
        INTO
            Actions
        FROM
            CombineAlarms CA PARTITION BY __partitionid
        WHERE
            CA.[rule.actions] IS NOT NULL"
    }
}
"@

$SAJobFunction = @"
{
	"value": [
    {
		"name": "applyRuleFilter",
		"type": "Microsoft.StreamAnalytics/streamingjobs/functions",
		"properties": {
			"type": "Scalar",
			"properties": {
				"inputs": [{
					"dataType": "record",
					"isConfigurationParameter": null
				}],
				"output": {
					"dataType": "any"
				},
				"binding": {
					"type": "Microsoft.StreamAnalytics/JavascriptUdf",
					"properties": {
						"script": "function main(record) {\n    let ruleFunction = new Function('record', record.__rulefilterjs);\n    return ruleFunction(record);\n}"
					}
				}
			}
		}
	}, 
    {
		"name": "flattenMeasurements",
		"type": "Microsoft.StreamAnalytics/streamingjobs/functions",
		"properties": {
			"type": "Scalar",
			"properties": {
				"inputs": [{
					"dataType": "record",
					"isConfigurationParameter": null
				}],
				"output": {
					"dataType": "any"
				},
				"binding": {
					"type": "Microsoft.StreamAnalytics/JavascriptUdf",
					"properties": {
						"script": "function main(record) {\n\n    let flatRecord = {\n        '__deviceid': record.__deviceid,\n        '__ruleid': record.__ruleid\n    };\n\n    record.measurements.forEach(function (item) {\n        if (item.hasOwnProperty('measurementvalue')) {\n            flatRecord[item.measurementname] = item.measurementvalue;\n        }\n        else {\n            flatRecord[item.measurementname] = {\n                'avg': item.avg,\n                'max': item.max,\n                'min': item.min,\n                'count': item.count\n            };\n        }\n    });\n\n    return flatRecord;\n}"
					}
				}
			}
		}
	},
    {
		"name": "removeUnusedProperties",
		"type": "Microsoft.StreamAnalytics/streamingjobs/functions",
		"properties": {
			"type": "Scalar",
			"properties": {
				"inputs": [{
					"dataType": "record",
					"isConfigurationParameter": null
				}],
				"output": {
					"dataType": "any"
				},
				"binding": {
					"type": "Microsoft.StreamAnalytics/JavascriptUdf",
					"properties": {
						"script": "function main(record) {\n    if (record) {\n        record.IoTHub && delete record.IoTHub;\n        record.PartitionId && delete record.PartitionId;\n        record.EventEnqueuedUtcTime && delete record.EventEnqueuedUtcTime;\n        record.EventProcessedUtcTime && delete record.EventProcessedUtcTime;\n    }\n    return record;\n}"
					}
				}
			}
		}
	}
  ]
}
"@

$containerProperties = @{
    "resource" = @{
        "id"= "alarms-$($tenantId)";
        "partitionKey" = @{
            "paths" = @("/_partitionKey");
            "kind" = "Hash"
        };
        "indexingPolicy" = @{
            "indexingMode" = "consistent";
            "includedPaths" = @(@{
                "path" = "/*";
                "indexes" = @(@{
                        "kind" = "Range";
                        "dataType" = "number";
                        "precision" = -1
                    },
                    @{
                        "kind" = "Range";
                        "dataType" = "string";
                        "precision" = -1
                    },
                    @{
                        "kind" = "Spatial";
                        "dataType" = "Point";
                    }
                )
            });
        };
        "conflictResolutionPolicy" = @{
            "mode" = "lastWriterWins";
            "conflictResolutionPath" = "/_ts"
        }
    };
    "options"=@{ "Throughput"= 400 }
}

# create the Blob for the new Tenant
function createBlobforTenant($tenantId){
    New-AzRmStorageContainer -StorageAccountName $storageAccountName `
                             -ResourceGroupName $resourceGroup `
                             -Name $tenantId `
                             -PublicAccess None
    
    Write-Output "Storage container created for tenant $tenantId"    
}

# create consumer group to the built in endpoint events in iothub
function addConsumerGroup($iotHubName){
    Add-AzIotHubEventHubConsumerGroup -ResourceGroupName $resourceGroup `
                                      -Name $iotHubName `
                                      -EventHubConsumerGroupName "sajobconsumergroup"

    Write-Output "Consumer Group added to the Iothub $iotHubName"
}

# create cosmos collection 
function createCosmosCollection($tenantId){
    #$databaseResourceType = "Microsoft.DocumentDb/databaseAccounts/apis/databases"
    #$databaseResourceName = $databaseAcctName + "/sql/" + $cosmosdbName
    $containerResourceType = "Microsoft.DocumentDb/databaseAccounts/apis/databases/containers"
    $containerName = "alarms-$tenantId"
    $apiVersion = "2015-04-08"
    $containerResourceName = $cosmosDbAccountName + "/sql/" + $cosmosDbDatabaseId + "/" + $containerName
    New-AzResource -ResourceType $containerResourceType `
                   -ApiVersion $apiVersion `
                   -ResourceGroupName $resourceGroup `
                   -Name $containerResourceName `
                   -PropertyObject $containerProperties -Force
    Write-Output "Collection $containerName created for Tenant $tenantId"
}

# get the current path
$currPath = (Get-Item -Path ".\").FullName

# Create SA Job
function createSAJob($saJobName){
    $SAJobFileName = "SAJobDefinition.json"
    $SAJobDefinitionFilePath = Join-Path $currPath $SAJobFileName
    $SAjobDefinition | Out-File $SAJobDefinitionFilePath

    New-AzStreamAnalyticsJob -ResourceGroupName $resourceGroup `
                             -Name $saJobName `
                             -File $SAJobDefinitionFilePath -Force
    Write-Output "SA Job $saJobName created successfully."
}

# Add inputs to SA Job
function addSAJobInputs($saJobName) {
    $inputPSObj = $SAJobInputDefinition | ConvertFrom-Json 
    foreach ($name in $inputPSObj.value.name) { 
        $JsonData = ($inputPSObj.value | Where-Object {$_.name -eq "$($name)" }) | ConvertTo-Json -Depth 12
        $SAJobInputFileName = "$($name).json" 
        $SAJobInputFilePath = Join-Path $currPath $SAJobInputFileName
        $JsonData | Out-File $SAJobInputFilePath
        New-AzStreamAnalyticsInput -ResourceGroupName $resourceGroup `
                                   -JobName $saJobName `
                                   -File $SAJobInputFilePath `
                                   -Name "$($name)" -Force
        Write-Output "Input $name added successfully."
       }
}

# Add outouts to SA Job
function addSAJobOutputs($saJobName) {
    $outputPSObj = $SAJobOutputDefinition | ConvertFrom-Json
    foreach ($name in $outputPSObj.value.name) {
        $JsonData = ($outputPSObj.value | Where-Object {$_.name -eq "$($name)" }) | ConvertTo-Json -Depth 12
        $SAJobOutputFileName = "$($name).json" 
        $SAJobOutputFilePath = Join-Path $currPath $SAJobOutputFileName
        $JsonData | Out-File $SAJobOutputFilePath
        New-AzStreamAnalyticsOutput -ResourceGroupName $resourceGroup `
                                    -JobName $saJobName `
                                    -File $SAJobOutputFilePath `
                                    -Name "$($name)" -Force
        Write-Output "Output $name added successfully."                              
        }
}
        
# Add the Query to SA Job
function addSAJobQuery($saJobName){
    $SAJobQueryFileName = "SAJobQuery.json"
    $SAJobQueryFilePath = Join-Path $currPath $SAJobQueryFileName
    $SAJobQuery | Out-File $SAJobQueryFilePath
    New-AzStreamAnalyticsTransformation -ResourceGroupName $resourceGroup `
                                        -JobName $saJobName `
                                        -File $SAJobQueryFilePath `
                                        -Name "SAQuery" -Force
    Write-Output "Query added successfully."
}

# add User Defined Functions to the SA Job
function addSAfunctions($saJobName){
    $functionPSObj = $SAJobFunction | ConvertFrom-Json
    foreach ($name in $functionPSObj.value.name) {
        $JsonData = ($functionPSObj.value | Where-Object {$_.name -eq "$($name)" }) | ConvertTo-Json -Depth 12
        $SAJobFunctionFileName = "$($name).json"
        $SAJobFunctionFilePath = Join-Path $currPath $SAJobFunctionFileName
        $JsonData | Out-File $SAJobFunctionFilePath 
        New-AzStreamAnalyticsFunction -ResourceGroupName $resourceGroup `
                                      -JobName $saJobName `
                                      -File $SAJobFunctionFilePath `
                                      -Name "$($name)" -Force
        Write-Output "Function $name added successfully."
    }
}
# update the column SAJobName in table storage
function updateUserTenantTtable($tenantId){
    $tableName = "tenant"
    $table = Get-AzTableTable -resourceGroup $resourceGroup `
                              -tableName $tableName `
                              -storageAccountName $storageAccountName
    $row = Get-AzTableRowByPartitionKeyRowKey -Table $table -PartitionKey $tenantId[0] -RowKey $tenantId
    # check if the column exists
    $ifexists = ($row | Get-Member -Name "SAJobName")
    if ([string]::IsNullOrEmpty($ifexists)){
        $row | Add-Member -Name "SAJobName" -Value $SAJobName -Type NoteProperty
    }
    else {
        $row.SAJobName = $SAJobName
    }
    $row | Update-AzTableRow -Table $table
    Write-Output "SAJob Name $SAJobName updated for tenant $tenantId in tenant table"
}

# call the functions 
try {
    createBlobforTenant -tenantId $tenantId
    createCosmosCollection -tenantId $tenantId
    addConsumerGroup -iotHubName $iotHubName
    createSAJob -saJobName $saJobName
    addSAJobInputs -saJobName $saJobName
    addSAJobOutputs -saJobName $saJobName
    addSAJobQuery -saJobName $saJobName
    addSAfunctions -saJobName $saJobName
    updateUserTenantTtable -tenantId $tenantId
    # Start the SA job if required
    #Start-AzStreamAnalyticsJob -Name $saJobName -ResourceGroupName $resourceGroup
    Write-Output "Finished provisioning Stream Analytics Job for the Tenant $tenantId"
}
catch {
    Write-Error -Message $_.Exception
    throw $_.Exception
}