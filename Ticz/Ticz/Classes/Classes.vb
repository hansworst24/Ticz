
Imports Microsoft.Xaml.Interactivity
Imports System.Threading
Imports Windows.Web.Http
Imports Windows.Web.Http.Filters
Imports Newtonsoft.Json
Imports GalaSoft.MvvmLight
Imports Windows.Storage.Streams
Imports System.Xml.Serialization

Public Class domoResponse
    Public Property message As String
    Public Property status As String
    Public Property title As String
End Class

Public Class retvalue
    Public Property issuccess As Boolean
    Public Property err As String
End Class

Public NotInheritable Class Constants
    Public Const VISIBLE As String = "Visible"
    Public Const COLLAPSED As String = "Collapsed"
    Public Const WIDE As String = "Wide"
    Public Const ICON As String = "Icon"
    Public Const LARGE As String = "Large"
    Public Const [OFF] As String = "Off"
    Public Const [ON] As String = "On"
    Public Const OPEN As String = "Open"
    Public Const CLOSED As String = "Closed"
    Public Const ICONVIEW As String = "Icon View"
    Public Const GRIDVIEW As String = "Grid View"
    Public Const LISTVIEW As String = "List View"
    Public Const RESIZEVIEW As String = "Resize View"
    Public Const DASHVIEW As String = "Dashboard View"
End Class


''' <summary>
''' TiczStorage contains Ticz settings and configurations that are stored on Storage
''' </summary>
Public NotInheritable Class TiczStorage
    Public Class RoomConfigurations
        Inherits List(Of RoomConfiguration)

        Public Async Function LoadRoomConfigurations() As Task
            Me.Clear()
            Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim storageFile As Windows.Storage.StorageFile
            Dim fileExists As Boolean = True
            Try
                storageFile = Await storageFolder.GetFileAsync("ticzconfig.xml")
            Catch ex As Exception
                fileExists = False
                Return
            End Try
            Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read)
            Dim sessionInputStream As IInputStream = stream.GetInputStreamAt(0)
            Dim serializer = New XmlSerializer((New TiczStorage.RoomConfigurations).GetType())
            Dim stuffToLoad As TiczStorage.RoomConfigurations
            Try
                stuffToLoad = serializer.Deserialize(sessionInputStream.AsStreamForRead())
            Catch ex As Exception
                'Casting the contents of the file to a RoomConfigurations object failed. Potentially the file is empty or malformed. Return a new object
                stuffToLoad = New RoomConfigurations
            End Try

            stream.Dispose()
            For Each s In stuffToLoad
                Me.Add(s)
            Next

            For Each r In TiczViewModel.DomoRooms.result
                Dim retreivedRoomConfig = (From configs In Me Where configs.RoomIDX = r.idx And configs.RoomName = r.Name Select configs).FirstOrDefault()
                If retreivedRoomConfig Is Nothing Then
                    Dim c = New RoomConfiguration With {.RoomIDX = r.idx, .RoomName = r.Name, .RoomView = 4, .ShowRoom = True}
                    Me.Add(c)
                End If
            Next
        End Function

        Public Async Function SaveRoomConfigurations() As Task
            If TiczViewModel.DomoRooms.result.Any(Function(x) x.Name = "Ticz") Then
                'We are running in 'Debug mode', therefore we won't save the roomconfigurations
                Exit Function
            End If
            Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim storageFile As Windows.Storage.StorageFile = Await storageFolder.CreateFileAsync("ticzconfig.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)
            Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
            Dim sessionOutputStream As IOutputStream = stream.GetOutputStreamAt(0)

            Dim RoomConfigsToKeep As New List(Of TiczStorage.RoomConfiguration)


            For i As Integer = Me.Count - 1 To 0 Step -1
                Dim rconfig As TiczStorage.RoomConfiguration = Me(i)
                Dim domoroom As Domoticz.Plan = (From d In TiczViewModel.DomoRooms.result Where d.idx = rconfig.RoomIDX And d.Name = rconfig.RoomName Select d).FirstOrDefault()
                If domoroom Is Nothing Then Me.Remove(rconfig)
            Next

            Dim stuffToSave = Me
            Dim serializer As XmlSerializer = New XmlSerializer(stuffToSave.GetType())
            serializer.Serialize(sessionOutputStream.AsStreamForWrite(), stuffToSave)
            Await sessionOutputStream.FlushAsync()
            sessionOutputStream.Dispose()
            stream.Dispose()
        End Function

        Public Function GetRoomConfig(idx As Integer, name As String)
            Dim c = (From config In Me Where config.RoomIDX = idx And config.RoomName = name Select config).FirstOrDefault()
            If c Is Nothing Then
                c = New RoomConfiguration With {.RoomIDX = idx, .RoomName = name, .RoomView = Constants.ICONVIEW, .ShowRoom = True}
                Me.Add(c)
            End If
            If c.RoomName = "Ticz" Then c.RoomView = Constants.LISTVIEW
            Return c
        End Function

    End Class

    Public Class RoomConfiguration
        Inherits ViewModelBase
        Public Property RoomIDX As Integer
        Public Property RoomName As String
        Public Property ShowRoom As Boolean
        '    Get
        '        Return _ShowRoom
        '    End Get
        '    Set(value As Boolean)
        '        _ShowRoom = value
        '        RaisePropertyChanged()
        '    End Set
        'End Property
        'Private Property _ShowRoom As Boolean
        Public Property RoomView As String
        '    Get
        '        Return _RoomView
        '    End Get
        '    Set(value As String)
        '        _RoomView = value
        '        RaisePropertyChanged()
        '    End Set
        'End Property
        'Private Property _RoomView As String
        Public Property DeviceConfigurations As DeviceConfigurations

        Public Sub New()
            DeviceConfigurations = New DeviceConfigurations
        End Sub
    End Class


    Public Class DeviceConfiguration
        Public Property DeviceIDX As Integer
        Public Property DeviceName As String
        Public Property DeviceOrder As Integer
        Public Property DeviceRepresentation As String
        Public Property ColumnSpan As Integer
        Public Property RowSpan As Integer

        Public Sub New()
            ColumnSpan = 1
            RowSpan = 1
        End Sub
    End Class

    Public Class DeviceConfigurations
        Inherits ObservableCollection(Of DeviceConfiguration)

        Public Sub New()
        End Sub

        Public Function GetDeviceConfigurationForDevice(idx As Integer, name As String) As DeviceConfiguration
            Dim d As DeviceConfiguration = (From deviceconfigs In Me Where deviceconfigs.DeviceIDX = idx And deviceconfigs.DeviceName = name Select deviceconfigs).FirstOrDefault()
            If d Is Nothing Then
                d = New DeviceConfiguration With {.ColumnSpan = 1, .DeviceIDX = idx, .DeviceName = name, .RowSpan = 1, .DeviceRepresentation = "Icon", .DeviceOrder = Me.Count}
                Me.Add(d)
            End If
            Return d
        End Function

        Public Sub SortRoomDevices()
            Dim intIndex As Integer = 0
            For Each Device In Me.OrderBy(Function(x) x.DeviceOrder)
                Device.DeviceOrder = intIndex
                intIndex += 1
                WriteToDebug(Device.DeviceName, Device.DeviceOrder)
            Next
        End Sub


        Public Sub MoveUp(idx As Integer, name As String)
            Dim devToMove = (From d In Me Where d.DeviceIDX = idx And d.DeviceName = name Select d).FirstOrDefault()
            If Not devToMove Is Nothing Then
                Dim oldDevice = (From d In Me Where d.DeviceOrder = devToMove.DeviceOrder - 1 Select d).FirstOrDefault()
                If Not oldDevice Is Nothing Then
                    Dim oldDeviceIndex = oldDevice.DeviceOrder
                    oldDevice.DeviceOrder = devToMove.DeviceOrder
                    devToMove.DeviceOrder = oldDeviceIndex
                End If
                SortRoomDevices()
            End If
        End Sub

        Public Sub MoveDown(idx As Integer, name As String)
            Dim devToMove = (From d In Me Where d.DeviceIDX = idx And d.DeviceName = name Select d).FirstOrDefault()
            If Not devToMove Is Nothing Then
                Dim oldDevice = (From d In Me Where d.DeviceOrder = devToMove.DeviceOrder + 1 Select d).FirstOrDefault()
                If Not oldDevice Is Nothing Then
                    Dim oldDeviceIndex = oldDevice.DeviceOrder
                    oldDevice.DeviceOrder = devToMove.DeviceOrder
                    devToMove.DeviceOrder = oldDeviceIndex
                End If
                SortRoomDevices()
            End If
        End Sub

        Public Function RowSpan(idx As Integer, name As String)
            Dim dc = (From d In Me Where d.DeviceIDX = idx And d.DeviceName = name Select d).FirstOrDefault()
            If Not dc Is Nothing Then
                Return dc.RowSpan
            Else
                Return 1
            End If
        End Function

        Public Function ColumnSpan(idx As Integer, name As String)
            Dim dc = (From d In Me Where d.DeviceIDX = idx And d.DeviceName = name Select d).FirstOrDefault()
            If Not dc Is Nothing Then
                Return dc.ColumnSpan
            Else
                Return 1
            End If
        End Function
    End Class


End Class

Public NotInheritable Class Domoticz
    Public Class Plans
        Public Property result As ObservableCollection(Of Plan)
        Public Property status As String
        Public Property title As String

        Private app As App = CType(Application.Current, App)

        Public Sub New()
            result = New ObservableCollection(Of Plan)
        End Sub

        Public Async Function RemovePlan(p As Plan) As Task
            Await RunOnUIThread(Sub()
                                    result.Remove(p)
                                End Sub)
        End Function

        Public Async Function AddPlan(p As Plan) As Task
            Await RunOnUIThread(Sub()
                                    result.Add(p)
                                End Sub)
        End Function

        Public Async Function ClearPlans() As Task
            Await RunOnUIThread(Sub()
                                    result.Clear()
                                End Sub)
        End Function

        Public Async Function Load() As Task(Of retvalue)
            WriteToDebug("DomoPlans.Load", "executed")
            Dim response As HttpResponseMessage = Await Domoticz.DownloadJSON(DomoApi.getPlans)
            If response.IsSuccessStatusCode Then
                Dim body As String = Await response.Content.ReadAsStringAsync()
                Dim deserialized = JsonConvert.DeserializeObject(Of Plans)(body)
                Await ClearPlans()
                If deserialized.result.Any(Function(x) x.Name = "Ticz") Then
                    'We found a RoomPlan called 'Ticz' on the Domoticz Server. This is used for debugging purposes, and allows to add individual devices to the 'Ticz' Room to see how the Ticz App (this) is handling it
                    'We therefore skip loading any other rooms and will only load this room. We use a default RoomView for the Ticz Room as well, the ListView.
                    Dim DomoPlan As Domoticz.Plan = (From d In deserialized.result Where d.Name = "Ticz" Select d).FirstOrDefault()
                    Dim matchingRoomConfig As TiczStorage.RoomConfiguration = (From roomconfig In TiczViewModel.TiczRoomConfigs Where roomconfig.RoomIDX = DomoPlan.idx Select roomconfig).FirstOrDefault()
                    If matchingRoomConfig Is Nothing Then
                        matchingRoomConfig = New TiczStorage.RoomConfiguration With {.ShowRoom = True, .RoomIDX = DomoPlan.idx, .RoomName = DomoPlan.Name, .RoomView = Constants.LISTVIEW}
                        TiczViewModel.TiczRoomConfigs.Add(matchingRoomConfig)
                    Else
                        matchingRoomConfig.RoomView = Constants.LISTVIEW
                    End If
                    Await AddPlan(DomoPlan)
                Else
                    For Each r In deserialized.result
                        Dim matchingRoomConfig As TiczStorage.RoomConfiguration = (From roomconfig In TiczViewModel.TiczRoomConfigs Where roomconfig.RoomIDX = r.idx Select roomconfig).FirstOrDefault()
                        If matchingRoomConfig Is Nothing Then
                            matchingRoomConfig = New TiczStorage.RoomConfiguration With {.ShowRoom = True, .RoomIDX = r.idx, .RoomName = r.Name, .RoomView = Constants.ICONVIEW}
                            TiczViewModel.TiczRoomConfigs.Add(matchingRoomConfig)
                        End If
                        If matchingRoomConfig.ShowRoom Then
                            Await AddPlan(r)
                        End If
                    Next
                End If
                Me.status = deserialized.status
                Me.title = deserialized.status
                Return New retvalue With {.issuccess = True}
            Else
                WriteToDebug("Plans.Load()", response.ReasonPhrase)
                Await TiczViewModel.Notify.Update(True, response.ReasonPhrase)
                Return New retvalue With {.issuccess = False, .err = response.ReasonPhrase}
            End If

        End Function
    End Class
    Public Class Plan
        Public Property Devices As Integer
        Public Property Name As String
        Public Property Order As String
        Public Property idx As String
    End Class



    Dim app As App = CType(Application.Current, App)
    Dim vm As TiczViewModel = app.myViewModel




    Public Shared Async Function DownloadJSON(url As String) As Task(Of HttpResponseMessage)

        Using filter As New HttpBaseProtocolFilter
            If Not TiczViewModel.TiczSettings.Password = "" AndAlso Not TiczViewModel.TiczSettings.Username = "" Then
                filter.ServerCredential = New Windows.Security.Credentials.PasswordCredential With {.Password = TiczViewModel.TiczSettings.Password, .UserName = TiczViewModel.TiczSettings.Username}
            End If
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.Default
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache
            filter.AllowUI = False
            filter.UseProxy = False
            Using wc As New HttpClient(filter)
                Dim cts As New CancellationTokenSource(5000)
                Try
                    WriteToDebug("Downloader.DownloadJSON", url)
                    Dim response As HttpResponseMessage = Await wc.GetAsync(New Uri(url)).AsTask(cts.Token)
                    Return response
                Catch ex As TaskCanceledException
                    Return New HttpResponseMessage With {.ReasonPhrase = "Connection timed out", .StatusCode = HttpStatusCode.RequestTimeout}
                Catch ex As Exception
                    WriteToDebug("Downloader.DownloadJSON", ex.Message.ToString)
                    Return New HttpResponseMessage With {.ReasonPhrase = ex.Message, .StatusCode = HttpStatusCode.Unauthorized}
                End Try
            End Using
        End Using
    End Function

End Class

Public Class MyTestGrid
    Inherits GridView
End Class


Public Class VariableGrid
    Inherits GridView

    Protected Overrides Sub PrepareContainerForItemOverride(element As DependencyObject, item As Object)
        MyBase.PrepareContainerForItemOverride(element, item)
        Dim tile = TryCast(item, Device)
        If Not tile Is Nothing Then
            Dim griditem = TryCast(element, GridViewItem)
            If Not griditem Is Nothing Then
                VariableSizedWrapGrid.SetColumnSpan(griditem, tile.ColumnSpan)
                VariableSizedWrapGrid.SetRowSpan(griditem, tile.RowSpan)
            End If
        End If
    End Sub
    'PrepareContainerForItemOverride(element, item)
End Class

Public Class OpenMenuFlyoutAction
    Inherits DependencyObject
    Implements IAction
    Private Function IAction_Execute(sender As Object, parameter As Object) As Object Implements IAction.Execute
        Dim senderElement As FrameworkElement = TryCast(sender, FrameworkElement)
        Dim flyoutBase__1 As FlyoutBase = FlyoutBase.GetAttachedFlyout(senderElement)

        flyoutBase__1.ShowAt(senderElement)

        Return Nothing
    End Function
End Class

Public NotInheritable Class DomoApi
    'Switch Command On/Off with passcode
    'http://{0}:{1}/json.htm?type=command&param=switchlight&idx=95&switchcmd=Off&level=0&passcode=234 
    'returns when wrong code
    '    {
    '   "message" : "WRONG CODE",
    '   "status" : "ERROR",
    '   "title" : "SwitchLight"
    '}

    Public Shared Function getAllDevicesForRoom(roomIDX As String)
        'Using order=Name, ensures that the devices are returned in the order in which they are set in the WebUI
        Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true&order=Name&plan={2}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, roomIDX)
    End Function

    Public Shared Function getAllDevices() As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
    End Function

    Public Shared Function getAllScenes() As String
        Return String.Format("http://{0}:{1}/json.htm?type=scenes", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
    End Function


    Public Shared Function getDeviceStatus(idx As String) As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&rid={2}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, idx)
    End Function

    Public Shared Function setDimmer(idx As String, switchstate As String, Optional passcode As String = "") As String
        Dim switchstring As String
        If Not switchstate = "On" Then switchstring = "Set%20Level&level=" Else switchstring = ""
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}{4}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, idx, switchstring, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}{4}&passcode={5}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, idx, switchstring, switchstate, passcode)
        End If

    End Function


    Public Shared Function SwitchScene(idx As String, switchstate As String, Optional passcode As String = "") As String
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchscene&idx={2}&switchcmd={3}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, idx, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchscene&idx={2}&switchcmd={3}&passcode={4}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, idx, switchstate, passcode)
        End If

    End Function

    Public Shared Function SwitchLight(idx As String, switchstate As String, Optional passcode As String = "") As String
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, idx, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}&passcode={4}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, idx, switchstate, passcode)
        End If

    End Function

    Public Shared Function getPlans() As String
        Return String.Format("http://{0}:{1}/json.htm?type=plans", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
    End Function
End Class



