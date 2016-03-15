'Returned JSON result from Domoticz for a single device. Domoticz only returns, apart from a standard set of properties, only those properties relevant for the specific device
'Therefore it could be that some devices return properties that aren't included here yet. These will have to be added when people encounter them (i.e. missing status or values)
Public Class DeviceModel
    Public Property AddjMulti As Double
    Public Property AddjMulti2 As Double
    Public Property AddjValue As Double
    Public Property AddjValue2 As Double
    Public Property Barometer As String
    Public Property BatteryLevel As Integer
    Public Property CameraIdx As String
    Public Property CameraIdz As Integer
    Public Property Chill As String
    Public Property Counter As String
    Public Property CounterDeliv As String
    Public Property CounterDelivToday As String
    Public Property CounterToday As String
    Public Property CustomImage As Integer
    Public Property Data As String
    Public Property Description As String
    Public Property DewPoint As String
    Public Property Direction As String
    Public Property DirectionStr As String
    Public Property Favorite As Integer
    Public Property Gust As String
    Public Property HardwareID As Integer
    Public Property HardwareName As String
    Public Property HardwareType As String
    Public Property HardwareTypeVal As Integer
    Public Property HaveDimmer As Boolean
    Public Property HaveGroupCmd As Boolean
    Public Property HaveTimeout As Boolean
    Public Property Humidity As String
    Public Property HumidityStatus As String
    Public Property ID As String
    Public Property idx As String
    Public Property Image As String
    Public Property IsSubDevice As Boolean
    Public Property LastUpdate As String
    Public Property Level As Integer
    Public Property LevelInt As Integer
    Public Property LevelNames As String
    Public Property MaxDimLevel As Integer
    Public Property Name As String
    Public Property Notifications As String
    Public Property PlanID As String
    Public Property PlanIDs As List(Of Integer)
    Public Property [Protected] As Boolean
    Public Property Rain As String
    Public Property RainRate As String
    Public Property ShowNotifications As Boolean
    Public Property SignalLevel As String
    Public Property Speed As String
    Public Property Status As String
    Public Property StrParam1 As String
    Public Property StrParam2 As String
    Public Property SubType As String
    Public Property SwitchType As String
    Public Property SwitchTypeVal As Integer
    Public Property Temp As String
    Public Property Timers As String
    Public Property Type As String
    Public Property TypeImg As String
    Public Property Unit As Integer
    Public Property Usage As String
    Public Property UsageDeliv As String
    Public Property Used As Integer
    Public Property UsedByCamera As Boolean
    Public Property XOffset As String
    Public Property YOffset As String


End Class
