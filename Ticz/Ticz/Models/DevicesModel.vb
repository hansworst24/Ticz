'Returned JSON result from Domoticz for a list of devices
Public Class DevicesModel
    Public Property result As List(Of DeviceModel)
    Public Property status As String
    Public Property title As String

    Public Sub New()
        result = New List(Of DeviceModel)
    End Sub
End Class
