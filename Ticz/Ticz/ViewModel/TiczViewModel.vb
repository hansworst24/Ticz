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
                Await r.Update(r)
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
    Public Property BatteryLevel As Integer
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
    Public Property Favorite As Integer
    Public Property HardwareID As Integer
    Public Property HardwareName As String
    Public Property HardwareType As String
    Public Property HardwareTypeVal As Integer
    Public Property HaveDimmer As Boolean
    Public Property HaveGroupCmd As Boolean
    Public Property HaveTimeout As Boolean
    Public Property ID As String
    Public Property Image As String
    Public Property IsSubDevice As Boolean
    Public Property LastUpdate As String
    Public Property Level As Integer
    Public Property LevelInt As Integer
    Public Property MaxDimLevel As Integer
    Public Property Name As String
    Public Property Notifications As String
    Public Property PlanID As String
    Public Property PlanIDs As List(Of Integer)
    Public Property [Protected] As Boolean
    Public Property ShowNotifications As Boolean
    Public Property SignalLevel As String
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
    Public Property Timers As String
    Public Property Type As String
    Public Property TypeImg As String
    Public Property Unit As Integer
    Public Property Used As Integer
    Public Property UsedByCamera As Boolean
    Public Property XOffset As String
    Public Property YOffset As String
    Public Property idx As String
    Public Property CameraIdx As String
#End Region
#Region "ViewModel Properties"

    Public Property IconVisibility As String
    Public Property BitmapIconVisibility As String
    Public Property VectorIconVisibility As String
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

    Public Property OnOffButtonVisibility As String
        Get
            Return _OnOffButtonVisibility
        End Get
        Set(value As String)
            _OnOffButtonVisibility = value
            RaisePropertyChanged("OnOffButtonVisibility")
        End Set
    End Property
    Private Property _OnOffButtonVisibility As String

    Public Property SwitchOnURI As String
    Public Property SwitchOffURI As String

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

    'Public Property ShowPassCodeInput As Boolean
    '    Get
    '        Return _ShowPassCodeInput
    '    End Get
    '    Set(value As Boolean)
    '        _ShowPassCodeInput = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _ShowPassCodeInput As Boolean


    Public Property DataVisibility As String
        Get
            Return _DataVisibility
        End Get
        Set(value As String)
            _DataVisibility = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _DataVisibility As String
    'Public Property ShowData As Boolean
    '    Get
    '        Return _ShowData
    '    End Get
    '    Set(value As Boolean)
    '        _ShowData = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _ShowData As Boolean

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

    'Public Property ShowOnOffButtons As Boolean
    '    Get
    '        Return _ShowOnOffButtons
    '    End Get
    '    Set(value As Boolean)
    '        _ShowOnOffButtons = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _ShowOnOffButtons As Boolean


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
    'Public Property IconURI As String
    '    Get
    '        Dim DomoticzIP As String = (New Api).serverIP
    '        Dim DomoticzPort As String = (New Api).serverPort
    '        Select Case SwitchType
    '            Case "On/Off"
    '                Return "ms-appx:///Images/lightbulb.svg"
    '            Case "Contact"
    '                Return "ms-appx:///Images/magnet.png"
    '            Case "Doorbell"
    '                Return String.Format("http://{0}:{1}/images/doorbell48.png", DomoticzIP, DomoticzPort)
    '            Case "Door Lock"
    '                If Status = "Open" Then Return String.Format("http://{0}:{1}/images/door48open.png", DomoticzIP, DomoticzPort)
    '                If Status = "Closed" Then Return String.Format("http://{0}:{1}/images/door48.png", DomoticzIP, DomoticzPort)
    '            Case "Dimmer"
    '                If Status = "On" Then Return String.Format("http://{0}:{1}/images/Dimmer48_On.png", DomoticzIP, DomoticzPort)
    '                If Status = "Off" Then Return String.Format("http://{0}:{1}/images/Dimmer48_Off.png", DomoticzIP, DomoticzPort)
    '            Case "Blinds"
    '                If Status = "Open" Then Return String.Format("http://{0}:{1}/images/blindsopen48sel.png", DomoticzIP, DomoticzPort)
    '                If Status = "Closed" Then Return String.Format("http://{0}:{1}/images/blinds48sel.png", DomoticzIP, DomoticzPort)
    '            Case "Smoke Detector"
    '                If Status = "On" Then Return String.Format("http://{0}:{1}/images/smoke48on.png", DomoticzIP, DomoticzPort)
    '                If Status = "Off" Then Return String.Format("http://{0}:{1}/images/smoke48off.png", DomoticzIP, DomoticzPort)
    '            Case "X10 Siren"
    '                If Status = "On" Then Return String.Format("http://{0}:{1}/images/siren-on.png", DomoticzIP, DomoticzPort)
    '                If Status = "Off" Then Return String.Format("http://{0}:{1}/images/siren-off.png", DomoticzIP, DomoticzPort)
    '            Case "Media Player"
    '                If Status = "On" Then Return String.Format("http://{0}:{1}/images/LogitechMediaServer48_On.png", DomoticzIP, DomoticzPort)
    '                If Status = "Playing" Then Return String.Format("http://{0}:{1}/images/LogitechMediaServer48_On.png", DomoticzIP, DomoticzPort)
    '                If Status = "Off" Then Return String.Format("http://{0}:{1}/images/LogitechMediaServer48_Off.png", DomoticzIP, DomoticzPort)

    '            Case Nothing
    '                Select Case Type
    '                    Case "Scene"
    '                        Return String.Format("http://{0}:{1}/images/scenes.png", DomoticzIP, DomoticzPort)
    '                    Case "General"
    '                        Select Case SubType
    '                            Case "Percentage"
    '                                Return "ms-appx:///Images/percentage.png"
    '                        End Select
    '                    Case "Usage"
    '                        Select Case SubType
    '                            Case "Electric"
    '                                Return "ms-appx:///Images/power.png"
    '                        End Select
    '                    Case "Temp"
    '                        Return "ms-appx:///Images/temperature.png"
    '                    Case "P1 Smart Meter"
    '                        Select Case SubType
    '                            Case "Gas"
    '                                Return String.Format("http://{0}:{1}/images/Gas48.png", DomoticzIP, DomoticzPort)
    '                            Case "Energy"
    '                                Return String.Format("http://{0}:{1}/images/Counter48.png", DomoticzIP, DomoticzPort)
    '                        End Select
    '                    Case "Thermostat"
    '                        Return String.Format("http://{0}:{1}/images/override.png", DomoticzIP, DomoticzPort)
    '                    Case Else

    '                        Return String.Format("http://{0}:{1}/images/current48.png", DomoticzIP, DomoticzPort)
    '                End Select
    '            Case Else
    '                If Status = "On" Then Return String.Format("http://{0}:{1}/images/Light48_On.png", DomoticzIP, DomoticzPort)
    '                If Status = "Off" Then Return String.Format("http://{0}:{1}/images/Light48_Off.png", DomoticzIP, DomoticzPort)

    '        End Select
    '    End Get
    '    Set(value As String)
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _IconURI As String

#End Region

    Private app As App = CType(Application.Current, App)

    Const const_Visible As String = "Visible"
    Const const_Collapsed As String = "Collapsed"
    Const switchOn As String = "On"
    Const switchOff As String = "Off"
    Const contactOpen As String = "Open"
    Const groupMixed As String = "Mixed"


    Public Property IsDimmer As Boolean
    Public Property CanBeSwitched As Boolean


    Public Async Function Update(Optional d As Device = Nothing) As Task
        'Check if the device's Icon has been set (either PNG or vector image)
        If IconDataTemplate Is Nothing Then
            Me.Initialize()
        End If

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
                                                                                                         Status = d.Status
                                                                                                         Data = d.Data
                                                                                                         If Status = "" Then Status = Data
                                                                                                         If Not SwitchType Is Nothing Then
                                                                                                             Select Case SwitchType
                                                                                                                 Case "On/Off"
                                                                                                                     CanBeSwitched = True
                                                                                                                     If Status = switchOn Then isOn = True Else isOn = False
                                                                                                                 Case "Media Player"
                                                                                                                     CanBeSwitched = True
                                                                                                                     If Status = switchOff Then isOn = False Else isOn = True
                                                                                                                 Case "Contact"
                                                                                                                     CanBeSwitched = True
                                                                                                                     If Status = contactOpen Then isOn = True Else isOn = False
                                                                                                             End Select
                                                                                                         Else
                                                                                                             If Not Type Is Nothing Then
                                                                                                                 Select Case Type
                                                                                                                     Case "Scene"
                                                                                                                         CanBeSwitched = True
                                                                                                                         If Status = switchOff Then isOn = False Else isOn = True
                                                                                                                     Case "Group"
                                                                                                                         CanBeSwitched = True
                                                                                                                         Select Case Status
                                                                                                                             Case switchOff
                                                                                                                                 isOn = False
                                                                                                                                 isMixed = False
                                                                                                                             Case switchOn
                                                                                                                                 isOn = True
                                                                                                                                 isMixed = False
                                                                                                                             Case groupMixed
                                                                                                                                 isOn = True
                                                                                                                                 isMixed = True
                                                                                                                         End Select
                                                                                                                     Case Else
                                                                                                                         CanBeSwitched = False
                                                                                                                         isOn = True
                                                                                                                 End Select
                                                                                                             End If
                                                                                                         End If
                                                                                                         needsInitializing = False
                                                                                                     End Sub)

    End Function


    'Public Sub setStatus()

    '    If Not SwitchType Is Nothing Then
    '        Select Case SwitchType
    '            Case "On/Off"
    '                CanBeSwitched = True
    '                If Status = switchOn Then isOn = True Else isOn = False
    '            Case "Media Player"
    '                CanBeSwitched = True
    '                If Status = switchOff Then isOn = False Else isOn = True
    '            Case "Contact"
    '                CanBeSwitched = True
    '                If Status = contactOpen Then isOn = True Else isOn = False
    '        End Select
    '    Else
    '        If Not Type Is Nothing Then
    '            Select Case Type
    '                Case "Scene"
    '                    CanBeSwitched = True
    '                    If Status = switchOff Then isOn = False Else isOn = True
    '                Case "Group"
    '                    CanBeSwitched = True
    '                    Select Case Status
    '                        Case switchOff
    '                            isOn = False
    '                            isMixed = False
    '                        Case switchOn
    '                            isOn = True
    '                            isMixed = False
    '                        Case groupMixed
    '                            isOn = True
    '                            isMixed = True
    '                    End Select
    '                Case Else
    '                    CanBeSwitched = False
    '                    isOn = True
    '            End Select
    '        End If
    '    End If

    'End Sub

    'Public Async Function getStatus() As Task(Of retvalue)
    'Await Task.Delay(2000)
    'Dim response As HttpResponseMessage
    'If Type = "Group" Or Type = "Scene" Then
    '        response = Await Task.Run(Function() (New Downloader).DownloadJSON((New Api).getSceneStatus()))
    '    Else
    '        response = Await Task.Run(Function() (New Downloader).DownloadJSON((New Api).getDeviceStatus(Me.idx)))
    '    End If

    'If response.IsSuccessStatusCode Then
    'Dim deserialized = JsonConvert.DeserializeObject(Of Devices)(Await response.Content.ReadAsStringAsync)
    'Dim myDevice As Device = (From d In deserialized.result Where d.idx = idx Select d).FirstOrDefault()
    '        If Not myDevice Is Nothing Then
    '            Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
    '                                                                                                             Me.Status = myDevice.Status
    '                                                                                                             Me.Data = myDevice.Data
    '                                                                                                             'Show Data Field as Status when the Status Field is empty
    '                                                                                                             If Me.Status = "" Then Me.Status = Me.Data
    '                                                                                                             setStatus()
    '                                                                                                             needsInitializing = False
    '                                                                                                         End Sub)
    '            Return New retvalue With {.issuccess = 1}
    '        Else
    '            'app.myViewModel.Notify.Update(True, "Error getting device status")
    '            Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
    '                                                                                                             Me.needsInitializing = False
    '                                                                                                         End Sub)

    '            Return New retvalue With {.issuccess = 0, .err = "Error getting device status"}
    '        End If
    '    Else
    '        'app.myViewModel.Notify.Update(True, "Error getting device status") 
    '        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
    '                                                                                                         Me.needsInitializing = False
    '                                                                                                     End Sub)

    '        Return New retvalue With {.issuccess = 0, .err = "Error getting device status"}
    '    End If
    '    Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
    '                                                                                                     needsInitializing = False
    '                                                                                                 End Sub)

    'End Function

    Public ReadOnly Property GroupSwitchOn As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchGroup(switchOn)
                                    End Sub)

        End Get
    End Property

    Public ReadOnly Property GroupSwitchOff As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchGroup(switchOff)
                                    End Sub)

        End Get
    End Property




    Public ReadOnly Property ButtonPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.ButtonPressedCommand()", "executed")
                                        If Me.CanBeSwitched Then
                                            Dim switchToState As String
                                            Dim url As String
                                            Me.needsInitializing = True
                                            'Exit Sub if the device represents a group (we have seperate buttons for Groups)
                                            If Type = "Group" Then
                                                If OnOffButtonVisibility = const_Collapsed Then
                                                    OnOffButtonVisibility = const_Visible
                                                Else
                                                    OnOffButtonVisibility = const_Collapsed
                                                    If [Protected] Then PassCodeInputVisibility = const_Collapsed
                                                End If
                                                Await Me.Update()
                                                Me.needsInitializing = False
                                                Exit Sub
                                            End If
                                            'Set switchTo state to opposite of current setting
                                            If Me.isOn Then switchToState = switchOff Else switchToState = switchOn
                                            'Open the PassCode box if the device/group/scene is protected and the PassCode box is not visible
                                            If [Protected] And PassCodeInputVisibility = const_Collapsed Then
                                                PassCodeInputVisibility = const_Visible
                                                Me.needsInitializing = False
                                                Exit Sub
                                            End If
                                            'Close the passcode input if it's open and no passcode was provided
                                            If [Protected] And PassCodeInputVisibility = const_Visible And PassCode = "" Then
                                                PassCodeInputVisibility = const_Collapsed
                                                Me.needsInitializing = False
                                                Exit Sub
                                            End If
                                            'Set the URI for switching the device
                                            If Type = "Group" Then
                                                url = (New Api).SwitchScene(Me.idx, switchToState, PassCode)
                                            ElseIf Type = "Scene" Then
                                                'Scenes can only be turned on
                                                url = (New Api).SwitchScene(Me.idx, switchOn, PassCode)
                                            ElseIf SwitchType = "On/Off" Or SwitchType = "Media Player" Or SwitchType = "Contact" Then
                                                url = (New Api).SwitchLight(Me.idx, switchToState, PassCode)
                                            Else
                                                'Exit Sub for the moment, if the device is neither of the types we checked
                                                Await Update()
                                                PassCodeInputVisibility = const_Collapsed
                                                Exit Sub
                                            End If
                                            PassCodeInputVisibility = const_Collapsed

                                            'Execute the switch
                                            If Not url = "" Then
                                                If Me.PassCode <> "" Then Me.PassCode = ""
                                                Dim ret As retvalue = Await SwitchDevice(url)
                                                Me.needsInitializing = False
                                                Await Update()
                                            End If
                                        Else
                                            'Only get the status of the device if it can't be switched
                                            Await Update()
                                        End If


                                    End Sub)

        End Get
    End Property


    Public Async Function SwitchGroup(ToStatus As String) As Task
        WriteToDebug("Device.SwitchGroup()", "executed")
        If [Protected] And PassCodeInputVisibility = const_Collapsed Then
            PassCodeInputVisibility = const_Visible
            Exit Function
        End If
        If [Protected] And PassCodeInputVisibility = const_Visible And PassCode = "" Then
            PassCodeInputVisibility = const_Collapsed
            OnOffButtonVisibility = const_Collapsed
            PassCode = ""
            Exit Function
        End If
        If [Protected] And PassCodeInputVisibility = const_Visible And PassCode <> "" Then
            Await SwitchDevice((New Api).SwitchScene(idx, ToStatus, PassCode))
            PassCodeInputVisibility = const_Collapsed
            OnOffButtonVisibility = const_Collapsed
            PassCode = ""
            Await Me.Update()
            Me.needsInitializing = False
        Else
            Await SwitchDevice((New Api).SwitchScene(idx, ToStatus))
        End If

    End Function


    ''' <summary>
    ''' Switch the Device On/Off based on the URL provided. The URL can either be for a normal switch, or for a protected switch
    ''' </summary>
    ''' <param name="url"></param>
    ''' <returns></returns>
    Public Async Function SwitchDevice(url As String) As Task(Of retvalue)
        Dim response As HttpResponseMessage = Await (New Downloader).DownloadJSON(url)
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
        OnOffButtonVisibility = const_Collapsed
        needsInitializing = False
        DetailsVisiblity = const_Collapsed
        isOn = False
        PlanIDs = New List(Of Integer)
        PassCodeInputVisibility = const_Collapsed
        DataVisibility = const_Collapsed
    End Sub

    ''' <summary>
    ''' Based on the JSON properties of the Device, set additional properties of the ViewModel. 
    ''' </summary>
    Public Sub Initialize()
        'Set the IconDataTemplate to reflect the device's Type
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

        'Check if the device supports switching on/off.
        'TODO : Probably better logic exists on how to determine if a device can switch or not, but for now this will do.
        If Not SwitchType Is Nothing Then
            Select Case SwitchType
                Case "On/Off"
                    CanBeSwitched = True
                    If Status = "On" Then isOn = True Else isOn = False
                Case "Media Player"
                    CanBeSwitched = True
                    If Status = "Off" Then
                        isOn = False
                        DataVisibility = const_Collapsed
                    Else
                        isOn = True
                        If app.myViewModel.TiczSettings.ShowMarquee Then DataVisibility = const_Visible Else DataVisibility = const_Collapsed
                    End If
                Case "Contact"
                    CanBeSwitched = True
                    If Status = "Open" Then isOn = True Else isOn = False
            End Select
        Else
            If Not Type Is Nothing Then
                Select Case Type
                    Case "Scene"
                        CanBeSwitched = True
                        If Status = "Off" Then isOn = False Else isOn = True
                    Case "Group"
                        CanBeSwitched = True
                        If Status = "Off" Then isOn = False Else isOn = True
                    Case Else
                        CanBeSwitched = False
                        isOn = True
                End Select
            End If
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
    'Public Property DeviceGroups As List(Of Devices)
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

Public Class ToastListViewModel
    Public Property Messages As ObservableCollection(Of ToastMessageViewModel)

    Public Sub New()
        Messages = New ObservableCollection(Of ToastMessageViewModel)
    End Sub

    Public Async Function AddMessage(msg As ToastMessageViewModel) As Task
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         Messages.Insert(0, msg)
                                                                                                     End Sub)
        Await Task.Delay(New TimeSpan(0, 0, msg.secondsToShow))
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         msg.isGoing = True
                                                                                                     End Sub)

        Await Task.Delay(1000)
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         Messages.Remove(msg)
                                                                                                     End Sub)
    End Function

End Class

Public Class ToastMessageViewModel
    Inherits ViewModelBase

    Public Property popupTask As Task


    'Public ReadOnly Property NotificationMargin As Thickness

    '    Get
    '        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
    '        If Not rootFrame Is Nothing Then
    '            Dim p As Page = TryCast(rootFrame.Content, Page)
    '            If Not p Is Nothing Then
    '                If p.BottomAppBar.IsOpen Then
    '                    Return New Thickness(0, 0, 0, 12)
    '                Else
    '                    Return New Thickness(0, 0, 0, 0)
    '                End If
    '            End If
    '        Else
    '            Return New Thickness(0, 0, 0, 0)
    '        End If

    '    End Get
    'End Property





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
            'RaisePropertyChanged("NotificationMargin")
        End Set
    End Property
    Private Property _isError As Boolean

    Public Property secondsToShow As Integer

    'Public Async Function Update(err As Boolean, show As Integer, message As String) As Task
    '    isGoing = False
    '    Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
    '                                                                                                     isError = err
    '                                                                                                 End Sub)

    '    Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
    '                                                                                                     msg = message
    '                                                                                                 End Sub)
    '    Await Task.Delay(New TimeSpan(0, 0, show))
    '    Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
    '                                                                                                     isGoing = True
    '                                                                                                 End Sub)
    '    Await Task.Delay(500)
    '    Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
    '                                                                                                     msg = ""
    '                                                                                                 End Sub)
    'End Function

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
        'cts.Cancel()
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

    Public Property MyRooms As ObservableCollection(Of Room)
    Public Property MyPlans As New Plans
    Public Property TiczSettings As New AppSettings
    Public Property Notify As ToastMessageViewModel
    Public Property TiczRefresher As Task
    Public ct As CancellationToken
    Public tokenSource As New CancellationTokenSource()


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

    'Public Property myLightSwitches As Light_Switches
    '    Get
    '        Return _myLightSwitches
    '    End Get
    '    Set(value As Light_Switches)
    '        _myLightSwitches = value
    '        RaisePropertyChanged("myLightSwitches")
    '    End Set
    'End Property
    'Private Property _myLightSwitches As Light_Switches

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
    'Public Property myFavourites As Devices
    '    Get
    '        Return _myFavourites
    '    End Get
    '    Set(value As Devices)
    '        _myFavourites = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _myFavourites As Devices


    Public Sub New()
        MyRooms = New ObservableCollection(Of Room)
        Notify = New ToastMessageViewModel
        'myLightSwitches = New Light_Switches
        myDevices = New Devices
        'myFavourites = New Devices
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
            'tokenSource.Dispose()
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
                    Await d.Update((From dev In refreshedDevices.result Where dev.idx = d.idx Select dev).FirstOrDefault())
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
