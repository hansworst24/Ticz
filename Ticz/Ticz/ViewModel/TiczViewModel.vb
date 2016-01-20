Imports System.Net
Imports System.Threading
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports Newtonsoft.Json
Imports Windows.ApplicationModel.Core
Imports Windows.UI
Imports Windows.UI.Core
Imports Windows.Web.Http

Public Class domoResponse
    Public Property message As String
    Public Property status As String
    Public Property title As String
End Class



Public Class retvalue
    Public Property issuccess As Boolean
    Public Property err As String
End Class


'Public Class Light_Switches
'    Public Property result As ObservableCollection(Of Light_Switch)
'    Public Property status As String
'    Public Property title As String

'    Public Async Function Load() As Task
'        Dim response As HttpResponseMessage = Await (New Downloader).DownloadJSON((New Api).getLightSwitches)
'        Dim body As String = Await response.Content.ReadAsStringAsync()
'        Dim deserialized = JsonConvert.DeserializeObject(Of Light_Switches)(body)
'        For Each r In deserialized.result
'            result.Add(r)
'            Await r.getStatus()

'        Next
'        Me.status = deserialized.status
'        Me.title = deserialized.status
'    End Function

'    Public Sub New()
'        result = New ObservableCollection(Of Light_Switch)
'    End Sub
'End Class
'Public Class Light_Switch
'    Inherits ViewModelBase
'    '/json.htm?type=command&param=getlightswitches
'    '/json.htm?type=devices&filter=light&used=true&order=Name

'    Public Property IsDimmer As Boolean
'    Public Property Name As String
'    Public Property SubType As String
'    Public Property Type As String
'    Public Property IDX As String
'    Public Property isOn As Boolean
'        Get
'            Return _isOn
'        End Get
'        Set(value As Boolean)
'            _isOn = value
'            RaisePropertyChanged("isOn")
'        End Set
'    End Property
'    Private Property _isOn As Boolean
'    Public Property Data As String
'    Public Property needsInitializing As Boolean
'        Get
'            Return _needsInitializing
'        End Get
'        Set(value As Boolean)
'            _needsInitializing = value
'            RaisePropertyChanged()
'        End Set
'    End Property
'    Private _needsInitializing As Boolean
'    Public Property IconURI As String
'        Get
'            If isOn Then
'                Return "http://192.168.168.4:8888/images/contact48_open.png"
'            Else Return "http://192.168.168.4:8888/images/contact48.png"
'            End If
'        End Get
'        Set(value As String)
'            RaisePropertyChanged()
'        End Set
'    End Property
'    Private Property _IconURI As String



'    Public Async Function getStatus() As Task
'        Dim a = Await (New Downloader).DownloadJSON((New Api).getDeviceStatus(Me.IDX))
'        Dim deserialized = JsonConvert.DeserializeObject(Of Light_Switches)(Await a.Content.ReadAsStringAsync)
'        If deserialized.result(0).Data = "On" Then Me.isOn = True Else Me.isOn = False
'        needsInitializing = False
'    End Function

'    Public ReadOnly Property LightOnOff As RelayCommand
'        Get
'            Return New RelayCommand(Async Sub()
'                                        Dim url As String
'                                        If Me.isOn Then
'                                            url = (New Api).SwitchLight(Me.IDX, "On")
'                                        Else
'                                            url = (New Api).SwitchLight(Me.IDX, "Off")
'                                        End If
'                                        Dim b = Await (New Downloader).DownloadJSON(url)
'                                        'Re-pull the status
'                                        Await Me.getStatus()
'                                    End Sub)

'        End Get
'    End Property

'    Public Sub New()
'        needsInitializing = True
'    End Sub
'End Class
Public Class Devices
    Inherits ViewModelBase
    Public Property result As ObservableCollection(Of Device)
    Public Property status As String
    Public Property title As String

    Public Sub New()
        result = New ObservableCollection(Of Device)
    End Sub

    Private app As App = CType(Application.Current, App)

    Public Overloads Async Function Load() As Task(Of retvalue)
        Dim url As String
        If Not app.myViewModel.MyPlans Is Nothing Then
            'Check if there is a room called "Ticz". If so, we will only load devices within this room, so that this room can be used as TestRoom, by adding / removing different devices
            If app.myViewModel.MyPlans.result.Any(Function(x) x.Name = "Ticz") Then
                'We found a room called "Ticz", get the IDX
                Dim roomIDX As String = (From r In app.myViewModel.MyPlans.result Where r.Name = "Ticz" Select r.idx).FirstOrDefault
                url = (New Api).getAllDevicesForRoom(roomIDX)
            Else
                url = (New Api).getAllDevices()
            End If
        Else
            url = (New Api).getAllDevices()
        End If

        Dim response As HttpResponseMessage = Await (New Downloader).DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim deserialized = JsonConvert.DeserializeObject(Of Devices)(body)
            result.Clear()
            For Each r In deserialized.result
                'Hack to show the Data Field as Status, if there is no Status Field
                If r.Status = "" Then
                    r.Status = r.Data
                End If
                r.Initialize()
                result.Add(r)
            Next
            Me.status = deserialized.status
            Me.title = deserialized.title
            Return New retvalue With {.issuccess = True}
        Else
            WriteToDebug("Devices.Load()", response.ReasonPhrase)
            Return New retvalue With {.issuccess = False, .err = response.ReasonPhrase}
        End If

    End Function
End Class
Public Class Device
    Inherits ViewModelBase
#Region "JSON Output"

    '/json.htm?type=command&param=getlightswitches
    '/json.htm?type=devices&filter=light&used=true&order=Name
    '"AddjMulti"  1.0,
    ' "AddjMulti2" : 1.0,
    ' "AddjValue" : 0.0,
    ' "AddjValue2" : 0.0,
    ' "BatteryLevel" : 255,
    ' "CustomImage" : 0,
    ' "Data" : "Off",
    ' "Description" : "",
    ' "Favorite" : 0,
    ' "HardwareID" : 3,
    ' "HardwareName" : "AEON",
    ' "HardwareType" : "OpenZWave USB",
    ' "HardwareTypeVal" : 21,
    ' "HaveDimmer" : true,
    ' "HaveGroupCmd" : false,
    ' "HaveTimeout" : false,
    ' "ID" : "0000201",
    ' "Image" : "Light",
    ' "IsSubDevice" : false,
    ' "LastUpdate" : "2016-01-04 09:41:00",
    ' "Level" : 0,
    ' "LevelInt" : 0,
    ' "MaxDimLevel" : 100,
    ' "Name" : "Woonkamer Spotjes Schakelaar",
    ' "Notifications" : "false",
    ' "PlanID" : "0",
    ' "PlanIDs" : [ 0 ],
    ' "Protected" : false,
    ' "ShowNotifications" : true,
    ' "SignalLevel" : "-",
    ' "Status" : "Off",
    ' "StrParam1" : "c2NyaXB0Oi8vd29vbmthbWVyX2h1ZS5zaCBvbg==",
    ' "StrParam2" : "c2NyaXB0Oi8vd29vbmthbWVyX2h1ZS5zaCBvZmY=",
    ' "SubType" : "ZWave",
    ' "SwitchType" : "On/Off",
    ' "SwitchTypeVal" : 0,
    ' "Timers" : "false",
    ' "Type" : "Lighting 2",
    ' "TypeImg" : "lightbulb",
    ' "Unit" : 1,
    ' "Used" : 1,
    ' "UsedByCamera" : false,
    ' "XOffset" : "0",
    ' "YOffset" : "0",
    ' "idx" : "9"
#End Region
#Region "JSON Properties"
    Public Property AddjMulti As Double
    Public Property AddjMulti2 As Double
    Public Property AddjValue As Double
    Public Property AddjValue2 As Double
    Public Property Barometer As String
        Get
            Return String.Format("Barometer: {0} hPa", _Barometer)
        End Get
        Set(value As String)
            _Barometer = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Barometer As String
    Public Property BatteryLevel As Integer
    Public Property CameraIdz As Integer
    Public Property Chill As String
        Get
            Return String.Format("Chill: {0} °C", _Chill)
        End Get
        Set(value As String)
            _Chill = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Chill As String

    Public Property Counter As String
        Get
            Return _Counter
        End Get
        Set(value As String)
            _Counter = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Counter As String

    Public Property CounterDeliv As String
        Get
            Return _CounterDeliv
        End Get
        Set(value As String)
            _CounterDeliv = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _CounterDeliv As String

    Public Property CounterDelivToday As String
        Get
            Return _CounterDelivToday
        End Get
        Set(value As String)
            _CounterDelivToday = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _CounterDelivToday As String

    Public Property CounterToday As String
        Get
            Return _CounterToday
        End Get
        Set(value As String)
            _CounterToday = value
            RaisePropertyChanged()
            RaisePropertyChanged("GasUsage")
            RaisePropertyChanged("EnergyUsage")
            RaisePropertyChanged("EnergyReturn")
        End Set
    End Property
    Private Property _CounterToday As String

    Public Property CustomImage As Integer
    Public Property Data As String
        Get
            Return _Data
        End Get
        Set(value As String)
            _Data = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Data As String
    Public Property Description As String
    Public Property DewPoint As String
        Get
            Return String.Format("Dewpoint: {0} °C", _DewPoint)
        End Get
        Set(value As String)
            _DewPoint = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _DewPoint As String


    Public Property Direction As String
        Get
            Return String.Format("{0}{1}", _Direction, DirectionStr)
        End Get
        Set(value As String)
            _Direction = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Direction As String
    Public Property DirectionStr As String
    Public Property Favorite As Integer
    Public Property Gust As String
        Get
            Return String.Format("Gust: {0} m/s", _Gust)
        End Get
        Set(value As String)
            _Gust = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Gust As String
    'Public Property Gust As String
    Public Property HardwareID As Integer
    Public Property HardwareName As String
    Public Property HardwareType As String
    Public Property HardwareTypeVal As Integer
    Public Property HaveDimmer As Boolean
    Public Property HaveGroupCmd As Boolean
    Public Property HaveTimeout As Boolean
    Public Property Humidity As String
        Get
            If Not _Humidity Is Nothing Then
                Return String.Format("Humidity: {0}%", _Humidity.ToString)
            Else
                Return ""
            End If

        End Get
        Set(value As String)
            _Humidity = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Humidity As String
    Public Property HumidityStatus As String
    Public Property ID As String
    Public Property Image As String
    Public Property IsSubDevice As Boolean
    Public Property LastUpdate As String
    Public Property Level As Integer
    Public Property LevelInt As Integer
        Get
            Return _LevelInt
        End Get
        Set(value As Integer)
            _LevelInt = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _LevelInt As Integer
    Public Property LevelNames As String
    Public Property MaxDimLevel As Integer
    Public Property Name As String
    Public Property Notifications As String
    Public Property PlanID As String
    Public Property PlanIDs As List(Of Integer)
    Public Property [Protected] As Boolean
    Public Property Rain As String
        Get
            Return String.Format("Rain: {0} mm", _Rain)
        End Get
        Set(value As String)
            _Rain = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Rain As String
    Public Property RainRate As String
        Get
            Return String.Format("Rate: {0} mm/h", _RainRate)
        End Get
        Set(value As String)
            _RainRate = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _RainRate As String
    Public Property ShowNotifications As Boolean
    Public Property SignalLevel As String
    Public Property Speed As String
        Get
            Return String.Format("Spd: {0} m/s", _Speed)
        End Get
        Set(value As String)
            _Speed = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Speed As String
    Public Property Status As String
        Get
            Return _Status
        End Get
        Set(value As String)
            _Status = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Status As String
    Public Property StrParam1 As String
    Public Property StrParam2 As String
    Public Property SubType As String
    Public Property SwitchType As String
    Public Property SwitchTypeVal As Integer
    Public Property Temp As String
        Get
            Return String.Format("Temp: {0} °C", _Temp)
        End Get
        Set(value As String)
            _Temp = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Temp As String
    Public Property Timers As String
    Public Property Type As String
    Public Property TypeImg As String
    Public Property Unit As Integer
    Public Property Usage As String
        Get
            Return _Usage
        End Get
        Set(value As String)
            _Usage = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _Usage As String
    Public Property UsageDeliv As String
        Get
            Return _UsageDeliv
        End Get
        Set(value As String)
            _usageDeliv = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _UsageDeliv As String
    Public Property Used As Integer
    Public Property UsedByCamera As Boolean
    Public Property XOffset As String
    Public Property YOffset As String
    Public Property idx As String
    Public Property CameraIdx As String
#End Region
#Region "ViewModel Properties"


    'Set constants for Types
    Private Const LIGHTING_LIMITLESS As String = "Lighting Limitless/Applamp"
    Private Const TEMP_HUMI_BARO As String = "Temp + Humidity + Baro"
    Private Const LIGHTING_2 As String = "Lighting 2"
    Private Const LIGHT_SWITCH As String = "Light/Switch"
    Private Const GROUP As String = "Group"
    Private Const SCENE As String = "Scene"
    Private Const WIND As String = "Wind"
    Private Const P1_SMART_METER As String = "P1 Smart Meter"
    Private Const TYPE_RAIN As String = "Rain"

    'Set constants for SubTypes
    Private Const P1_GAS As String = "Gas"
    Private Const P1_ELECTRIC As String = "Energy"


    'Set constants for SwitchTypes
    Private Const BLINDS As String = "Blinds"
    Private Const BLINDS_INVERTED As String = "Blinds Inverted"
    Private Const BLINDS_PERCENTAGE As String = "Blinds Percentage"
    Private Const BLINDS_PERCENTAGE_INVERTED As String = "Blinds Percentage Inverted"
    Private Const CONTACT As String = "Contact"
    Private Const DIMMER As String = "Dimmer"
    Private Const DOOR_LOCK As String = "Door Lock"
    Private Const DOORBELL As String = "Doorbell"
    Private Const DUSK_SENSOR As String = "Dusk Sensor"
    Private Const MEDIA_PLAYER As String = "Media Player"
    Private Const MOTION_SENSOR As String = "Motion Sensor"
    Private Const ON_OFF As String = "On/Off"
    Private Const PUSH_ON_BUTTON As String = "Push On Button"
    Private Const PUSH_OFF_BUTTON As String = "Push Off Button"
    Private Const SELECTOR As String = "Selector"
    Private Const SMOKE_DETECTOR As String = "Smoke Detector"
    Private Const VEN_BLINDS_EU As String = "Venetian Blinds EU"
    Private Const VEN_BLINDS_US As String = "Venetian Blinds US"
    Private Const X10_SIREN As String = "X10 Siren"
    Private Const GENERAL As String = "General"


    'Set constants for Switch Status
    Private Const OPEN As String = "Open"
    Private Const CLOSED As String = "Closed"
    Private Const [ON] As String = "On"
    Private Const [OFF] As String = "Off"


    Public ReadOnly Property GasUsage As String
        Get
            Return String.Format("Usage: {0} | Today: {1}", Counter, CounterToday)
        End Get
    End Property

    Public ReadOnly Property EnergyUsage As String
        Get
            Return String.Format("Usage: {0} | Today: {1}", Counter, CounterToday)
        End Get
    End Property

    Public ReadOnly Property EnergyReturn As String
        Get
            Return String.Format("Return: {0} | Today: {1} ", CounterDeliv, CounterDelivToday)
        End Get
    End Property

    Public ReadOnly Property DirectionSpeedGust As String
        Get
            Return String.Format("{0} | {1} | {2}", Direction, Speed, Gust)
        End Get
    End Property

    Public ReadOnly Property TempChill As String
        Get
            Return String.Format("{0} | {1} ", Temp, Chill)
        End Get
    End Property

    Public Property DeviceType As String

    Public Property SwitchingToState As String

    Public Property MinDimmerLevel As Integer
        Get
            Return _MinDimmerLevel
        End Get
        Set(value As Integer)
            _MinDimmerLevel = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _MinDimmerLevel As Integer

    Public Property MaxDimmerLevel As Integer
        Get
            Return _MaxDimmerLevel
        End Get
        Set(value As Integer)
            _MaxDimmerLevel = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _MaxDimmerLevel As Integer

    Public Property LevelNamesList As List(Of String)

    Public Property SelectedLevelName As String
        Get
            Return _SelectedLevelName
        End Get
        Set(value As String)
            _SelectedLevelName = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _SelectedLevelName As String

    Public Property RainVisibility As String
    Public Property P1GasVisibility As String
    Public Property P1ElectricVisibility As String
    Public Property GroupVisibility As String
    Public Property SceneVisibility As String
    Public Property StatusVisibility As String
    Public Property SelectorVisibility As String
    Public Property IconVisibility As String
    Public Property BitmapIconVisibility As String
    Public Property WindVisibility As String
    Public Property TempHumBarVisibility As String
    Public Property VectorIconVisibility As String
    Public Property DimmerVisibility As String
    Public Property BlindsVisibility As String
    Public Property MediaPlayerVisibility As String

    Public ReadOnly Property IconForegroundColor As Brush
        Get
            If isOn Then
                Return Application.Current.Resources("SystemControlHighlightAccentBrush")
            Else
                Dim myBrush As New SolidColorBrush
                myBrush.Color = Color.FromArgb(255, 128, 128, 128)
                Return myBrush
            End If
        End Get
    End Property

    Public ReadOnly Property BatteryLevelVisibility As String
        Get
            If BatteryLevel <= 100 Then Return const_Visible Else Return const_Collapsed
        End Get
    End Property

    Public ReadOnly Property BatteryLevelString As String
        Get
            Return String.Format("{0} %", BatteryLevel)
        End Get
    End Property


    Public Property IconDataTemplate As DataTemplate

    Public Property PassCode As String
        Get
            Return _PassCode
        End Get
        Set(value As String)
            _PassCode = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _PassCode As String

    Public Property PassCodeInputVisibility As String
        Get
            Return _PassCodeInputVisibility
        End Get
        Set(value As String)
            _PassCodeInputVisibility = value
            RaisePropertyChanged("PassCodeInputVisibility")
        End Set
    End Property
    Private Property _PassCodeInputVisibility As String

    Public Property MediaPlayerMarquee As String
        Get
            Return _MediaPlayerMarquee
        End Get
        Set(value As String)
            _MediaPlayerMarquee = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _MediaPlayerMarquee As String


    Public Property DetailsVisiblity As String
        Get
            Return _DetailsVisiblity
        End Get
        Set(value As String)
            _DetailsVisiblity = value
            RaisePropertyChanged()
        End Set
    End Property
    Private _DetailsVisiblity As String



    Public Property isMixed As Boolean
        Get
            Return _isMixed
        End Get
        Set(value As Boolean)
            _isMixed = value
            RaisePropertyChanged("isMixed")
        End Set
    End Property
    Private Property _isMixed As Boolean


    Public Property isOn As Boolean
        Get
            Return _isOn
        End Get
        Set(value As Boolean)
            _isOn = value
            RaisePropertyChanged("isOn")
            RaisePropertyChanged("IconForegroundColor")
        End Set
    End Property
    Private Property _isOn As Boolean

    Public Property needsInitializing As Boolean
        Get
            Return _needsInitializing
        End Get
        Set(value As Boolean)
            _needsInitializing = value
            RaisePropertyChanged()
        End Set
    End Property
    Private _needsInitializing As Boolean

    Public Property IconURI As String


#End Region

    Private app As App = CType(Application.Current, App)

    Const const_Visible As String = "Visible"
    Const const_Collapsed As String = "Collapsed"
    Const groupMixed As String = "Mixed"


    Public Property IsDimmer As Boolean
    Public Property CanBeSwitched As Boolean


    Public Async Function Update(Optional d As Device = Nothing) As Task
        WriteToDebug("Device.Update()", "executed")
        'If we haven't sent an updated device to this function, retrieve the device's latest status from the server
        If d Is Nothing Then
            Dim response As HttpResponseMessage
            If Type = "Group" Or Type = "Scene" Then
                response = Await Task.Run(Function() (New Downloader).DownloadJSON((New Api).getAllScenes()))
            Else
                response = Await Task.Run(Function() (New Downloader).DownloadJSON((New Api).getDeviceStatus(Me.idx)))
            End If

            If response.IsSuccessStatusCode Then
                Dim deserialized = JsonConvert.DeserializeObject(Of Devices)(Await response.Content.ReadAsStringAsync)
                'Dim myDevice As Device = (From dev In deserialized.result Where dev.idx = idx Select dev).FirstOrDefault()
                Dim myDevice As Device = (From dev In deserialized.result Where dev.idx = idx Select dev).FirstOrDefault()
                If Not myDevice Is Nothing Then
                    d = myDevice
                Else
                    Await app.myViewModel.Notify.Update(True, "couldn't get device's status", 2)
                End If
            End If
        End If

        'Set properties which raise propertychanged events on the UI thread
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         Level = d.Level
                                                                                                         LevelInt = d.LevelInt
                                                                                                         Status = d.Status
                                                                                                         Data = d.Data
                                                                                                         Counter = d.Counter
                                                                                                         CounterToday = d.CounterToday
                                                                                                         CounterDeliv = d.CounterDeliv
                                                                                                         CounterDelivToday = d.CounterDelivToday
                                                                                                         Usage = d.Usage
                                                                                                         UsageDeliv = d.UsageDeliv
                                                                                                         'For Selector Device we need to redo somevalidation
                                                                                                         If Not LevelNamesList.Count = 0 Then
                                                                                                             If LevelInt Mod 10 > 0 Then
                                                                                                                 'Dimmer Level not set to a 10-value, therefore illegal
                                                                                                                 SelectedLevelName = ""
                                                                                                             Else
                                                                                                                 If LevelNamesList.Count > (LevelInt / 10) Then
                                                                                                                     If SelectedLevelName <> LevelNamesList(LevelInt / 10) Then
                                                                                                                         SelectedLevelName = LevelNamesList(LevelInt / 10)
                                                                                                                     End If
                                                                                                                 Else
                                                                                                                     SelectedLevelName = ""
                                                                                                                 End If
                                                                                                             End If
                                                                                                             Status = SelectedLevelName
                                                                                                         End If
                                                                                                         If Status = "" Then Status = Data
                                                                                                         Initialize()
                                                                                                     End Sub)

    End Function



    Public ReadOnly Property GroupSwitchOn As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchGroup([ON])
                                    End Sub)

        End Get
    End Property

    Public ReadOnly Property GroupSwitchOff As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchGroup([OFF])
                                    End Sub)

        End Get
    End Property


    Public ReadOnly Property SelectorSelectionChanged As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)
                                                   Dim selected As String = TryCast(x, String)
                                                   If selected Is Nothing Then
                                                       Exit Sub
                                                   End If
                                                   WriteToDebug("Device.SelectorSelectionChanged()", selected)
                                                   If Not SelectedLevelName = "" Then
                                                       Dim SwitchToState As String = (LevelNamesList.IndexOf(selected) * 10).ToString
                                                       If [Protected] Then
                                                           SwitchingToState = SwitchToState
                                                           app.myViewModel.selectedDevice = Me
                                                           app.myViewModel.PasswordEntryVisibility = "Visible"
                                                           Exit Sub
                                                       End If
                                                       Dim ret As retvalue = Await SwitchDevice(SwitchToState)
                                                   End If

                                               End Sub)
        End Get
    End Property

    Public ReadOnly Property SliderValueChanged As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        If Me.SwitchType = "Dimmer" Then
                                            WriteToDebug("Device.SliderValueChanged()", String.Format("executed : value {0}", LevelInt))
                                            Dim SwitchToState As String = (LevelInt + 1).ToString
                                            If [Protected] Then
                                                SwitchingToState = SwitchToState
                                                app.myViewModel.selectedDevice = Me
                                                app.myViewModel.PasswordEntryVisibility = "Visible"
                                                Exit Sub
                                            End If
                                            Dim ret As retvalue = Await SwitchDevice(SwitchToState)
                                        End If
                                    End Sub)

        End Get
    End Property

    Public ReadOnly Property OpenButtonCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.OpenButtonCommand()", "executed")
                                        Dim switchToState As String
                                        Select Case SwitchType
                                            Case "Blinds"
                                                switchToState = [OFF]
                                            Case "Blinds Inverted"
                                                switchToState = [ON]
                                            Case Else
                                                switchToState = [OFF]
                                        End Select
                                        If [Protected] Then
                                            SwitchingToState = switchToState
                                            app.myViewModel.selectedDevice = Me
                                            app.myViewModel.PasswordEntryVisibility = "Visible"
                                            Exit Sub
                                        End If
                                        Dim ret As retvalue = Await SwitchDevice(switchToState)
                                    End Sub)

        End Get
    End Property

    Public ReadOnly Property CloseButtonCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.CloseButtonCommand()", "executed")
                                        Dim switchToState As String
                                        Select Case SwitchType
                                            Case "Blinds"
                                                switchToState = [ON]
                                            Case "Blinds Inverted"
                                                switchToState = [OFF]
                                            Case Else
                                                switchToState = [ON]
                                        End Select
                                        If [Protected] Then
                                            SwitchingToState = switchToState
                                            app.myViewModel.selectedDevice = Me
                                            app.myViewModel.PasswordEntryVisibility = "Visible"
                                            Exit Sub
                                        End If
                                        Dim ret As retvalue = Await SwitchDevice(switchToState)
                                    End Sub)

        End Get
    End Property


    Public ReadOnly Property ShowDeviceDetails As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("Device.ShowDeviceDetails()", "executed")
                                        app.myViewModel.selectedDevice = Me
                                        app.myViewModel.DeviceDetailsVisibility = "Visible"
                                        WriteToDebug(app.myViewModel.selectedDevice.Name, "should be there")
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ButtonPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.ButtonPressedCommand()", "executed")
                                        If Me.CanBeSwitched Then
                                            'Exit Sub if the device represents a group (we have seperate buttons for Groups)
                                            If Type = "Group" Then
                                                Await Me.Update()
                                                Exit Sub
                                            End If
                                            'Exit the Sub if the device is password protected. Show the password context menu, and let that handle the switch
                                            If [Protected] Then
                                                SwitchingToState = ""
                                                app.myViewModel.selectedDevice = Me
                                                app.myViewModel.PasswordEntryVisibility = "Visible"
                                                Exit Sub
                                            End If
                                            'Else, Execute the switch
                                            Dim ret As retvalue = Await SwitchDevice()
                                        Else
                                            'Only get the status of the device if it can't be switched
                                            Await Update()
                                        End If
                                    End Sub)

        End Get
    End Property


    Public Async Function SwitchGroup(ToStatus As String) As Task
        WriteToDebug("Device.SwitchGroup()", "executed")
        If [Protected] Then
            SwitchingToState = ToStatus
            app.myViewModel.selectedDevice = Me
            app.myViewModel.PasswordEntryVisibility = "Visible"
            Exit Function
        Else
            Await SwitchDevice(ToStatus)
        End If
    End Function


    Public Async Function SwitchDevice(Optional forcedSwitchToState As String = "") As Task(Of retvalue)
        'Identify what kind of device we are and in what state we're in in order to perform the switch
        Dim url, switchToState As String
        If Not forcedSwitchToState = "" Then
            switchToState = forcedSwitchToState
        Else
            If Me.isOn Then switchToState = [OFF] Else switchToState = [ON]
        End If
        Select Case Type
            Case GROUP
                url = (New Api).SwitchScene(Me.idx, switchToState, PassCode)
            Case SCENE
                url = (New Api).SwitchScene(Me.idx, [ON], PassCode)
        End Select
        Select Case SwitchType
            Case Nothing
                Exit Select
            Case PUSH_ON_BUTTON
                url = (New Api).SwitchLight(Me.idx, [ON], PassCode)
            Case PUSH_OFF_BUTTON
                url = (New Api).SwitchLight(Me.idx, OFF, PassCode)
            Case DIMMER
                url = (New Api).setDimmer(idx, switchToState)
            Case SELECTOR
                url = (New Api).setDimmer(idx, switchToState)
            Case Else
                url = (New Api).SwitchLight(Me.idx, switchToState, PassCode)
        End Select


        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Downloader).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device")
            Return New retvalue With {.err = "Error switching device", .issuccess = 0}
        Else
            If Not response.Content Is Nothing Then
                Dim domoRes As domoResponse = JsonConvert.DeserializeObject(Of domoResponse)(Await response.Content.ReadAsStringAsync())
                If domoRes.status <> "OK" Then
                    Await app.myViewModel.Notify.Update(True, domoRes.message)
                    Return New retvalue With {.err = "Error switching device", .issuccess = 0}
                Else
                    Await app.myViewModel.Notify.Update(False, "Device switched")
                End If
                Await Update()
                Return New retvalue With {.issuccess = 1}
            End If
            Return New retvalue With {.issuccess = 0, .err = "server sent empty response"}
        End If

    End Function
    Public ReadOnly Property ButtonRightTappedCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("Device.ButtonRightTappedCommand()", "executed")
                                        If Me.DetailsVisiblity = const_Visible Then
                                            Me.DetailsVisiblity = const_Collapsed
                                        Else
                                            Me.DetailsVisiblity = const_Visible
                                        End If
                                    End Sub)

        End Get
    End Property



    Public Sub New()
        LevelNamesList = New List(Of String)
        DeviceType = ""
        P1GasVisibility = const_Collapsed
        P1ElectricVisibility = const_Collapsed
        RainVisibility = const_Collapsed
        DimmerVisibility = const_Collapsed
        StatusVisibility = const_Collapsed
        TempHumBarVisibility = const_Collapsed
        GroupVisibility = const_Collapsed
        SelectorVisibility = const_Collapsed
        WindVisibility = const_Collapsed
        BlindsVisibility = const_Collapsed
        needsInitializing = False
        DetailsVisiblity = const_Collapsed
        isOn = False
        PlanIDs = New List(Of Integer)
        PassCodeInputVisibility = const_Collapsed
        MediaPlayerVisibility = const_Collapsed
        MediaPlayerMarquee = const_Collapsed
    End Sub

    ''' <summary>
    ''' Based on the JSON properties of the Device, set additional properties of the ViewModel. 
    ''' </summary>
    Public Sub Initialize()
        'Set the IconDataTemplate and .PNG path to reflect the device's TypeImg
        If IconDataTemplate Is Nothing Or IconURI = "" Then
            Select Case TypeImg
                Case "lightbulb"
                    IconURI = "ms-appx:///Images/lightbulb.png"
                    IconDataTemplate = CType(Application.Current.Resources("lightbulb"), DataTemplate)
                Case "contact"
                    IconURI = "ms-appx:///Images/magnet.png"
                    IconDataTemplate = CType(Application.Current.Resources("contact"), DataTemplate)
                Case "temperature"
                    IconURI = "ms-appx:///Images/temperature.png"
                    IconDataTemplate = CType(Application.Current.Resources("temperature"), DataTemplate)
                Case "LogitechMediaServer"
                    IconURI = "ms-appx:///Images/music.png"
                    IconDataTemplate = CType(Application.Current.Resources("music"), DataTemplate)
                Case "hardware"
                    IconURI = "ms-appx:///Images/percentage.png"
                    IconDataTemplate = CType(Application.Current.Resources("percentage"), DataTemplate)
                Case "doorbell"
                    IconURI = "ms-appx:///Images/doorbell.png"
                    IconDataTemplate = CType(Application.Current.Resources("doorbell"), DataTemplate)
                Case "door"
                    IconURI = "ms-appx:///Images/doorlock.png"
                    IconDataTemplate = CType(Application.Current.Resources("doorlock"), DataTemplate)
                Case "counter"
                    IconURI = "ms-appx:///Images/counter.png"
                    IconDataTemplate = CType(Application.Current.Resources("counter"), DataTemplate)
                Case "Media"
                    IconURI = "ms-appx:///Images/media.png"
                    IconDataTemplate = CType(Application.Current.Resources("media"), DataTemplate)
                Case "current"
                    IconURI = "ms-appx:///Images/current.png"
                    IconDataTemplate = CType(Application.Current.Resources("current"), DataTemplate)
                Case "override_mini"
                    IconURI = "ms-appx:///Images/setpoint.png"
                    IconDataTemplate = CType(Application.Current.Resources("setpoint"), DataTemplate)
                Case "error"
                    IconURI = "ms-appx:///Images/error.png"
                    IconDataTemplate = CType(Application.Current.Resources("error"), DataTemplate)
                Case "info"
                    IconURI = "ms-appx:///Images/info.png"
                    IconDataTemplate = CType(Application.Current.Resources("info"), DataTemplate)
                Case "scene"
                    IconURI = "ms-appx:///Images/scene.png"
                    IconDataTemplate = CType(Application.Current.Resources("scene"), DataTemplate)
                Case "group"
                    IconURI = "ms-appx:///Images/group.png"
                    IconDataTemplate = CType(Application.Current.Resources("group"), DataTemplate)
                Case "visibility"
                    IconURI = "ms-appx:///Images/visibility.png"
                    IconDataTemplate = CType(Application.Current.Resources("visibility"), DataTemplate)
                Case "rain"
                    IconURI = "ms-appx:///Images/rain.png"
                    IconDataTemplate = CType(Application.Current.Resources("rain"), DataTemplate)
                Case "wind"
                    IconURI = "ms-appx:///Images/wind.png"
                    IconDataTemplate = CType(Application.Current.Resources("wind"), DataTemplate)
                Case "uv"
                    IconURI = "ms-appx:///Images/uvi.png"
                    IconDataTemplate = CType(Application.Current.Resources("uvi"), DataTemplate)
                Case "dimmer"
                    IconURI = "ms-appx:///Images/dimmer.png"
                    IconDataTemplate = CType(Application.Current.Resources("dimmer"), DataTemplate)
                Case "blinds"
                    IconURI = "ms-appx:///Images/blinds.png"
                    IconDataTemplate = CType(Application.Current.Resources("blinds"), DataTemplate)
                Case "push"
                    IconURI = "ms-appx:///Images/on.png"
                    IconDataTemplate = CType(Application.Current.Resources("on"), DataTemplate)
                Case "pushoff"
                    IconURI = "ms-appx:///Images/off.png"
                    IconDataTemplate = CType(Application.Current.Resources("off"), DataTemplate)
                Case Else
                    IconURI = "ms-appx:///Images/unknown.png"
                    IconDataTemplate = CType(Application.Current.Resources("unknown"), DataTemplate)
            End Select

            If app.myViewModel.TiczSettings.UseBitmapIcons Then
                If app.myViewModel.TiczSettings.SwitchIconBackground Then
                    IconVisibility = const_Visible
                    BitmapIconVisibility = const_Collapsed
                    VectorIconVisibility = const_Collapsed
                Else
                    BitmapIconVisibility = const_Visible
                    IconVisibility = const_Collapsed
                    VectorIconVisibility = const_Collapsed
                End If
            Else
                IconVisibility = const_Collapsed
                BitmapIconVisibility = const_Collapsed
                VectorIconVisibility = const_Visible
            End If
        End If


        'Set Dimmer Range, for use with the Slider Control which represents the Dimmer
        If MaxDimLevel = 15 Then
            MinDimmerLevel = 1
            MaxDimmerLevel = 15
        End If
        If MaxDimLevel = 100 Then
            MinDimmerLevel = 1
            MaxDimmerLevel = 100
        End If

        'Set Selecttor Value

        If Not LevelNames = "" Then
            If LevelNamesList.Count = 0 Then
                Dim arrLevelNames() As String = LevelNames.Split("|")
                LevelNamesList = arrLevelNames.ToList()
                If LevelInt Mod 10 > 0 Then
                    'Dimmer Level not set to a 10-value, therefore illegal
                    SelectedLevelName = ""
                Else
                    If LevelNamesList.Count > (LevelInt / 10) Then
                        SelectedLevelName = LevelNamesList((LevelInt / 10))
                    Else
                        SelectedLevelName = ""
                    End If
                End If
            End If
        End If

        ' Set if the Device can be switched or not
        Select Case Type
            Case P1_SMART_METER
                CanBeSwitched = False
                If SubType = P1_GAS Then P1GasVisibility = const_Visible
                If SubType = P1_ELECTRIC Then P1ElectricVisibility = const_Visible
            Case LIGHTING_LIMITLESS
                CanBeSwitched = True
            Case LIGHTING_2
                CanBeSwitched = True
            Case SCENE
                StatusVisibility = const_Visible
                DeviceType = SCENE
                CanBeSwitched = True
            Case GROUP
                GroupVisibility = const_Visible
                DeviceType = GROUP
                CanBeSwitched = True
            Case WIND
                WindVisibility = const_Visible
                DeviceType = WIND
                CanBeSwitched = False
            Case TYPE_RAIN
                RainVisibility = const_Visible
                DeviceType = TYPE_RAIN
                CanBeSwitched = False
            Case TEMP_HUMI_BARO
                TempHumBarVisibility = const_Visible
                DeviceType = TEMP_HUMI_BARO
                CanBeSwitched = False
            Case LIGHT_SWITCH
                CanBeSwitched = True
            Case Else
                StatusVisibility = const_Visible
                CanBeSwitched = False
        End Select

        'Set the Status for the switch (On or Off, which is used for Icon indication
        If CanBeSwitched Then
            Select Case SwitchType
                Case ON_OFF
                    StatusVisibility = const_Visible
                    DeviceType = ON_OFF
                    If Status = [ON] Then isOn = True Else isOn = False
                Case DOOR_LOCK
                    StatusVisibility = const_Visible
                    DeviceType = DOOR_LOCK
                    If Status = OPEN Then isOn = True Else isOn = False
                Case CONTACT
                    StatusVisibility = const_Visible
                    DeviceType = CONTACT
                    If Status = OPEN Then isOn = True Else isOn = False
                Case BLINDS
                    BlindsVisibility = const_Visible
                    DeviceType = BLINDS
                    If Status = OPEN Then isOn = False Else isOn = True
                Case BLINDS_INVERTED
                    BlindsVisibility = const_Visible
                    DeviceType = BLINDS
                    If Status = OPEN Then isOn = True Else isOn = False
                Case DIMMER
                    DimmerVisibility = const_Visible
                    DeviceType = DIMMER
                    If Status = [OFF] Then isOn = False Else isOn = True
                Case MEDIA_PLAYER
                    MediaPlayerVisibility = const_Visible
                    DeviceType = MEDIA_PLAYER
                    If Status = [OFF] Then isOn = False Else isOn = True
                Case SELECTOR
                    SelectorVisibility = const_Visible
                    DeviceType = SELECTOR
                    If Status = [OFF] Then isOn = False Else isOn = True
                Case Else
                    Select Case Type
                        Case GROUP
                            If Status = [OFF] Then isOn = False Else isOn = True
                        Case SCENE
                            If Status = [OFF] Then isOn = False Else isOn = True
                        Case Else
                            StatusVisibility = const_Visible
                            DeviceType = GENERAL
                    End Select
            End Select
        End If
    End Sub
End Class

Public Class Group(Of T)
    Inherits ObservableCollection(Of T)

    Public ReadOnly Property vm As Ticz.TiczViewModel
        Get
            Return CType(Application.Current, App).myViewModel
        End Get

    End Property

    Public Sub New(name As String, items As IEnumerable(Of T))
        Me.Key = name
        For Each item As T In items
            Me.Add(item)
        Next
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim that As Group(Of T) = TryCast(obj, Group(Of T))

        Return (that IsNot Nothing) AndAlso (Me.Key.Equals(that.Key))
    End Function

    Public Property Key As String
        Get
            Return m_Key
        End Get
        Set(value As String)
            m_Key = value
        End Set
    End Property
    Private m_Key As String
End Class

Public Class Rooms
    Public Property rooms As List(Of Room)

    Public Sub New()
        rooms = New List(Of Room)
        rooms.Add(New Room With {.RoomName = "Room1"})
        rooms.Add(New Room With {.RoomName = "Room2"})
        rooms.Add(New Room With {.RoomName = "Room3"})
        rooms.Add(New Room With {.RoomName = "Room4"})

    End Sub
End Class



Public Class Room
    Public ReadOnly Property vm As Ticz.TiczViewModel
        Get
            Return CType(Application.Current, App).myViewModel
        End Get

    End Property

    Public Property RoomName As String
    Public Property RoomIDX As String
    Public Property DeviceGroups As ObservableCollection(Of Group(Of Device))



    Public Sub New()

    End Sub

    Public Overloads Async Function LoadDevicesForRoom() As Task(Of IEnumerable(Of Device))
        Dim url As String = (New Api).getAllDevicesForRoom(RoomIDX)
        Dim response As HttpResponseMessage = Await (New Downloader).DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim deserialized = JsonConvert.DeserializeObject(Of Devices)(body)
            Return deserialized.result
        Else
            Return Nothing
        End If
    End Function
End Class


Public Class Plans
    Public Property result As ObservableCollection(Of Plan)
    Public Property status As String
    Public Property title As String

    Private app As App = CType(Application.Current, App)

    Public Sub New()
        result = New ObservableCollection(Of Plan)
    End Sub

    Public Async Function Load() As Task(Of retvalue)
        Dim response As HttpResponseMessage = Await (New Downloader).DownloadJSON((New Api).getPlans)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim deserialized = JsonConvert.DeserializeObject(Of Plans)(body)
            result.Clear()
            For Each r In deserialized.result
                result.Add(r)
            Next
            Me.status = deserialized.status
            Me.title = deserialized.status
            Return New retvalue With {.issuccess = True}
        Else
            WriteToDebug("Plans.Load()", response.ReasonPhrase)
            Await app.myViewModel.Notify.Update(True, response.ReasonPhrase)
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


Public Class ToastMessageViewModel
    Inherits ViewModelBase

    Public Property popupTask As Task

    Public Property msg As String
        Get
            Return _msg
        End Get
        Set(value As String)
            _msg = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _msg As String
    Public Property IconDataTemplate As DataTemplate
        Get
            If isError Then Return CType(Application.Current.Resources("error"), DataTemplate) Else Return CType(Application.Current.Resources("info"), DataTemplate)
        End Get
        Set(value As DataTemplate)
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _IconDataTemplate As DataTemplate

    Public Property isGoing As Boolean
        Get
            Return _isGoing
        End Get
        Set(value As Boolean)
            _isGoing = value
            RaisePropertyChanged("isGoing")
        End Set
    End Property
    Private Property _isGoing As Boolean

    Public Property isError As Boolean
        Get
            Return _isError
        End Get
        Set(value As Boolean)
            _isError = value
            RaisePropertyChanged()
            RaisePropertyChanged("IconDataTemplate")
        End Set
    End Property

    Private Property _isError As Boolean
    Public Property secondsToShow As Integer

    Public cts As New CancellationTokenSource
    Public ct As CancellationToken = cts.Token

    Private Async Function ShowMessage(message As String, err As Boolean, intSeconds As Integer, ct As CancellationToken) As Task
        Dim timeWaited As Integer
        While Not ct.IsCancellationRequested
            Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                             isGoing = False
                                                                                                             msg = message
                                                                                                             isError = err
                                                                                                         End Sub)
            If intSeconds > 0 Then
                If intSeconds * 1000 > timeWaited Then
                    timeWaited += 100
                    Await Task.Delay(100)
                Else
                    cts.Cancel()
                End If
            End If
        End While
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         isGoing = True
                                                                                                     End Sub)
    End Function


    Public Sub Clear()
        If ct.CanBeCanceled Then
            cts.Cancel()
        End If
    End Sub

    Public Async Function Update(err As Boolean, message As String, Optional seconds As Integer = 2) As Task
        WriteToDebug("ToasMessageViewModel.Update()", "executed")
        If ct.CanBeCanceled Then
            cts.Cancel()
        End If
        cts = New CancellationTokenSource
        ct = cts.Token
        popupTask = Await Task.Factory.StartNew(Function() ShowMessage(message, err, seconds, ct), ct)

    End Function

    Public Sub New()
        isGoing = True
    End Sub
End Class


Public Class TiczViewModel
    Inherits ViewModelBase

    Public Property selectedDevice As Device
        Get
            Return _selectedDevice
        End Get
        Set(value As Device)
            _selectedDevice = value
            RaisePropertyChanged()
        End Set
    End Property
    Private _selectedDevice As Device

    Public Property MyRooms As ObservableCollection(Of Room)
    Public Property MyPlans As New Plans
    Public Property TiczSettings As New AppSettings
    Public Property Notify As ToastMessageViewModel
    Public Property DeviceDetailsVisibility As String
        Get
            Return _DeviceDetailsVisibility
        End Get
        Set(value As String)
            _DeviceDetailsVisibility = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _DeviceDetailsVisibility As String

    Public Property PasswordEntryVisibility As String
        Get
            Return _PasswordEntryVisibility
        End Get
        Set(value As String)
            _PasswordEntryVisibility = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _PasswordEntryVisibility As String

    Public Property TiczRefresher As Task
    Public ct As CancellationToken
    Public tokenSource As New CancellationTokenSource()

    Public ReadOnly Property CancelPasswordEntry As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("TiczViewModel.CancelPasswordEntry()", "executed")
                                        PasswordEntryVisibility = "Collapsed"
                                        selectedDevice.PassCode = ""
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ConfirmPasswordEntry As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("TiczViewModel.ConfirmPasswordEntry()", "executed")
                                        PasswordEntryVisibility = "Collapsed"
                                        If Not selectedDevice.PassCode = "" Then
                                            Await selectedDevice.SwitchDevice(selectedDevice.SwitchingToState)
                                        End If
                                        selectedDevice.PassCode = ""

                                    End Sub)
        End Get
    End Property


    Public ReadOnly Property HideDeviceDetails As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        DeviceDetailsVisibility = "Collapsed"
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property GoToSettingsCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
                                        If Not rootFrame.Navigate(GetType(AppSettingsPage)) Then
                                            Throw New Exception("Couldn't nagivate to settings page")
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property NavigateBackCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
                                        If rootFrame.CanGoBack Then rootFrame.GoBack()
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property GoToAboutCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
                                        If Not rootFrame.Navigate(GetType(AboutPage)) Then
                                            Throw New Exception("Couldn't nagivate to settings page")
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property RefreshCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await Refresh()
                                    End Sub)
        End Get
    End Property

    Public Property myDevices As Devices
        Get
            Return _myDevices
        End Get
        Set(value As Devices)
            _myDevices = value
            RaisePropertyChanged("myDevices")
        End Set
    End Property
    Private Property _myDevices As Devices

    Public Sub New()
        selectedDevice = New Device
        DeviceDetailsVisibility = "Collapsed"
        PasswordEntryVisibility = "Collapsed"
        MyRooms = New ObservableCollection(Of Room)
        Notify = New ToastMessageViewModel
        myDevices = New Devices
    End Sub

    Public Async Sub StartRefresh()
        WriteToDebug("TiczViewModel.StartRefresh()", "")
        If TiczRefresher Is Nothing OrElse TiczRefresher.IsCompleted Then
            If TiczSettings.SecondsForRefresh > 0 Then
                tokenSource = New CancellationTokenSource
                ct = tokenSource.Token
                TiczRefresher = Await Task.Factory.StartNew(Function() PerformAutoRefresh(ct), ct)
            Else
                WriteToDebug("TiczViewModel.StartRefresh()", "SecondsForRefresh = 0, not starting background task...")
            End If
        End If
    End Sub

    Public Sub StopRefresh()
        If ct.CanBeCanceled Then
            tokenSource.Cancel()
        End If
        WriteToDebug("TiczViewModel.StopRefresh()", "")
    End Sub


    Public Async Function PerformAutoRefresh(ct As CancellationToken) As Task
        Dim refreshperiod As Integer = TiczSettings.SecondsForRefresh
        While Not ct.IsCancellationRequested
            WriteToDebug("TiczViewModel.PerformAutoRefresh", "executed")
            Dim i As Integer = 0
            WriteToDebug("TiczViewModel.PerformAutoRefresh", "sleeping")
            While i < refreshperiod * 1000
                Await Task.Delay(250)
                i += 250
                If ct.IsCancellationRequested Then Exit While
            End While
            If ct.IsCancellationRequested Then Exit While
            WriteToDebug("TiczViewModel.PerformAutoRefresh", "refreshing")
            Await Refresh()
        End While

    End Function

    Public Async Function Refresh() As Task
        Await Notify.Update(False, "refreshing...", 0)
        Dim sWatch = Stopwatch.StartNew()

        'Get all devices
        Dim dev_response = Await Task.Run(Function() (New Downloader).DownloadJSON((New Api).getAllDevices()))
        If dev_response.IsSuccessStatusCode Then
            Dim refreshedDevices = JsonConvert.DeserializeObject(Of Devices)(Await dev_response.Content.ReadAsStringAsync)
            If Not refreshedDevices Is Nothing Then
                For Each d In myDevices.result
                    'Send each devices it's up-to-date status so it can update itself
                    Await d.Update((From dev In refreshedDevices.result Where dev.idx = d.idx And dev.Name = d.Name Select dev).FirstOrDefault())
                Next
            End If
        Else
            Await Notify.Update(True, "couldn't load device status", 2)
        End If

        'Get all scenes
        Dim grp_response = Await Task.Run(Function() (New Downloader).DownloadJSON((New Api).getAllScenes()))
        If grp_response.IsSuccessStatusCode Then
            Dim refreshedScenes = JsonConvert.DeserializeObject(Of Devices)(Await grp_response.Content.ReadAsStringAsync)
            If Not refreshedScenes Is Nothing Then
                For Each d In (myDevices.result.Where(Function(x) x.Type = "Group" Or x.Type = "Scene").ToList())
                    'Send each scene it's up-to-date status so it can update itself
                    Await d.Update((From dev In refreshedScenes.result Where dev.idx = d.idx Select dev).FirstOrDefault())
                Next
            End If
        Else
            Await Notify.Update(True, "couldn't load scene/group status", 2)
        End If


        'Clear the Notification
        If dev_response.IsSuccessStatusCode AndAlso grp_response.IsSuccessStatusCode Then
            'But only if the amount of time passed for the Refresh is around 500ms (approx. time for the animation showing "Refreshing" to be on the screen
            If sWatch.ElapsedMilliseconds < 500 Then
                WriteToDebug("TiczViewModel.Refresh()", String.Format("Refresh took {0} ms", sWatch.ElapsedMilliseconds))
                Await Task.Delay(500 - sWatch.ElapsedMilliseconds)
            End If
            Notify.Clear()
        End If
    End Function
End Class
