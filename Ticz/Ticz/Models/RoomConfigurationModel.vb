'Class which contains the configuration for each Room. The configuration contains the view that has been selected for the room, as well as a list of device IDX's and their associated View


Public Class RoomConfigurationModel
    Public ReadOnly Property RoomViewChoices As List(Of String)
        Get
            Return New List(Of String)({Constants.ROOMVIEW.ICONVIEW, Constants.ROOMVIEW.GRIDVIEW, Constants.ROOMVIEW.LISTVIEW,
                                                            Constants.ROOMVIEW.RESIZEVIEW, Constants.ROOMVIEW.DASHVIEW}).ToList
        End Get
    End Property

    Public Property RoomIDX As Integer
    Public Property RoomName As String
    Public Property ShowRoom As Boolean
    Public Property RoomView As String
        Get
            Return _RoomView
        End Get
        Set(value As String)
            If _RoomView <> value And value <> "" Then
                _RoomView = value
                'RaisePropertyChanged("RoomView")
            End If
        End Set
    End Property
    Private Property _RoomView As String
    Public Property DeviceConfigurations As TiczStorage.DeviceConfigurations

    Public Sub New()
        DeviceConfigurations = New TiczStorage.DeviceConfigurations
        RoomView = "Icon View"
    End Sub
End Class
