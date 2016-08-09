
Imports GalaSoft.MvvmLight
Imports Windows.UI


Public Class DeviceProperty
    Public Property Key As String
    Public Property Value As String
End Class


'Public Class DeviceGroup(Of T)
'    Inherits ObservableCollection(Of DevicesViewModel)

'    Public Sub New()
'    End Sub


'End Class










Public Class TiczMenuSettings
    Inherits ViewModelBase

    Private app As Application = CType(Xaml.Application.Current, Application)

    Public Sub New()
        ActiveMenuContents = "Rooms"
    End Sub


    Public ReadOnly Property BackButtonVisibility As String
        Get
            Return If(Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"), Constants.COLLAPSED, Constants.VISIBLE)
        End Get
    End Property

    Public Property IsMenuOpen As Boolean
        Get
            Return _IsMenuOpen
        End Get
        Set(value As Boolean)
            _IsMenuOpen = value
            RaisePropertyChanged("IsMenuOpen")
        End Set
    End Property
    Private Property _IsMenuOpen As Boolean

    Public Property ActiveMenuContents As String
        Get
            Return _ActiveMenuContents
        End Get
        Set(value As String)
            _ActiveMenuContents = value
            RaisePropertyChanged("ActiveMenuContents")
        End Set
    End Property
    Private Property _ActiveMenuContents As String


    Public Sub MenuSwitch()
        If Not IsMenuOpen Then ActiveMenuContents = "Rooms"
        IsMenuOpen = Not IsMenuOpen
    End Sub

    Public Sub ShowRoomSettingsMenu()
        ActiveMenuContents = "Rooms Configuration"
    End Sub

    Public Sub ShowServerSettingsMenu()
        ActiveMenuContents = "Server settings"
    End Sub

    Public Sub ShowGeneralSettingsMenu()
        ActiveMenuContents = "General"
    End Sub


    Public Sub ShowSettings()
        ActiveMenuContents = "Settings"
    End Sub

    Public Async Sub MenuGoBack()
        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
        If IsMenuOpen And ActiveMenuContents = "Rooms" Then
            IsMenuOpen = False
        ElseIf IsMenuOpen And ActiveMenuContents = "Rooms Configuration" Then
            Await vm.Rooms.SaveRoomConfigurations()
            Await vm.Rooms.LoadRoomConfigurations()
            ActiveMenuContents = "Settings"
        ElseIf IsMenuOpen And ActiveMenuContents = "General" Then
            ActiveMenuContents = "Settings"
        ElseIf IsMenuOpen And ActiveMenuContents = "Server settings" Then
            ActiveMenuContents = "Settings"
        ElseIf IsMenuOpen And ActiveMenuContents = "Settings" Then
            ActiveMenuContents = "Rooms"
        End If
    End Sub



End Class

Public Class GraphListViewModel
    Inherits ViewModelBase
    Implements IDisposable

    Public Property graphDataList As ObservableCollection(Of Domoticz.DeviceGraphContainer)
    Public Property deviceName As String
        Get
            Return _deviceName
        End Get
        Set(value As String)
            _deviceName = value
            RaisePropertyChanged("deviceName")
        End Set
    End Property
    Private Property _deviceName As String
    Public Property deviceIDX As Integer
    Public Property deviceType As String
    Public Property deviceSubType As String

    Public Sub New()
        graphDataList = New ObservableCollection(Of Domoticz.DeviceGraphContainer)
    End Sub

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
                If Not graphDataList Is Nothing Then
                    For Each g In graphDataList
                        g.Dispose()
                    Next
                    'graphDataList.Clear()
                    graphDataList = Nothing
                End If
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class




