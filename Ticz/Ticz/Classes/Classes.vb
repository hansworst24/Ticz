
Imports Microsoft.Xaml.Interactivity
Imports System.Threading
Imports Windows.Web.Http
Imports Windows.Web.Http.Filters
Imports Newtonsoft.Json
Imports GalaSoft.MvvmLight
Imports Windows.Storage.Streams
Imports System.Xml.Serialization

Public Class retvalue
    Public Property issuccess As Boolean
    Public Property err As String
End Class

Public NotInheritable Class Constants

    Public NotInheritable Class SECPANEL
        'constants for security panel
        Public Const SEC_DISARM As Integer = 0
        Public Const SEC_ARMHOME As Integer = 1
        Public Const SEC_ARMAWAY As Integer = 2

        Public Const SEC_DISARM_STATUS As String = "Normal"
        Public Const SEC_ARMHOME_STATUS As String = "Arm Home"
        Public Const SEC_ARMAWAY_STATUS As String = "Arm Away"
    End Class

    Public NotInheritable Class ROOMVIEW
        'constants for room view names
        Public Const ICONVIEW As String = "Icon View"
        Public Const GRIDVIEW As String = "Grid View"
        Public Const LISTVIEW As String = "List View"
        Public Const RESIZEVIEW As String = "Resize View"
        Public Const DASHVIEW As String = "Dashboard View"
    End Class

    Public NotInheritable Class DEVICEGROUPS
        'Constants for device group names
        Public Const GRP_GROUPS_SCENES As String = "Groups / Scenes"
        Public Const GRP_LIGHTS_SWITCHES As String = "Lights / Switches"
        Public Const GRP_WEATHER As String = "Weather Sensors"
        Public Const GRP_TEMPERATURE As String = "Temperature Sensors"
        Public Const GRP_UTILITY As String = "Utility Sensors"
        Public Const GRP_OTHER As String = "Other Devices"
    End Class

    Public NotInheritable Class DEVICEVIEWS
        Public Const WIDE As String = "Wide"
        Public Const ICON As String = "Icon"
        Public Const LARGE As String = "Large"
    End Class


    Public NotInheritable Class DEVICE

        Public NotInheritable Class STATUS
            Public Const OFF As String = "Off"
            Public Const [ON] As String = "On"
            Public Const OPEN As String = "Open"
            Public Const CLOSED As String = "Closed"
        End Class

        Public NotInheritable Class TYPE
            'constants for Device Types
            Public Const AIR_QUALITY As String = "Air Quality"
            Public Const CURRENT As String = "Current"
            Public Const LIGHTING_LIMITLESS As String = "Lighting Limitless/Applamp"
            Public Const TEMP As String = "Temp"
            Public Const THERMOSTAT As String = "Thermostat"
            Public Const TEMP_HUMI_BARO As String = "Temp + Humidity + Baro"
            Public Const TEMP_HUMI As String = "Temp + Humidity"
            Public Const LIGHTING_2 As String = "Lighting 2"
            Public Const LIGHT_SWITCH As String = "Light/Switch"
            Public Const LUX As String = "Lux"
            Public Const GROUP As String = "Group"
            Public Const HUMIDITY As String = "Humidity"
            Public Const RFXMETER As String = "RFXMeter"
            Public Const SCENE As String = "Scene"
            Public Const SECURITY As String = "Security"
            Public Const WIND As String = "Wind"
            Public Const GENERAL As String = "General"
            Public Const USAGE As String = "Usage"
            Public Const P1_SMART_METER As String = "P1 Smart Meter"
            Public Const UV As String = "UV"
            Public Const WATERFLOW As String = "Waterflow"
            Public Const RAIN As String = "Rain"

        End Class

        Public NotInheritable Class SUBTYPE
            'Constants for Device SubTypes
            Public Const ELECTRIC As String = "Electric"
            Public Const P1_GAS As String = "Gas"
            Public Const P1_ELECTRIC As String = "Energy"
            Public Const PERCENTAGE As String = "Percentage"
            Public Const SETPOINT As String = "SetPoint"
            Public Const SELECTOR_SWITCH As String = "Selector Switch"
        End Class

        Public NotInheritable Class SWITCHTYPE
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
        End Class

    End Class


    Public Const VISIBLE As String = "Visible"
    Public Const COLLAPSED As String = "Collapsed"

End Class






''' <summary>
''' TiczStorage contains Ticz settings and configurations that are stored on Storage
''' </summary>
Public NotInheritable Class TiczStorage
    Public Class RoomConfigurations
        Inherits ObservableCollection(Of RoomConfiguration)

        Private app As Application = CType(Windows.UI.Xaml.Application.Current, Application)


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
                app.myViewModel.Notify.Update(False, String.Format("No configuration file present. We will create a new one"), 0)
            End Try
            If fileExists Then
                Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read)
                Dim sessionInputStream As IInputStream = stream.GetInputStreamAt(0)
                Dim serializer = New XmlSerializer((New TiczStorage.RoomConfigurations).GetType())
                Try
                    stuffToLoad = serializer.Deserialize(sessionInputStream.AsStreamForRead())
                Catch ex As Exception
                    'Casting the contents of the file to a RoomConfigurations object failed. Potentially the file is empty or malformed. Return a new object
                    app.myViewModel.Notify.Update(True, String.Format("Config file seems corrupt. We created a new one : {0}", ex.Message), 2)
                End Try
                stream.Dispose()
            End If

            app.myViewModel.EnabledRooms.Clear()

            For Each r In app.myViewModel.DomoRooms.result.OrderBy(Function(x) x.Order)
                Dim retreivedRoomConfig = (From configs In stuffToLoad Where configs.RoomIDX = r.idx And configs.RoomName = r.Name Select configs).FirstOrDefault()
                If retreivedRoomConfig Is Nothing Then
                    retreivedRoomConfig = New RoomConfiguration With {.RoomIDX = r.idx, .RoomName = r.Name, .RoomView = Constants.ROOMVIEW.ICONVIEW, .ShowRoom = True}
                End If
                Me.Add(retreivedRoomConfig)
                If retreivedRoomConfig.ShowRoom Then app.myViewModel.EnabledRooms.Add(retreivedRoomConfig)
            Next
            WriteToDebug("RoomsConfigurations.LoadRoomConfigurations()", "end")
            Return True
        End Function

        Public Async Function SaveRoomConfigurations() As Task
            WriteToDebug("RoomsConfigurations.SaveRoomConfigurations()", "start")
            If app.myViewModel.DomoRooms.result.Any(Function(x) x.Name = "Ticz") Then
                'We are running in 'Debug mode', therefore we won't save the roomconfigurations
                Exit Function
            End If
            Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim storageFile As Windows.Storage.StorageFile = Await storageFolder.CreateFileAsync("ticzconfig.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)
            Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
            Dim sessionOutputStream As IOutputStream = stream.GetOutputStreamAt(0)

            For i As Integer = Me.Count - 1 To 0 Step -1
                Dim rconfig As TiczStorage.RoomConfiguration = Me(i)
                Dim domoroom As Domoticz.Plan = (From d In app.myViewModel.DomoRooms.result Where d.idx = rconfig.RoomIDX And d.Name = rconfig.RoomName Select d).FirstOrDefault()
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
                c = New RoomConfiguration With {.RoomIDX = idx, .RoomName = name, .RoomView = Constants.ROOMVIEW.ICONVIEW, .ShowRoom = True}
                Me.Add(c)
            End If
            If c.RoomName = "Ticz" Then c.RoomView = Constants.ROOMVIEW.LISTVIEW
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
        'Public Property ColumnSpan As Integer
        'Public Property RowSpan As Integer

        Public Sub New()
            'ColumnSpan = 1
            'RowSpan = 1
        End Sub
    End Class

    Public Class DeviceConfigurations
        Inherits ObservableCollection(Of DeviceConfiguration)

        Public Sub New()
        End Sub

        Public Function GetDeviceConfigurationForDevice(idx As Integer, name As String) As DeviceConfiguration
            Dim d As DeviceConfiguration = (From deviceconfigs In Me Where deviceconfigs.DeviceIDX = idx And deviceconfigs.DeviceName = name Select deviceconfigs).FirstOrDefault()
            If d Is Nothing Then
                d = New DeviceConfiguration With {.DeviceIDX = idx, .DeviceName = name, .DeviceRepresentation = "Icon", .DeviceOrder = Me.Count}
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

        'Public Function RowSpan(idx As Integer, name As String)
        '    Dim dc = (From d In Me Where d.DeviceIDX = idx And d.DeviceName = name Select d).FirstOrDefault()
        '    If Not dc Is Nothing Then
        '        Return dc.RowSpan
        '    Else
        '        Return 1
        '    End If
        'End Function

        'Public Function ColumnSpan(idx As Integer, name As String)
        '    Dim dc = (From d In Me Where d.DeviceIDX = idx And d.DeviceName = name Select d).FirstOrDefault()
        '    If Not dc Is Nothing Then
        '        Return dc.ColumnSpan
        '    Else
        '        Return 1
        '    End If
        'End Function
    End Class


End Class


Public NotInheritable Class Domoticz

    Private app As Application = CType(Windows.UI.Xaml.Application.Current, Application)

    Public Class Settings
        Public Property SecOnDelay As Integer
        Public Property SecPassword As String



        Public Async Function Load() As Task(Of retvalue)
            Dim ret As New retvalue
            Dim url As String = (New DomoApi).getSettings()
            Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
            If response.IsSuccessStatusCode Then
                Dim body As String = Await response.Content.ReadAsStringAsync()
                Dim DoSettings As Domoticz.Settings
                Try
                    DoSettings = JsonConvert.DeserializeObject(Of Domoticz.Settings)(body)
                    SecOnDelay = DoSettings.SecOnDelay
                    SecPassword = DoSettings.SecPassword
                    ret.issuccess = True
                Catch ex As Exception
                    ret.issuccess = False : ret.err = "Error parsing settings"
                End Try
            Else
                ret.err = response.ReasonPhrase
                Dim app As Application = CType(Application.Current, Application)
                Await app.myViewModel.Notify.Update(True, String.Format("Error loading Domoticz settings ({0})", ret.err), 2, False, 0)
            End If
            Return ret
        End Function
    End Class
    Public Class Version
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
            Dim url As String = (New DomoApi).getVersion()
            Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
            If response.IsSuccessStatusCode Then
                Dim body As String = Await response.Content.ReadAsStringAsync()
                Dim doversion As Domoticz.Version
                Try
                    doversion = JsonConvert.DeserializeObject(Of Domoticz.Version)(body)
                    version = doversion.version
                    build_time = doversion.build_time
                    ret.issuccess = True
                Catch ex As Exception
                    ret.issuccess = False : ret.err = "Error parsing config"
                End Try
            Else
                ret.err = response.ReasonPhrase
                Dim app As Application = CType(Application.Current, Application)
                Await app.myViewModel.Notify.Update(True, String.Format("Error loading Domoticz version information ({0})", ret.err), 2, False, 0)
            End If
            Return ret
        End Function

    End Class
    Public Class Config
        Public Property TempScale As Double
        Public Property TempSign As String
        Public Property WindScale As Double
        Public Property WindSign As String

        Public Async Function Load() As Task(Of retvalue)
            Dim ret As New retvalue
            Dim url As String = (New DomoApi).getConfig()
            Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
            If response.IsSuccessStatusCode Then
                Dim body As String = Await response.Content.ReadAsStringAsync()
                Dim config As Domoticz.Config
                Try
                    config = JsonConvert.DeserializeObject(Of Domoticz.Config)(body)
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
                Dim app As Application = CType(Application.Current, Application)
                Await app.myViewModel.Notify.Update(True, String.Format("Error loading Domoticz Config ({0})", ret.err), 2, False, 0)
            End If
            Return ret
        End Function
    End Class
    Public Class Response
        Public Property secstatus As Integer
        Public Property message As String
        Public Property status As String
        Public Property title As String
    End Class
    Public Class GraphValue
        Public Property c1 As Double
        Public Property c3 As Double
        Public Property ba As Integer
        Public Property d As DateTime
        Public Property [Date] As DateTime 'Used by LightLog
        Public ReadOnly Property TimeStamp As DateTime
            Get
                Return [Date]
            End Get
        End Property

        Public Property Level As Double    'Used by LightLog
        Public Property eu As Double
        Public Property gu As Double        'Used by Wind (Gust)
        Public Property hu As Integer
        Public Property mm As Double        'Used by Rain (m/s)
        Public Property r1 As Double
        Public Property r2 As Double
        Public Property sp As Double        'Used by Wind (Speed)
        Public Property te As Double        'Used by Temperature
        Public Property ta As Double        'Used by Temperature
        Public Property tm As Double        'Used by Temperature
        Public Property u As Double         'Used by Usage / Electricity
        Public Property u_min As Double     'Used by Usage / Electricity
        Public Property u_max As Double     'Used by Usage / Electricity
        Public Property v As Double
        Public ReadOnly Property v_percentage As Double
            Get
                Return v / 100
            End Get
        End Property
        Public Property v_avg As Double
        Public ReadOnly Property v_avg_percentage As Double
            Get
                Return v_avg / 100
            End Get
        End Property
        Public Property v_min As Double
        Public Property v_max As Double
        Public Property v2 As Double
        Public Property Status As String
        Public ReadOnly Property Status_Int As Integer
            Get
                If Status = Constants.DEVICE.STATUS.OFF Then Return 0 Else Return 100
            End Get
        End Property

    End Class
    Public Class DeviceGraphContainer
        Inherits ViewModelBase
        Implements IDisposable
        Public Property result As List(Of GraphValue)
            Get
                Return _result
            End Get
            Set(value As List(Of GraphValue))
                _result = value
                RaisePropertyChanged("result")
            End Set
        End Property
        Private Property _result As List(Of GraphValue)
        Public Property url As String
        Public Property GraphTitle As String
        Public Property GraphDataTemplate As DataTemplate
        Public Property range As String
        Public Property idx As Integer
        Public Property status As String
        Public Property devicename As String
        Public Property title As String
        Public Property devicetype As String
        Public ReadOnly Property WindGraphHeader As String
            Get
                Return String.Format("Wind Speed in {0}", WindSign)
            End Get
        End Property

        Public ReadOnly Property WindSign As String
            Get
                Dim app As Application = CType(Application.Current, Application)
                Return app.myViewModel.DomoConfig.WindSign
            End Get
        End Property

        Public ReadOnly Property WindYScale As String
            Get
                Return String.Format("#.0 {0}", WindSign)
            End Get
        End Property
        Public Property subtype As String


        Public Sub New()
            result = New List(Of GraphValue)
            title = "No data available"
        End Sub

        Public Sub New(idx As Integer, type As String, subtype As String, name As String, range As String, graphtemplate As DataTemplate, url As String)
            Me.idx = idx
            Me.devicetype = type
            Me.subtype = subtype
            Me.url = url
            Me.devicename = name
            Me.GraphDataTemplate = graphtemplate
            result = New List(Of GraphValue)
            Me.range = range

            Select Case range
                Case ""
                    Me.GraphTitle = "Past 200 Datapoints"
                Case "day"
                    Me.GraphTitle = "Past 24 Hours"
                Case "week"
                    Me.GraphTitle = "Past Week"
                Case "month"
                    Me.GraphTitle = "Past Month "
                Case "year"
                    Me.GraphTitle = "Past Year"
            End Select

        End Sub

        Public Async Function Load(d As DeviceViewModel) As Task
            If Not url = "" Then
                Dim ret As New retvalue
                Dim zut As New DeviceGraphContainer
                Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
                If response.IsSuccessStatusCode Then
                    Dim body As String = Await response.Content.ReadAsStringAsync()
                    Try
                        zut = JsonConvert.DeserializeObject(Of DeviceGraphContainer)(body)
                    Catch ex As Exception
                        'app.myViewModel.Notify.Update(True, String.Format("Error loading Domoticz graph data ({0})", response.ReasonPhrase), 2)
                    End Try
                Else
                    'Await App.myViewModel.Notify.Update(True, String.Format("Error loading Domoticz graph data ({0})", response.ReasonPhrase), 2)
                End If

                'HACK : Filter the returned results for a On/Off device or MediaPlayer to only contain the last 200 results, in case it's thousands of records
                If d.Type = Constants.DEVICE.TYPE.LIGHTING_2 Or d.SwitchType = Constants.DEVICE.SWITCHTYPE.MEDIA_PLAYER Or d.SwitchType = Constants.DEVICE.SWITCHTYPE.SELECTOR Then
                    If zut.result.Count > 200 Then
                        zut.result = zut.result.GetRange(zut.result.Count - 200, 200)
                    End If
                End If

                Me.result = zut.result
                Me.title = zut.title
                response.Dispose()
            End If
            Return
        End Function

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                    If Not result Is Nothing Then
                        result.Clear()
                        result = Nothing
                    End If
                    GraphDataTemplate = Nothing
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
    Public Class SunRiseSet
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
            Dim url As String = (New DomoApi).getSunRiseSet()
            Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
            If response.IsSuccessStatusCode Then
                Dim body As String = Await response.Content.ReadAsStringAsync()
                Dim sunriseset As Domoticz.SunRiseSet
                Try
                    sunriseset = JsonConvert.DeserializeObject(Of Domoticz.SunRiseSet)(body)
                    Await RunOnUIThread(Sub()
                                            Sunrise = sunriseset.Sunrise
                                            Sunset = sunriseset.Sunset

                                        End Sub)
                    ret.issuccess = True
                Catch ex As Exception
                    ret.issuccess = False : ret.err = "Error parsing config"
                End Try
            Else
                ret.issuccess = False : ret.err = response.ReasonPhrase
                ret.issuccess = False : ret.err = response.ReasonPhrase
                Dim app As Application = CType(Application.Current, Application)
                Await app.myViewModel.Notify.Update(True, String.Format("Error loading Sunrise / Sunset info ({0})", ret.err), 2, False, 0)
            End If
            Return ret
        End Function

    End Class
    Public Class Plans
        Public Property result As ObservableCollection(Of Plan)
        Public Property status As String
        Public Property title As String

        Private app As Application = CType(Windows.UI.Xaml.Application.Current, Application)

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
            Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON((New DomoApi).getPlans)
            If response.IsSuccessStatusCode Then
                Dim body As String = Await response.Content.ReadAsStringAsync()
                Dim deserialized = JsonConvert.DeserializeObject(Of Plans)(body)
                Await ClearPlans()

                'Check if there exists a "Ticz" room in Domoticz. If so, ignore all other rooms
                If deserialized.result.Any(Function(x) x.Name = "Ticz") Then
                    Me.result.Add(deserialized.result.Where(Function(x) x.Name = "Ticz").FirstOrDefault)
                Else
                    For Each p In deserialized.result.OrderBy(Function(x) x.Order)
                        Me.result.Add(p)
                    Next
                    If app.myViewModel.TiczSettings.ShowAllDevices Then Me.result.Insert(0, New Plan With {.idx = 12321, .Name = "All Devices", .Order = 0})
                End If


                'Re-order the Plans
                For i As Integer = 0 To Me.result.Count - 1 Step 1
                    Me.result(i).Order = i
                Next
                Me.status = deserialized.status
                Me.title = deserialized.status
                deserialized = Nothing
                Return New retvalue With {.issuccess = True}
            Else
                WriteToDebug("Plans.Load()", response.ReasonPhrase)
                'Await TiczViewModel.Notify.Update(True, response.ReasonPhrase)
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

    Public Async Function DownloadJSON(url As String) As Task(Of HttpResponseMessage)

        Using filter As New HttpBaseProtocolFilter
            If Not app.myViewModel.TiczSettings.Password = "" AndAlso Not app.myViewModel.TiczSettings.Username = "" Then
                filter.ServerCredential = New Windows.Security.Credentials.PasswordCredential With {.Password = app.myViewModel.TiczSettings.Password, .UserName = app.myViewModel.TiczSettings.Username}
                filter.CookieUsageBehavior = HttpCookieUsageBehavior.NoCookies
                WriteToDebug(filter.ServerCredential.UserName, filter.ServerCredential.Password)
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
        Dim tile = TryCast(item, DeviceViewModel)
        If Not tile Is Nothing Then
            Dim griditem = TryCast(element, GridViewItem)
            If Not griditem Is Nothing Then
                VariableSizedWrapGrid.SetColumnSpan(griditem, tile.DeviceColumnSpan)
                VariableSizedWrapGrid.SetRowSpan(griditem, tile.DeviceRowSpan)
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
    '   "status" : "Error",
    '   "title" : "SwitchLight"
    '}

    Private app As Application = CType(Windows.UI.Xaml.Application.Current, Application)

    Public Function getSecurityStatus()
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getsecstatus", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function


    Public Function setSetpoint(idx As Integer, setpointvalue As Double)
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=setsetpoint&idx={2}&setpoint={3}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx, setpointvalue)
    End Function
    Public Function setSecurityStatus(status As Integer, HashCode As String)
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=setsecstatus&secstatus={2}&seccode={3}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, status, HashCode)
    End Function

    Public Function getButtonPressedSound()
        Return String.Format("http://{0}:{1}/secpanel/media/key.mp3", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getWrongCodeSound()
        Return String.Format("http://{0}:{1}/secpanel/media/wrongcode.mp3", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getArmSound()
        Return String.Format("http://{0}:{1}/secpanel/media/arm.mp3", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getDisarmedSound()
        Return String.Format("http://{0}:{1}/secpanel/media/disarm.mp3", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getLightLog(idx As Integer)
        Return String.Format("http://{0}:{1}/json.htm?type=lightlog&idx={2}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx.ToString)
    End Function

    Public Function getGraph(idx As Integer, range As String, sensor As String)
        Return String.Format("http://{0}:{1}/json.htm?type=graph&sensor={2}&idx={3}&range={4}",
                             app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort,
                             sensor, idx.ToString, range)
    End Function



    Public Function getAllDevicesForRoom(roomIDX As String, Optional LoadAllUpdates As Boolean = False)
        'By sending a lastupdate parameter with a unix epoch number, we'll only get the updated devices since that epoch
        WriteToDebug("DomoApi", TimeToUnixSeconds(app.myViewModel.LastRefresh).ToString)
        If LoadAllUpdates Then
            Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true&order=Name&plan={2}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, roomIDX)
        Else
            Dim lastUpdateEpoch As Long = TimeToUnixSeconds(app.myViewModel.LastRefresh).ToString
            Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true&order=Name&plan={2}&lastupdate={3}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, roomIDX, lastUpdateEpoch)

        End If
    End Function

    Public Function getAllScenesForRoom(roomIDX As String)
        Return String.Format("http://{0}:{1}/json.htm?type=scenes&filter=all&used=true&order=Name&plan={2}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, roomIDX)
    End Function

    Public Function getAllDevices() As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getAllScenes() As String
        Return String.Format("http://{0}:{1}/json.htm?type=scenes", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function


    Public Function getVersion() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getversion", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getConfig() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getconfig", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getSettings() As String
        Return String.Format("http://{0}:{1}/json.htm?type=settings", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getauth() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getauth", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function

    Public Function getSunRiseSet() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getSunRiseSet", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function


    Public Function getDeviceStatus(idx As String) As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&rid={2}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx)
    End Function

    Public Function setDimmer(idx As String, switchstate As String, Optional passcode As String = "") As String
        Dim switchstring As String
        If Not switchstate = "On" Then switchstring = "Set%20Level&level=" Else switchstring = ""
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}{4}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx, switchstring, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}{4}&passcode={5}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx, switchstring, switchstate, passcode)
        End If

    End Function


    Public Function SwitchScene(idx As String, switchstate As String, Optional passcode As String = "") As String
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchscene&idx={2}&switchcmd={3}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchscene&idx={2}&switchcmd={3}&passcode={4}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx, switchstate, passcode)
        End If

    End Function

    Public Function SwitchLight(idx As String, switchstate As String, Optional passcode As String = "") As String
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}&passcode={4}", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort, idx, switchstate, passcode)
        End If

    End Function

    Public Function getPlans() As String
        Return String.Format("http://{0}:{1}/json.htm?type=plans", app.myViewModel.TiczSettings.ServerIP, app.myViewModel.TiczSettings.ServerPort)
    End Function
End Class



