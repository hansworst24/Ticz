Imports System.Net
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports Newtonsoft.Json
Imports Windows.ApplicationModel.Core
Imports Windows.UI
Imports Windows.UI.Core
Imports Windows.Web.Http

Public Class DeviceStatusToColorConvertor
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
        Dim isOn As Boolean = CType(value, Boolean)
        If isOn Then
            Return Colors.Red
        Else
            Return Colors.Blue
        End If

    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
        Dim ts As TimeSpan = CType(value, TimeSpan)
        Dim dt As New Date
        Return dt.Add(ts)
    End Function
End Class

Public Class TypeImageToDataTemplateConvertor
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
        Dim imageType As String = CType(value, String)
        Select Case imageType
            Case "lightbulb"
                Return CType(Application.Current.Resources("lightbulb"), DataTemplate)
            Case "contact"
                Return CType(Application.Current.Resources("contact"), DataTemplate)
            Case "temperature"
                Return CType(Application.Current.Resources("temperature"), DataTemplate)
            Case "LogitechMediaServer"
                Return CType(Application.Current.Resources("music"), DataTemplate)
            Case "hardware"
                Return CType(Application.Current.Resources("percentage"), DataTemplate)
            Case "doorbell"
                Return CType(Application.Current.Resources("doorbell"), DataTemplate)
            Case "counter"
                Return CType(Application.Current.Resources("counter"), DataTemplate)
            Case "Media"
                Return CType(Application.Current.Resources("media"), DataTemplate)
            Case "current"
                Return CType(Application.Current.Resources("current"), DataTemplate)
            Case "override_mini"
                Return CType(Application.Current.Resources("setpoint"), DataTemplate)
            Case "error"
                Return CType(Application.Current.Resources("error"), DataTemplate)
            Case "info"
                Return CType(Application.Current.Resources("info"), DataTemplate)
            Case "scene"
                Return CType(Application.Current.Resources("scene"), DataTemplate)
            Case "group"
                Return CType(Application.Current.Resources("group"), DataTemplate)
            Case Else
                Return CType(Application.Current.Resources("unknown"), DataTemplate)

        End Select
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
        Dim ts As TimeSpan = CType(value, TimeSpan)
        Dim dt As New Date
        Return dt.Add(ts)
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


Public Class Light_Switches
    Public Property result As ObservableCollection(Of Light_Switch)
    Public Property status As String
    Public Property title As String

    Public Async Function Load() As Task
        Dim response As HttpResponseMessage = Await (New Downloader).DownloadJSON((New Api).getLightSwitches)
        Dim body As String = Await response.Content.ReadAsStringAsync()
        Dim deserialized = JsonConvert.DeserializeObject(Of Light_Switches)(body)
        For Each r In deserialized.result
            result.Add(r)
            Await r.getStatus()

        Next
        Me.status = deserialized.status
        Me.title = deserialized.status
    End Function

    Public Sub New()
        result = New ObservableCollection(Of Light_Switch)
    End Sub
End Class
Public Class Light_Switch
    Inherits ViewModelBase
    '/json.htm?type=command&param=getlightswitches
    '/json.htm?type=devices&filter=light&used=true&order=Name

    Public Property IsDimmer As Boolean
    Public Property Name As String
    Public Property SubType As String
    Public Property Type As String
    Public Property IDX As String
    Public Property isOn As Boolean
        Get
            Return _isOn
        End Get
        Set(value As Boolean)
            _isOn = value
            RaisePropertyChanged("isOn")
        End Set
    End Property
    Private Property _isOn As Boolean
    Public Property Data As String
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
        Get
            If isOn Then
                Return "http://192.168.168.4:8888/images/contact48_open.png"
            Else Return "http://192.168.168.4:8888/images/contact48.png"
            End If
        End Get
        Set(value As String)
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _IconURI As String



    Public Async Function getStatus() As Task
        Dim a = Await (New Downloader).DownloadJSON((New Api).getDeviceStatus(Me.IDX))
        Dim deserialized = JsonConvert.DeserializeObject(Of Light_Switches)(Await a.Content.ReadAsStringAsync)
        If deserialized.result(0).Data = "On" Then Me.isOn = True Else Me.isOn = False
        needsInitializing = False
    End Function

    Public ReadOnly Property LightOnOff As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim url As String
                                        If Me.isOn Then
                                            url = (New Api).SwitchLight(Me.IDX, "On")
                                        Else
                                            url = (New Api).SwitchLight(Me.IDX, "Off")
                                        End If
                                        Dim b = Await (New Downloader).DownloadJSON(url)
                                        'Re-pull the status
                                        Await Me.getStatus()
                                    End Sub)

        End Get
    End Property

    Public Sub New()
        needsInitializing = True
    End Sub
End Class
Public Class Devices
    Inherits ViewModelBase
    Public Property result As ObservableCollection(Of Device)
    Public Property status As String
    Public Property title As String



    Public Sub New()
        result = New ObservableCollection(Of Device)
    End Sub

    Public Overloads Async Function Load() As Task(Of retvalue)
        Dim response As HttpResponseMessage = Await (New Downloader).DownloadJSON((New Api).getDevices)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim deserialized = JsonConvert.DeserializeObject(Of Devices)(body)
            result.Clear()
            For Each r In deserialized.result
                'Hack to show the Data Field as Status, if there is no Status Field
                If r.Status = "" Then r.Status = r.Data
                'Set additional (ViewModel) Properties based on the received json data
                r.Initialize()

                r.setStatus()
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

    Public Property SwitchOnURI As String
    Public Property SwitchOffURI As String

    Public ReadOnly Property BatteryLevelVisibility As String
        Get
            If BatteryLevel <= 100 Then Return Visible Else Return Collapsed
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

    Public Property ShowPassCodeInput As Boolean
        Get
            Return _ShowPassCodeInput
        End Get
        Set(value As Boolean)
            _ShowPassCodeInput = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _ShowPassCodeInput As Boolean


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

    Public Property OnOffButtonVisibility As String


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
        Get
            Dim DomoticzIP As String = (New Api).serverIP
            Dim DomoticzPort As String = (New Api).serverPort
            Select Case SwitchType
                Case "On/Off"
                    Return "ms-appx:///Images/lightbulb.svg"
                Case "Contact"
                    Return "ms-appx:///Images/magnet.png"
                Case "Doorbell"
                    Return String.Format("http://{0}:{1}/images/doorbell48.png", DomoticzIP, DomoticzPort)
                Case "Door Lock"
                    If Status = "Open" Then Return String.Format("http://{0}:{1}/images/door48open.png", DomoticzIP, DomoticzPort)
                    If Status = "Closed" Then Return String.Format("http://{0}:{1}/images/door48.png", DomoticzIP, DomoticzPort)
                Case "Dimmer"
                    If Status = "On" Then Return String.Format("http://{0}:{1}/images/Dimmer48_On.png", DomoticzIP, DomoticzPort)
                    If Status = "Off" Then Return String.Format("http://{0}:{1}/images/Dimmer48_Off.png", DomoticzIP, DomoticzPort)
                Case "Blinds"
                    If Status = "Open" Then Return String.Format("http://{0}:{1}/images/blindsopen48sel.png", DomoticzIP, DomoticzPort)
                    If Status = "Closed" Then Return String.Format("http://{0}:{1}/images/blinds48sel.png", DomoticzIP, DomoticzPort)
                Case "Smoke Detector"
                    If Status = "On" Then Return String.Format("http://{0}:{1}/images/smoke48on.png", DomoticzIP, DomoticzPort)
                    If Status = "Off" Then Return String.Format("http://{0}:{1}/images/smoke48off.png", DomoticzIP, DomoticzPort)
                Case "X10 Siren"
                    If Status = "On" Then Return String.Format("http://{0}:{1}/images/siren-on.png", DomoticzIP, DomoticzPort)
                    If Status = "Off" Then Return String.Format("http://{0}:{1}/images/siren-off.png", DomoticzIP, DomoticzPort)
                Case "Media Player"
                    If Status = "On" Then Return String.Format("http://{0}:{1}/images/LogitechMediaServer48_On.png", DomoticzIP, DomoticzPort)
                    If Status = "Playing" Then Return String.Format("http://{0}:{1}/images/LogitechMediaServer48_On.png", DomoticzIP, DomoticzPort)
                    If Status = "Off" Then Return String.Format("http://{0}:{1}/images/LogitechMediaServer48_Off.png", DomoticzIP, DomoticzPort)

                Case Nothing
                    Select Case Type
                        Case "Scene"
                            Return String.Format("http://{0}:{1}/images/scenes.png", DomoticzIP, DomoticzPort)
                        Case "General"
                            Select Case SubType
                                Case "Percentage"
                                    Return "ms-appx:///Images/percentage.png"
                            End Select
                        Case "Usage"
                            Select Case SubType
                                Case "Electric"
                                    Return "ms-appx:///Images/power.png"
                            End Select
                        Case "Temp"
                            Return "ms-appx:///Images/temperature.png"
                        Case "P1 Smart Meter"
                            Select Case SubType
                                Case "Gas"
                                    Return String.Format("http://{0}:{1}/images/Gas48.png", DomoticzIP, DomoticzPort)
                                Case "Energy"
                                    Return String.Format("http://{0}:{1}/images/Counter48.png", DomoticzIP, DomoticzPort)
                            End Select
                        Case "Thermostat"
                            Return String.Format("http://{0}:{1}/images/override.png", DomoticzIP, DomoticzPort)
                        Case Else

                            Return String.Format("http://{0}:{1}/images/current48.png", DomoticzIP, DomoticzPort)
                    End Select
                Case Else
                    If Status = "On" Then Return String.Format("http://{0}:{1}/images/Light48_On.png", DomoticzIP, DomoticzPort)
                    If Status = "Off" Then Return String.Format("http://{0}:{1}/images/Light48_Off.png", DomoticzIP, DomoticzPort)

            End Select
        End Get
        Set(value As String)
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _IconURI As String

#End Region

    Private app As App = CType(Application.Current, App)

    Const Visible As String = "Visible"
    Const Collapsed As String = "Collapsed"


    Public Property IsDimmer As Boolean
    Public Property CanBeSwitched As Boolean


    Public Function setStatus()

        If Not SwitchType Is Nothing Then
            Select Case SwitchType
                Case "On/Off"
                    CanBeSwitched = True
                    If Status = "On" Then isOn = True Else isOn = False
                Case "Media Player"
                    CanBeSwitched = True
                    If Status = "Off" Then isOn = False Else isOn = True
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
                        Select Case Status
                            Case "Off"
                                isOn = False
                                isMixed = False
                            Case "On"
                                isOn = True
                                isMixed = False
                            Case "Mixed"
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
        '            Me.IconURI = "zut" 'Trigger iNotify
    End Function

    Public Async Function getStatus() As Task(Of retvalue)
        Dim response As HttpResponseMessage
        needsInitializing = True
        If Type = "Group" Or Type = "Scene" Then
            response = Await Task.Run(Function() (New Downloader).DownloadJSON((New Api).getSceneStatus()))
        Else
            response = Await Task.Run(Function() (New Downloader).DownloadJSON((New Api).getDeviceStatus(Me.idx)))
        End If
        If response.IsSuccessStatusCode Then
            Dim deserialized = JsonConvert.DeserializeObject(Of Devices)(Await response.Content.ReadAsStringAsync)
            Dim myDevice As Device = (From d In deserialized.result Where d.idx = idx Select d).FirstOrDefault()
            If Not myDevice Is Nothing Then
                Me.Status = myDevice.Status
                Me.Data = myDevice.Data
                If Me.Status = "" Then Me.Status = Me.Data
                setStatus()
                Return New retvalue With {.issuccess = 1}
            Else
                app.myViewModel.Notify.Update(True, 2, "Error getting device status")
                Me.needsInitializing = False
                Return New retvalue With {.issuccess = 0, .err = "Error getting device status"}
            End If
        Else
            app.myViewModel.Notify.Update(True, 2, "Error getting device status")
            Me.needsInitializing = False
            Return New retvalue With {.issuccess = 0, .err = "Error getting device status"}
        End If

    End Function

    Public ReadOnly Property GroupSwitchOn As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.GroupSwitchOn()", "executed")
                                        If [Protected] And ShowPassCodeInput = False Then
                                            ShowPassCodeInput = True
                                            Exit Sub
                                        End If
                                        If [Protected] And ShowPassCodeInput = True And PassCode = "" Then
                                            ShowPassCodeInput = False
                                            PassCode = ""
                                            Exit Sub
                                        End If
                                        If [Protected] And ShowPassCodeInput = True And PassCode <> "" Then
                                            Await SwitchDevice((New Api).SwitchProtectedScene(idx, "On", PassCode))
                                            ShowPassCodeInput = False
                                            PassCode = ""
                                        Else
                                            Await SwitchDevice((New Api).SwitchScene(idx, "On"))
                                        End If

                                    End Sub)

        End Get
    End Property

    Public ReadOnly Property GroupSwitchOff As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.GroupSwitchOff()", "executed")
                                        If [Protected] And ShowPassCodeInput = False Then
                                            ShowPassCodeInput = True
                                            Exit Sub
                                        End If
                                        If [Protected] And ShowPassCodeInput = True And PassCode = "" Then
                                            ShowPassCodeInput = False
                                            PassCode = ""
                                            Exit Sub
                                        End If
                                        If [Protected] And ShowPassCodeInput = True And PassCode <> "" Then
                                            ShowPassCodeInput = False
                                            PassCode = ""
                                            Await SwitchDevice((New Api).SwitchProtectedScene(idx, "Off", PassCode))
                                        Else
                                            Await SwitchDevice((New Api).SwitchScene(idx, "Off"))
                                        End If

                                    End Sub)

        End Get
    End Property




    Public ReadOnly Property ButtonPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        If Me.CanBeSwitched Then
                                            Dim switchToState As String
                                            Dim url As String
                                            Me.needsInitializing = True
                                            'Exit Sub if the device represents a group (we have seperate buttons for that
                                            If Type = "Group" Then
                                                Me.getStatus()
                                                Me.needsInitializing = False
                                                Exit Sub
                                            End If
                                            If Me.isOn Then switchToState = "Off" Else switchToState = "On"
                                            'Open the PassCode box if the device is protected and the PassCode box is not visible
                                            If [Protected] And ShowPassCodeInput = False Then
                                                ShowPassCodeInput = True
                                                Me.needsInitializing = False
                                                Exit Sub
                                            End If
                                            'Close the passcode input if it's open and no passcode was provided
                                            If [Protected] And ShowPassCodeInput = True And PassCode = "" Then
                                                ShowPassCodeInput = False
                                                Me.needsInitializing = False
                                                Exit Sub
                                            End If
                                            'Set the URI for switching the device
                                            If [Protected] Then
                                                If Type = "Group" Or Type = "Scene" Then
                                                    url = (New Api).SwitchProtectedScene(Me.idx, switchToState, PassCode)
                                                ElseIf SwitchType = "On/Off" Or SwitchType = "Media Player" Or SwitchType = "Contact" Then
                                                    url = (New Api).SwitchProtectedLight(Me.idx, switchToState, PassCode)
                                                Else
                                                    'Exit Sub for the moment, if the device is neither of the types we checked
                                                    Await getStatus()
                                                    ShowPassCodeInput = False
                                                    Exit Sub
                                                End If
                                                ShowPassCodeInput = False
                                            Else
                                                If Type = "Group" Or Type = "Scene" Then
                                                    url = (New Api).SwitchScene(Me.idx, switchToState)
                                                ElseIf SwitchType = "On/Off" Or SwitchType = "Media Player" Or SwitchType = "Contact" Then
                                                    url = (New Api).SwitchLight(Me.idx, switchToState)
                                                Else
                                                    'Exit Sub for the moment, if the device is neither of the types we checked
                                                    Await getStatus()
                                                    Exit Sub
                                                End If

                                            End If

                                            'Execute the switch
                                            If Not url = "" Then
                                                If Me.PassCode <> "" Then Me.PassCode = ""
                                                Dim ret As retvalue = Await SwitchDevice(url)
                                                Me.needsInitializing = False
                                                Await getStatus()
                                            End If
                                        Else
                                            'Only get the status of the device if it can't be switched
                                            Await getStatus()
                                        End If


                                    End Sub)

        End Get
    End Property

    ''' <summary>
    ''' Switch the Device On/Off based on the URL provided. The URL can either be for a normal switch, or for a protected switch
    ''' </summary>
    ''' <param name="url"></param>
    ''' <returns></returns>
    Public Async Function SwitchDevice(url As String) As Task(Of retvalue)
        Dim response As HttpResponseMessage = Await (New Downloader).DownloadJSON(url)
        If Not response.IsSuccessStatusCode Then
            app.myViewModel.Notify.Update(True, 2, "Error switching device")
            Return New retvalue With {.err = "Error switching device", .issuccess = 0}
        Else
            If Not response.Content Is Nothing Then
                Dim domoRes As domoResponse = JsonConvert.DeserializeObject(Of domoResponse)(Await response.Content.ReadAsStringAsync())
                If domoRes.status <> "OK" Then
                    app.myViewModel.Notify.Update(True, 2, domoRes.message)
                    Return New retvalue With {.err = "Error switching device", .issuccess = 0}
                End If
                Return New retvalue With {.issuccess = 1}
            End If
        End If
    End Function
    Public ReadOnly Property ButtonRightTappedCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        If Me.DetailsVisiblity = Visible Then
                                            Me.DetailsVisiblity = Collapsed
                                        Else
                                            Me.DetailsVisiblity = Visible
                                        End If
                                    End Sub)

        End Get
    End Property



    Public Sub New()
        OnOffButtonVisibility = Collapsed
        needsInitializing = False
        DetailsVisiblity = Collapsed
        isOn = False
        PlanIDs = New List(Of Integer)
        ShowPassCodeInput = False

    End Sub

    ''' <summary>
    ''' Based on the JSON properties of the Device, set additional properties of the ViewModel. 
    ''' </summary>
    Public Sub Initialize()
        'Set the IconDataTemplate to reflect the device's Type
        Select Case TypeImg
            Case "lightbulb"
                IconDataTemplate = CType(Application.Current.Resources("lightbulb"), DataTemplate)
            Case "contact"
                IconDataTemplate = CType(Application.Current.Resources("contact"), DataTemplate)
            Case "temperature"
                IconDataTemplate = CType(Application.Current.Resources("temperature"), DataTemplate)
            Case "LogitechMediaServer"
                IconDataTemplate = CType(Application.Current.Resources("music"), DataTemplate)
            Case "hardware"
                IconDataTemplate = CType(Application.Current.Resources("percentage"), DataTemplate)
            Case "doorbell"
                IconDataTemplate = CType(Application.Current.Resources("doorbell"), DataTemplate)
            Case "counter"
                IconDataTemplate = CType(Application.Current.Resources("counter"), DataTemplate)
            Case "Media"
                IconDataTemplate = CType(Application.Current.Resources("media"), DataTemplate)
            Case "current"
                IconDataTemplate = CType(Application.Current.Resources("current"), DataTemplate)
            Case "override_mini"
                IconDataTemplate = CType(Application.Current.Resources("setpoint"), DataTemplate)
            Case "error"
                IconDataTemplate = CType(Application.Current.Resources("error"), DataTemplate)
            Case "info"
                IconDataTemplate = CType(Application.Current.Resources("info"), DataTemplate)
            Case "scene"
                IconDataTemplate = CType(Application.Current.Resources("scene"), DataTemplate)
            Case "group"
                IconDataTemplate = CType(Application.Current.Resources("group"), DataTemplate)
            Case Else
                IconDataTemplate = CType(Application.Current.Resources("unknown"), DataTemplate)
        End Select

        'Check if the device supports switching on/off.
        'TODO : Probably better logic exists on how to determine if a device can switch or not, but for now this will do.
        If Not SwitchType Is Nothing Then
            Select Case SwitchType
                Case "On/Off"
                    CanBeSwitched = True
                    If Status = "On" Then isOn = True Else isOn = False
                Case "Media Player"
                    CanBeSwitched = True
                    If Status = "Off" Then isOn = False Else isOn = True
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
                        OnOffButtonVisibility = Visible
                        If Status = "Off" Then isOn = False Else isOn = True
                    Case Else
                        CanBeSwitched = False
                        isOn = True
                End Select
            End If
        End If
    End Sub
End Class
Public Class DeviceGroup
    Public Property DeviceGroupName As String
    Public Property Devices As ObservableCollection(Of Device)

    Public Sub New()
        'Devices = New ObservableCollection(Of Device)
    End Sub
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
            app.myViewModel.Notify.Update(True, 2, response.ReasonPhrase)
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
    Public ReadOnly Property IconDataTemplate As DataTemplate
        Get
            If isError Then Return CType(Application.Current.Resources("error"), DataTemplate) Else Return CType(Application.Current.Resources("info"), DataTemplate)
        End Get
    End Property

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

    Public Property secondsToShow As Integer

    Public Async Function Update(err As Boolean, show As Integer, message As String) As Task
        isGoing = False
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         isError = err
                                                                                                     End Sub)

        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         msg = message
                                                                                                     End Sub)
        Await Task.Delay(New TimeSpan(0, 0, show))
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         isGoing = True
                                                                                                     End Sub)
        Await Task.Delay(500)
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Sub()
                                                                                                         msg = ""
                                                                                                     End Sub)
    End Function

    Public Sub New()
        isGoing = True
    End Sub
End Class


Public Class TiczViewModel
    Inherits ViewModelBase

    Public Property MyDeviceGroups As New List(Of DeviceGroup)
    Public Property MyPlans As New Plans
    Public Property TiczSettings As New AppSettings
    Public Property Notify As New ToastMessageViewModel

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






    Public ReadOnly Property RefreshCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        For Each d In myDevices.result
                                            Dim ret As retvalue = Await d.getStatus()
                                            'Exit the for loop if for one device we couldn't get the status
                                            If Not ret.issuccess Then Exit For
                                        Next
                                    End Sub)
        End Get
    End Property

    Public Property myLightSwitches As Light_Switches
        Get
            Return _myLightSwitches
        End Get
        Set(value As Light_Switches)
            _myLightSwitches = value
            RaisePropertyChanged("myLightSwitches")
        End Set
    End Property
    Private Property _myLightSwitches As Light_Switches

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
    Public Property myFavourites As Devices
        Get
            Return _myFavourites
        End Get
        Set(value As Devices)
            _myFavourites = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _myFavourites As Devices


    Public Sub New()
        myLightSwitches = New Light_Switches
        myDevices = New Devices
        myFavourites = New Devices
    End Sub
End Class
