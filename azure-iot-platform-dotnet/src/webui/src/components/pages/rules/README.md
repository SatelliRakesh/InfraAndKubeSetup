# Creating A Rule

## Conditions

### Fields
Fields options are serialized from two different IoT Hub entities: devices, and edge modules.

#### Devices
Device field options are read from the device's reported properties. In order to utilize telemetry fields for your alerting rules, you'll need to setup your device to have a specific reported property: telemetry. Your device should have data in the following format:
```
{
  ...
  "properties": {
    "desired": {
      ...
    },
    "reported": {
      ...
      "telemetry": {
        "messageSchema": {
          "fields" {
            "exampleField": "Text",
            "exampleField2": "Double"
          }
        }
      }
    }
  }
}
```
This includes the reported property object **telemetry**, which includes a **messageSchema** object, which contains a **fields** object, which, finally, contains the necessary data that will populate the Field options for a rule condition. With this example, the Field options available for creating a rule would be: exampleField, and exampleField2. 