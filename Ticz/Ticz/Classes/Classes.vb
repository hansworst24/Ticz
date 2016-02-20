
Imports Microsoft.Xaml.Interactivity
Imports System.Threading
Imports Windows.Web.Http
Imports Windows.Web.Http.Filters
Imports Newtonsoft.Json
Imports GalaSoft.MvvmLight
Imports Windows.Storage.Streams
Imports System.Xml.Serialization
Imports GalaSoft.MvvmLight.Command

Public Class domoVersion
    Inherits ViewModelBase
    Public Property build_time As String
    Public Property hash As String
    Public Property haveupdate As Boolean
    Public Property revision As Integer
    Public Property status As String
    Public Property title As String
    Public Property version As String
        Get
            Return _version
        End Get
        Set(value As String)
            _version = value
            RaisePropertyChanged("version")
        End Set
    End Property
    Private Property _version As String

    Public Async Function Load() As Task(Of retvalue)
        Dim ret As New retvalue
        Dim url As String = DomoApi.getVersion()
        Dim response As HttpResponseMessage = Await Domoticz.DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim config As domoVersion
            Try
                config = JsonConvert.DeserializeObject(Of domoVersion)(body)
                version = config.version
                build_time = config.build_time
                ret.issuccess = True
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing config"
            End Try
        Else
            Await TiczViewModel.Notify.Update(True, String.Format("Error loading Domoticz version information ({0})", response.ReasonPhrase), 0)
        End If
        Return ret
    End Function

End Class

Public Class domoConfig
    Public Property TempScale As Double
    Public Property TempSign As String
    Public Property WindScale As Double
    Public Property WindSign As String

    Public Async Function Load() As Task(Of retvalue)
        Dim ret As New retvalue
        Dim url As String = DomoApi.getConfig()
        Dim response As HttpResponseMessage = Await Domoticz.DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim config As domoConfig
            Try
                config = JsonConvert.DeserializeObject(Of domoConfig)(body)
                TempScale = config.TempScale
                TempSign = config.TempSign
                WindScale = config.WindScale
                WindSign = config.WindSign
                ret.issuccess = True
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing config"
            End Try
        Else
            ret.issuccess = False : ret.err = response.ReasonPhrase
        End If
        If Not ret.issuccess Then Await TiczViewModel.Notify.Update(True, String.Format("Error loading Domoticz Config ({0})", ret.err), 0)
        Return ret
    End Function
End Class

Public Class domoSunRiseSet
    Inherits ViewModelBase
    Public Property Sunrise As String
        Get
            Return _Sunrise
        End Get
        Set(value As String)
            _Sunrise = value
            RaisePropertyChanged("Sunrise")
        End Set
    End Property
    Private Property _Sunrise As String
    Public Property Sunset As String
        Get
            Return _Sunset
        End Get
        Set(value As String)
            _Sunset = value
            RaisePropertyChanged("Sunset")
        End Set
    End Property
    Private Property _Sunset As String

    Public Async Function Load() As Task(Of retvalue)
        Dim ret As New retvalue
        Dim url As String = DomoApi.getSunRiseSet()
        Dim response As HttpResponseMessage = Await Domoticz.DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim config As domoSunRiseSet
            Try
                config = JsonConvert.DeserializeObject(Of domoSunRiseSet)(body)
                Await RunOnUIThread(Sub()
                                        Sunrise = config.Sunrise
                                        Sunset = config.Sunset

                                    End Sub)
                ret.issuccess = True
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing config"
            End Try
        Else
            ret.issuccess = False : ret.err = response.ReasonPhrase
        End If
        If Not ret.issuccess Then Await TiczViewModel.Notify.Update(True, String.Format("Error loading Sunrise / Sunset info ({0})", ret.err), 0)
        Return ret
    End Function

End Class

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

    'constants for Device Types
    Public Const LIGHTING_LIMITLESS As String = "Lighting Limitless/Applamp"
    Public Const TEMP As String = "Temp"
    Public Const THERMOSTAT As String = "Thermostat"
    Public Const TEMP_HUMI_BARO As String = "Temp + Humidity + Baro"
    Public Const LIGHTING_2 As String = "Lighting 2"
    Public Const LIGHT_SWITCH As String = "Light/Switch"
    Public Const GROUP As String = "Group"
    Public Const SCENE As String = "Scene"
    Public Const WIND As String = "Wind"
    Public Const GENERAL As String = "General"
    Public Const USAGE As String = "Usage"
    Public Const P1_SMART_METER As String = "P1 Smart Meter"
    Public Const UV As String = "UV"
    Public Const TYPE_RAIN As String = "Rain"

    'Constants for Device SubTypes
    Public Const P1_GAS As String = "Gas"
    Public Const P1_ELECTRIC As String = "Energy"


    'Constants for Device SwitchTypes
    Public Const BLINDS As String = "Blinds"
    Public Const BLINDS_INVERTED As String = "Blinds Inverted"
    Public Const BLINDS_PERCENTAGE As String = "Blinds Percentage"
    Public Const BLINDS_PERCENTAGE_INVERTED As String = "Blinds Percentage Inverted"
    Public Const CONTACT As String = "Contact"
    Public Const DIMMER As String = "Dimmer"
    Public Const DOOR_LOCK As String = "Door Lock"
    Public Const DOORBELL As String = "Doorbell"
    Public Const DUSK_SENSOR As String = "Dusk Sensor"
    Public Const MEDIA_PLAYER As String = "Media Player"
    Public Const MOTION_SENSOR As String = "Motion Sensor"
    Public Const ON_OFF As String = "On/Off"
    Public Const PUSH_ON_BUTTON As String = "Push On Button"
    Public Const PUSH_OFF_BUTTON As String = "Push Off Button"
    Public Const SELECTOR As String = "Selector"
    Public Const SMOKE_DETECTOR As String = "Smoke Detector"
    Public Const VEN_BLINDS_EU As String = "Venetian Blinds EU"
    Public Const VEN_BLINDS_US As String = "Venetian Blinds US"
    Public Const X10_SIREN As String = "X10 Siren"
    'Public Const GENERAL As String = "General"

    'Constants for Group Names
    Public Const GRP_GROUPS_SCENES As String = "Groups / Scenes"
    Public Const GRP_LIGHTS_SWITCHES As String = "Lights / Switches"
    Public Const GRP_WEATHER As String = "Weather Sensors"
    Public Const GRP_TEMPERATURE As String = "Temperature Sensors"
    Public Const GRP_UTILITY As String = "Utility Sensors"
    Public Const GRP_OTHER As String = "Other Devices"


End Class






''' <summary>
''' TiczStorage contains Ticz settings and configurations that are stored on Storage
''' </summary>
Public NotInheritable Class TiczStorage
    Public Class RoomConfigurations
        Inherits ObservableCollection(Of RoomConfiguration)

        Public Async Function LoadRoomConfigurations() As Task(Of Boolean)
            WriteToDebug("RoomsConfigurations.LoadRoomConfigurations()", "start")
            Me.Clear()
            Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim storageFile As Windows.Storage.StorageFile
            Dim fileExists As Boolean = True
            Dim stuffToLoad As New TiczStorage.RoomConfigurations
            Try
                storageFile = Await storageFolder.GetFileAsync("ticzconfig.xml")
            Catch ex As Exception
                fileExists = False
                TiczViewModel.Notify.Update(False, String.Format("No configuration file present. We will create a new one"))
            End Try
            If fileExists Then
                Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read)
                Dim sessionInputStream As IInputStream = stream.GetInputStreamAt(0)
                Dim serializer = New XmlSerializer((New TiczStorage.RoomConfigurations).GetType())
                Try
                    stuffToLoad = serializer.Deserialize(sessionInputStream.AsStreamForRead())
                Catch ex As Exception
                    'Casting the contents of the file to a RoomConfigurations object failed. Potentially the file is empty or malformed. Return a new object
                    TiczViewModel.Notify.Update(True, String.Format("Config file seems corrupt. We created a new one : {0}", ex.Message))
                End Try
                stream.Dispose()
            End If

            TiczViewModel.EnabledRooms.Clear()

            For Each r In TiczViewModel.DomoRooms.result.OrderBy(Function(x) x.Order)
                Dim retreivedRoomConfig = (From configs In stuffToLoad Where configs.RoomIDX = r.idx And configs.RoomName = r.Name Select configs).FirstOrDefault()
                If retreivedRoomConfig Is Nothing Then
                    retreivedRoomConfig = New RoomConfiguration With {.RoomIDX = r.idx, .RoomName = r.Name, .RoomView = Constants.ICONVIEW, .ShowRoom = True}
                End If
                Me.Add(retreivedRoomConfig)
                If retreivedRoomConfig.ShowRoom Then TiczViewModel.EnabledRooms.Add(retreivedRoomConfig)
            Next
            WriteToDebug("RoomsConfigurations.LoadRoomConfigurations()", "end")
            Return True
        End Function

        Public Async Function SaveRoomConfigurations() As Task
            WriteToDebug("RoomsConfigurations.SaveRoomConfigurations()", "start")
            If TiczViewModel.DomoRooms.result.Any(Function(x) x.Name = "Ticz") Then
                'We are running in 'Debug mode', therefore we won't save the roomconfigurations
                Exit Function
            End If
            Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim storageFile As Windows.Storage.StorageFile = Await storageFolder.CreateFileAsync("ticzconfig.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)
            Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
            Dim sessionOutputStream As IOutputStream = stream.GetOutputStreamAt(0)

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
            WriteToDebug("RoomsConfigurations.SaveRoomConfigurations()", "end")

        End Function

        Public Function GetRoomConfig(idx As Integer, name As String) As RoomConfiguration
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
        Public Property RoomView As String
            Get
                Return _RoomView
            End Get
            Set(value As String)
                _RoomView = value
                RaisePropertyChanged("RoomView")
                'WriteToDebug("--------------Roomview----------------", _RoomView)
            End Set
        End Property
        Private Property _RoomView As String
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
            Me.result.Clear()
            Dim response As HttpResponseMessage = Await Domoticz.DownloadJSON(DomoApi.getPlans)
            If response.IsSuccessStatusCode Then
                Dim body As String = Await response.Content.ReadAsStringAsync()
                Dim deserialized = JsonConvert.DeserializeObject(Of Plans)(body)
                Await ClearPlans()
                For Each p In deserialized.result.OrderBy(Function(x) x.Order)
                    Me.result.Add(p)
                Next

                If TiczViewModel.TiczSettings.ShowAllDevices Then Me.result.Insert(0, New Plan With {.idx = 12321, .Name = "All Devices", .Order = 0})

                'Re-order the Plans
                For i As Integer = 0 To Me.result.Count - 1 Step 1
                    Me.result(i).Order = i
                Next
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

    Public Shared Function getAllDevicesForRoom(roomIDX As String, Optional LoadAllUpdates As Boolean = False)
        'By sending a lastupdate parameter with a unix epoch number, we'll only get the updated devices since that epoch
        WriteToDebug("DomoApi", TimeToUnixSeconds(TiczViewModel.LastRefresh).ToString)
        If LoadAllUpdates Then
            Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true&order=Name&plan={2}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, roomIDX)
        Else
            Dim lastUpdateEpoch As Long = TimeToUnixSeconds(TiczViewModel.LastRefresh).ToString
            Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true&order=Name&plan={2}&lastupdate={3}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, roomIDX, lastUpdateEpoch)

        End If
    End Function

    Public Shared Function getAllScenesForRoom(roomIDX As String)
        Return String.Format("http://{0}:{1}/json.htm?type=scenes&filter=all&used=true&order=Name&plan={2}", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort, roomIDX)
    End Function

    Public Shared Function getAllDevices() As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
    End Function

    Public Shared Function getAllScenes() As String
        Return String.Format("http://{0}:{1}/json.htm?type=scenes", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
    End Function


    Public Shared Function getVersion() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getversion", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
    End Function

    Public Shared Function getConfig() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getconfig", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
    End Function

    Public Shared Function getauth() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getauth", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
    End Function

    Public Shared Function getSunRiseSet() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getSunRiseSet", TiczViewModel.TiczSettings.ServerIP, TiczViewModel.TiczSettings.ServerPort)
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



