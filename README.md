# ABB Robot Test Software

This project is a test software for communicating with ABB robots. It establishes a connection with the robot controller, enabling motion control, RAPID data read/write, and IO signal management.

## Features
- **Connection Management:** Establish and disconnect from the robot controller.
- **Motion Control:** Read robot position data and adjust motion parameters.
- **RAPID Data Read/Write:** Access and modify RAPID program variables.
- **IO Control:** Read and write digital input/output signals.
- **Program Control:** Start, stop, abort robot programs, and retrieve program position information.

## Usage
### Connecting to the Controller
```csharp
ABB_interface robot = new ABB_interface();
robot.Connect();
if (robot.IsConnected)
{
    Console.WriteLine("Connection successful.");
}
```

### Retrieving Robot Position Data
```csharp
var jointPosition = robot.GetPosJ();
var cartesianPosition = robot.GetPosX();
```

### Reading/Writing RAPID Variables
```csharp
RDItem rapidData = new RDItem { Name = "MyRapidVar" };
robot.ReadNumData(rapidData);
Console.WriteLine($"Value: {rapidData.Value}");

rapidData.Value = "100";
robot.WriteNumData(rapidData);
```

### Managing IO Signals
```csharp
RDItem ioSignal = new RDItem { Name = "do_Start" };
ioSignal.IsON = true;
robot.WriteIOData(ioSignal);
```

### Starting/Stopping a Program
```csharp
robot.Start();
robot.Stop();
robot.Abort();
```

## Requirements
To use this project, you need the following dependencies:
- [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)  
- [ABB PC SDK](https://developercenter.robotstudio.com/api/pcsdk/)  

## License
This project is licensed under the MIT License.

