Imports System.Text.RegularExpressions
Imports System.Threading
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports GalaSoft.MvvmLight.Threading
Imports Newtonsoft.Json
Imports Windows.ApplicationModel.Core
Imports Windows.Security.Cryptography
Imports Windows.Security.Cryptography.Core
Imports Windows.Storage.Streams
Imports Windows.UI
Imports Windows.UI.Core
Imports Windows.Web.Http


Public Class DevicesViewModel
    Inherits ObservableCollection(Of Device)

    Public Sub New()

    End Sub

    Public Sub New(name As String, items As IEnumerable(Of Device))
        Me.Key = name
        For Each item As Device In items
            Me.Add(item)
        Next
    End Sub

    Public Sub New(name As String)
        Me.Key = name
    End Sub

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


Public Class SecurityPanelViewModel
    Inherits ViewModelBase

    Public Event PlayDigitSoundRequested As EventHandler
    Public Event PlayArmRequested As EventHandler
    Public Event PlayDisArmRequested As EventHandler
    Public Event PlayWrongCodeRequested As EventHandler

    Private CountDownTask As Task
    Private cts As CancellationTokenSource
    Private ct As CancellationToken


    Public Property IsFadingIn As Boolean
        Get
            Return _IsFadingIn
        End Get
        Set(value As Boolean)
            _IsFadingIn = value
            RaisePropertyChanged("IsFadingIn")
        End Set
    End Property
    Private Property _IsFadingIn As Boolean

    Public Property DisplayText As String
        Get
            Return _DisplayText
        End Get
        Set(value As String)
            _DisplayText = value
            DispatcherHelper.CheckBeginInvokeOnUI(Sub()
                                                      RaisePropertyChanged("DisplayText")
                                                  End Sub)
        End Set
    End Property
    Private Property _DisplayText As String

    Public Property CodeInput As String
        Get
            Return _CodeInput
        End Get
        Set(value As String)
            _CodeInput = value
        End Set
    End Property
    Private Property _CodeInput As String

    Public Property CodeHash As String

    Public Property CurrentArmState As String

    Public ReadOnly Property DigitKeyPressedSound As String
        Get
            Return DomoApi.getButtonPressedSound()
        End Get
    End Property

    Public ReadOnly Property WrongCodeSound As String
        Get
            Return DomoApi.getWrongCodeSound()
        End Get
    End Property
    Public ReadOnly Property DisarmSound As String
        Get
            Return DomoApi.getDisarmedSound()
        End Get
    End Property
    Public ReadOnly Property ArmSound As String
        Get
            Return DomoApi.getArmSound()
        End Get
    End Property

    Public Property AudioFile As String
        Get
            Return _AudioFile
        End Get
        Set(value As String)
            _AudioFile = value
            DispatcherHelper.CheckBeginInvokeOnUI(Sub()
                                                      RaisePropertyChanged("AudioFile")
                                                  End Sub)
        End Set
    End Property
    Private Property _AudioFile As String


    Public Async Function StopCountDown() As Task
        If ct.CanBeCanceled Then
            cts.Cancel()
        End If
    End Function



    Public Async Function StartCountDown() As Task
        Await TiczViewModel.DomoSettings.Load()
        If TiczViewModel.DomoSettings.SecOnDelay > 0 Then
            If CountDownTask Is Nothing OrElse CountDownTask.IsCompleted Then
                cts = New CancellationTokenSource
                ct = cts.Token
                CountDownTask = Await Task.Factory.StartNew(Function() PerformCountDown(ct), ct)
            End If
        Else
            Await RunOnUIThread(Sub()
                                    If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayArmRequested(Me, EventArgs.Empty)
                                    DisplayText = CurrentArmState
                                End Sub)
        End If
    End Function

    Public Async Function PerformCountDown(ct As CancellationToken) As Task
        For i As Integer = 0 To TiczViewModel.DomoSettings.SecOnDelay Step 1
            If CodeInput = "" Then
                DisplayText = String.Format("ARM DELAY : {0}", TiczViewModel.DomoSettings.SecOnDelay - i)
                Await RunOnUIThread(Sub()
                                        If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDigitSoundRequested(Me, EventArgs.Empty)
                                    End Sub)

            End If
            'Wait for 1 seconds in blocks of 250ms in order to respond to cancel requests in the meantime
            For j As Integer = 0 To 3
                Task.Delay(250).Wait()
                If ct.IsCancellationRequested Then Exit Function
            Next
            'When phones suspend, this task gets suspended as well. So we need to build in checks to verify if during suspend the
            'Security Panel Delay is finished, or if the timer should be re-tuned to the actual amount of seconds remaining
            If Date.Now > TimestampLastSet.AddSeconds(TiczViewModel.DomoSettings.SecOnDelay) Then Exit For
            ' For after app resume (i.e. phones). Check if during suspend the timer has reduced, if so 
            If Date.Now < TimestampLastSet.AddSeconds(TiczViewModel.DomoSettings.SecOnDelay) And
            TimestampLastSet.AddSeconds(i + 1) < Date.Now Then
                Dim secDifference As Integer
                secDifference = (Date.Now - TimestampLastSet.AddSeconds(i + 1)).Seconds
                'If time has drifted more than a second, retune
                If secDifference > 1 Then i += secDifference
            End If
            If ct.IsCancellationRequested Then Exit Function
        Next
        CodeInput = ""
        DisplayText = CurrentArmState
        Await RunOnUIThread(Sub()
                                If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayArmRequested(Me, EventArgs.Empty)
                            End Sub)
    End Function

    Public ReadOnly Property DigitPressedCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)
                                                   Dim btn As Button = TryCast(x, Button)
                                                   If Not btn Is Nothing Then
                                                       Await RunOnUIThread(Sub()
                                                                               If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDigitSoundRequested(Me, EventArgs.Empty)
                                                                           End Sub)
                                                       Dim digit As Integer = btn.Content
                                                       CodeInput = If(CodeInput = "", digit, CodeInput & digit)
                                                       DisplayText = ""
                                                       For Each d In CodeInput
                                                           DisplayText += "?"
                                                       Next
                                                   End If
                                               End Sub)
        End Get
    End Property

    Public ReadOnly Property CancelPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        'Clear the contents of the Sec Panel Display and restore the current arm state when digits were pressed.
                                        'If not digits were pressed, remove the secpanel from view
                                        If CodeInput = "" Then
                                            IsFadingIn = False
                                            TiczViewModel.TiczMenu.ShowSecurityPanel = False
                                        Else
                                            CodeInput = ""
                                            Await RunOnUIThread(Sub()
                                                                    If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDigitSoundRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                            DisplayText = CurrentArmState
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property DisarmPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SEC_DISARM)
                                        CodeInput = ""
                                        If ret.issuccess Then
                                            Await StopCountDown()
                                            CurrentArmState = "DISARMED"
                                            DisplayText = CurrentArmState

                                            Await RunOnUIThread(Sub()
                                                                    If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDisArmRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                            Await Task.Delay(2000)
                                            If CodeInput = "" Then DisplayText = CurrentArmState
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ArmHomePressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SEC_ARMHOME)
                                        CodeInput = ""
                                        If ret.issuccess Then
                                            CurrentArmState = "ARM HOME"
                                            Await StartCountDown()
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                            Await Task.Delay(2000)
                                            If CodeInput = "" Then DisplayText = CurrentArmState
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ArmAwayPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SEC_ARMAWAY)
                                        CodeInput = ""
                                        If ret.issuccess Then
                                            CurrentArmState = "ARM AWAY"
                                            Await StartCountDown()
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If TiczViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                            Await Task.Delay(2000)
                                            If CodeInput = "" Then DisplayText = CurrentArmState
                                        End If
                                    End Sub)
        End Get
    End Property

    Public Sub CreateSecurityHash()
        Dim codeBuffer As IBuffer = CryptographicBuffer.ConvertStringToBinary(CodeInput, BinaryStringEncoding.Utf8)
        Dim alg As HashAlgorithmProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5)
        Dim buffHash As IBuffer = alg.HashData(codeBuffer)
        If Not buffHash.Length = alg.HashLength Then
            Throw New Exception("There was an error creating the hash")
        Else
            CodeHash = CryptographicBuffer.EncodeToHexString(buffHash)
            WriteToDebug("SecurityPanel.CreateSecurityHash()", String.Format("Created a MD5 hash from {0} : {1}", CodeInput, CodeHash))
        End If

    End Sub

    Public Async Function SetSecurityStatus(status As Integer) As Task(Of retvalue)
        CreateSecurityHash()
        Dim ret As New retvalue
        Dim url As String = DomoApi.setSecurityStatus(status, CodeHash)
        Dim response As HttpResponseMessage = Await Domoticz.DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim result As Domoticz.Response
            Try
                result = JsonConvert.DeserializeObject(Of Domoticz.Response)(body)
                If result.status = "OK" Then
                    ret.issuccess = 1
                Else
                    ret.issuccess = 0
                    ret.err = result.message
                End If
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing security response"
            End Try
        Else
            Await TiczViewModel.Notify.Update(True, String.Format("Error setting Security Status ({0})", response.ReasonPhrase), 0)
        End If
        Return ret
    End Function

    Public Async Function GetSecurityStatus() As Task(Of retvalue)
        Dim ret As New retvalue
        Dim url As String = DomoApi.getSecurityStatus()
        Dim response As HttpResponseMessage = Await Domoticz.DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim result As Domoticz.Response
            Try
                result = JsonConvert.DeserializeObject(Of Domoticz.Response)(body)
                If result.status = "OK" Then
                    Select Case result.secstatus
                        Case Constants.SEC_ARMAWAY : CurrentArmState = Constants.SEC_ARMAWAY_STATUS : DisplayText = Constants.SEC_ARMAWAY_STATUS
                        Case Constants.SEC_ARMHOME : CurrentArmState = Constants.SEC_ARMHOME_STATUS : DisplayText = Constants.SEC_ARMHOME_STATUS
                        Case Constants.SEC_DISARM : CurrentArmState = Constants.SEC_DISARM_STATUS : DisplayText = Constants.SEC_DISARM_STATUS
                    End Select
                    ret.issuccess = 1
                Else
                    ret.issuccess = 0
                    ret.err = result.message

                End If
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing security response"
            End Try
        Else
            Await TiczViewModel.Notify.Update(True, String.Format("Error getting Security Status ({0})", response.ReasonPhrase), 0)
        End If
        Return ret
    End Function


    Public Sub New()

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

    Private app As App = CType(Application.Current, App)

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
            RaisePropertyChanged("Barometer")
        End Set
    End Property
    Private Property _Barometer As String
    Public Property BatteryLevel As Integer
    Public Property CameraIdz As Integer
    Public Property Chill As String
        Get
            Return String.Format("Chill: {0} {1}", _Chill, TiczViewModel.DomoConfig.TempSign)
        End Get
        Set(value As String)
            _Chill = value
            RaisePropertyChanged("Chill")
        End Set
    End Property
    Private Property _Chill As String

    Public Property Counter As String
        Get
            Return _Counter
        End Get
        Set(value As String)
            _Counter = value
            RaisePropertyChanged("Counter")
        End Set
    End Property
    Private Property _Counter As String

    Public Property CounterDeliv As String
        Get
            Return _CounterDeliv
        End Get
        Set(value As String)
            _CounterDeliv = value
            RaisePropertyChanged("CounterDeliv")
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
            RaisePropertyChanged("CounterToday")
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
            RaisePropertyChanged("Data")
        End Set
    End Property
    Private Property _Data As String
    Public Property Description As String
    Public Property DewPoint As String
        Get
            Return String.Format("Dewpoint: {0} {1}", _DewPoint, TiczViewModel.DomoConfig.TempSign)
        End Get
        Set(value As String)
            _DewPoint = value
            RaisePropertyChanged("DewPoint")
        End Set
    End Property
    Private Property _DewPoint As String


    Public Property Direction As String
        Get
            Return String.Format("{0}{1}", _Direction, DirectionStr)
        End Get
        Set(value As String)
            _Direction = value
            RaisePropertyChanged("Direction")
        End Set
    End Property
    Private Property _Direction As String
    Public Property DirectionStr As String
    Public Property Favorite As Integer
    Public Property Gust As String
        Get
            Return String.Format("Gust: {0} {1}", _Gust, TiczViewModel.DomoConfig.WindSign)
        End Get
        Set(value As String)
            _Gust = value
            RaisePropertyChanged("Gust")
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
            RaisePropertyChanged("Humidity")
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
            RaisePropertyChanged("LevelInt")
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
            RaisePropertyChanged("Rain")
        End Set
    End Property
    Private Property _Rain As String
    Public Property RainRate As String
        Get
            Return String.Format("Rate: {0} mm/h", _RainRate)
        End Get
        Set(value As String)
            _RainRate = value
            RaisePropertyChanged("RainRate")
        End Set
    End Property
    Private Property _RainRate As String
    Public Property ShowNotifications As Boolean
    Public Property SignalLevel As String
    Public Property Speed As String
        Get
            Return String.Format("Speed: {0} {1}", _Speed, TiczViewModel.DomoConfig.WindSign)
        End Get
        Set(value As String)
            _Speed = value
            RaisePropertyChanged("Speed")
        End Set
    End Property
    Private Property _Speed As String
    Public Property Status As String
        Get
            Return _Status
        End Get
        Set(value As String)
            _Status = value
            RaisePropertyChanged("Status")
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
            Return String.Format("Temp: {0} {1} ", _Temp, TiczViewModel.DomoConfig.TempSign)
        End Get
        Set(value As String)
            _Temp = value
            RaisePropertyChanged("Temp")
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
            _UsageDeliv = value
            RaisePropertyChanged("UsageDeliv")
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

    Private app As App = CType(Application.Current, App)
    Private vm As TiczViewModel = app.myViewModel

    Public Property CanBeSwitched As Boolean

    Public Property ResizeContextMenuVisibility As String
    '    Get
    '        Return _ResizeContextMenuVisibility
    '    End Get
    '    Set(value As String)
    '        _ResizeContextMenuVisibility = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _ResizeContextMenuVisibility As String

    Public Property MappedRoomIDX As Integer

    Public Property DeviceOrder As Integer

    Public ReadOnly Property ColumnSpan As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.ICON
                    Return 1
                Case Constants.WIDE
                    Return 2
                Case Constants.LARGE
                    Return 2
                Case Else
                    Return 1
            End Select
        End Get
    End Property

    Public ReadOnly Property RowSpan As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.ICON
                    Return 1
                Case Constants.WIDE
                    Return 1
                Case Constants.LARGE
                    Return 2
                Case Else
                    Return 1
            End Select
        End Get
    End Property

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

    Public ReadOnly Property SpeedGust As String
        Get
            Return String.Format("{0} | {1}", Speed, Gust)
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
    '    Get
    '        Return _MinDimmerLevel
    '    End Get
    '    Set(value As Integer)
    '        If _MinDimmerLevel <> value Then
    '            _MinDimmerLevel = value
    '            RaisePropertyChanged("MinDimmerLevel")
    '        End If
    '    End Set
    'End Property
    'Private Property _MinDimmerLevel As Integer

    Public Property MaxDimmerLevel As Integer
    '    Get
    '        Return _MaxDimmerLevel
    '    End Get
    '    Set(value As Integer)
    '        If _MinDimmerLevel <> value Then
    '            _MaxDimmerLevel = value
    '            RaisePropertyChanged("MaxDimmerLevel")
    '        End If
    '    End Set
    'End Property
    'Private Property _MaxDimmerLevel As Integer

    Public Property LevelNamesList As List(Of String)

    'Public Property SelectedLevelName As String
    '    Get
    '        Return _SelectedLevelName
    '    End Get
    '    Set(value As String)
    '        If _SelectedLevelName <> value Then
    '            _SelectedLevelName = value
    '            RaisePropertyChanged("SelectedLevelName")
    '        End If
    '    End Set
    'End Property
    'Private Property _SelectedLevelName As String

    Public Property LevelNameIndex As Integer
        Get
            Return _LevelNameIndex
        End Get
        Set(value As Integer)
            _LevelNameIndex = value
            RaisePropertyChanged("LevelNameIndex")
        End Set
    End Property
    Private Property _LevelNameIndex As Integer

    'Public Property HeaderFontSize As Integer
    '    Get
    '        Return _HeaderFontSize
    '    End Get
    '    Set(value As Integer)
    '        _HeaderFontSize = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _HeaderFontSize As Integer

    'Public Property FooterFontSize As Integer
    '    Get
    '        Return _FooterFontSize
    '    End Get
    '    Set(value As Integer)
    '        _FooterFontSize = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _FooterFontSize As Integer

    'Public Property ContentFontSize As Integer
    '    Get
    '        Return _ContentFontSize
    '    End Get
    '    Set(value As Integer)
    '        _ContentFontSize = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _ContentFontSize As Integer

    Public Property MoveUpDashboardVisibility As String
        Get
            Return _MoveUpDashboardVisibility
        End Get
        Set(value As String)
            _MoveUpDashboardVisibility = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _MoveUpDashboardVisibility As String

    Public Property MoveDownDashboardVisibility As String
        Get
            Return _MoveDownDashboardVisibility
        End Get
        Set(value As String)
            _MoveDownDashboardVisibility = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _MoveDownDashboardVisibility As String



    Public Property DeviceRepresentation As String
        Get
            Return _DeviceRepresentation
        End Get
        Set(value As String)
            _DeviceRepresentation = value
            RaisePropertyChanged("DeviceRepresentation")
            'RaisePropertyChanged(ColumnSpan)
            'RaisePropertyChanged(RowSpan)
        End Set
    End Property
    Private Property _DeviceRepresentation As String

    Public Property GroupVisibility As String
    Public Property SceneVisibility As String
    Public Property StatusVisibility As String
    Public Property SelectorVisibility As String
    Public Property DimmerVisibility As String
    Public Property BlindsVisibility As String

    Public Property PrimaryInformation As String
        Get
            Return _PrimaryInformation
        End Get
        Set(value As String)
            _PrimaryInformation = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _PrimaryInformation As String

    Public Property SecondaryInformation As String
        Get
            Return _SecondaryInformation
        End Get
        Set(value As String)
            _SecondaryInformation = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _SecondaryInformation As String

    Public Property MarqueeLength As Integer
        Get
            Return _MarqueeLength
        End Get
        Set(value As Integer)
            _MarqueeLength = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _MarqueeLength As Integer

    Public Property MarqueeStart As Boolean
        Get
            Return _MarqueeStart
        End Get
        Set(value As Boolean)
            _MarqueeStart = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _MarqueeStart As Boolean

    Public Property TertiaryInformation As String
        Get
            Return _TertiaryInformation
        End Get
        Set(value As String)
            _TertiaryInformation = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _TertiaryInformation As String

    Public Property QuaternaryInformation As String
        Get
            Return _QuaternaryInformation
        End Get
        Set(value As String)
            _QuaternaryInformation = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _QuaternaryInformation As String

    Public Property ShowMore As Boolean
        Get
            Return _ShowMore
        End Get
        Set(value As Boolean)
            _ShowMore = value
        End Set
    End Property
    Private Property _ShowMore As Boolean

    Public ReadOnly Property IconForegroundColor As Brush
        Get
            If isOn Then
                Return Application.Current.Resources("SystemControlHighlightAccentBrush")
            Else
                Dim myBrush As New SolidColorBrush
                myBrush.Color = Color.FromArgb(128, 128, 128, 128)
                Return myBrush
            End If
        End Get
    End Property

    Public ReadOnly Property BatteryLevelVisibility As String
        Get
            If BatteryLevel <= 100 Then Return Constants.VISIBLE Else Return Constants.VISIBLE
        End Get
    End Property

    Public ReadOnly Property BatteryLevelString As String
        Get
            Return String.Format("{0} %", BatteryLevel)
        End Get
    End Property

    Public ReadOnly Property IconPathGeometry As String
        Get
            Select Case TypeImg
                Case "lightbulb"
                    Return "m 292.2057 483.61879 c -6.7998 -2.61077 -14.0981 -9.71578 -16.5352 -16.09724 -0.9895 -2.59097 -2.4344 -4.71085 -3.2109 -4.71085 -0.7765 0 -3.5508 -2.13902 -6.1651 -4.75337 -5.6951 -5.69506 -7.8097 -11.72541 -6.6614 -18.99686 0.4125 -2.61237 0.6568 -6.54977 0.5428 -8.74977 -0.2807 -5.41699 -0.3138 -14.33959 -0.067 -18 0.1113 -1.65 -0.3316 -5.475 -0.9843 -8.5 -0.6527 -3.025 -1.8611 -8.82936 -2.6855 -12.89859 -1.1257 -5.55693 -3.9143 -12.15329 -11.203 -26.5 -12.0007 -23.62153 -13.6124 -29.40363 -13.5476 -48.60141 0.053 -15.66811 1.6022 -23.02993 7.5436 -35.84352 9.0153 -19.44313 22.684 -33.08255 42.2375 -42.14704 12.7425 -5.90711 20.0592 -7.43127 35.6733 -7.43127 15.6141 0 22.9308 1.52416 35.6733 7.43127 19.5534 9.06449 33.2221 22.70391 42.2375 42.14704 5.9414 12.81359 7.4907 20.17541 7.5436 35.84352 0.064 19.08233 -1.587 25.11681 -12.9993 47.5 -5.0477 9.9 -9.7241 19.68527 -10.3922 21.74505 -1.919 5.91637 -5.2444 23.95148 -5.0254 27.25495 0.2438 3.67731 0.2089 12.61123 -0.07 18 -0.1139 2.2 0.1303 6.1374 0.5428 8.74977 1.1483 7.27145 -0.9663 13.3018 -6.6614 18.99686 -2.6143 2.61435 -5.3886 4.75337 -6.1651 4.75337 -0.7765 0 -2.2213 2.11988 -3.2108 4.71085 -2.5054 6.56027 -9.7263 13.49206 -16.8193 16.14588 -5.1489 1.92644 -7.6721 2.138 -24.9409 2.09116 -17.001 -0.0461 -19.8384 -0.29241 -24.6499 -2.1398 z m 45.6965 -15.80269 c 3.5072 -2.13835 7.2311 -7.41529 7.2372 -10.2554 0 -1.62509 -1.9949 -1.75 -27.9963 -1.75 -31.22 0 -30.2761 -0.24533 -26.0054 6.75914 3.9941 6.5509 6.4478 7.23338 26.0054 7.23338 15.7021 0 17.8348 -0.20415 20.7591 -1.98712 z m 18.9264 -21.17971 c 2.9053 -2.72935 2.961 -6.50863 0.1402 -9.51124 l -2.1743 -2.31445 -37.5113 0 c -37.1373 0 -37.5343 0.0217 -39.8257 2.17431 -2.9052 2.72935 -2.9609 6.50863 -0.1401 9.51124 l 2.1743 2.31445 37.5113 0 c 37.1373 0 37.5343 -0.0217 39.8256 -2.17431 z m 0 -21 c 2.9053 -2.72935 2.961 -6.50863 0.1402 -9.51124 l -2.1743 -2.31445 -37.5113 0 c -37.1373 0 -37.5343 0.0217 -39.8257 2.17431 -2.9052 2.72935 -2.9609 6.50863 -0.1401 9.51124 l 2.1743 2.31445 37.5113 0 c 37.1373 0 37.5343 -0.0217 39.8256 -2.17431 z m 0.8599 -21.28024 c 1.6484 -1.64834 2.4546 -3.57693 2.4546 -5.87177 0 -8.70562 4.7716 -22.32428 14.4692 -41.29665 7.8436 -15.34515 9.9185 -20.34351 11.0899 -26.715 3.0439 -16.5576 1.1127 -30.8165 -6.1843 -45.66203 -3.8756 -7.88484 -6.1321 -10.96259 -13.2722 -18.10263 -7.14 -7.14004 -10.2178 -9.39653 -18.1026 -13.27217 -10.1912 -5.00929 -20.8261 -7.6252 -31 -7.6252 -10.1739 0 -20.8088 2.61591 -31 7.6252 -7.8848 3.87564 -10.9626 6.13213 -18.1026 13.27217 -7.1401 7.14004 -9.3966 10.21779 -13.2722 18.10263 -7.297 14.84553 -9.2282 29.10443 -6.1843 45.66203 1.1713 6.37149 3.2462 11.36985 11.0898 26.715 9.6977 18.97237 14.4693 32.59103 14.4693 41.29665 0 2.29484 0.8062 4.22343 2.4545 5.87177 l 2.4546 2.45455 38.0909 0 38.0909 0 2.4545 -2.45455 z"
                Case "contact"
                    Return "M 244.05294 577.35461 C 171.88632 566.13472 111.24346 514.88927 88.288668 445.72806 76.538073 410.32432 74.548026 376.13632 82.185791 340.88372 c 5.4563 -25.1839 12.281421 -40.45285 37.573039 -84.05725 8.57349 -14.78125 24.55027 -42.44196 35.50395 -61.46826 l 19.9158 -34.59326 6.25 3.62818 c 3.4375 1.99549 24.65199 14.18746 47.1433 27.09325 28.77365 16.51069 40.70981 23.98805 40.2742 25.22964 -0.34051 0.97051 -17.055 30.22051 -37.1433 65 -41.1126 71.17949 -43.26988 75.90077 -44.31523 96.98545 -0.78217 15.77622 2.40814 30.21635 9.90409 44.8284 7.49704 14.61416 22.81885 29.74466 37.31912 36.85315 15.01882 7.3627 24.12477 9.46466 40.56782 9.36444 24.80834 -0.15123 43.75485 -8.18283 61.25 -25.96442 9.55793 -9.71441 11.65776 -13.04791 47.5 -75.40645 20.625 -35.8835 37.80996 -65.11502 38.18883 -64.95892 0.37885 0.15609 21.62051 12.37475 47.2037 27.15259 33.74397 19.49181 46.32555 27.36216 45.8253 28.66581 -1.31544 3.42796 -72.25685 125.7539 -77.25663 133.2154 -30.85147 46.04171 -79.43532 76.57562 -135.2112 84.97734 -15.31157 2.30643 -43.54311 2.27071 -58.62564 -0.0743 z M 465.51179 400.88897 c 7.00068 -12.20313 14.38368 -25.03918 16.40668 -28.52455 l 3.67816 -6.33705 -27.61156 -16.00666 c -26.08445 -15.12139 -27.70649 -15.87704 -29.3276 -13.66295 -4.52789 6.18412 -31.7041 54.53517 -31.0306 55.20867 1.05707 1.05706 53.63585 31.4363 54.47907 31.47719 0.37255 0.0181 6.40518 -9.95153 13.40585 -22.15465 z m -244.12657 -146.25 c 8.1906 -14.26563 15.59647 -27.0625 16.4575 -28.4375 1.47019 -2.34778 -0.12163 -3.4687 -26.14074 -18.40859 -15.23843 -8.74971 -28.11057 -15.49971 -28.60475 -15 -3.02828 3.06223 -32.3195 55.75635 -31.46499 56.60468 1.70606 1.69371 52.18117 30.97437 53.57867 31.08103 0.70527 0.0539 7.98372 -11.574 16.17431 -25.83962 z m 259.82995 16.875 c -2.10811 -25.60938 -3.72576 -46.69675 -3.59478 -46.86083 0.13099 -0.16407 9.45065 3.33295 20.71037 7.77115 11.25971 4.43822 20.63021 7.91145 20.82333 7.71833 0.19313 -0.19313 2.47139 -17.89306 5.06282 -39.33319 2.59142 -21.44014 4.99448 -38.98226 5.34013 -38.98251 0.85637 -6.1e-4 14.10659 104.16834 13.55827 106.59061 -0.36288 1.60303 -3.9243 0.8142 -21.06173 -4.66497 -11.34375 -3.62684 -20.94532 -6.41722 -21.33684 -6.20087 -0.3915 0.21637 -3.74413 13.92294 -7.45031 30.45908 -3.70616 16.53614 -7.07145 30.0657 -7.4784 30.0657 -0.40696 0 -2.46475 -20.95313 -4.57286 -46.5625 z M 231.02499 170.77182 c 0.22198 -0.53689 10.61739 -10.41823 23.10092 -21.95856 l 22.69733 -20.98241 -16.6593 -15.18969 c -9.16261 -8.35433 -16.28266 -15.56026 -15.82232 -16.013176 0.46032 -0.452917 21.08696 -9.274863 45.83696 -19.604321 24.75 -10.32946 47.10938 -19.669566 49.6875 -20.75579 2.57813 -1.086225 4.6875 -1.691697 4.6875 -1.345494 0 0.346203 -13.92187 11.078773 -30.9375 23.850155 -17.01562 12.771382 -30.9375 23.795316 -30.9375 24.497636 0 0.70232 7.26481 6.99293 16.14404 13.97912 8.87921 6.9862 15.95319 13.26468 15.71994 13.95218 -0.23325 0.6875 -15.09183 8.18247 -33.01904 16.6555 -46.78443 22.11194 -50.94763 24.00109 -50.49853 22.91485 z"
                Case "temperature"
                    Return "m 1026.36 1018.385 c -35.99441 -9.5788 -60.69415 -41.34151 -60.62723 -77.9638 0.0468 -25.58791 12.43163 -49.47699 33.77181 -65.14213 l 6.20972 -4.55835 0 -68.42086 c 0 -76.11145 -0.1862 -74.23001 8.4835 -85.74121 3.548 -4.71078 7.008 -7.42038 14.0494 -11.00233 8.7775 -4.4651 9.9087 -4.72535 18.7171 -4.30594 16.4715 0.78429 30.6977 10.7554 36.1548 25.34073 2.5112 6.71187 2.5952 9.1575 2.5952 75.57938 l 0 68.64306 5.3125 3.66784 c 13.6243 9.40646 26.255 26.21993 31.1257 41.43316 3.6473 11.3922 4.5234 31.76865 1.8287 42.53472 -11.0074 43.97843 -55.3292 71.19033 -97.6212 59.93573 z m 33.7293 -20.40304 c 14.3578 -3.84104 29.6507 -15.04966 36.5152 -26.76305 7.3352 -12.51667 10.171 -32.0598 6.5846 -45.37866 -4.2071 -15.62419 -15.921 -30.31225 -29.7359 -37.286 l -7.663 -3.86828 -0.3505 -74.06366 c -0.3382 -71.47965 -0.4376 -74.21174 -2.8504 -78.308 -4.1425 -7.03278 -8.5325 -9.50566 -16.875 -9.50566 -8.3426 0 -12.7326 2.47288 -16.875 9.50566 -2.4124 4.09567 -2.5126 6.83688 -2.8593 78.25846 l -0.3592 74.01413 -7.7307 4.11087 c -10.731 5.70637 -19.74352 15.11999 -25.36466 26.49359 -4.12992 8.35633 -4.72639 10.77085 -5.28123 21.37835 -0.47402 9.06225 -0.0936 13.92301 1.57087 20.07209 5.20477 19.22799 21.91722 35.68169 41.89922 41.25064 7.5413 2.10173 21.6921 2.14486 29.375 0.0895 z m -26.5175 -15.049 c -31.2254 -10.13071 -41.8209 -46.02542 -21.0634 -71.35722 4.0391 -4.92922 17.1473 -13.14209 20.9755 -13.14209 0.987 0 3.0177 -1.30188 4.5125 -2.89306 2.6581 -2.82944 2.7179 -3.66974 2.7179 -38.20718 0 -21.95249 0.4896 -36.22893 1.294 -37.73194 1.6648 -3.11068 5.7472 -3.11068 7.412 0 0.8049 1.50392 1.294 15.84138 1.294 37.93127 0 40.5722 -1.1189 36.73947 12.5592 43.02286 10.6239 4.88036 16.1565 9.85751 20.8667 18.77153 7.4958 14.18601 7.7884 27.04042 0.9354 41.09837 -6.8324 14.01571 -21.4175 23.31396 -37.4863 23.89826 -4.9104 0.17855 -11.0614 -0.43174 -14.0175 -1.3908 z m 84.6521 -115.11414 c -1.7331 -1.84478 -2.9263 -4.65213 -2.9263 -6.88517 0 -2.23304 1.1932 -5.04039 2.9263 -6.88517 2.8785 -3.06412 3.2468 -3.11483 22.6176 -3.11483 l 19.6913 0 2.5908 3.29357 c 1.4249 1.81147 2.5907 4.82935 2.5907 6.70643 0 1.87707 -1.1658 4.89496 -2.5907 6.70642 l -2.5908 3.29358 -19.6913 0 c -19.3708 0 -19.7391 -0.0507 -22.6176 -3.11483 z m -10.0096 -39.38517 c -3.5776 -3.57761 -3.3738 -11.24335 0.3931 -14.78211 2.7633 -2.59601 3.971 -2.71789 26.9318 -2.71789 23.8559 0 24.0621 0.0233 27.1069 3.06818 3.7955 3.79554 3.9841 9.18034 0.4775 13.63824 l -2.5908 3.29358 -24.9092 0 c -23.2426 0 -25.0766 -0.16728 -27.4093 -2.5 z m 3.125 -37.65142 c -4.0794 -2.29977 -5.625 -5.04668 -5.625 -9.99677 0 -3.55663 0.7854 -5.28817 3.2936 -7.26109 3.0683 -2.41352 4.9104 -2.59072 26.9318 -2.59072 23.4307 0 23.6652 0.0269 26.7064 3.06818 3.9723 3.97228 3.9934 9.46581 0.054 14.04961 l -3.0142 3.50721 -22.9233 0.31649 c -15.261 0.2107 -23.7591 -0.15462 -25.4234 -1.09291 z m -0.2308 -39.85731 c -6.2308 -2.51045 -7.5143 -13.14216 -2.1006 -17.40055 3.0683 -2.41352 4.9104 -2.59072 26.9318 -2.59072 23.4307 0 23.6652 0.0269 26.7064 3.06818 4.5175 4.51748 4.1925 10.76357 -0.7777 14.9457 -3.8291 3.22199 -3.9424 3.23577 -25.9375 3.15689 -12.1504 -0.0436 -23.3205 -0.57435 -24.8224 -1.1795 z"
                Case "LogitechMediaServer"
                    Return "m 232.94643 292.72258 c -1.375 -0.59856 -7.8894 -5.76603 -14.47644 -11.48326 -37.03147 -32.14148 -47.23415 -40.96425 -53.54238 -46.30072 l -6.98118 -5.90575 -33.51968 -0.0318 c -20.5361 -0.0194 -34.451191 -0.53026 -35.924561 -1.31879 -5.83379 -3.12215 -6.18076 -5.94367 -6.18076 -50.26059 l 0 -41.67179 3.61085 -4.32779 3.61085 -4.32778 35.472991 -0.625 35.473 -0.625 27.35956 -23.75 c 37.28366 -32.36481 42.4693 -36.65042 45.94759 -37.97286 4.16299 -1.58276 10.70592 1.34547 12.28467 5.49789 1.65273 4.34697 1.69514 212.99141 0.0441 217.33372 -0.65797 1.73059 -2.88003 4.0172 -4.93793 5.08138 -4.20372 2.17383 -4.72739 2.21756 -8.24077 0.68809 z m -9.375 -114.4729 0 -71.96966 -7.91148 6.65717 c -17.34822 14.59778 -41.98209 36.09869 -42.71219 37.28002 -0.48462 0.78414 -12.89383 1.25215 -33.2001 1.25215 l -32.42623 0 0 26.875 0 26.875 31.65289 0 31.65288 0 14.19032 12.1875 c 33.1782 28.49544 38.14475 32.72923 38.44141 32.76979 0.17187 0.0235 0.3125 -32.34364 0.3125 -71.92697 z M 326.4729 291.23345 c -3.37051 -1.62951 -5.91062 -7.44069 -4.79241 -10.96389 0.45983 -1.44875 3.81626 -6.97459 7.45876 -12.27965 12.23488 -17.81926 20.56148 -37.41925 25.19665 -59.31032 3.01492 -14.23897 3.01492 -45.18149 0 -59.42045 -4.73523 -22.36372 -14.4543 -44.74158 -26.98422 -62.13042 -3.75726 -5.21427 -6.28025 -10.04268 -6.28025 -12.01893 0 -4.20571 4.03524 -9.37727 8.11601 -10.40148 5.24761 -1.31707 9.86533 2.28657 17.79726 13.88887 13.69515 20.03235 22.62532 41.13028 28.02053 66.19983 3.77835 17.55664 3.77835 50.78806 0 68.3447 -3.37913 15.70149 -9.13645 32.4948 -15.49704 45.20265 -5.9522 11.89191 -19.09991 31.47296 -22.27269 33.17099 -2.85253 1.52662 -7.26061 1.41117 -10.7626 -0.2819 z m -27.44637 -27.47608 c -2.82209 -1.36647 -5.45671 -5.97323 -5.45271 -9.53432 10e-4 -1.17078 2.69562 -6.62869 5.98736 -12.12869 10.66442 -17.81869 15.40116 -31.49177 17.76334 -51.27584 2.97828 -24.94427 -3.2579 -51.50213 -17.47676 -74.42766 -4.16821 -6.72052 -6.27633 -11.48257 -6.27633 -14.17762 0 -5.32851 4.37059 -9.49388 9.96161 -9.49388 5.82529 0 11.5753 6.42953 19.61539 21.93344 21.07967 40.64851 21.1132 88.0278 0.0909 128.41117 -10.07617 19.35608 -16.18762 24.57923 -24.21273 20.6934 z m -30 -29.9516 c -2.82466 -1.41892 -5.45135 -6.02837 -5.4607 -9.58272 -0.003 -1.17078 1.9608 -6.06619 4.36419 -10.87869 5.82961 -11.6731 8.14141 -21.43407 8.14141 -34.375 0 -12.94092 -2.3118 -22.7019 -8.14141 -34.375 -2.40339 -4.8125 -4.36728 -10.48007 -4.36419 -12.59461 0.0112 -7.7231 9.41951 -11.92788 16.14636 -7.21621 3.95288 2.7687 13.13035 22.64437 15.61045 33.80758 1.09934 4.94822 1.99879 14.11843 1.99879 20.37824 0 6.25981 -0.89945 15.43003 -1.99879 20.37824 -2.11441 9.51724 -11.26805 30.42959 -14.46725 33.05184 -2.74335 2.2486 -8.73425 2.96086 -11.82886 1.40633 z"
                Case "hardware"
                    Return "m 603.42859 883.79077 0 -128 8 0 8 0 0 24 0 24 8 0 8 0 0 8 0 8 -8 0 -8 0 0 16 0 16 8 0 8 0 0 8 0 8 -8 0 -8 0 0 16 0 16 8 0 8 0 0 8 0 8 -8 0 -8 0 0 16 0 16 8 0 8 0 0 8 0 8 -8 0 -8 0 0 16 0 16 120 0 120 0 0 8.00003 0 8 -128 0 -128 0 0 -128.00003 z m 48 56 0 -40 16 0 16 0 0 40 0 40 -16 0 -16 0 0 -40 z m 48 -24 0 -64 16 0 16 0 0 64 0 64 -16 0 -16 0 0 -64 z m 48 48 0 -16 16 0 16 0 0 16 0 16 -16 0 -16 0 0 -16 z m 48 -72 0 -88 16 0 16 0 0 88 0 88 -16 0 -16 0 0 -88 z"
                Case "doorbell"
                    Return "m 296.07143 505.24893 c -61.22082 -5.3331 -120.61231 -21.7497 -171 -47.2667 -19.48972 -9.8698 -31.378891 -17.0878 -45.690461 -27.7388 -39.29196 -29.24207 -55.6561 -60.89761 -43.86355 -84.85167 3.62146 -7.35623 8.08597 -12.12615 22.05401 -23.5627 22.50798 -18.42876 40.12056 -35.7979 54.650851 -53.89545 12.07941 -15.04498 19.641 -27.64002 34.98517 -58.27342 29.80141 -59.49612 49.71153 -85.89584 74.04518 -98.17982 9.0453 -4.5662 19.77084 -7.96148 31.34516 -9.92263 10.97673 -1.859891 12.54587 -3.076961 15.89044 -12.324991 3.31463 -9.16522 4.46733 -10.58213 11.63281 -14.29914 6.70927 -3.48036 6.82066 -3.49996 19.84876 -3.49213 10.49238 0.006 14.76191 0.46025 21.43826 2.27933 20.95145 5.70856 37.991 17.70922 40.74926 28.698991 0.40531 1.61487 -0.0889 6.81111 -1.17159 12.31736 l -1.87264 9.52414 4.44284 3.58616 c 27.35575 22.0809 38.04071 40.35004 42.59068 72.82155 1.97404 14.088 2.16027 49.00156 0.41481 77.7646 -1.49269 24.5977 -1.51783 49.66175 -0.0612 61 3.10233 24.14789 10.72092 52.9045 21.75277 82.10644 6.6863 17.69908 6.7601 18.00248 6.78911 27.91458 0.0273 9.3185 -0.19891 10.4846 -3.22634 16.6342 -10.21032 20.7401 -39.80486 34.5673 -83.10029 38.8261 -9.53136 0.9376 -43.25912 1.1516 -52.64405 0.334 z m 62 -30.4239 c 25.82608 -3.1648 42.2463 -11.8081 45.51847 -23.96 6.88564 -25.57132 -45.9249 -63.94252 -123.61191 -89.81423 -46.55916 -15.50536 -94.09081 -24.55255 -135.40656 -25.77333 -43.56251 -1.28716 -71.552461 6.05775 -79.043851 20.74209 -20.19638 39.58819 107.254671 104.83517 231.165361 118.34217 15.39181 1.6778 49.3748 1.9343 61.37849 0.4633 z m -66.37227 -25.9245 c -15.62 -2.1387 -30.28269 -9.1474 -40.71383 -19.4608 -9.89639 -9.78468 -12.9139 -16.21188 -12.9139 -27.50612 0 -7.65812 0.30726 -9.04474 3.10228 -14 4.03739 -7.15786 10.23678 -12 15.36359 -12 9.01811 0 39.09081 11.03666 65.53413 24.05098 8.8 4.33099 17.6875 9.27406 19.75 10.98458 3.4968 2.90006 3.75 3.47918 3.75 8.57708 0 6.60815 -2.20826 11.31928 -7.97921 17.02298 -10.12004 10.002 -27.93839 14.7898 -45.89306 12.3313 z"
                Case "door"
                    Return "m 555.26211 1205.7699 c -26.6804 -6.4682 -48.24862 -28.2679 -54.59354 -55.1797 -3.141 -13.3218 -3.141 -265.55155 0 -278.87363 6.4244 -27.24916 27.99769 -48.82244 55.24685 -55.24684 13.39201 -3.15758 363.89745 -3.15758 377.28947 0 27.24888 6.4244 48.82244 27.99768 55.24685 55.24684 3.08876 13.10076 3.13538 263.62223 0.0562 278.17423 -5.75536 27.1593 -28.8389 50.2431 -55.99846 55.9985 -14.10038 2.9879 -364.88388 2.8767 -377.24313 -0.1124 z m 199.69554 -98.9015 c 13.05721 -6.6643 14.56297 -11.6296 14.56297 -48.0262 l 0 -29.4239 7.16816 -6.445 c 16.76361 -15.0719 21.49014 -36.61061 12.39379 -56.47955 -6.97435 -15.2337 -19.30017 -24.82523 -36.11238 -28.10133 -21.22584 -4.13585 -43.544 7.45352 -52.75017 27.39268 -3.55697 7.70322 -4.15467 10.74142 -4.15467 21.12108 0 10.38499 0.57298 13.28752 3.98559 20.23942 2.37618 4.8366 7.12967 10.9897 11.76716 15.2278 l 7.78159 7.112 0.0281 29.7012 c 0.027 25.7566 0.37918 30.4587 2.65144 35.4062 6.09382 13.2707 20.28211 18.6008 32.67563 12.2756 z M 572.70094 766.89735 c 0.0143 -27.5862 1.63468 -43.39654 6.61989 -64.54064 16.16564 -68.57623 59.15885 -114.13999 121.73675 -129.01501 60.43373 -14.36525 122.66757 1.57288 162.56022 41.63293 20.97811 21.06603 36.39017 49.55412 45.02167 83.21786 5.76211 22.47347 7.79141 40.68667 7.79141 69.92554 l 0 21.95695 -36.37106 0 -36.37079 0 -0.0281 -12.4803 c -0.14043 -51.63482 -10.04176 -86.75179 -30.61177 -108.49781 -11.02931 -11.66016 -21.5876 -18.19663 -36.87523 -22.82963 -9.99992 -3.03062 -14.45568 -3.52776 -31.60943 -3.52776 -17.15347 0 -21.60924 0.49714 -31.60915 3.52776 -15.51008 4.70041 -26.0049 11.25486 -37.05162 23.14055 -20.47395 22.02886 -30.41207 56.8658 -30.45561 106.76062 l -0.0124 13.90657 -36.37107 0 -36.37078 0 0.0112 -23.17763 z"
                Case "counter"
                    Return "m 336.96429 911.02292 0 -219.375 102.5 0 102.5 0 0 219.375 0 219.37498 -102.5 0 -102.5 0 0 -219.37498 z m 168.75 76.91872 0 -18.70626 12.8125 -0.35624 12.8125 -0.35622 0.34134 -22.1875 0.34135 -22.1875 -13.15385 0 -13.15384 0 0 -63.15089 0 -63.15087 -25.23346 0.33837 -25.23348 0.33839 -51.58161 61.25 -51.58162 61.25 -0.0599 24.0625 -0.0599 24.0625 51.875 0 51.875 0 0 18.75 0 18.74998 25 0 25 0 0 -18.70626 z M 402.75406 922.58542 c 0.99718 -1.6623 46.75004 -56.10968 50.65223 -60.27778 2.24807 -2.40127 2.308 -1.63047 2.308 29.6875 l 0 32.15278 -26.94878 0 c -21.22557 0 -26.7497 -0.33184 -26.01145 -1.5625 z m 377.96023 -11.54241 0 -219.39509 16.12977 0 16.12978 0 -3.65085 6.74058 c -9.08881 16.78067 -12.33637 31.76011 -13.17275 60.75942 -1.2704 44.04839 6.52442 70.75666 26.47211 90.70435 14.86115 14.86115 33.94627 21.13801 63.71694 20.95571 28.31301 -0.17336 42.9935 -5.3153 58.2241 -20.39333 13.54815 -13.41245 19.9009 -29.70427 19.9009 -51.03642 0 -21.16825 -6.09409 -36.60829 -19.85565 -50.30652 -13.52414 -13.4619 -29.62525 -19.50208 -48.89435 -18.34225 -13.35174 0.80365 -22.66712 4.25292 -33.37584 12.35832 -4.4926 3.40044 -8.3584 5.99258 -8.59066 5.76032 -0.79974 -0.79974 3.13963 -27.54597 4.99749 -33.93034 3.18346 -10.93968 9.91641 -18.19759 19.20051 -20.69754 6.33764 -1.70655 13.33371 -0.18565 17.65196 3.83741 3.21923 2.99916 7.54654 11.70196 7.58909 15.26266 0.0233 1.9484 0.73949 2.18647 4.71495 1.56728 2.57813 -0.40156 14.25 -1.87691 25.9375 -3.27858 11.6875 -1.40166 22.50981 -2.81932 24.04959 -3.15035 l 2.79959 -0.60187 -2.32723 -5.91693 c -1.27997 -3.25431 -2.7406 -6.9013 -3.24582 -8.10442 -0.84668 -2.01624 0.31521 -2.1875 14.84013 -2.1875 l 15.75874 0 0 219.375 0 219.37498 -41.12013 0 -41.12012 0 2.4856 -11.5625 c 9.67302 -44.9969 29.37351 -83.2068 58.89309 -114.2257 l 10.86156 -11.41321 0 -18.89928 0 -18.89929 -83.75 0 -83.75 0 0 24.36098 0 24.361 50.49149 0.3265 50.4915 0.3265 -10.60979 14.375 c -24.16272 32.7376 -36.845 58.7552 -46.00311 94.375 l -4.17799 16.25 -33.84605 0.3326 -33.84605 0.3326 0 -219.39509 z m -221.25 -1.27009 0 -219.375 102.5 0 102.5 0 0 219.375 0 219.37498 -102.5 0 -102.5 0 0 -219.37498 z m 135.625 93.85228 c 32.97311 -8.85827 54.36341 -37.01222 54.68561 -71.97728 0.18198 -19.74847 -5.40511 -34.07599 -18.32715 -46.99801 -14.2248 -14.22481 -30.4657 -19.92218 -53.85846 -18.89376 -10.49568 0.46141 -15.94671 1.37809 -23.03144 3.87312 -5.1048 1.79776 -9.66141 3.26865 -10.12582 3.26865 -0.8175 0 -0.3621 -3.77371 2.60215 -21.5625 l 1.40599 -8.4375 46.13706 0 46.13706 0 0 -22.5 0 -22.5 -67.96875 0 -67.96875 0 -0.67232 3.4375 c -3.40153 17.39138 -17.21393 105.65541 -16.62599 106.24336 0.41991 0.4199 11.30412 2.2795 24.18715 4.13241 l 23.42366 3.36895 5.16955 -4.73508 c 8.51996 -7.80393 12.30609 -9.28504 23.78528 -9.30468 9.31536 -0.0159 10.67725 0.30939 15.625 3.73254 3.71557 2.57065 6.56676 6.04434 9.06601 11.04539 3.32149 6.64635 3.64582 8.44384 3.64582 20.20461 0 11.77319 -0.32083 13.54646 -3.64582 20.15031 -2.02895 4.02974 -5.77615 8.92826 -8.44936 11.04539 -4.481 3.54888 -5.50134 3.8043 -15.19648 3.8043 -9.74829 0 -10.6909 -0.2398 -15.19647 -3.86604 -6.19172 -4.98327 -10.17715 -10.9036 -11.8725 -17.63645 -1.07448 -4.26716 -1.90094 -5.35818 -4.01692 -5.30291 -6.32576 0.16526 -53.23217 5.70679 -53.89221 6.36682 -1.19646 1.19648 4.92711 16.66628 9.28609 23.45914 7.7167 12.02544 22.11804 22.8263 36.76726 27.57502 16.34548 5.2987 52.72831 6.3579 68.92475 2.0067 z m 179.6281 -169.53821 c -5.60148 -2.24781 -11.14766 -7.51446 -14.73899 -13.99616 -3.40021 -6.13673 -3.63911 -7.47058 -3.63911 -20.31791 0 -12.3489 0.31844 -14.31723 3.125 -19.31663 11.17673 -19.90933 37.05866 -19.61206 47.43175 0.54481 2.88076 5.59787 3.19325 7.61506 3.19325 20.61346 0 15.22058 -1.13999 19.27653 -7.33371 26.09251 -6.13445 6.75074 -19.5226 9.79712 -28.03819 6.37992 z"
                Case "Media"
                    Return "m 36.76101 520.80127 c -6.32339 -2.1906 -11.51366 -6.0403 -15.42701 -11.4426 -6.33996 -8.752 -5.9476 8.2027 -5.66391 -244.74579 l 0.25847 -230.464949 2.13733 -4.6224 c 3.0678 -6.63474 8.68357 -12.39668 15.20213 -15.59782 l 5.66054 -2.77978 232.5 0 232.5 0 5.66054 2.77978 c 6.51856 3.20114 12.13433 8.96308 15.20213 15.59782 l 2.13733 4.6224 0 232.499999 0 232.50004 -2.77978 5.6605 c -3.20114 6.5186 -8.96308 12.1343 -15.59782 15.2021 l -4.6224 2.1374 -231 0.2217 c -219.71196 0.211 -231.25252 0.1343 -236.16755 -1.5684 z m 129.70566 -32.4033 c 6.69948 -3.3784 8.96189 -8.5439 8.96189 -20.4619 l 0 -9.2881 137.28811 0 137.28811 0 4.46189 -2.25 c 4.80304 -2.4221 8.96189 -8.8029 8.96189 -13.75 0 -4.9472 -4.15885 -11.328 -8.96189 -13.75 l -4.46189 -2.25 -137.28811 0 -137.28811 0 0 -9.2881 c 0 -11.91803 -2.26241 -17.08359 -8.96189 -20.46194 -2.45404 -1.2375 -5.62119 -2.25 -7.03811 -2.25 -1.41692 0 -4.58407 1.0125 -7.03811 2.25 -6.69948 3.37835 -8.96189 8.54391 -8.96189 20.46194 l 0 9.2881 -25.28811 0 c -24.236 0 -25.47375 0.094 -29.75 2.25 -4.80304 2.422 -8.96189 8.8028 -8.96189 13.75 0 4.9471 4.15885 11.3279 8.96189 13.75 4.27625 2.1564 5.514 2.25 29.75 2.25 l 25.28811 0 0 9.2881 c 0 11.918 2.26241 17.0835 8.96189 20.4619 2.45404 1.2375 5.62119 2.25 7.03811 2.25 1.41692 0 4.58407 -1.0125 7.03811 -2.25 z m 328.96189 -285.75004 0 -159.999999 -224 0 -224 0 0 159.999999 0 160 224 0 224 0 0 -160 z M 184.84375 317.22652 c -3.89924 -1.69773 -7.11673 -5.75647 -8.43022 -10.6344 -0.67283 -2.49872 -0.93014 -38.74147 -0.75339 -106.11945 0.26305 -100.27575 0.30847 -102.392729 2.26842 -105.720209 3.96224 -6.72683 10.40788 -9.28351 18.14928 -7.19897 5.88581 1.58488 177.04202 99.518289 181.62993 103.926329 7.26774 6.98281 7.26774 15.35341 0 22.33622 -4.32415 4.15462 -175.46386 102.23328 -181.14056 103.81006 -4.8146 1.33731 -7.98224 1.22935 -11.72346 -0.39958 z"
                Case "current"
                    Return "m 658.02644 1099.1252 c -1.9206 -2.4417 -3.2272 -6.3944 -3.2272 -9.763 0 -3.693 9.14999 -28.5137 26.32719 -71.4163 l 26.3272 -65.75605 -34.50194 -0.0371 c -18.97606 -0.0204 -37.3047 -0.68392 -40.73031 -1.47443 -9.23077 -2.13012 -14.79899 -9.05528 -14.76726 -18.36593 0.022 -6.45773 6.20544 -20.20285 49.87708 -110.87131 27.41859 -56.92482 51.40598 -105.16559 53.30533 -107.2017 1.89935 -2.03611 6.05304 -5.03522 9.23043 -6.66468 5.34575 -2.74145 9.74552 -2.96265 58.92931 -2.96265 46.79557 0 53.67719 0.3101 57.54181 2.59298 5.49828 3.24792 7.78633 8.16199 7.70811 16.55468 -0.0523 5.61904 -5.07196 15.35101 -35.50036 68.82812 l -35.43936 62.28373 30.96053 0.77855 30.96055 0.77855 4.71718 5.28362 c 4.11101 4.60465 4.61907 6.1541 3.95366 12.05754 -0.46622 4.1364 -2.49192 9.0513 -5.20262 12.62295 -14.38214 18.95015 -163.90112 206.16303 -168.5137 210.99613 -4.72078 4.9465 -6.57816 5.8391 -12.15053 5.8391 -5.2516 0 -7.2286 -0.8272 -9.8051 -4.1027 z m 113.4021 -170.3166 32.75607 -41.23813 -18.24804 -0.44056 -18.24806 -0.44056 -8.17474 -8.07026 -8.17474 -8.07026 0 -15.04553 0 -15.04553 30.34267 -53.51977 30.34268 -53.51977 -28.88511 -0.424 c -27.15115 -0.39854 -29.13619 -0.23102 -33.06759 2.79061 -2.99694 2.30342 -16.57297 28.90532 -47.89526 93.8496 l -43.71277 90.635 32.01875 0.422 32.01875 0.422 8.26864 8.14201 8.26865 8.14201 -0.46736 16.61544 c -0.25704 9.13847 -0.33938 16.48455 -0.18299 16.32462 0.15641 -0.15994 15.02461 -18.84794 33.04045 -41.52892 z"
                Case "override_mini"
                    Return "m 713.88257 1147.042 c -46.86336 -7.8226 -89.11731 -40.2641 -115.04141 -88.3257 -2.67769 -4.9643 -5.73648 -11.5447 -6.79729 -14.6232 -1.06083 -3.0785 -2.459 -6.7223 -3.10707 -8.0973 -3.17423 -6.7347 -8.84426 -28.0146 -11.15759 -41.87501 -6.4491 -38.64002 -4.63604 -67.42028 6.18665 -98.20576 9.88311 -28.11286 25.55752 -53.34028 63.12258 -101.59365 3.39302 -4.35843 7.85663 -10.27408 9.91913 -13.14588 2.0625 -2.8718 4.70508 -6.33706 5.87238 -7.70059 4.41532 -5.15747 24.83415 -33.12046 31.55436 -43.21276 15.97994 -23.99843 22.12801 -52.40305 18.73236 -86.54514 -0.901 -9.05917 -2.4761 -21.80675 -3.50022 -28.32796 -1.02414 -6.52121 -1.60555 -12.11325 -1.29204 -12.42676 1.63367 -1.63368 40.83552 41.40455 57.98404 63.6585 2.38398 3.09375 4.65815 5.90625 5.05368 6.25 2.05339 1.78453 34.12255 45.82814 41.41199 56.875 6.88805 10.43856 17.30845 26.98318 17.30845 27.48088 0 0.28533 1.6875 3.01919 3.75 6.07523 2.0625 3.05605 3.75 5.90793 3.75 6.33751 0 0.42958 1.50364 3.1605 3.3414 6.06872 4.68311 7.41086 20.772 38.87301 24.80651 48.50963 1.84858 4.41542 4.42779 10.38779 5.73158 13.27195 1.30377 2.88416 2.3896 5.69666 2.41292 6.25 0.0234 0.55334 1.12924 3.25608 2.45759 6.00608 1.32835 2.75 2.43426 5.95117 2.45759 7.11372 0.0234 1.16255 0.53775 2.41987 1.14316 2.79404 0.60541 0.37416 1.39544 1.95423 1.7556 3.51126 0.36018 1.55704 2.41836 8.73723 4.57376 15.95598 2.15539 7.21875 4.39882 15.375 4.98538 18.125 11.42662 53.57104 10.93444 85.34814 -1.94629 125.65731 -11.86742 37.1381 -42.73191 78.3728 -75.2278 100.5037 -12.12152 8.2552 -33.27834 18.5073 -41.43407 20.078 -1.18458 0.2281 -4.35395 0.9844 -7.04305 1.6807 -17.12099 4.4333 -32.99938 5.0089 -51.76428 1.8765 z m 33.81405 -18.7017 c 9.14904 -4.9795 15.33061 -13.3422 21.13664 -28.5945 6.60556 -17.3527 8.80538 -33.6211 8.77456 -64.8911 -0.0364 -36.89599 -3.74434 -64.09535 -13.64739 -100.10891 -4.34241 -15.79164 -12.35154 -32.50324 -19.20233 -40.06698 -4.45079 -4.91395 -6.1073 -5.88435 -10.87553 -6.37097 -7.5197 -0.76743 -12.40468 2.39711 -18.67429 12.09741 -14.56298 22.5318 -25.22912 65.58563 -28.84856 116.44715 -1.62156 22.7867 -0.005 49.0431 4.02523 65.3934 1.35582 5.5 2.77617 11.3344 3.15632 12.9654 2.70377 11.5999 14.90259 29.0635 23.4663 33.594 9.52347 5.0382 20.89458 4.8659 30.68905 -0.4649 z"
                Case "error"
                    Return "m 597.15147 1021.456 c -8.72773 -3.5989 -12.40178 -6.8088 -15.77945 -13.7861 -3.27878 -6.7731 -3.13742 -14.60606 0.41965 -23.25266 3.25771 -7.91892 125.21913 -219.87912 131.68879 -228.86569 2.31457 -3.21501 6.8185 -7.57438 10.00871 -9.6875 5.01793 -3.32374 6.92295 -3.84202 14.12179 -3.84202 7.19884 0 9.10386 0.51828 14.12179 3.84202 3.19021 2.11312 7.69413 6.47249 10.00871 9.6875 6.88745 9.56689 128.59091 221.29918 131.87277 229.42426 6.14047 15.20229 1.45227 28.00599 -12.87827 35.17119 l -7.5 3.75 -134.375 0.2917 -134.375 0.2916 -7.33449 -3.0243 z m 149.58166 -38.62277 c 17.3916 -7.26668 17.64272 -33.08864 0.39794 -40.9198 -17.331 -7.87033 -35.06785 6.59585 -31.48742 25.68115 0.99444 5.3008 7.26201 12.87306 12.56907 15.18549 4.93145 2.14877 13.44641 2.17321 18.52041 0.0531 z m 2.4597 -68.9362 c 0.52628 -4.8125 3.25254 -23.09375 6.05835 -40.625 2.96167 -18.50492 4.85197 -34.11294 4.5066 -37.21038 -0.73522 -6.59403 -5.83966 -14.54946 -11.07113 -17.25476 -6.29995 -3.25783 -17.6813 -2.70895 -23.48638 1.13266 -5.60184 3.70712 -10.08931 12.05312 -10.08931 18.76453 0 2.55351 1.96577 16.01654 4.36839 29.91785 3.41197 19.74141 8.13161 51.26251 8.13161 54.30868 0 0.24338 4.64062 0.27916 10.3125 0.0795 l 10.3125 -0.36305 0.95687 -8.75 z"
                Case "info"
                    Return "m 653.76938 1027.2491 c -0.4313 -0.6979 -1.43577 -1.034 -2.23214 -0.7468 -0.79638 0.2871 -2.43233 0.016 -3.63545 -0.603 -1.42161 -0.7312 -2.1875 -0.6566 -2.1875 0.2129 0 0.9506 -0.72376 0.9506 -2.5 0 -1.375 -0.7359 -2.5 -1.0171 -2.5 -0.625 0 0.3921 -1.19288 0.074 -2.65085 -0.7057 -2.03559 -1.0895 -2.43576 -1.0707 -1.72415 0.081 0.71161 1.1514 0.31144 1.1702 -1.72415 0.081 -1.45798 -0.7804 -2.65085 -1.0489 -2.65085 -0.5967 0 0.4521 -0.76261 0.1891 -1.6947 -0.5845 -0.93208 -0.7735 -3.39161 -1.604 -5.4656 -1.8455 -2.074 -0.2415 -5.0928 -1.365 -6.70844 -2.4966 -1.61564 -1.1316 -3.94012 -2.0575 -5.16552 -2.0575 -1.2254 0 -5.14301 -1.4063 -8.70581 -3.125 -3.5628 -1.7188 -7.21629 -3.125 -8.11887 -3.125 -0.90258 0 -1.64106 -0.5625 -1.64106 -1.25 0 -0.6875 -1.14265 -1.25 -2.53923 -1.25 -1.39658 0 -2.86301 -0.8438 -3.25874 -1.875 -0.39573 -1.0313 -1.75112 -1.875 -3.012 -1.875 -1.26087 0 -2.62283 -0.5345 -3.02659 -1.1878 -0.87452 -1.415 -19.1254 -16.31222 -19.98443 -16.31222 -0.57659 0 -5.42321 -4.96521 -11.9896 -12.28298 -5.43243 -6.05405 -15.54998 -20.51281 -19.39505 -27.71702 -2.01815 -3.78125 -4.09124 -7.0625 -4.60686 -7.29166 -0.51563 -0.22918 -0.9375 -1.4948 -0.9375 -2.8125 0 -1.31772 -0.5625 -2.39584 -1.25 -2.39584 -0.6875 0 -1.17475 -0.42188 -1.08277 -0.9375 0.33295 -1.8666 -1.76653 -9.13084 -2.82868 -9.78729 -0.5987 -0.37001 -1.08855 -2.01261 -1.08855 -3.65021 0 -1.6376 -0.50035 -3.28669 -1.11189 -3.66464 -0.61154 -0.37796 -1.39013 -2.52052 -1.7302 -4.76127 -0.34007 -2.24075 -1.51158 -8.04118 -2.60334 -12.88984 -2.10906 -9.36661 -2.78605 -30.00613 -1.39187 -42.43425 0.46275 -4.125 0.93981 -8.625 1.06015 -10 0.276 -3.15374 4.43153 -19.17091 5.83743 -22.5 0.58068 -1.375 2.27861 -5.59375 3.77319 -9.375 2.52047 -6.37672 6.81211 -15.50464 11.06316 -23.5303 0.97439 -1.83958 2.44548 -3.9072 3.26908 -4.5947 0.82359 -0.6875 4.45792 -5.1875 8.07628 -10 3.61836 -4.8125 8.75665 -10.77297 11.41842 -13.24549 2.66177 -2.47252 4.83959 -5.14439 4.83959 -5.9375 0 -0.7931 0.84375 -1.44201 1.875 -1.44201 1.03125 0 1.875 -0.5625 1.875 -1.25 0 -0.6875 0.64223 -1.25 1.42717 -1.25 0.78495 0 3.03984 -1.6875 5.01087 -3.75 1.97104 -2.0625 4.42256 -3.75 5.44783 -3.75 1.02527 0 1.86413 -0.5625 1.86413 -1.25 0 -0.6875 0.58602 -1.25 1.30227 -1.25 0.71624 0 2.48893 -1.10555 3.9393 -2.45678 1.45037 -1.35122 3.11435 -2.16177 3.69773 -1.80122 0.58339 0.36055 1.0607 0.0266 1.0607 -0.742 0 -0.76865 0.50356 -1.08633 1.11902 -0.70595 0.61546 0.38037 1.45524 -0.18458 1.86617 -1.25546 0.41093 -1.07087 1.25468 -1.63337 1.875 -1.25 0.62032 0.38338 1.46407 -0.17912 1.875 -1.25 0.41093 -1.07087 1.31363 -1.59695 2.00598 -1.16905 0.69236 0.4279 1.25883 0.1167 1.25883 -0.69156 0 -0.85415 1.04693 -1.19578 2.5 -0.81579 1.375 0.35957 2.5 0.14392 2.5 -0.47922 0 -0.62313 0.65883 -1.13297 1.46407 -1.13297 0.80524 0 2.49274 -0.93093 3.75 -2.06873 1.41689 -1.28227 2.28593 -1.50892 2.28593 -0.59619 0 0.97189 1.07576 0.76769 3.16401 -0.60059 1.74021 -1.14022 3.50166 -1.73548 3.91435 -1.32279 0.41269 0.41269 1.81835 0.17876 3.12369 -0.51984 1.30535 -0.69859 3.90326 -1.52581 5.77315 -1.83825 1.86989 -0.31244 7.05605 -1.37249 11.5248 -2.35566 10.46739 -2.30293 38.08054 -3.36741 39.90945 -1.5385 0.95406 0.95407 1.34055 0.89542 1.34055 -0.20343 0 -1.20389 0.60635 -1.21947 2.75275 -0.0708 2.11664 1.13279 2.86686 1.13089 3.24656 -0.008 0.32076 -0.96229 0.99875 -1.06628 1.93475 -0.29675 0.79252 0.65157 3.40969 1.54062 5.81594 1.97568 6.10435 1.10369 6.67301 1.26311 8.4375 2.36542 0.85937 0.53686 2.125 0.62847 2.8125 0.20357 0.6875 -0.42489 1.25 -0.20914 1.25 0.47946 0 0.6886 1.11317 1.03762 2.47371 0.77561 1.36055 -0.26202 2.76507 -0.005 3.12115 0.57117 0.35609 0.57616 3.46166 1.72269 6.90129 2.54784 3.43961 0.82515 6.25385 2.03987 6.25385 2.69937 0 0.6595 0.5625 0.85145 1.25 0.42655 0.6875 -0.42489 1.25 -0.24245 1.25 0.40544 0 0.97178 2.07348 2.02708 6.25 3.18093 0.34375 0.095 3.25641 1.71751 6.47259 3.60565 3.21617 1.88814 6.15752 3.43298 6.53633 3.43298 2.33632 0 34.49108 27.53117 34.49108 29.53154 0 0.31515 1.17818 1.8151 2.61817 3.33323 1.44 1.51812 4.356 5.29148 6.48 8.38523 2.12402 3.09375 4.1802 5.90668 4.56933 6.25094 0.38913 0.34427 1.74479 2.59427 3.0126 5 3.88135 7.36509 6.41469 11.84385 7.0699 12.49906 0.34375 0.34375 0.80939 1.46875 1.03476 2.5 0.22537 1.03125 1.14951 3.5625 2.05368 5.625 2.79323 6.3718 3.6012 8.66903 3.83891 10.91497 0.12604 1.19074 0.79122 2.87824 1.47821 3.75 0.68699 0.87177 1.60876 4.34078 2.04839 7.70892 0.43962 3.36813 1.46226 6.9227 2.27254 7.89901 0.88558 1.06706 1.52224 6.60187 1.59612 13.8761 0.0676 6.65555 0.5831 12.83808 1.14555 13.73895 0.5737 0.91889 0.33348 3.30174 -0.54723 5.42795 -1.30184 3.14293 -1.26904 3.9837 0.19209 4.92455 1.32759 0.85484 1.39314 1.37375 0.26589 2.10477 -0.82283 0.53363 -1.74894 3.76801 -2.058 7.1875 -0.30906 3.41951 -1.50918 10.15478 -2.66693 14.96728 -1.15777 4.8125 -2.17948 9.73437 -2.27051 10.9375 -0.091 1.20313 -0.56358 2.1875 -1.05016 2.1875 -0.48657 0 -1.26782 1.74449 -1.73612 3.87665 -0.4683 2.13215 -2.13065 6.77277 -3.6941 10.3125 -1.56346 3.53971 -3.19995 7.2796 -3.63664 8.31085 -0.43669 1.03125 -1.90659 3.5625 -3.26645 5.625 -1.35986 2.0625 -2.85283 4.59375 -3.3177 5.625 -1.07212 2.37831 -16.34132 22.4806 -19.03574 25.06105 -1.11186 1.06483 -2.64536 3.03358 -3.4078 4.375 -0.76242 1.34142 -1.97385 2.43895 -2.69203 2.43895 -0.7182 0 -2.73942 1.54688 -4.49159 3.4375 -1.75218 1.89062 -3.56853 3.4375 -4.03634 3.4375 -0.46781 0 -1.76029 1.04174 -2.87217 2.31499 -1.1119 1.27324 -3.92073 3.29314 -6.24185 4.48866 -2.32113 1.19553 -5.53773 3.66933 -7.148 5.49743 -1.80145 2.0452 -2.94297 2.6492 -2.96728 1.5701 -0.0267 -1.1861 -0.79497 -0.8827 -2.37362 0.9375 -1.28377 1.4802 -3.21932 2.6913 -4.30123 2.6913 -1.08191 0 -2.32042 0.5717 -2.75225 1.2704 -0.43182 0.6987 -1.30877 0.9467 -1.94877 0.5512 -0.63999 -0.3955 -1.16363 -0.149 -1.16363 0.548 0 0.6969 -1.92979 1.6291 -4.28841 2.0716 -2.35863 0.4425 -4.61338 1.6513 -5.01054 2.6863 -0.39716 1.035 -1.44407 1.6048 -2.32645 1.2662 -0.88239 -0.3386 -2.64801 0.1475 -3.92361 1.0803 -1.27561 0.9327 -2.7873 1.4067 -3.35931 1.0531 -0.57201 -0.3535 -2.14093 -0.054 -3.48648 0.6666 -1.34556 0.7201 -4.79666 1.6046 -7.66911 1.9655 -2.87245 0.3609 -6.4238 1.3061 -7.89187 2.1004 -1.46952 0.7951 -7.86606 1.4455 -14.23172 1.4471 -6.35938 0 -11.5625 0.3423 -11.5625 0.7571 0 1.4068 -5.54252 1.1809 -6.43447 -0.2623 -0.66649 -1.0784 -1.67319 -1.0759 -4.05638 0.01 -2.11029 0.9615 -3.432 1.0202 -3.95406 0.1754 z m 5.90997 -58.91695 c 0.91296 -0.18973 1.94119 -0.64174 2.28494 -1.00447 0.34375 -0.36273 3.01562 -1.71378 5.9375 -3.00234 2.92187 -1.28857 5.3125 -2.89236 5.3125 -3.564 0 -0.67164 1.99699 -3.53934 4.43774 -6.37266 6.24418 -7.2485 6.81226 -8.02574 6.81226 -9.32069 0 -0.63234 0.84375 -1.84995 1.875 -2.70581 1.03125 -0.85586 1.875 -2.56536 1.875 -3.79889 0 -1.23352 0.52319 -2.43027 1.16265 -2.65945 0.63946 -0.22916 1.34259 -2.10416 1.5625 -4.16666 0.3252 -3.04995 -0.0669 -3.75 -2.10015 -3.75 -1.64091 0 -2.63726 0.96648 -2.89944 2.8125 -0.21969 1.54688 -0.92281 2.8125 -1.5625 2.8125 -0.63969 0 -1.16306 1.03782 -1.16306 2.30626 0 1.26845 -1.18955 3.37783 -2.64345 4.6875 -1.4539 1.30969 -3.12747 3.64686 -3.71906 5.19374 -0.77542 2.02755 -2.09268 2.8125 -4.71979 2.8125 -3.26805 0 -3.69961 -0.47475 -4.18115 -4.59951 -0.55901 -4.78833 0.21006 -10.03078 3.63459 -24.77549 1.11772 -4.8125 2.16675 -10.15625 2.33116 -11.875 1.28681 -13.45199 2.21206 -19.56368 3.1337 -20.69931 0.59111 -0.72837 1.36852 -3.54087 1.72759 -6.25 0.35906 -2.70913 0.97426 -7.17569 1.3671 -9.92569 0.39283 -2.75 0.92911 -7.15166 1.19171 -9.78148 0.26261 -2.62981 1.15391 -5.59653 1.98066 -6.59271 0.82675 -0.99617 1.20164 -2.11277 0.83308 -2.48133 -0.36856 -0.36858 0.10712 -3.58936 1.05703 -7.1573 0.94994 -3.56796 1.73383 -6.61672 1.74199 -6.77504 0.0538 -1.04388 -10.2896 1.00609 -13.52921 2.68136 -2.17578 1.12514 -3.95595 1.49626 -3.95595 0.82471 0 -0.67153 -2.36573 -0.16181 -5.25718 1.13273 -2.89145 1.29453 -5.72859 2.06236 -6.30476 1.70627 -0.57616 -0.35608 -1.72606 -0.0894 -2.55531 0.59268 -0.82926 0.68206 -3.3014 1.51972 -5.49364 1.86148 -2.19224 0.34177 -4.65935 1.03762 -5.48247 1.54633 -0.82313 0.50873 -2.40448 1.02344 -3.51412 1.14383 -4.14893 0.45009 -8.89252 3.17961 -8.89252 5.11684 0 1.57777 0.79945 1.82968 3.75 1.18163 2.30524 -0.50631 3.75 -0.3189 3.75 0.48645 0 0.72055 0.84375 0.98631 1.875 0.59059 1.03125 -0.39574 1.875 -0.13474 1.875 0.57998 0 0.71473 1.16678 1.66983 2.59284 2.12244 2.32051 0.73651 2.51449 1.50024 1.84682 7.27174 -0.4103 3.54684 -1.06309 9.8238 -1.45064 13.9488 -0.38754 4.125 -1.29878 8.21671 -2.02496 9.09271 -0.72617 0.87599 -1.01031 1.9027 -0.63142 2.28159 0.37887 0.37889 0.18532 2.44466 -0.43013 4.59061 -0.61545 2.14595 -1.39947 6.18174 -1.74229 8.96842 -0.34281 2.78667 -1.17781 6.97488 -1.85553 9.30713 -0.67774 2.33226 -0.99305 4.6275 -0.7007 5.10054 0.29236 0.47305 -0.22659 3.73406 -1.1532 7.24671 -0.92663 3.51265 -1.40705 6.66436 -1.06761 7.0038 0.33943 0.33944 0.16558 1.34783 -0.38633 2.24085 -1.10711 1.79133 -1.12281 1.88468 -2.30686 13.70959 -0.6997 6.98789 -0.4444 8.92646 1.62337 12.32665 1.34951 2.21909 3.7717 4.72132 5.38265 5.5605 2.50467 1.30475 13.33715 1.54477 18.76905 0.41587 z m 23.81317 -158.27026 c 4.07378 -3.9483 5.62688 -6.45045 5.70479 -9.19088 0.0589 -2.07108 0.3814 -4.03992 0.71671 -4.37522 0.94557 -0.94558 -2.77451 -10.61813 -5.05138 -13.13405 -1.1197 -1.23726 -3.00476 -2.24956 -4.18903 -2.24956 -1.18427 0 -2.86475 -0.85734 -3.7344 -1.90521 -0.86965 -1.04786 -1.87113 -1.61524 -2.22553 -1.26084 -0.3544 0.3544 -1.39925 0.0179 -2.32188 -0.74785 -0.92783 -0.77003 -1.67751 -0.85854 -1.67751 -0.19806 0 0.65679 -1.26563 1.08805 -2.8125 0.95835 -1.54688 -0.1297 -4.30559 0.80805 -6.13046 2.0839 -1.82488 1.27584 -3.83068 2.31971 -4.45733 2.31971 -0.62665 0 -2.28021 2.10937 -3.67457 4.6875 -2.76423 5.11093 -3.98773 16.5625 -1.76955 16.5625 0.73942 0 1.34441 0.6284 1.34441 1.39645 0 0.76804 2.10937 3.46491 4.6875 5.99305 4.38916 4.30404 5.17456 4.59306 12.34024 4.54105 7.30477 -0.053 7.90726 -0.30223 13.25049 -5.48084 z"
                Case "scene"
                    Return "m 552.86461 922.71981 -1.44574 -1.5389 0 -42.21176 0 -42.21168 -5.31912 -5.7947 c -4.78964 -5.21759 -5.31923 -6.05837 -5.31923 -8.44397 0 -1.63695 0.44505 -3.0941 1.16373 -3.813 0.63961 -0.63962 24.68848 -15.45058 53.4409 -32.9125 55.48029 -33.69436 54.9771 -33.43046 58.31743 -30.58539 1.61369 1.3744 7.42483 15.6049 7.42483 18.18216 0 3.41458 1.91158 2.1028 -45.87809 31.48007 -12.61647 7.75571 -29.37201 17.96847 -37.2344 22.69527 l -14.29536 8.59404 58.23245 0.16996 58.23245 0.16997 1.60855 1.71221 1.60855 1.71221 0 40.45173 0 40.45168 -1.61191 1.7158 -1.61202 1.7159 -62.93364 0 -62.93364 0 -1.44574 -1.5389 z m 119.29677 -5.179 c 1.1843 -1.3077 1.27957 -2.8633 1.46576 -23.9364 l 0.19904 -22.53768 -56.21687 0 -56.21686 0 0 22.72048 c 0 20.2085 0.12188 22.8548 1.10032 23.9363 1.04888 1.1588 3.61966 1.216 54.75122 1.216 l 53.65079 0 1.2666 -1.3987 z m -81.84588 -66.42112 7.9581 -7.97879 -10.30602 0 -10.3059 0 -7.958 7.97879 -7.95799 7.9788 10.30591 0 10.30591 0 7.95799 -7.9788 z m 38.56429 0 7.95799 -7.97879 -10.63846 0 -10.63835 0 -7.958 7.97879 -7.95799 7.9788 10.63835 0 10.63836 0 7.9581 -7.9788 z m 38.0204 0.19234 7.435 -7.83902 -10.07321 -0.1845 -10.07309 -0.18451 -7.99691 7.99691 -7.9968 7.99691 10.635 0.0224 10.635 0.0224 7.43501 -7.83901 z M 576.7992 813.83475 c 0.61166 -3.08079 1.31301 -7.02551 1.55935 -8.76613 l 0.44728 -3.16477 -9.20569 5.90809 -9.2057 5.90797 -1.42796 6.89781 c -2.43871 11.77927 -3.01303 11.42614 7.57836 4.65949 l 9.14241 -5.84088 1.1115 -5.60158 z m 27.551 -12.04685 c 4.16602 -2.56217 7.57579 -4.70344 7.57713 -4.75823 10e-4 -0.0559 0.73355 -3.83078 1.62667 -8.39108 0.89345 -4.5603 1.53139 -8.38425 1.41801 -8.49764 -0.11294 -0.11294 -4.36876 2.39343 -9.45607 5.57061 l -9.24987 5.7768 -1.49527 8.04007 c -0.82189 4.42198 -1.48801 8.29883 -1.47917 8.61517 0.009 0.31645 0.79616 0.0671 1.74989 -0.56134 0.95383 -0.62508 5.14255 -3.23254 9.30868 -5.7947 z m 34.54286 -21.25188 8.10784 -5.24633 1.33011 -8.46197 c 0.73131 -4.65401 1.29199 -8.50424 1.24536 -8.5559 -0.19122 -0.21247 -17.19221 10.93792 -17.98525 11.79603 -0.473 0.51215 -1.37618 4.07276 -2.00663 7.91249 -0.63067 3.83983 -1.2997 7.53072 -1.48711 8.2021 -0.43499 1.55991 0.0559 1.30305 10.79568 -5.64642 z"
                Case "group"
                    Return "m 625.85197 1031.6875 c -0.825 -0.825 -1.5 -2.1634 -1.5 -2.9743 0 -0.8108 3.61451 -10.2487 8.03224 -20.9731 4.41774 -10.72441 8.39314 -20.63601 8.83424 -22.02571 0.76978 -2.4254 0.27925 -2.5269 -12.21351 -2.5269 -11.02172 0 -13.3324 -0.3502 -15.08424 -2.2859 -1.1378 -1.2573 -2.06873 -2.995 -2.06873 -3.8616 0 -0.8666 2.75568 -7.2911 6.12374 -14.2766 3.36805 -6.9855 10.98417 -22.82588 16.92469 -35.20088 5.94053 -12.375 11.72986 -23.20313 12.86518 -24.0625 2.83597 -2.14668 39.19723 -2.20167 41.33639 -0.0625 2.49453 2.49452 1.71331 6.2606 -2.87821 13.8751 -2.40449 3.98755 -4.37179 7.41811 -4.37179 7.62345 0 0.20534 -3.38215 6.22405 -7.5159 13.3749 l -7.51589 13.00153 10.2661 0.3671 c 10.6497 0.3808 13.51569 1.5035 13.51569 5.2946 0 3.2613 -58.49186 76.21331 -61.10672 76.21331 -1.1788 0 -2.81828 -0.675 -3.64328 -1.5 z m 30.53963 -46.97731 c 0.76505 -1.2251 4.83786 -6.5121 9.05069 -11.7489 4.21282 -5.2369 7.65968 -9.8595 7.65968 -10.2727 0 -0.4131 -2.4332 -0.7511 -5.40711 -0.7511 -4.12466 0 -6.09074 -0.7277 -8.28949 -3.0681 -4.94813 -5.2671 -3.19857 -12.27543 7.81539 -31.30688 l 9.40426 -16.25 -9.48274 -0.36907 c -7.80687 -0.30383 -9.86335 0.0275 -11.63635 1.87501 -1.18448 1.23423 -6.40006 11.24406 -11.59017 22.24406 -5.1901 11 -11.35854 23.93748 -13.70764 28.74998 l -4.27108 8.75 10.74568 0.3654 c 12.604 0.4285 16.41925 2.7179 16.41925 9.8525 0 4.6135 1.18097 5.3063 3.28963 1.9298 z m 143.42107 39.36731 c -4.04096 -4.3286 -26.66707 -23.39 -27.76419 -23.39 -0.70195 0 -2.47789 -1.68751 -3.94651 -3.75001 l -2.67024 -3.75 -17.16487 0 c -11.6838 0 -17.64386 -0.4789 -18.66489 -1.5 -2.10783 -2.1078 -2.10783 -43.64215 0 -45.74998 1.02171 -1.02171 6.99941 -1.5 18.74735 -1.5 l 17.24735 0 17.13781 -15 c 16.76853 -14.67678 20.55116 -16.81633 23.86749 -13.5 1.11806 1.11806 1.5 14.59639 1.5 52.93378 0 48.42111 -0.13671 51.59131 -2.33413 54.12501 -2.63355 3.0365 -3.91293 3.2688 -5.95517 1.0812 z m -4.48793 -71.72591 -0.34777 -17.08584 -9.36928 8.02339 c -5.15308 4.41285 -10.66303 9.28895 -12.24431 10.83585 -2.67031 2.6122 -3.98365 2.8125 -18.44322 2.8125 l -15.56819 0 0 12.3663 c 0 10.1233 0.35863 12.5039 1.97724 13.125 1.08747 0.4173 8.02229 0.7587 15.41068 0.7587 12.91206 0 13.65907 0.1494 19.24567 3.8499 3.19671 2.1174 7.09002 5.3529 8.6518 7.19 1.56178 1.8371 4.52711 4.5606 6.58961 6.05231 l 3.75 2.7122 0.34778 -16.77721 c 0.19127 -9.2275 0.19127 -24.4658 0 -33.8631 z m 52.77723 72.48891 c 0 -0.7488 -1.125 -1.6556 -2.5 -2.0152 -3.38734 -0.8858 -3.19192 -5.1036 0.40072 -8.6497 3.42231 -3.3779 10.32195 -18.36831 11.53006 -25.05061 0.4661 -2.5781 1.34735 -4.6875 1.95833 -4.6875 1.57893 0 1.4124 -28.4161 -0.17226 -29.3955 -0.70574 -0.4361 -1.17803 -1.4537 -1.04954 -2.2612 0.12849 -0.8076 -0.4191 -3.05177 -1.21685 -4.98715 -0.79775 -1.93538 -1.45046 -4.0608 -1.45046 -4.72315 0 -2.03272 -7.0921 -15.32804 -9.87469 -18.51174 -3.55495 -4.06742 -3.31391 -7.36109 0.67161 -9.17702 3.06056 -1.39448 3.59827 -1.15357 7.5 3.36007 2.31169 2.67422 4.20308 5.61227 4.20308 6.52897 0 0.9167 0.46635 1.66674 1.03633 1.66674 1.39538 0 5.89196 10.15335 8.58053 19.375 1.57663 5.40768 2.17774 11.85888 2.15475 23.12498 -0.0362 17.746 -2.10189 27.3957 -8.40296 39.25381 -3.88585 7.3129 -13.36865 18.768 -13.36865 16.1492 z m -16.5625 -15.5071 c -2.84321 -3.114 -2.76103 -4.1432 0.625 -7.8262 3.53906 -3.84951 8.04443 -16.72981 9.1379 -26.12421 0.49076 -4.2163 0.19154 -10.6654 -0.72494 -15.625 -1.71406 -9.2759 -7.79719 -24.07048 -9.89706 -24.07048 -2.32841 0 -1.42621 -4.33755 1.24979 -6.00874 3.90166 -2.43662 6.80165 -0.78764 10.07917 5.73124 1.6322 3.24637 3.37295 6.20671 3.86834 6.57853 4.87983 3.6626 7.52394 30.71435 4.25086 43.49045 -2.78946 10.8884 -12.25561 26.67751 -15.77656 26.31461 -0.34375 -0.035 -1.60938 -1.1425 -2.8125 -2.4602 z m -15.1701 -15.55701 c -1.86719 -1.8672 -1.51584 -5.5889 0.52764 -5.5889 1.59506 0 4.95651 -12.8857 4.94144 -18.9424 -0.0164 -6.5724 -2.92757 -16.6772 -5.56439 -19.31407 -1.05715 -1.05713 -1.92209 -2.45646 -1.92209 -3.1096 0 -1.74173 5.78008 -4.08171 7.98881 -3.23413 2.11005 0.8097 7.01119 9.6704 7.01119 12.6754 0 1.0587 0.5625 1.9248 1.25 1.9248 0.74074 0 1.25 4.5834 1.25 11.25 0 6.1875 -0.49219 11.25 -1.09375 11.25 -0.60156 0 -1.42349 1.5469 -1.82649 3.4375 -1.15281 5.4082 -5.52913 11.5625 -8.2221 11.5625 -1.336 0 -3.28912 -0.86 -4.34026 -1.9111 z M 620.47432 890.36317 c -2.33604 -1.55339 -5.0911 -4.63955 -6.12235 -6.85817 -1.03125 -2.2186 -2.86554 -4.36251 -4.07621 -4.76425 -2.63674 -0.87495 -5.93382 -6.8497 -5.97311 -10.82408 -0.0155 -1.56979 -0.0161 -6.79167 -10e-4 -11.60417 0.0419 -13.62802 -2.11191 -22.77102 -7.97161 -33.84059 -8.143 -15.38293 -9.41715 -20.14643 -9.11554 -34.079 0.2385 -11.01703 0.65849 -12.94416 4.53733 -20.81964 5.47025 -11.10665 14.05244 -19.67839 24.79956 -24.76936 7.88673 -3.73599 9.30586 -3.99142 22.17594 -3.99142 16.95272 0 24.70514 2.68357 35.04064 12.12964 8.03601 7.34446 12.43004 14.30074 15.72936 24.90149 4.60317 14.79 2.52524 27.55067 -7.52582 46.21633 -6.36118 11.81326 -7.90343 18.63193 -7.88806 34.8751 0.0128 13.57272 -1.22346 18.21843 -5.71862 21.48908 -1.23848 0.9011 -3.50056 3.68786 -5.02684 6.1928 -4.23367 6.94828 -8.95208 8.57058 -24.92722 8.57058 -12.12618 0 -14.17356 -0.32239 -17.93609 -2.82434 z m 32.23459 -9.89355 c 5.70049 -5.35534 4.20191 -6.03212 -13.35694 -6.03212 -15.25861 0 -16.25 0.1482 -16.25 2.42913 0 4.47891 4.70191 6.32087 16.13507 6.32087 9.14113 0 10.97197 -0.36936 13.47187 -2.71788 z m 10.88314 -12.66783 c 1.10847 -1.80944 0.90058 -2.62037 -1.04149 -4.0625 -1.88117 -1.3969 -7.02586 -1.80179 -22.89468 -1.80179 -11.71886 0 -21.50179 0.55313 -22.88609 1.29398 -2.37869 1.27305 -3.24334 4.96384 -1.55777 6.64941 0.47303 0.47303 11.24044 0.7074 23.92758 0.52083 20.69412 -0.30432 23.21001 -0.57182 24.45245 -2.59993 z m -2.44645 -10.83335 c 3.79845 -1.44416 4.17525 -4.42445 0.78855 -6.23696 -2.57344 -1.37726 -42.19875 -1.84232 -45.60495 -0.53524 -2.64402 1.01461 -2.51616 5.05314 0.21027 6.64114 2.81066 1.63706 40.35491 1.74737 44.60613 0.13106 z m 1.07877 -12.84344 c 1.15512 -0.85937 2.68097 -4.65625 3.39077 -8.4375 0.70981 -3.78125 4.23159 -12.78125 7.82619 -20 l 6.53564 -13.125 -0.0135 -12.5 c -0.012 -11.1107 -0.38232 -13.19466 -3.33227 -18.75001 -4.38303 -8.25413 -12.54954 -15.98836 -21.02928 -19.91612 -5.34655 -2.47649 -8.87935 -3.18717 -15.89061 -3.19667 -17.41577 -0.0236 -30.54215 8.0565 -38.12596 23.46884 -3.69147 7.50208 -4.10312 9.43002 -4.05609 18.9967 0.051 10.37166 0.23549 11.01935 7.12799 25.02226 4.52744 9.19801 7.38518 16.77622 7.93513 21.04251 0.66345 5.1468 1.46103 6.91058 3.49701 7.73326 4.37474 1.76773 43.69166 1.47945 46.13493 -0.33827 z m 123.3776 43.33418 c -3.78125 -0.99759 -10.25 -3.48896 -14.375 -5.53638 -26.31999 -13.06375 -39.31729 -45.61504 -29.51255 -73.91322 2.08363 -6.01371 23.61884 -45.05337 26.37027 -47.80481 0.65524 -0.65524 6.18219 1.76185 13.37936 5.85115 18.49696 10.50966 17.45236 8.7746 11.44298 19.00658 -22.01367 37.48198 -22.50187 38.75836 -19.18659 50.16253 3.51167 12.07971 13.09817 19.21247 25.82173 19.21247 7.96965 0 16.20211 -3.71456 20.38416 -9.1975 1.65352 -2.16786 7.87408 -12.37591 13.82348 -22.68454 5.9494 -10.30863 10.99196 -18.91931 11.20567 -19.13485 0.308 -0.31061 19.67224 10.64917 28.01149 15.85395 1.4006 0.87415 0.81438 2.64176 -3.00539 9.06196 -6.15127 10.33899 -11.75786 20.01221 -16.61668 28.66927 -5.46039 9.72889 -18.35019 21.73534 -28.00512 26.0859 -14.2243 6.40957 -26.77081 7.78852 -39.73781 4.36749 z M 859.02719 832.875 c 6.35479 -10.54862 6.43756 -10.10004 -2.81848 -15.27631 -4.56253 -2.55151 -8.44034 -4.4472 -8.61737 -4.21265 -3.57749 4.73991 -9.51181 16.50869 -8.70265 17.25889 2.14693 1.9905 13.61487 8.74556 14.88667 8.76882 0.71411 0.0131 3.07744 -2.92937 5.25183 -6.53875 z m -75.92522 -45.01484 c 2.40625 -4.28871 4.54732 -8.17401 4.75795 -8.634 0.41041 -0.89633 -14.13816 -9.78867 -16.01504 -9.78867 -1.17947 0 -9.99291 14.32722 -9.99291 16.24459 0 0.97805 14.63741 9.95985 16.25 9.97131 0.34375 0.002 2.59375 -3.50451 5 -7.79323 z m 80.5074 5.84775 c -0.62282 -7.96378 -0.9487 -14.6631 -0.72419 -14.88738 0.22451 -0.22428 2.94314 0.71353 6.04139 2.08404 3.09824 1.3705 5.87164 2.25336 6.16307 1.96191 0.29146 -0.29145 1.25518 -5.82283 2.14161 -12.29195 l 1.61168 -11.76204 1.57093 12.5 c 0.8641 6.875 1.9 14.40856 2.3022 16.74123 0.8052 4.67038 1.7245 4.46946 -9.18668 2.00784 l -3.94758 -0.8906 -2.11539 9.50827 c -1.16347 5.22955 -2.25248 9.50827 -2.42001 9.50827 -0.16755 0 -0.81421 -6.51582 -1.43703 -14.47959 z m -71.08505 -38.37897 7.45265 -6.99366 -4.74541 -3.96264 c -2.60999 -2.17945 -4.56308 -4.50969 -4.3402 -5.17831 0.22287 -0.66862 6.9816 -3.89942 15.0194 -7.17956 8.03779 -3.28014 14.38618 -5.59502 14.10755 -5.14416 -0.27864 0.45085 -4.47093 3.60346 -9.31618 7.0058 -4.84525 3.40235 -8.80538 6.46733 -8.80031 6.81108 0.005 0.34375 2.24595 2.27978 4.9797 4.30229 2.73375 2.0225 4.97045 4.01223 4.97045 4.42161 0 0.60228 -20.26611 10.49323 -25.41966 12.40615 -0.74835 0.27778 1.99306 -2.64209 6.09201 -6.4886 z"
                Case "visibility"
                    Return "m 590.21429 1053.8673 c -94.5189 -12.8569 -175.0566 -72.66657 -214.6296 -159.39034 l -5.95181 -13.04331 5.95181 -13.04331 c 36.6499 -80.31784 110.02644 -138.95893 195.1296 -155.94375 21.86176 -4.36315 32.46713 -5.39385 55.5 -5.39385 23.03287 0 33.63824 1.0307 55.5 5.39385 75.32441 15.03319 142.63788 63.29242 181.85827 130.38016 8.47672 14.4997 18.14173 35.06753 18.14173 38.6069 0 3.53937 -9.66501 24.1072 -18.14173 38.6069 -42.92917 73.43173 -117.43754 122.74005 -202.27368 133.86105 -14.82031 1.9428 -56.69848 1.9226 -71.08459 -0.034 z m 52.06567 -57.50607 c 43.57154 -6.06024 81.02303 -37.58855 94.34954 -79.42758 3.95613 -12.42041 5.42201 -22.01809 5.42201 -35.5 0 -13.48191 -1.46588 -23.07959 -5.42201 -35.5 -15.15087 -47.56668 -60.3228 -80.5 -110.41521 -80.5 -56.36727 0 -105.0584 41.22781 -114.55955 97 -1.63908 9.62148 -1.63908 28.37852 0 38 8.38179 49.20153 48.72422 88.85419 97.55955 95.8915 8.70896 1.25498 24.1807 1.27187 33.06567 0.0361 z m -31.06567 -46.55504 c -3.85 -0.80542 -11.275 -3.56642 -16.5 -6.13555 -35.91629 -17.66003 -49.19286 -62.74484 -28.78157 -97.73699 6.29935 -10.79931 17.52983 -21.2041 28.93056 -26.80352 20.90203 -10.26596 41.84746 -10.2418 62.85101 0.0725 26.04539 12.79019 41.53545 41.56289 37.91599 70.42869 -2.09161 16.68091 -8.55305 29.66255 -20.82294 41.83527 -16.38141 16.25168 -40.42657 23.18607 -63.59305 18.33962 z"
                Case "rain"
                    Return "m 496.77573 1071.2833 c -3.32745 -1.1632 -9.02525 -5.1523 -11.80982 -8.2683 -6.29169 -7.0405 -8.02199 -20.0769 -3.81754 -28.7621 3.99125 -8.2448 10.34093 -12.7939 34.50931 -24.7231 22.6118 -11.16101 48.61805 -22.42591 48.61805 -21.05949 0 1.18958 -12.07309 28.07109 -19.32107 43.01949 -13.08726 26.9916 -18.31138 34.4134 -27.17893 38.6127 -5.47245 2.5915 -15.3718 3.1481 -21 1.1808 z m 112 -1.1463 c -5.62349 -2.6243 -11.92701 -9.0504 -14.23081 -14.5076 -2.47971 -5.8739 -2.02819 -15.7227 0.99207 -21.6396 3.06938 -6.0131 6.28181 -9.1752 14.23874 -14.0158 7.8368 -4.7675 30.28069 -15.6301 48.5 -23.47338 7.975 -3.43319 15.7375 -6.87666 17.25 -7.65216 1.5125 -0.77551 2.75 -0.98205 2.75 -0.45899 0 1.85841 -11.57498 27.86873 -20.17662 45.33923 -15.94614 32.3877 -22.28519 38.7782 -38.4394 38.7513 -4.05262 -0.01 -7.44021 -0.736 -10.88398 -2.343 z m 114.93462 0.4624 c -10.40024 -4.7238 -16.41508 -13.9245 -16.41508 -25.1096 0 -10.7158 4.38471 -17.9823 14.71094 -24.3796 10.35643 -6.416 50.67231 -25.51491 68.96157 -32.66933 2.09354 -0.81896 2.04309 -0.51134 -1.12263 6.84572 -12.15536 28.24881 -27.6598 59.29031 -33.29144 66.65301 -5.50235 7.1937 -12.41529 10.5567 -21.64217 10.5283 -4.76348 -0.015 -8.48736 -0.6359 -11.20119 -1.8685 z M 573.27573 957.01237 c -24.26766 -4.11706 -45.22966 -15.52713 -63.32871 -34.47119 -26.04305 -27.25896 -36.00499 -61.86634 -29.05149 -100.92377 3.9812 -22.36222 14.79099 -41.58304 33.57966 -59.70777 15.16664 -14.6307 29.74978 -23.08007 48.68165 -28.20585 8.53699 -2.31137 11.62007 -2.61898 27.41087 -2.73482 l 17.79198 -0.13053 3.61917 -5.42432 c 10.93985 -16.39632 29.21875 -32.83249 45.79687 -41.17999 7.11264 -3.58139 20.80975 -8.23125 29 -9.84484 8.69091 -1.71222 31.89975 -1.72993 40 -0.0305 9.24356 1.93929 21.27629 6.05129 29 9.91031 17.01547 8.50149 34.91965 23.83822 44.95807 38.51109 l 5.51298 8.05819 17.76448 0.13056 c 15.76095 0.11584 18.84931 0.42428 27.38335 2.73486 18.93188 5.12578 33.51502 13.57515 48.68166 28.20585 22.6893 21.88753 33.04755 43.81965 35.69552 75.58017 0.77709 9.32072 -0.88582 26.28533 -3.59297 36.6545 -9.19629 35.22449 -39.07553 67.01823 -74.00063 78.7422 -16.85307 5.65739 -15.37525 5.60391 -151.90246 5.49676 -96.60277 -0.0758 -127.22693 -0.39148 -133 -1.3709 z m 261.5399 -57.04047 c 23.77921 -6.05606 41.46191 -26.9581 43.15981 -51.01756 1.25384 -17.76699 -4.05066 -32.01499 -16.59647 -44.57849 -11.89544 -11.91221 -23.70797 -16.88604 -40.10324 -16.88604 -16.08338 0 -27.93636 4.95377 -39.60646 16.5529 -9.2241 9.16801 -14.88255 20.13273 -16.61563 32.1971 l -1.18513 8.25 -28.29639 0 -28.29639 0 0 -6.36901 c 0 -18.73465 7.75042 -41.71764 19.72902 -58.50413 5.56717 -7.80169 15.62183 -18.55956 21.1478 -22.62686 2.24175 -1.65 4.08654 -3.48199 4.09954 -4.0711 0.0388 -1.75936 -8.44416 -10.05543 -14.15321 -13.84135 -7.61725 -5.05133 -15.84053 -7.62491 -26.50755 -8.29587 -11.47758 -0.72194 -19.60343 0.76801 -28.84357 5.28877 -23.64301 11.56736 -36.51315 37.81432 -31.07803 63.37956 0.87229 4.10301 1.25298 7.79179 0.84599 8.1973 -1.29696 1.29224 -50.57938 18.92643 -51.69616 18.49788 -0.98851 -0.37933 -4.40537 -10.08289 -6.04075 -17.15519 -0.38154 -1.65 -1.03907 -6.72575 -1.46117 -11.27944 -0.4221 -4.55369 -0.99699 -8.50897 -1.27752 -8.7895 -1.39101 -1.39101 -15.038 1.99715 -22.67297 5.62906 -10.94338 5.20569 -23.18757 17.04784 -27.85448 26.93988 -14.70715 31.1735 3.73229 69.46597 37.76306 78.42107 8.46979 2.2288 246.80461 2.28571 255.5399 0.061 z"
                Case "wind"
                    Return "m 701.76537 887.63639 c -0.16757 -0.0369 -0.41894 -0.11225 -0.55859 -0.16738 -0.22012 -0.0869 -0.25601 -0.12112 -0.26968 -0.25706 -0.009 -0.0862 0.0544 -0.98979 0.14014 -2.00789 l 0.15592 -1.85109 0.66521 -0.65175 c 0.7722 -0.75659 1.2707 -1.39683 1.8701 -2.40184 0.23588 -0.3955 0.44206 -0.72724 0.45819 -0.73721 0.0161 -0.01 0.15131 1.69759 0.30043 3.79456 l 0.27111 3.81268 -0.11992 0.0918 c -0.15481 0.11855 -0.75393 0.33557 -1.1044 0.40003 -0.38172 0.0702 -1.44316 0.0556 -1.80851 -0.0249 z m -6.27151 -2.8019 c -0.20391 -0.0854 -0.37461 -0.25605 -0.46882 -0.4686 -0.13351 -0.30122 -0.14031 -1.0184 -0.0155 -1.63088 0.38841 -1.90557 1.86881 -4.9759 3.90213 -8.09295 l 0.46695 -0.71582 0.032 0.50691 c 0.0572 0.90509 0.36836 1.58031 1.0294 2.2337 0.59212 0.58526 1.21635 0.8914 2.02778 0.99449 0.43833 0.0557 1.00036 10e-4 1.42094 -0.13732 0.13448 -0.0443 0.25771 -0.0674 0.27385 -0.0513 0.0299 0.0299 -0.82571 1.11013 -1.53352 1.93609 -2.39975 2.80029 -4.74073 4.83941 -6.09424 5.30842 -0.31131 0.10788 -0.90403 0.17463 -1.04102 0.11724 z m 10.30864 -7.37937 c -0.40498 -0.0217 -0.89058 -0.0423 -1.0791 -0.0458 -0.18853 -0.004 -0.34278 -0.0239 -0.34278 -0.0453 0 -0.0214 0.11997 -0.11115 0.2666 -0.19944 0.81635 -0.49153 1.40878 -1.33368 1.61025 -2.28899 0.0929 -0.44067 0.0444 -1.31272 -0.0951 -1.70781 -0.17465 -0.4948 -0.45339 -0.93721 -0.8559 -1.35851 -0.34681 -0.36301 -0.36782 -0.39644 -0.23446 -0.37308 0.0806 0.0141 0.53495 0.0837 1.00976 0.15465 3.13658 0.46863 6.22435 1.31607 7.9473 2.18115 1.25313 0.62918 1.83134 1.27194 1.67436 1.8613 -0.23616 0.88664 -2.12033 1.51256 -5.33063 1.77083 -0.85294 0.0686 -3.65621 0.0999 -4.57033 0.051 z m -3.56301 -1.3146 c -0.73928 -0.24407 -1.23312 -0.82079 -1.36548 -1.59463 -0.21133 -1.23558 0.72089 -2.3316 1.98317 -2.3316 1.52352 0 2.47631 1.57155 1.78685 2.94728 -0.18298 0.36513 -0.63703 0.78017 -1.01068 0.92385 -0.40885 0.15722 -1.01229 0.18107 -1.39386 0.0551 z m -3.00643 -2.71121 c -1.37936 -3.49832 -2.14875 -6.28233 -2.32973 -8.43003 -0.10788 -1.28027 0.10119 -2.26337 0.53799 -2.52971 0.34541 -0.21061 0.89478 -0.0945 1.48973 0.31482 1.11854 0.76957 2.71138 2.8079 4.24099 5.42712 0.82583 1.4141 1.70178 3.09767 1.61169 3.09767 -0.0302 0 -0.15042 -0.0556 -0.26711 -0.12363 -0.1167 -0.068 -0.39498 -0.18696 -0.61842 -0.26436 -0.36084 -0.12499 -0.47722 -0.1406 -1.04102 -0.13958 -0.54949 9.8e-4 -0.68935 0.0193 -1.04102 0.1361 -0.22344 0.0742 -0.55134 0.22107 -0.72866 0.32631 -0.40528 0.24052 -1.00506 0.86371 -1.24059 1.289 -0.18805 0.33956 -0.39209 0.93429 -0.39209 1.14285 0 0.21278 -0.0724 0.13231 -0.22176 -0.24656 z"
                Case "uv"
                    Return "m 690.09831 1122.5169 c -2.34304 -1.6082 -5.43679 -4.512 -6.875 -6.453 -2.49169 -3.3629 -2.61494 -5.0229 -2.61494 -35.2209 0 -30.198 0.12325 -31.858 2.61494 -35.2208 4.99753 -6.7448 10.39763 -9.4361 17.7494 -8.8459 7.51841 0.6036 12.13809 3.46 15.88566 9.8224 2.31266 3.9262 2.5 6.4924 2.5 34.2443 0 27.7519 -0.18734 30.3181 -2.5 34.2444 -3.74757 6.3623 -8.36725 9.2188 -15.88566 9.8223 -5.43374 0.4362 -7.37473 0.01 -10.8744 -2.3928 z m -163.35083 -62.1626 c -9.05018 -5.5187 -12.39383 -14.9368 -8.41903 -23.7139 2.87383 -6.346 38.25067 -44.45846 43.64203 -47.01682 16.25214 -7.71214 34.15574 10.08958 26.00308 25.85512 -2.69226 5.2062 -38.70645 43.1491 -43.12418 45.4336 -4.8817 2.5244 -13.48181 2.2593 -18.1019 -0.558 z m 331.35219 -1.8721 c -5.05131 -2.2013 -43.9015 -38.7395 -46.4681 -43.7028 -4.81343 -9.3081 -0.7875 -21.08457 8.7936 -25.72271 5.99002 -2.89971 11.05585 -3.206 16.04041 -0.96981 5.35252 2.40126 44.41002 39.47742 46.72398 44.35372 2.56588 5.4072 1.83187 14.1711 -1.5794 18.8577 -2.88576 3.9646 -12.18187 8.9386 -16.57914 8.8708 -1.74495 -0.027 -4.86406 -0.786 -6.93135 -1.6869 z m -181.2413 -40.2775 c -25.38664 -4.5415 -43.28082 -11.3083 -61.42991 -23.23006 -31.06266 -20.40443 -53.87784 -51.01957 -62.69704 -84.13165 -8.46638 -31.78742 -8.37436 -49.66039 0.42714 -82.96303 5.46107 -20.66329 17.95476 -42.56927 34.8511 -61.10663 8.08071 -8.86555 31.1785 -25.49396 44.10048 -31.74857 12.25743 -5.93293 38.94663 -12.93702 55.9097 -14.67247 26.60504 -2.72188 65.4347 5.79124 88.75501 19.45891 10.80759 6.33415 29.32526 20.50153 35.37421 27.06387 65.24176 70.77895 52.12361 177.22478 -28.21556 228.95209 -30.71249 19.77464 -72.64209 28.53744 -107.07513 22.37754 z m -12.14795 -96.87999 c 9.48541 -4.84363 14.79637 -10.93949 19.17064 -22.00383 2.7129 -6.86208 2.88186 -9.17323 3.33838 -45.66539 l 0.48087 -38.43751 -12.54089 0 -12.5409 0 -0.38008 37.81251 c -0.3506 34.88 -0.57396 38.19899 -2.88007 42.796 -7.18785 14.32828 -26.16929 14.33759 -33.64127 0.0165 -2.44909 -4.69401 -2.632 -7.314 -2.98881 -42.8125 l -0.38008 -37.81251 -12.43242 0 -12.43243 0 0.01 33.43751 c 0.005 18.39062 0.59723 37.09375 1.31561 41.5625 3.0295 18.84509 13.16812 30.82525 29.59304 34.96828 2.5888 0.653 9.95278 1.00341 16.36439 0.77871 10.34515 -0.36256 12.59039 -0.88493 19.94458 -4.64027 z m 118.0022 -51.14807 18.33657 -55.04137 -13.25376 0.35386 -13.25377 0.35385 -6.71341 22.50001 c -3.69239 12.375 -9.11856 31.72425 -12.05819 42.99834 l -5.34475 20.49833 -2.2607 -10.49833 c -2.34512 -10.8904 -20.11191 -74.72068 -21.06272 -75.6715 -0.29687 -0.29687 -6.43882 -0.37936 -13.64878 -0.18331 l -13.109 0.35646 15.4463 48.12501 c 8.49546 26.46875 16.32921 51.07813 17.40833 54.6875 l 1.96203 6.56251 14.60763 0 14.60763 0 18.33659 -55.04136 z m -330.24198 15.97885 c -6.91157 -4.70396 -9.36229 -9.03946 -9.36229 -16.5625 0 -7.22814 2.22103 -11.40336 8.60319 -16.17276 3.22636 -2.41108 5.413 -2.60137 33.91421 -2.9514 28.50569 -0.35009 30.78359 -0.21187 35.41058 2.14865 13.21919 6.74392 14.0435 25.08547 1.50709 33.53376 -4.05628 2.73353 -5.09218 2.81675 -35.06007 2.81675 -29.96625 0 -31.00261 -0.0833 -35.01271 -2.8125 z m 423.75003 0 c -6.91156 -4.70396 -9.36228 -9.03946 -9.36228 -16.5625 0 -7.22814 2.22103 -11.40336 8.60318 -16.17276 3.22637 -2.41108 5.413 -2.60137 33.91422 -2.9514 28.50569 -0.35009 30.78359 -0.21187 35.41058 2.14865 13.21919 6.74392 14.0435 25.08547 1.50709 33.53376 -4.05628 2.73353 -5.09218 2.81675 -35.06007 2.81675 -29.96625 0 -31.00262 -0.0833 -35.01272 -2.8125 z m -47.14422 -124.804 c -8.49301 -4.20397 -12.75344 -13.43209 -10.33974 -22.39587 1.95778 -7.27067 40.55934 -48.05685 47.43141 -50.11577 12.3492 -3.69991 25.15994 5.77849 25.00889 18.50355 -0.031 2.61226 -0.73413 6.26031 -1.5625 8.10678 -0.82837 1.84646 -10.15705 12.64265 -20.7304 23.99151 -23.59743 25.3282 -28.00303 27.753 -39.80766 21.9098 z m -273.60616 -1.67375 c -5.08028 -2.21134 -43.76727 -38.67568 -46.36712 -43.70325 -4.78563 -9.25438 -0.73435 -21.04169 8.82197 -25.66783 5.99003 -2.89971 11.05585 -3.206 16.04041 -0.96981 5.35252 2.40126 44.41002 39.47744 46.72398 44.35374 2.58014 5.43723 1.82515 14.2054 -1.62375 18.85769 -3.27216 4.4139 -11.33413 8.88517 -15.95742 8.85021 -2.0625 -0.0156 -5.49963 -0.78994 -7.63807 -1.72075 z m 134.92577 -60.44967 c -2.17927 -1.1743 -5.27302 -3.80885 -6.875 -5.85453 -2.88037 -3.67817 -2.91269 -4.08053 -2.91269 -36.25001 0 -36.48003 -0.0228 -36.38017 9.54519 -41.82096 9.02955 -5.13461 20.76315 -1.7771 25.93245 7.42045 2.48975 4.42991 2.64736 6.47608 2.64736 34.36995 0 28.4113 -0.118 29.8695 -2.80269 34.64633 -4.9479 8.8036 -16.69892 12.24994 -25.53462 7.48877 z"
                Case "dimmer"
                    Return "m 320.34286 483.83054 c -2.27583 -0.27196 -4.46102 -0.87251 -5.1 -1.40162 -0.2475 -0.20494 -1.35547 -0.66399 -2.46217 -1.02011 -2.09142 -0.67299 -5.85149 -2.64408 -7.83527 -4.10737 -1.66783 -1.23024 -6.50252 -5.49775 -6.50279 -5.73993 -1.3e-4 -0.11355 -0.48724 -0.74645 -1.08247 -1.40645 -0.59524 -0.66 -1.42516 -1.74 -1.84427 -2.4 -0.41912 -0.66 -1.07596 -1.57739 -1.45964 -2.03864 -0.38368 -0.46126 -0.84391 -1.38195 -1.02271 -2.046 -0.17881 -0.66404 -0.78236 -2.04165 -1.34121 -3.06136 -0.80373 -1.4665 -1.18137 -2.7632 -1.80678 -6.204 -1.11039 -6.10899 -1.21308 -8.06032 -0.61743 -11.73305 1.09719 -6.76528 1.45616 -8.19734 2.58611 -10.31695 0.61571 -1.155 1.21217 -2.505 1.32545 -3.00001 0.11329 -0.495 0.52002 -1.2375 0.90386 -1.65 0.38384 -0.4125 0.9626 -1.25234 1.28613 -1.86632 0.68346 -1.29698 2.35004 -3.29252 5.37525 -6.43626 2.01937 -2.09847 2.22231 -2.23054 3.29127 -2.14189 1.02628 0.0851 1.23641 0.25804 2.11973 1.74447 0.53928 0.9075 1.40034 2.52031 1.91347 3.58402 0.51313 1.06371 1.23672 2.34621 1.60798 2.85 0.37125 0.50379 0.90315 1.50567 1.18199 2.22639 0.27884 0.72072 0.83229 1.73691 1.22989 2.25819 0.3976 0.52128 1.0124 1.70427 1.36623 2.62887 0.39626 1.03551 0.91715 1.82763 1.3564 2.06271 0.94914 0.50797 2.43093 0.44011 3.23199 -0.148 0.35695 -0.26206 1.34737 -0.94326 2.20093 -1.51379 0.85357 -0.57052 1.60537 -1.27605 1.67067 -1.56785 0.20402 -0.91165 -0.56749 -3.01147 -1.57286 -4.28085 -0.53023 -0.66947 -1.22425 -1.90199 -1.54226 -2.73894 -0.31801 -0.83694 -0.96381 -2.01425 -1.43511 -2.61623 -0.47129 -0.60198 -1.00758 -1.57191 -1.19174 -2.15538 -0.18416 -0.58348 -0.8632 -1.76406 -1.50898 -2.6235 -2.86509 -3.81309 -2.24563 -5.18786 2.97834 -6.60983 3.37984 -0.92 8.83465 -1.34056 12.45 -0.95989 3.32887 0.35051 6.7851 1.0787 7.98343 1.68205 0.43088 0.21694 1.68354 0.62054 2.78368 0.89689 1.10014 0.27635 2.36094 0.78616 2.80178 1.13293 0.44084 0.34676 1.43453 0.92375 2.20819 1.2822 0.77366 0.35844 1.94418 1.13733 2.60115 1.73086 0.65697 0.59354 1.3696 1.07915 1.58362 1.07915 0.49088 0 7.18816 6.85996 7.18816 7.36277 0 0.20455 0.54145 1.02713 1.20321 1.82796 0.66177 0.80083 1.43363 2.06428 1.71524 2.80767 0.28162 0.74338 0.81201 1.79718 1.17866 2.34176 0.36666 0.54459 0.81479 1.75959 0.99586 2.7 0.18107 0.94042 0.69548 2.78985 1.14312 4.10985 0.77533 2.28625 0.81391 2.67726 0.81391 8.25 0 5.71883 -0.0215 5.92063 -0.95686 9 -0.52627 1.7325 -1.04005 3.57419 -1.14173 4.09264 -0.10168 0.51846 -0.56374 1.54006 -1.0268 2.27023 -0.46306 0.73017 -1.00983 1.83631 -1.21503 2.45809 -0.2052 0.62177 -0.76035 1.57156 -1.23366 2.11063 -0.47331 0.53907 -1.19708 1.57374 -1.60838 2.29927 -0.84085 1.48325 -6.67123 7.46914 -7.27511 7.46914 -0.21489 0 -0.90951 0.46871 -1.54361 1.04158 -0.6341 0.57287 -1.81574 1.35603 -2.62587 1.74036 -0.81013 0.38433 -1.94545 0.99727 -2.52295 1.3621 -0.5775 0.36482 -1.59 0.79771 -2.25 0.96199 -0.66 0.16427 -1.8075 0.62287 -2.55 1.01911 -0.7425 0.39623 -2.16001 0.87442 -3.15001 1.06263 -2.23636 0.42516 -12.21851 0.65217 -14.85 0.33771 z m 81.45001 -27.65634 c -1.815 -0.13533 -5.325 -0.38588 -7.8 -0.55678 -8.86168 -0.61188 -16.51235 -1.2603 -17.22168 -1.4596 -0.40641 -0.11418 -0.96582 -0.67414 -1.28055 -1.28178 -0.52527 -1.01415 -0.53148 -1.2092 -0.10332 -3.24461 0.25055 -1.19108 0.45555 -3.16049 0.45555 -4.37646 0 -2.53972 0.30731 -3.57216 1.2065 -4.0534 0.37945 -0.20307 5.24735 -0.56924 12.08937 -0.90936 6.29977 -0.31317 12.53413 -0.72382 13.85413 -0.91255 3.06271 -0.4379 4.72379 -0.0697 5.19864 1.15238 0.18351 0.47229 0.35848 3.83919 0.41058 7.90076 0.0726 5.66003 0.008 7.17734 -0.32095 7.575 -0.45634 0.55112 -1.10554 0.56777 -6.48827 0.1664 z m -132.73098 -1.40855 c -1.31278 -0.65016 -1.41637 -1.06889 -1.68292 -6.80241 -0.27761 -5.97148 -0.24663 -6.03423 3.07382 -6.22678 2.54324 -0.14749 4.04587 0.24024 4.52736 1.1682 0.3691 0.71135 0.20754 9.60389 -0.1934 10.64512 -0.16831 0.4371 -0.61458 1.01085 -0.99171 1.275 -0.90136 0.63134 -3.40204 0.6001 -4.73315 -0.0591 z m 106.84236 -20.38559 c -0.55487 -0.48143 -3.36138 -8.08249 -3.36138 -9.10385 0 -0.23269 -0.4725 -1.08766 -1.05 -1.89992 -0.5775 -0.81227 -1.05 -1.60793 -1.05 -1.76814 0 -0.16022 -0.4725 -0.86793 -1.05 -1.57269 -0.5775 -0.70476 -1.04911 -1.46116 -1.04803 -1.6809 0.004 -0.86253 1.16726 -2.07968 3.34645 -3.50227 1.25382 -0.8185 2.37258 -1.63851 2.48614 -1.82225 0.11356 -0.18374 0.6879 -0.53523 1.27631 -0.78109 0.58842 -0.24585 1.58561 -0.91296 2.21597 -1.48246 0.63037 -0.5695 1.69221 -1.31404 2.35964 -1.65454 0.66743 -0.3405 1.21352 -0.72649 1.21352 -0.85775 0 -0.13127 0.55241 -0.54914 1.22759 -0.92861 0.67517 -0.37946 2.30811 -1.51807 3.62875 -2.53024 3.84773 -2.94897 3.43957 -2.75106 4.56594 -2.21393 0.54395 0.25939 1.28675 0.85458 1.65065 1.32263 1.98776 2.55665 4.13997 5.70299 5.60979 8.201 0.43689 0.7425 1.23768 2.09371 1.77955 3.00268 0.54187 0.90897 1.24145 2.32647 1.55463 3.15 0.31318 0.82353 0.99999 2.09133 1.52626 2.81734 1.18595 1.63608 1.29372 3.21892 0.28184 4.13946 -0.72683 0.66122 -3.916 1.99182 -5.625 2.3469 -0.5775 0.11998 -1.59 0.4648 -2.25 0.76626 -0.66 0.30146 -2.6175 1.01773 -4.35 1.59172 -1.7325 0.57397 -3.96 1.38735 -4.95 1.8075 -0.99 0.42015 -2.07714 0.76711 -2.41587 0.77102 -0.33873 0.004 -1.73252 0.54713 -3.09732 1.20713 -2.74599 1.32792 -3.57823 1.45345 -4.47543 0.675 z m -103.21338 -2.59978 c -2.46436 -0.99942 -4.24801 -2.29824 -4.24801 -3.0933 0 -0.22321 0.27047 -0.93599 0.60104 -1.58397 0.33057 -0.64797 0.75021 -1.89135 0.93253 -2.76305 0.18232 -0.8717 0.63957 -2.19241 1.01611 -2.93491 0.37655 -0.7425 0.98516 -2.2275 1.35246 -3.3 0.36731 -1.0725 0.84356 -2.1525 1.05833 -2.4 0.21477 -0.2475 0.8664 -1.4625 1.44805 -2.7 1.46909 -3.12553 2.59303 -4.62965 3.54303 -4.74148 0.75641 -0.0891 2.84193 1.07793 5.34905 2.99313 0.57717 0.4409 1.41527 0.89293 1.86245 1.00452 1.37258 0.3425 2.33695 1.46793 2.33695 2.72722 0 0.92712 -0.21879 1.31612 -1.31571 2.33933 -0.72365 0.67501 -1.5544 1.69978 -1.84611 2.27728 -0.29171 0.5775 -0.7016 1.185 -0.91086 1.35 -0.20927 0.165 -0.88467 1.245 -1.50089 2.4 -0.61623 1.155 -1.35975 2.4375 -1.65228 2.85 -0.29252 0.4125 -0.76536 1.60692 -1.05075 2.65427 -0.66477 2.43964 -1.98869 3.948 -3.45882 3.94069 -0.55802 -0.003 -2.14047 -0.46165 -3.51657 -1.01973 z m 89.55725 -20.95023 c -2.83433 -2.72332 -6.3688 -5.51098 -8.53913 -6.73488 -5.80859 -3.27559 -5.9141 -3.40768 -4.75123 -5.94773 0.43181 -0.94319 0.78511 -1.87585 0.78511 -2.07258 0 -0.19673 0.521 -1.22667 1.15777 -2.28875 0.63678 -1.06208 1.2554 -2.47106 1.3747 -3.13106 0.11931 -0.66 0.59819 -1.875 1.06419 -2.7 0.46599 -0.825 1.04996 -2.19144 1.29771 -3.03654 0.81722 -2.78766 2.18844 -4.6108 3.47108 -4.61507 0.703 -0.002 5.16398 1.81506 5.97363 2.43365 0.379 0.28956 1.1183 0.62075 1.64289 0.73597 1.28895 0.2831 8.32799 4.00888 8.5202 4.50977 0.0843 0.21966 0.44453 0.48813 0.80054 0.5966 0.82566 0.25156 4.12862 2.44281 4.62229 3.06652 0.67991 0.85902 0.3978 1.51255 -1.67224 3.87383 -1.12598 1.2844 -2.64473 3.12973 -3.375 4.10073 -2.08778 2.77602 -2.70582 3.56273 -4.62776 5.89075 -0.99 1.19918 -2.17992 2.7346 -2.64427 3.41205 -1.21535 1.77314 -2.50428 2.88174 -3.35047 2.88174 -0.42138 0 -1.16846 -0.41623 -1.75001 -0.975 z m -68.21041 -2.81707 c -0.19534 -0.0796 -1.56433 -1.52017 -3.0422 -3.20131 -1.47787 -1.68114 -3.2399 -3.66412 -3.91561 -4.40662 -1.66163 -1.82587 -1.6285 -2.56667 0.19287 -4.3122 0.78646 -0.75371 1.87216 -1.69666 2.41269 -2.09545 0.54052 -0.39878 1.28081 -1.01547 1.64509 -1.37041 1.36723 -1.33221 4.29686 -3.51417 5.22736 -3.89329 0.53079 -0.21626 1.1298 -0.59171 1.33116 -0.83433 0.20136 -0.24262 1.04958 -0.78559 1.88495 -1.2066 0.83536 -0.42102 2.02548 -1.22049 2.64471 -1.7766 2.40802 -2.16261 4.69269 -0.94607 5.91196 3.148 0.33927 1.13922 0.80569 2.22802 1.03648 2.41956 0.2308 0.19155 0.79762 1.25345 1.25961 2.35979 0.46199 1.10634 1.16648 2.4514 1.56553 2.98901 0.39905 0.53762 0.72555 1.33946 0.72555 1.78188 0 0.83663 -0.99223 1.89064 -1.77981 1.89064 -0.24109 0 -1.00238 0.32303 -1.69177 0.71785 -0.68938 0.39481 -2.13092 1.01802 -3.20342 1.3849 -1.0725 0.36689 -2.2875 0.97257 -2.7 1.34597 -0.4125 0.3734 -1.25292 0.83956 -1.8676 1.03592 -0.61468 0.19636 -1.71361 0.89045 -2.44207 1.54244 -1.3671 1.22358 -3.84912 2.69092 -4.4812 2.64924 -0.19752 -0.013 -0.51895 -0.0888 -0.71428 -0.16839 z m 42.80515 -10.38237 c -1.46773 -0.55359 -2.39028 -0.62898 -8.35178 -0.68254 -8.44007 -0.0758 -7.78291 0.38018 -8.85262 -6.14302 -0.4194 -2.5575 -0.93362 -6.04982 -1.14273 -7.76072 l -0.3802 -3.11073 0.78597 -0.67606 c 0.43992 -0.3784 1.42381 -0.77171 2.23471 -0.89331 0.79681 -0.11949 1.80854 -0.40331 2.24829 -0.63071 2.0162 -1.04261 15.79835 -1.8001 18.59887 -1.02222 1.06401 0.29555 1.37896 0.72176 1.89041 2.55831 0.0941 0.33801 -0.0855 1.62051 -0.39914 2.85 -0.72467 2.84057 -0.88171 3.99369 -1.34521 9.87771 -0.34289 4.35296 -0.44473 4.87453 -1.08479 5.55584 -0.86419 0.9199 -1.91676 0.9393 -4.20178 0.0774 z"
                Case "blinds"
                    Return "m -547.78946 1435.9438 c -8.5424 -9.0931 -11.1172 -16.9095 -11.1172 -33.7489 l 0 -21.9148 917.1749 0 917.17516 0 0 18.8908 c 0 25.6163 -3.3978 33.2727 -18.4409 41.5534 -11.1188 6.1204 -130.6138 7.0535 -903.24436 7.0535 l -890.4304 0 -11.1172 -11.834 z m 61.3508 -152.089 0 -57.8551 826.5899 -0.6028 c 454.6246 -0.3316 831.68536 -0.6027 837.91306 -0.6027 26.2043 0 24.9109 -3.0665 24.9109 59.0604 l 0 57.8553 -844.70696 0 -844.7069 0 0 -57.8553 z m 1 -170.6667 1.265 -57.9676 843.5746 -0.1869 843.57466 -0.1887 0 58.1297 0 58.1297 -844.83946 0.018 -844.8395 0.018 1.265 -57.9678 z m -1 -169.19407 0 -57.9987 844.7069 -0.037 844.70696 -0.037 0 58.0364 0 58.03647 -844.70696 4e-4 -844.7069 3e-4 0 -57.99887 z m 0 -171.19308 0 -57.85526 844.7069 0 844.70696 0 0 57.85526 0 57.85508 -844.70696 0 -844.7069 0 0 -57.85508 z M 1250.9719 737.57957 c -6.584 -5.12469 -8.217 -14.11178 -8.7918 -48.38542 -0.7659 -45.71132 1.8708 -51.17275 24.7825 -51.31492 18.6017 -0.10992 23.1193 10.17391 22.5381 51.33288 -0.6902 48.93138 -14.8953 66.76283 -38.5288 48.36746 z m -1737.41056 -133.52292 0 -57.85526 19.2494 -0.0367 c 40.3044 -0.055 1588.40226 -0.0916 1628.26886 -0.0366 l 41.8956 0.055 0 57.85527 0 57.85508 -844.70696 0 -844.7069 0 0 -57.85508 z M 1257.3264 326.83396 c 0 -286.200074 0.1721 -291.686424 8.8516 -291.686424 8.6837 0 8.8543 5.53582 8.9882 291.686424 0.1377 287.76026 0.014 291.68641 -8.8516 291.68641 -8.8658 0 -8.9882 -3.97506 -8.9882 -291.68641 z m -1743.76506 108.47839 0 -57.85508 14.7202 0.002 c 8.096 9.1e-4 384.647298 -0.0366 836.7807 -0.0916 452.1334 -0.055 825.62746 -0.055 829.98686 -0.002 7.1039 0.0733 7.9261 6.08636 7.9261 57.94009 l 0 57.85508 -844.70696 0 -844.7069 0 0 -57.85508 z m 3.0196 -114.10325 c -1.6609 -1.76795 -3.0196 -28.34503 -3.0196 -59.0604 l 0 -55.84639 841.31 0.62841 c 462.7205 0.34443 842.83866 0.88855 844.70696 1.2055 1.8684 0.31695 3.3969 26.61225 3.3969 58.43274 l 0 57.85508 -841.68736 0 c -462.9281 0 -843.0462 -1.44551 -844.7069 -3.2142 z m -3.0196 -225.783194 0 -60.27837 844.8464 0 844.84626 0 -1.2719 59.06041 -1.2719 59.060584 -843.57456 1.21834 -843.5746 1.21833 0 -60.278554 z m -72.468 -232.638646 0 -133.79004 913.778 -0.0366 c 609.1949 -0.0366 914.88746 1.57925 917.10576 4.8213 1.8302 2.67447 3.3588 62.89861 3.397 133.83145 l 0.069 128.968736 -917.17516 0 -917.1749 0 0 -133.790036 z m -70.63405 70.616983 c -6.90392 -8.113001 -8.9431 -19.232446 -10.20399 -55.645403 -1.77138 -51.14619 0.0688 -63.33648 11.64337 -76.80722 7.06381 -8.22659 33.98277 -12.20752 39.90667 -5.90168 1.3288 1.4162 3.2459 118.193725 2.3378 142.466957 -0.3941 10.540324 -33.8986 7.386214 -43.68385 -4.112654 z M 1301.0434 -120.338 c 0.3786 -35.13508 0.8881 -68.53574 1.1325 -74.22343 0.3597 -8.37664 3.7647 -10.67865 17.9244 -12.1161 28.7232 -2.91575 36.8699 14.28492 36.8699 77.84601 0 38.911732 -1.0412 44.025984 -11.7258 57.54124 -9.1617 11.589923 -15.3517 14.833805 -28.3077 14.833988 l -16.5821 1.83e-4 0.6884 -63.881891 z"
                Case "push"
                    Return "m 655.16968 1164.4164 c -58.09637 -6.4526 -109.62125 -27.1389 -154.99608 -62.2281 -14.65704 -11.3345 -41.26232 -38.3207 -52.32334 -53.0724 -25.11493 -33.495 -42.34192 -69.14428 -52.15122 -107.92107 -6.67498 -26.38663 -8.55585 -42.39874 -8.56801 -72.94077 -0.0121 -30.32695 1.85361 -46.11433 8.6416 -73.12501 21.01485 -83.62198 78.41145 -154.63914 156.05428 -193.0869 28.78774 -14.25533 58.09519 -23.44746 90.21777 -28.29634 20.82802 -3.14397 62.92198 -3.14397 83.75 0 72.66634 10.96892 136.89098 46.32247 184.00305 101.28761 29.32336 34.21125 51.29199 76.59613 62.34259 120.27987 6.6827 26.41717 8.55731 42.3959 8.55731 72.94077 0 30.54487 -1.87461 46.5236 -8.55731 72.94077 -13.26055 52.41982 -39.43737 98.28837 -78.21862 137.05927 -25.70183 25.6949 -47.97795 41.592 -80.62701 57.5383 -27.10838 13.2402 -56.19995 22.2372 -86.87501 26.8672 -15.81652 2.3874 -56.53363 3.3913 -71.25 1.7568 z m 50.04751 -31.7247 c 56.74718 -7.1782 105.68331 -27.7454 149.95251 -63.0228 13.95042 -11.1168 21.15774 -18.3812 31.78822 -32.0398 34.83332 -44.75579 55.69807 -95.81568 62.06113 -151.87504 4.25558 -37.49215 -7.21261 -90.51647 -29.00534 -134.10904 -17.4226 -34.85091 -37.87105 -62.48505 -59.84401 -80.87352 -31.81011 -26.62085 -73.25046 -48.20142 -112.71684 -58.69867 -48.0555 -12.78177 -79.01086 -12.78177 -127.06636 0 -50.80944 13.51427 -105.19719 45.86567 -133.45588 79.38343 -38.29237 45.41874 -62.35875 102.37438 -68.23497 161.4853 -4.81138 48.39927 14.5253 114.76893 48.16348 165.31254 15.39987 23.1393 27.81881 37.0775 45.81053 51.4148 33.73403 26.8821 69.50447 44.8439 110.14388 55.3076 35.32609 9.0957 56.10811 11.0415 82.40365 7.7152 z M 568.12332 943.22435 c -30.68322 -5.53853 -53.35962 -27.49882 -61.28047 -59.34529 -3.22037 -12.94775 -2.91405 -39.28872 0.60763 -52.2506 3.62853 -13.35523 10.62703 -26.12711 19.58015 -35.73271 9.57362 -10.27132 19.6608 -16.79832 32.41603 -20.97509 8.56467 -2.80455 12.37568 -3.31708 25.09801 -3.37538 10.41676 -0.0478 17.48259 0.58972 23.125 2.08632 26.66169 7.07174 46.24114 27.1977 53.98987 55.49686 3.78679 13.82975 3.77389 41.6997 -0.0258 55.734 -7.79569 28.79368 -27.46954 49.10532 -54.6728 56.44521 -9.07776 2.44932 -30.13082 3.48832 -38.83761 1.91668 z m 33.5608 -33.38347 c 18.9262 -10.61834 28.43743 -43.07527 21.06735 -71.89201 -4.3995 -17.20185 -10.70926 -26.91767 -21.1835 -32.61845 -16.26246 -8.85114 -36.14049 -3.74742 -46.8052 12.01733 -14.70462 21.73662 -14.15669 62.63467 1.10694 82.62485 4.55033 5.95939 12.94418 11.66723 19.71493 13.40621 7.70434 1.97877 18.99432 0.44835 26.09948 -3.53793 z m 88.48556 -52.21182 0 -84.37501 22.22357 0 22.22357 0 19.96393 35.29578 c 20.78249 36.74295 28.88586 52.18607 37.88729 72.20423 l 5.33974 11.875 -0.6859 -8.125 c -0.37725 -4.46875 -1.04975 -31.32813 -1.49444 -59.6875 l -0.80854 -51.56251 17.6754 0 17.67539 0 0 84.40597 0 84.40597 -20.13952 -0.34346 -20.13953 -0.34346 -21.82659 -38.75001 C 745.48229 862.53833 735.1848 843.02427 728.22391 827.13071 l -4.0505 -9.24836 0.68831 62.06086 0.6883 62.06086 -17.69016 0 -17.69018 0 0 -84.37501 z"
                Case "pushoff"
                    Return "m 677.67264 1139.7739 c -58.09637 -6.4526 -109.62125 -27.1389 -154.99608 -62.2281 -14.65704 -11.3345 -41.26232 -38.3207 -52.32334 -53.0724 -25.11493 -33.49498 -42.34192 -69.1443 -52.15122 -107.92109 -6.67498 -26.38663 -8.55585 -42.39874 -8.56801 -72.94077 -0.0121 -30.32695 1.85361 -46.11433 8.6416 -73.12501 21.01485 -83.62198 78.41145 -154.63914 156.05428 -193.0869 28.78774 -14.25533 58.09519 -23.44746 90.21777 -28.29634 20.82802 -3.14397 62.92198 -3.14397 83.75 0 72.66634 10.96892 136.89098 46.32247 184.00305 101.28761 29.32336 34.21125 51.29199 76.59613 62.34259 120.27987 6.68272 26.41717 8.55732 42.3959 8.55732 72.94077 0 30.54487 -1.8746 46.5236 -8.55732 72.94077 -13.26055 52.41982 -39.43737 98.28839 -78.21862 137.05929 -25.70183 25.6949 -47.97795 41.592 -80.62701 57.5383 -27.10838 13.2402 -56.19995 22.2372 -86.87501 26.8672 -15.81652 2.3874 -56.53363 3.3913 -71.25 1.7568 z m 50.04751 -31.7247 c 56.74718 -7.1783 105.68331 -27.7454 149.95251 -63.0228 13.95042 -11.1168 21.15774 -18.3812 31.78822 -32.0398 34.83332 -44.75581 55.69807 -95.8157 62.06113 -151.87506 4.25558 -37.49215 -7.21261 -90.51647 -29.00534 -134.10904 -17.4226 -34.85091 -37.87105 -62.48505 -59.84401 -80.87352 -31.81011 -26.62085 -73.25046 -48.20142 -112.71684 -58.69867 -48.0555 -12.78177 -79.01086 -12.78177 -127.06636 0 -50.80944 13.51427 -105.19719 45.86567 -133.45588 79.38343 -38.29237 45.41874 -62.35875 102.37438 -68.23497 161.4853 -4.81138 48.39927 14.5253 114.76893 48.16348 165.31251 15.39987 23.13935 27.81881 37.07755 45.81053 51.41485 33.73403 26.8821 69.50447 44.8439 110.14388 55.3076 35.32609 9.0957 56.10811 11.0415 82.40365 7.7152 z M 546.87628 929.83184 c -30.68322 -5.53854 -53.35962 -27.49883 -61.28048 -59.3453 -3.22037 -12.94775 -2.91404 -39.28871 0.60763 -52.2506 3.62854 -13.35523 10.62704 -26.12711 19.58015 -35.73271 9.57363 -10.27132 19.66081 -16.79832 32.41603 -20.97509 8.56468 -2.80455 12.37569 -3.31708 25.09802 -3.37538 10.41676 -0.0477 17.48259 0.58972 23.125 2.08632 26.66169 7.07174 46.24114 27.1977 53.98987 55.49686 3.78678 13.82975 3.77388 41.6997 -0.0258 55.734 -7.79569 28.79368 -27.46954 49.10532 -54.67281 56.44521 -9.07775 2.44932 -30.13081 3.48832 -38.8376 1.91669 z m 33.5608 -33.38348 c 18.9262 -10.61834 28.43743 -43.07527 21.06735 -71.89201 -4.3995 -17.20185 -10.70926 -26.91766 -21.1835 -32.61845 -16.26247 -8.85114 -36.14049 -3.74742 -46.80521 12.01734 -14.70461 21.73661 -14.15669 62.63466 1.10694 82.62484 4.55034 5.95939 12.94419 11.66723 19.71494 13.40621 7.70434 1.97877 18.99432 0.44835 26.09948 -3.53793 z m 89.73556 -52.21182 0 -84.37501 51.25 0 51.25001 0 0 15.625 0 15.625 -32.50001 0 -32.5 0 0 19.37501 0 19.375 30 0 30.00001 0 0 15.625 0 15.625 -30.00001 0 -30 0 0 33.75 0 33.75001 -18.75 0 -18.75 0 0 -84.37501 z m 132.50001 0 0 -84.37501 51.25 0 51.25001 0 0 15.625 0 15.625 -32.5 0 -32.50001 0 0 19.37501 0 19.375 30.00001 0 30 0 0 15.625 0 15.625 -30 0 -30.00001 0 0 33.75 0 33.75001 -18.75 0 -18.75 0 0 -84.37501 z"
                Case Else
                    Return "m 101.28077 274.46124 c -9.484489 -3.5478 -15.830179 -12.57953 -15.846619 -22.55443 -0.0106 -6.41071 2.25032 -11.43861 7.53412 -16.75482 4.22896 -4.25491 6.08385 -5.35731 10.783889 -6.40913 9.05464 -2.02635 15.65077 -0.1166 22.13671 6.40913 9.03034 9.08574 10.09286 20.46109 2.88295 30.86495 -5.50643 7.9458 -18.30984 11.8786 -27.49105 8.4443 z M 89.743051 198.54079 c 1.25929 -23.1607 7.58876 -35.27373 26.279079 -50.29163 13.39147 -10.76021 18.00759 -15.77643 20.39838 -22.16637 3.62804 -9.69681 1.95118 -23.64085 -3.64239 -30.288427 -8.26408 -9.8213 -28.36564 -10.6734 -38.908249 -1.64931 -6.88333 5.891867 -12.4413 19.006167 -12.4413 29.355837 l 0 4.2899 -22.5 0 -22.5 0 0.0131 -4.75 c 0.039 -14.09596 7.16359 -33.806707 16.44336 -45.491807 8.44977 -10.63995 22.24494 -19.47867 36.5435 -23.41383 9.91026 -2.72743 35.473919 -2.49527 47.209259 0.42875 21.59451 5.38057 36.66 16.69046 44.71502 33.56823 3.95849 8.29428 5.02546 13.414827 5.03391 24.158657 0.019 24.16196 -6.90868 36.33385 -31.06299 54.57733 -18.76272 14.17131 -23.89516 21.89518 -23.89516 35.96018 l 0 6.96249 -21.1486 0 -21.148609 0 0.61169 -11.25 z"
            End Select
        End Get
    End Property



    ' Public Property IconDataTemplate As DataTemplate

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




    Public Property mainGridVisibility As String
        Get
            Return _mainGridVisibility
        End Get
        Set(value As String)
            _mainGridVisibility = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _mainGridVisibility As String

    Public Property tmpPlaceHolderVisibility As String
        Get
            Return _tmpPlaceHolderVisibility
        End Get
        Set(value As String)
            _tmpPlaceHolderVisibility = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _tmpPlaceHolderVisibility As String


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

    Public Property DetailsVisibility As String
        Get
            Return _DetailsVisibility
        End Get
        Set(value As String)
            _DetailsVisibility = value
            RaisePropertyChanged()
        End Set
    End Property
    Private _DetailsVisibility As String



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
        Get
            Return _IconURI
        End Get
        Set(value As String)
            _IconURI = value
        End Set
    End Property
    Private Property _IconURI As String

    Public Property IconSelect As BitmapImage
        Get
            Dim a = Application.Current.Resources("bitmap_lightbulb")
            Return a
        End Get
        Set(value As BitmapImage)
            _IconSelect = value
        End Set
    End Property
    Private Property _IconSelect As BitmapImage

#End Region
#Region "Relay Commands"
    Public ReadOnly Property DataFieldChanged As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Sub(x)
                                                   If Me.SwitchType <> Constants.MEDIA_PLAYER Then Exit Sub
                                                   If Not TiczViewModel.TiczSettings.ShowMarquee Then MarqueeStart = False : Exit Sub
                                                   Dim CanvasLength, TextLength As Integer
                                                   Dim MarqueeCanvas As Canvas = TryCast(x, Canvas)
                                                   If Not MarqueeCanvas Is Nothing Then
                                                       Dim MarqueeTextBlock = TryCast(MarqueeCanvas.Children(0), TextBlock)
                                                       If Not MarqueeTextBlock Is Nothing Then
                                                           TextLength = MarqueeTextBlock.ActualWidth
                                                           CanvasLength = MarqueeCanvas.ActualWidth
                                                           If TextLength > CanvasLength Then
                                                               MarqueeLength = CanvasLength - TextLength
                                                               MarqueeStart = True
                                                           Else
                                                               MarqueeLength = 0
                                                               MarqueeStart = False
                                                           End If
                                                       End If
                                                   End If
                                                   WriteToDebug("TiczViewModel.DataFieldChanged()", String.Format("Canvas Width:{0} / TextLength:{1} / MarqueeStart:{2}", CanvasLength, TextLength, MarqueeStart))

                                               End Sub)
        End Get
    End Property

    Public ReadOnly Property DeviceLoaded As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Sub(x)
                                                   WriteToDebug("TiczViewModel.DeviceLoaded()", "executed")
                                                   'Dim dev = x
                                                   'WriteToDebug("TiczViewModel.DeviceLoaded()", String.Format("Item Width : {0} / ItemRes : {1}", dev.ToString(), Me.DeviceRepresentation))
                                                   'HACK FOR SLIDERS
                                                   Dim dip = LevelInt
                                                   LevelInt = 0
                                                   If Not MaxDimLevel = 0 Then
                                                       LevelInt = Math.Floor((100 / MaxDimLevel) * dip)
                                                   End If

                                                   Dim devres = Me.DeviceRepresentation
                                                   Me.DeviceRepresentation = ""
                                                   Me.DeviceRepresentation = devres
                                               End Sub)
        End Get
    End Property

    Public ReadOnly Property ResizeCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)
                                                   WriteToDebug("Device.ResizeCommand()", "executed")
                                                   Dim newSizeRequested As String = TryCast(x, String)
                                                   If Not newSizeRequested Is Nothing Then
                                                       'First, remove the Device from the ViewModel, otherwise the device isn't resized properly
                                                       Dim myIndex As Integer
                                                       Dim myGroupIndex As Integer
                                                       If TiczViewModel.currentRoom.RoomConfiguration.RoomView = Constants.DASHVIEW Then
                                                           myIndex = TiczViewModel.currentRoom.GetActiveDeviceList.IndexOf(Me)
                                                           TiczViewModel.currentRoom.GetActiveDeviceList.Remove(Me)
                                                       Else
                                                           For Each g In TiczViewModel.currentRoom.GetActiveGroupedDeviceList
                                                               If g.Contains(Me) Then
                                                                   myGroupIndex = TiczViewModel.currentRoom.GetActiveGroupedDeviceList.IndexOf(g)
                                                                   myIndex = g.IndexOf(Me)
                                                                   g.Remove(Me)
                                                               End If
                                                           Next
                                                       End If
                                                       'Secondly change the DeviceRepresentation to the one selected
                                                       Select Case newSizeRequested
                                                           Case Constants.WIDE
                                                               DeviceRepresentation = Constants.WIDE
                                                           Case Constants.ICON
                                                               DeviceRepresentation = Constants.ICON
                                                           Case Constants.LARGE
                                                               DeviceRepresentation = Constants.LARGE
                                                       End Select
                                                       'Save the DeviceRepresentation to storage
                                                       Dim devConfig = (From d In TiczViewModel.currentRoom.RoomConfiguration.DeviceConfigurations Where d.DeviceIDX = Me.idx And d.DeviceName = Me.Name Select d).FirstOrDefault
                                                       If Not devConfig Is Nothing Then
                                                           devConfig.ColumnSpan = Me.ColumnSpan
                                                           devConfig.RowSpan = Me.RowSpan
                                                           devConfig.DeviceRepresentation = DeviceRepresentation
                                                       End If
                                                       Await TiczViewModel.TiczRoomConfigs.SaveRoomConfigurations()
                                                       'Re-initialize the deviceviewmodel
                                                       Await Me.Initialize(TiczViewModel.currentRoom.RoomConfiguration.RoomView)

                                                       're-insert the device back into the view
                                                       If TiczViewModel.currentRoom.RoomConfiguration.RoomView = Constants.DASHVIEW Then
                                                           TiczViewModel.currentRoom.GetActiveDeviceList.Insert(myIndex, Me)
                                                       Else
                                                           TiczViewModel.currentRoom.GetActiveGroupedDeviceList(myGroupIndex).Insert(myIndex, Me)
                                                       End If
                                                   End If
                                               End Sub)
        End Get
    End Property


    Public ReadOnly Property GroupSwitchOn As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchGroup(Constants.[ON])
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property GroupSwitchOff As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchGroup(Constants.[OFF])
                                    End Sub)
        End Get
    End Property


    Public ReadOnly Property SelectorSelectionChanged As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)

                                                   Dim combobox As ComboBox = TryCast(x, ComboBox)
                                                   If Not combobox Is Nothing Then
                                                       If combobox.SelectedIndex = -1 Then Exit Sub
                                                       Dim s As String = combobox.SelectedItem
                                                       WriteToDebug("Device.SelectorSelectionChanged()", String.Format("Selected Item : {0} / Selected Index {1}", s, combobox.SelectedIndex))
                                                       Dim SwitchToState As String = (combobox.SelectedIndex * 10).ToString
                                                       If [Protected] Then
                                                           SwitchingToState = SwitchToState
                                                           vm.selectedDevice = Me
                                                           vm.ShowDevicePassword = True
                                                           Exit Sub
                                                       End If
                                                       Dim ret As retvalue = Await SwitchDevice(SwitchToState)
                                                   Else
                                                       WriteToDebug("Device.SelectorSelectionChanged()", "ignoring...")
                                                   End If
                                               End Sub)
        End Get
    End Property

    Public ReadOnly Property SliderValueChanged As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        If Me.SwitchType = Constants.DIMMER Then
                                            'Identify what kind of range the Device handles, either 1-15 or 1-100. Based on this, calculate the value to be sent
                                            Dim ValueToSend As Integer = Math.Round((MaxDimLevel / 100) * LevelInt)
                                            WriteToDebug("Device.SliderValueChanged()", String.Format("executed : value {0}", ValueToSend))
                                            Dim SwitchToState As String = (ValueToSend).ToString
                                            If [Protected] Then
                                                SwitchingToState = SwitchToState
                                                vm.selectedDevice = Me
                                                vm.ShowDevicePassword = True
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
                                            Case Constants.BLINDS
                                                switchToState = Constants.[OFF]
                                            Case Constants.BLINDS_INVERTED
                                                switchToState = Constants.[ON]
                                            Case Else
                                                switchToState = Constants.[OFF]
                                        End Select
                                        If [Protected] Then
                                            SwitchingToState = switchToState
                                            vm.selectedDevice = Me
                                            vm.ShowDevicePassword = True
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
                                            Case Constants.BLINDS
                                                switchToState = Constants.[ON]
                                            Case Constants.BLINDS_INVERTED
                                                switchToState = Constants.[OFF]
                                            Case Else
                                                switchToState = Constants.[ON]
                                        End Select
                                        If [Protected] Then
                                            SwitchingToState = switchToState
                                            vm.selectedDevice = Me
                                            vm.ShowDevicePassword = True
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
                                        vm.selectedDevice = Me
                                        vm.ShowDeviceDetails = True
                                        WriteToDebug(vm.selectedDevice.Name, "should be there")
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ButtonPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.ButtonPressedCommand()", "executed")
                                        If Me.CanBeSwitched Then
                                            'Exit the Sub if the device is password protected. Show the password context menu, and let that handle the switch
                                            If [Protected] Then
                                                SwitchingToState = ""
                                                vm.selectedDevice = Me
                                                vm.ShowDevicePassword = True
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
#End Region

    Public Async Function LoadStatus() As Task(Of Device)
        Dim response As HttpResponseMessage
        If Type = "Group" Or Type = "Scene" Then
            response = Await Task.Run(Function() Domoticz.DownloadJSON(DomoApi.getAllScenes()))
        Else
            response = Await Task.Run(Function() Domoticz.DownloadJSON(DomoApi.getDeviceStatus(Me.idx)))
        End If

        If response.IsSuccessStatusCode Then
            Dim deserialized = JsonConvert.DeserializeObject(Of Devices)(Await response.Content.ReadAsStringAsync)
            'Dim myDevice As Device = (From dev In deserialized.result Where dev.idx = idx Select dev).FirstOrDefault()
            Dim myDevice As Device = (From dev In deserialized.result Where dev.idx = idx Select dev).FirstOrDefault()
            If Not myDevice Is Nothing Then
                Return myDevice
            Else
                Await TiczViewModel.Notify.Update(True, "couldn't get device's status", 2)
                Return Nothing
            End If
        Else
            Await TiczViewModel.Notify.Update(True, "couldn't get device's status", 2)
            Return Nothing
        End If
    End Function



    Public Async Function Update(Optional d As Device = Nothing) As Task
        'If we haven't sent an updated device to this function, retrieve the device's latest status from the server
        If d Is Nothing Then
            d = Await LoadStatus()
        End If

        If Not d Is Nothing Then
            'Set properties which raise propertychanged events on the UI thread
            Await RunOnUIThread(Sub()
                                    Level = d.Level
                                    If (SwitchType = Constants.DIMMER Or SwitchType = Constants.SELECTOR) AndAlso MaxDimLevel <> 0 Then
                                        LevelInt = Math.Floor((100 / MaxDimLevel) * d.LevelInt)
                                    End If
                                    Status = d.Status
                                    Data = d.Data
                                    Counter = d.Counter
                                    CounterToday = d.CounterToday
                                    CounterDeliv = d.CounterDeliv
                                    CounterDelivToday = d.CounterDelivToday
                                    Usage = d.Usage
                                    UsageDeliv = d.UsageDeliv
                                    If Status = "" Then Status = Data
                                    ' Set if the Device can be switched or not
                                    Select Case Type
                                        Case Constants.P1_SMART_METER
                                            CanBeSwitched = False
                                            Select Case SubType
                                                Case Constants.P1_GAS
                                                    PrimaryInformation = CounterToday
                                                    SecondaryInformation = Usage
                                                    TertiaryInformation = UsageDeliv
                                                Case Constants.P1_ELECTRIC
                                                    PrimaryInformation = Usage
                                                    SecondaryInformation = EnergyUsage
                                                    TertiaryInformation = EnergyReturn
                                            End Select
                                        Case Constants.LIGHTING_LIMITLESS
                                            CanBeSwitched = True
                                            PrimaryInformation = Status
                                        Case Constants.LIGHTING_2
                                            CanBeSwitched = True
                                            PrimaryInformation = Status
                                        Case Constants.SCENE
                                            StatusVisibility = Constants.VISIBLE
                                            DeviceType = Constants.SCENE
                                            CanBeSwitched = True
                                            PrimaryInformation = Status
                                        Case Constants.GROUP
                                            GroupVisibility = Constants.VISIBLE
                                            DeviceType = Constants.GROUP
                                            CanBeSwitched = True
                                            PrimaryInformation = Status
                                        Case Constants.WIND
                                            DeviceType = Constants.WIND
                                            CanBeSwitched = False
                                            PrimaryInformation = DirectionStr
                                            SecondaryInformation = SpeedGust
                                            TertiaryInformation = TempChill
                                        Case Constants.TYPE_RAIN
                                            DeviceType = Constants.TYPE_RAIN
                                            CanBeSwitched = False
                                            PrimaryInformation = Rain
                                        Case Constants.TEMP_HUMI_BARO
                                            DeviceType = Constants.TEMP_HUMI_BARO
                                            CanBeSwitched = False
                                            PrimaryInformation = Temp
                                            SecondaryInformation = Humidity
                                            TertiaryInformation = Barometer
                                        Case Constants.LIGHT_SWITCH
                                            CanBeSwitched = True
                                            PrimaryInformation = Status
                                        Case Else
                                            StatusVisibility = Constants.VISIBLE
                                            CanBeSwitched = False
                                            If Status = "" Then Status = Data
                                            PrimaryInformation = Status
                                    End Select
                                    'Only update/set the 4th Information Line with the LastUpdate date when the user chose this in the Settings page
                                    If TiczViewModel.TiczSettings.ShowLastSeen Then QuaternaryInformation = LastUpdate Else QuaternaryInformation = ""

                                    'Set the Status for the switch (On or Off, which is used for Icon indication
                                    If CanBeSwitched Then
                                        Select Case SwitchType
                                            Case Constants.ON_OFF
                                                StatusVisibility = Constants.VISIBLE
                                                DeviceType = Constants.ON_OFF
                                                If Status = Constants.[ON] Then isOn = True Else isOn = False
                                            Case Constants.DOOR_LOCK
                                                StatusVisibility = Constants.VISIBLE
                                                DeviceType = Constants.DOOR_LOCK
                                                If Status = Constants.OPEN Then isOn = True Else isOn = False
                                            Case Constants.CONTACT
                                                StatusVisibility = Constants.VISIBLE
                                                DeviceType = Constants.CONTACT
                                                If Status = Constants.OPEN Then isOn = True Else isOn = False
                                            Case Constants.BLINDS
                                                BlindsVisibility = Constants.VISIBLE
                                                DeviceType = Constants.BLINDS
                                                If Status = Constants.OPEN Then isOn = False Else isOn = True
                                            Case Constants.BLINDS_INVERTED
                                                BlindsVisibility = Constants.VISIBLE
                                                DeviceType = Constants.BLINDS
                                                If Status = Constants.OPEN Then isOn = True Else isOn = False
                                            Case Constants.DIMMER

                                                DimmerVisibility = Constants.VISIBLE
                                                DeviceType = Constants.DIMMER
                                                If Status = Constants.[OFF] Then isOn = False Else isOn = True
                                            Case Constants.MEDIA_PLAYER
                                                DeviceType = Constants.MEDIA_PLAYER
                                                SecondaryInformation = Data
                                                If Status = Constants.[OFF] Then isOn = False Else isOn = True
                                            Case Constants.SELECTOR
                                                SelectorVisibility = Constants.VISIBLE
                                                DeviceType = Constants.SELECTOR
                                                If Status = Constants.[OFF] Then isOn = False Else isOn = True
                                                If Not LevelNamesList.Count = 0 Then
                                                    If LevelInt Mod 10 > 0 Then
                                                        'Dimmer Level not set to a 10-value, therefore illegal
                                                        LevelNameIndex = 0
                                                    Else
                                                        LevelNameIndex = LevelInt / 10
                                                    End If
                                                    PrimaryInformation = LevelNamesList(LevelNameIndex)
                                                End If
                                            Case Else
                                                Select Case Type
                                                    Case Constants.GROUP
                                                        If Status = Constants.[OFF] Then isOn = False Else isOn = True
                                                    Case Constants.SCENE
                                                        If Status = Constants.[OFF] Then isOn = False Else isOn = True
                                                    Case Else
                                                        StatusVisibility = Constants.VISIBLE
                                                        DeviceType = Constants.GENERAL
                                                End Select
                                        End Select

                                    End If
                                End Sub)
        Else
            Throw New Exception
        End If

    End Function

    Public Async Function SwitchGroup(ToStatus As String) As Task
        WriteToDebug("Device.SwitchGroup()", "executed")
        If [Protected] Then
            SwitchingToState = ToStatus
            vm.selectedDevice = Me
            vm.ShowDevicePassword = False
            Exit Function
        Else
            Await SwitchDevice(ToStatus)
        End If
    End Function


    Public Async Function SetMoveUpDownVisibility() As Task
        Await RunOnUIThread(Sub()
                                Select Case TiczViewModel.currentRoom.GetActiveDeviceList.IndexOf(Me)
                                    Case 0
                                        Me.MoveUpDashboardVisibility = Constants.COLLAPSED
                                        Me.MoveDownDashboardVisibility = Constants.VISIBLE
                                    Case TiczViewModel.currentRoom.GetActiveDeviceList.Count - 1
                                        Me.MoveUpDashboardVisibility = Constants.VISIBLE
                                        Me.MoveDownDashboardVisibility = Constants.COLLAPSED
                                    Case Else
                                        Me.MoveUpDashboardVisibility = Constants.VISIBLE
                                        Me.MoveDownDashboardVisibility = Constants.VISIBLE
                                End Select
                            End Sub)


    End Function




    Public Async Function SwitchDevice(Optional forcedSwitchToState As String = "") As Task(Of retvalue)
        'Identify what kind of device we are and in what state we're in in order to perform the switch
        Dim url, switchToState As String
        If Not forcedSwitchToState = "" Then
            switchToState = forcedSwitchToState
        Else
            If Me.isOn Then switchToState = Constants.[OFF] Else switchToState = Constants.[ON]
        End If
        Select Case Type
            Case Constants.GROUP
                If forcedSwitchToState = "" Then
                    If Me.Status = Constants.OFF Or Me.Status = "Mixed" Then switchToState = Constants.ON Else switchToState = Constants.OFF
                End If
                url = DomoApi.SwitchScene(Me.idx, switchToState, PassCode)
            Case Constants.SCENE
                url = DomoApi.SwitchScene(Me.idx, Constants.[ON], PassCode)
        End Select
        Select Case SwitchType
            Case Nothing
                Exit Select
            Case Constants.PUSH_ON_BUTTON
                url = DomoApi.SwitchLight(Me.idx, Constants.[ON], PassCode)
            Case Constants.PUSH_OFF_BUTTON
                url = DomoApi.SwitchLight(Me.idx, Constants.[OFF], PassCode)
            Case Constants.DIMMER
                url = DomoApi.setDimmer(idx, switchToState)
            Case Constants.SELECTOR
                url = DomoApi.setDimmer(idx, switchToState)
            Case Else
                url = DomoApi.SwitchLight(Me.idx, switchToState, PassCode)
        End Select


        Dim response As HttpResponseMessage = Await Task.Run(Function() Domoticz.DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await TiczViewModel.Notify.Update(True, "Error switching device")
            Return New retvalue With {.err = "Error switching device", .issuccess = 0}
        Else
            If Not response.Content Is Nothing Then
                Dim domoRes As Domoticz.Response
                Try
                    domoRes = JsonConvert.DeserializeObject(Of Domoticz.Response)(Await response.Content.ReadAsStringAsync())
                    If domoRes.status <> "OK" Then
                        Await TiczViewModel.Notify.Update(True, domoRes.message)
                        Return New retvalue With {.err = "Error switching device", .issuccess = 0}
                    Else
                        Await TiczViewModel.Notify.Update(False, "Device switched")
                    End If
                    Await Me.Update()
                    Return New retvalue With {.issuccess = 1}
                Catch ex As Exception
                    TiczViewModel.Notify.Update(True, "Server sent empty response")
                    Return New retvalue With {.issuccess = 0, .err = "server sent empty response"}
                End Try
            End If
            Return New retvalue With {.issuccess = 0, .err = "server sent empty response"}
        End If

    End Function
    Public ReadOnly Property ButtonRightTappedCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("Device.ButtonRightTappedCommand()", "executed")
                                        If Me.DetailsVisibility = Constants.VISIBLE Then
                                            Me.DetailsVisibility = Constants.COLLAPSED
                                        Else
                                            Me.DetailsVisibility = Constants.VISIBLE
                                        End If
                                    End Sub)

        End Get
    End Property

    Public ReadOnly Property MoveUpDashboardCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.MoveUpDashboardCommand()", "executed")
                                        TiczViewModel.currentRoom.RoomConfiguration.DeviceConfigurations.MoveUp(idx, Name)
                                        Dim myIndex As Integer = TiczViewModel.currentRoom.GetActiveDeviceList.IndexOf(Me)
                                        If Not myIndex = 0 Then
                                            TiczViewModel.currentRoom.GetActiveDeviceList.Remove(Me)
                                            TiczViewModel.currentRoom.GetActiveDeviceList.Insert(myIndex - 1, Me)
                                        End If
                                        Await TiczViewModel.TiczRoomConfigs.SaveRoomConfigurations()
                                        Await Me.SetMoveUpDownVisibility()
                                        Await TiczViewModel.currentRoom.GetActiveDeviceList(myIndex).SetMoveUpDownVisibility()
                                    End Sub)

        End Get
    End Property

    Public ReadOnly Property MoveDownDashboardCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("Device.MoveDownDashboardCommand()", "executed")
                                        TiczViewModel.currentRoom.RoomConfiguration.DeviceConfigurations.MoveDown(idx, Name)
                                        Dim myIndex As Integer = TiczViewModel.currentRoom.GetActiveDeviceList.IndexOf(Me)
                                        If Not myIndex = TiczViewModel.currentRoom.GetActiveDeviceList.Count - 1 Then
                                            TiczViewModel.currentRoom.GetActiveDeviceList.Remove(Me)
                                            TiczViewModel.currentRoom.GetActiveDeviceList.Insert(myIndex + 1, Me)
                                        End If
                                        Await TiczViewModel.TiczRoomConfigs.SaveRoomConfigurations()
                                        Await Me.SetMoveUpDownVisibility()
                                        Await TiczViewModel.currentRoom.GetActiveDeviceList(myIndex).SetMoveUpDownVisibility()
                                    End Sub)

        End Get
    End Property





    Public Sub New()
        mainGridVisibility = Constants.COLLAPSED
        tmpPlaceHolderVisibility = Constants.VISIBLE
        ResizeContextMenuVisibility = Constants.COLLAPSED
        DeviceRepresentation = "Icon"
        MoveUpDashboardVisibility = Constants.COLLAPSED
        MoveDownDashboardVisibility = Constants.COLLAPSED
        LevelNamesList = New List(Of String)
        DeviceType = ""
        DimmerVisibility = Constants.COLLAPSED
        StatusVisibility = Constants.COLLAPSED
        GroupVisibility = Constants.COLLAPSED
        SelectorVisibility = Constants.COLLAPSED
        BlindsVisibility = Constants.COLLAPSED
        needsInitializing = False
        DetailsVisibility = Constants.COLLAPSED
        isOn = False
        PlanIDs = New List(Of Integer)
        PassCodeInputVisibility = Constants.COLLAPSED
    End Sub

    ''' <summary>
    ''' Based on the JSON properties of the Device, set additional properties of the ViewModel. Some are required to be set only once, others get updated each time
    ''' </summary>
    Public Async Function Initialize(Optional RoomView As String = "") As Task
        'Retrieve the DeviceConfiguration for this device 
        Dim devConfig As TiczStorage.DeviceConfiguration = (From dev In TiczViewModel.currentRoom.RoomConfiguration.DeviceConfigurations
                                                            Where dev.DeviceIDX = idx And dev.DeviceName = Name Select dev).FirstOrDefault
        If devConfig Is Nothing Then
            Dim newDevConfig As TiczStorage.DeviceConfiguration = (New TiczStorage.DeviceConfiguration With {.ColumnSpan = 1,
                                                                   .RowSpan = 1, .DeviceIDX = idx, .DeviceName = Name,
                                                                   .DeviceRepresentation = Constants.ICON, .DeviceOrder = 9999})
            TiczViewModel.currentRoom.RoomConfiguration.DeviceConfigurations.Add(newDevConfig)
            devConfig = newDevConfig
        End If
        If devConfig.DeviceRepresentation = "" Then
            devConfig.DeviceRepresentation = Constants.ICON
        End If
        DeviceOrder = devConfig.DeviceOrder


        'Set Dimmer Range, for use with the Slider Control which represents the Dimmer
        'If MaxDimLevel = 15 Then MinDimmerLevel = 1 : MaxDimmerLevel = 15
        'If MaxDimLevel = 100 Then MinDimmerLevel = 1 : MaxDimmerLevel = 100

        'For Selector, create a list of LevelNames
        If Not LevelNames = "" AndAlso LevelNamesList.Count = 0 Then
            LevelNamesList = LevelNames.Split("|").ToList()
        End If

        Select Case RoomView
            Case Constants.ICONVIEW
                DeviceRepresentation = Constants.ICON
                ResizeContextMenuVisibility = Constants.COLLAPSED
            Case Constants.GRIDVIEW
                DeviceRepresentation = Constants.WIDE
                ResizeContextMenuVisibility = Constants.COLLAPSED
                DetailsVisibility = Constants.VISIBLE
            Case Constants.LISTVIEW
                DeviceRepresentation = Constants.WIDE
                ResizeContextMenuVisibility = Constants.COLLAPSED
                DetailsVisibility = Constants.VISIBLE
            Case Constants.RESIZEVIEW
                DeviceRepresentation = devConfig.DeviceRepresentation
                ResizeContextMenuVisibility = Constants.VISIBLE
            Case Constants.DASHVIEW
                DeviceRepresentation = devConfig.DeviceRepresentation
                ResizeContextMenuVisibility = Constants.VISIBLE
                MoveUpDashboardVisibility = Constants.VISIBLE
                MoveDownDashboardVisibility = Constants.VISIBLE
        End Select

        Await Update(Me)

    End Function

End Class

Public Class DeviceGroup(Of T)
    Inherits ObservableCollection(Of DevicesViewModel)

    Public Sub New()
    End Sub

    'Public Async Function AddGroup(g As DevicesViewModel) As Task
    '    WriteToDebug("DevicesViewModel.AddDevice", "executed")
    '    Await RunOnUIThread(Sub()
    '                            Me.Add(g)
    '                        End Sub)
    'End Function

    'Public Async Function ClearGroup() As Task
    '    Await RunOnUIThread(Sub()
    '                            Me.Clear()
    '                        End Sub)
    'End Function

    Public Function GetDevice(idx As Integer, name As String) As Device
        Dim returnDevice As New Device
        For Each group In Me
            Dim dev As Device = (From d In group Where d.idx = idx And d.Name = name Select d).FirstOrDefault()
            If Not dev Is Nothing Then returnDevice = dev : Exit For
        Next
        Return returnDevice
    End Function


End Class


Public Class RoomViewModel
    Inherits ViewModelBase

    Public Const constAllDevices As String = "All Devices"
    Public Const constDashboard As String = "Dashboard"
    Public Const constFavourites As String = "Favourites"

    Public Property RoomConfiguration As New TiczStorage.RoomConfiguration

    'Public Property RoomConfiguration As TiczStorage.RoomConfiguration
    '    Get
    '        Return _RoomConfiguration
    '    End Get
    '    Set(value As TiczStorage.RoomConfiguration)
    '        If _RoomConfiguration Is Nothing Then
    '            _RoomConfiguration = value
    '            RaisePropertyChanged("RoomConfiguration")
    '        Else
    '            _RoomConfiguration = value
    '            RaisePropertyChanged("RoomConfiguration")
    '            Select Case _RoomConfiguration.RoomView
    '                Case "Icon View"
    '                    IconViewVisibility = Constants.VISIBLE
    '                    GridViewVisibility = Constants.COLLAPSED
    '                    ListViewVisibility = Constants.COLLAPSED
    '                    ResizeGridViewVisibility = Constants.COLLAPSED
    '                    DashboardViewVisibility = Constants.COLLAPSED
    '                Case "Grid View"
    '                    IconViewVisibility = Constants.COLLAPSED
    '                    GridViewVisibility = Constants.VISIBLE
    '                    ListViewVisibility = Constants.COLLAPSED
    '                    ResizeGridViewVisibility = Constants.COLLAPSED
    '                    DashboardViewVisibility = Constants.COLLAPSED
    '                Case "List View"
    '                    IconViewVisibility = Constants.COLLAPSED
    '                    GridViewVisibility = Constants.COLLAPSED
    '                    ListViewVisibility = Constants.VISIBLE
    '                    ResizeGridViewVisibility = Constants.COLLAPSED
    '                    DashboardViewVisibility = Constants.COLLAPSED
    '                Case "Resize View"
    '                    IconViewVisibility = Constants.COLLAPSED
    '                    GridViewVisibility = Constants.COLLAPSED
    '                    ListViewVisibility = Constants.COLLAPSED
    '                    ResizeGridViewVisibility = Constants.VISIBLE
    '                    DashboardViewVisibility = Constants.COLLAPSED
    '                Case "Dashboard View"
    '                    IconViewVisibility = Constants.COLLAPSED
    '                    GridViewVisibility = Constants.COLLAPSED
    '                    ListViewVisibility = Constants.COLLAPSED
    '                    ResizeGridViewVisibility = Constants.COLLAPSED
    '                    DashboardViewVisibility = Constants.VISIBLE
    '                Case Else
    '            End Select
    '        End If
    '    End Set
    'End Property
    'Private Property _RoomConfiguration As TiczStorage.RoomConfiguration

    Public Property RoomName As String
        Get
            Return _RoomName
        End Get
        Set(value As String)
            _RoomName = value
            RaisePropertyChanged("RoomName")
        End Set
    End Property
    Private Property _RoomName As String

    Public Property RoomIDX As String

    'Public Property RoomDevices As DevicesViewModel
    '    Get
    '        Return _RoomDevices
    '    End Get
    '    Set(value As DevicesViewModel)
    '        _RoomDevices = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _RoomDevices As DevicesViewModel



    Public Property IconViewDevices As New DeviceGroup(Of DevicesViewModel)
    Public Property ListViewDevices As New DeviceGroup(Of DevicesViewModel)
    Public Property GridViewDevices As New DeviceGroup(Of DevicesViewModel)
    Public Property ResizeViewDevices As New DeviceGroup(Of DevicesViewModel)
    Public Property DashboardViewDevices As New DevicesViewModel





    'Public Property GroupedRoomDevices As DeviceGroup(Of DevicesViewModel)
    '    Get
    '        Return _GroupedRoomDevices
    '    End Get
    '    Set(value As DeviceGroup(Of DevicesViewModel))
    '        _GroupedRoomDevices = value
    '        RaisePropertyChanged()
    '    End Set
    'End Property
    'Private Property _GroupedRoomDevices As DeviceGroup(Of DevicesViewModel)

    Public Property ItemWidth As Integer
        Get
            Return _ItemWidth
        End Get
        Set(value As Integer)
            _ItemWidth = value
            RaisePropertyChanged("ItemWidth")
        End Set
    End Property
    Private Property _ItemWidth As Integer
    Public Property ItemHeight As Integer

    Public ReadOnly Property GridViewSizeChangedCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Sub(x)
                                                   Dim gv As GridView = TryCast(x, GridView)
                                                   If Not gv Is Nothing Then
                                                       WriteToDebug("Room.GridViewSizeChangedCommand()", "executed")
                                                       Dim Panel = CType(gv.ItemsPanelRoot, ItemsWrapGrid)
                                                       Dim amountOfColumns = Math.Ceiling(gv.ActualWidth / 360)
                                                       If amountOfColumns < TiczViewModel.TiczSettings.MinimumNumberOfColumns Then amountOfColumns = TiczViewModel.TiczSettings.MinimumNumberOfColumns
                                                       Panel.ItemWidth = gv.ActualWidth / amountOfColumns
                                                       WriteToDebug("Panel Width = ", Panel.ItemWidth)
                                                   End If
                                               End Sub)
        End Get
    End Property

    Public Sub New()
        RoomConfiguration = New TiczStorage.RoomConfiguration
        'RoomDevices = New DevicesViewModel
        'GroupedRoomDevices = New DeviceGroup(Of DevicesViewModel)
        'IconViewVisibility = Constants.COLLAPSED
        'GridViewVisibility = Constants.COLLAPSED
        'ListViewVisibility = Constants.COLLAPSED
        'ResizeGridViewVisibility = Constants.COLLAPSED
        'DashboardViewVisibility = Constants.COLLAPSED
    End Sub

    ''' <summary>
    ''' 'Initialize() Calculates the width of each Device on screen, depending on the selected RoomView. Small differences in margins exist between views
    ''' and we want to stretch each device sufficiently to fit the whole screen on any screen-size. The Device-Width values are stored within the RoomViewModel, as it applies to all
    ''' devices within the RoomViewModel
    ''' </summary>
    Public Sub Initialize()
        Dim iWidth As Integer = 120
        Dim iMargin As Integer = 4
        If Me.RoomConfiguration.RoomView = Constants.DASHVIEW Or Me.RoomConfiguration.RoomView = Constants.RESIZEVIEW Then iWidth = 120 : iMargin = 0
        If Me.RoomConfiguration.RoomView = Constants.GRIDVIEW Then iWidth = 180 : iMargin = 4
        Dim completeItems = Math.Floor(ApplicationView.GetForCurrentView.VisibleBounds.Width / iWidth)
        Dim remainder = ApplicationView.GetForCurrentView.VisibleBounds.Width - (completeItems * iWidth) - (completeItems * iMargin)
        ItemWidth = (iWidth + Math.Floor(remainder / completeItems))
        WriteToDebug("RoomViewModel.Initialize()", String.Format("Visible Bounds:{0} / Complete Items:{1} / Remainder:{2} ItemWidth:{3}", ApplicationView.GetForCurrentView.VisibleBounds.Width, completeItems, remainder, ItemWidth))
    End Sub

    Public Sub SetRoomToLoad(Optional idx As Integer = 0)
        Dim RoomToLoad As Domoticz.Plan
        If idx = 0 Then
            ' Check for the existence of a Ticz Room. If it exists, load the contents of that room
            Dim TiczRoom As Domoticz.Plan = (From r In TiczViewModel.DomoRooms.result Where r.Name = "Ticz" Select r).FirstOrDefault()
            If Not TiczRoom Is Nothing Then
                RoomToLoad = TiczRoom
            Else
                Dim PreferredRoom As Domoticz.Plan = (From r In TiczViewModel.DomoRooms.result Where r.idx = TiczViewModel.TiczSettings.PreferredRoomIDX Select r).FirstOrDefault()
                If Not PreferredRoom Is Nothing Then
                    RoomToLoad = PreferredRoom
                    TiczViewModel.TiczSettings.PreferredRoom = TiczViewModel.TiczRoomConfigs.GetRoomConfig(RoomToLoad.idx, RoomToLoad.Name)
                Else
                    RoomToLoad = TiczViewModel.DomoRooms.result(0)
                    TiczViewModel.TiczSettings.PreferredRoom = TiczViewModel.TiczRoomConfigs.GetRoomConfig(TiczViewModel.DomoRooms.result(0).idx, TiczViewModel.DomoRooms.result(0).Name)
                End If
            End If
        Else
            RoomToLoad = (From r In TiczViewModel.DomoRooms.result Where r.idx = idx Select r).FirstOrDefault()
        End If
        RoomIDX = RoomToLoad.idx
        RoomName = RoomToLoad.Name
        RoomConfiguration.RoomView = ""
        Dim tmpRoomConfiguration = TiczViewModel.TiczRoomConfigs.GetRoomConfig(RoomToLoad.idx, RoomToLoad.Name)
        RoomConfiguration.RoomIDX = tmpRoomConfiguration.RoomIDX
        RoomConfiguration.RoomName = tmpRoomConfiguration.RoomName
        RoomConfiguration.ShowRoom = tmpRoomConfiguration.ShowRoom
        RoomConfiguration.RoomView = tmpRoomConfiguration.RoomView
        RoomConfiguration.DeviceConfigurations = tmpRoomConfiguration.DeviceConfigurations
        Initialize()
    End Sub

    Public Function GetActiveGroupedDeviceList() As DeviceGroup(Of DevicesViewModel)
        Select Case RoomConfiguration.RoomView
            Case Constants.DASHVIEW : Return Nothing
            Case Constants.ICONVIEW : Return IconViewDevices
            Case Constants.LISTVIEW : Return ListViewDevices
            Case Constants.GRIDVIEW : Return GridViewDevices
            Case Constants.RESIZEVIEW : Return ResizeViewDevices
            Case Else : Return Nothing
        End Select
    End Function

    Public Function GetActiveDeviceList() As DevicesViewModel
        Select Case RoomConfiguration.RoomView
            Case Constants.DASHVIEW : Return DashboardViewDevices
            Case Else : Return Nothing
        End Select
    End Function


    Public Async Function GetDevicesForRoom(RoomView As String) As Task(Of DevicesViewModel)
        Dim ret As New DevicesViewModel
        Dim url As String = DomoApi.getAllDevicesForRoom(RoomIDX, True)
        'Hack to change the URL used when the Room is a "All Devices" room, with a static IDX of 12321
        If Me.RoomIDX = 12321 Then url = DomoApi.getAllDevices()
        Dim response As HttpResponseMessage = Await Domoticz.DownloadJSON(url)
        Dim devicelist As New List(Of Device)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim deserialized = JsonConvert.DeserializeObject(Of Devices)(body)
            devicelist = deserialized.result.ToList()
            For Each d In devicelist
                d.MappedRoomIDX = RoomIDX
                If RoomView <> "" Then Await d.Initialize(RoomView) Else Await d.Initialize()
                ret.Add(d)
            Next
        End If
        Return ret
    End Function


    ''' <summary>
    ''' Loads the devices for this room. Depending on the Room's configuration for the View, it will load the devices in a grouped list, or in a single list (Dashboard only)
    ''' </summary>
    ''' <returns></returns>
    Public Async Function LoadDevices() As Task
        If Me.RoomConfiguration.RoomView <> Constants.DASHVIEW Then
            Await LoadGroupedDevicesForRoom()
        Else
            Await LoadDevicesForRoom()
        End If
    End Function


    ''' <summary>
    ''' 'Loads the devices for this room into RoomDevices, used for Dashboard View only (not grouped)
    ''' </summary>
    ''' <returns></returns>
    Public Async Function LoadDevicesForRoom() As Task
        Me.GetActiveDeviceList.Clear()
        Await TiczViewModel.Notify.Update(False, "loading devices for room...")
        Dim devicesToAdd = (Await Task.Run(Function() GetDevicesForRoom(Me.RoomConfiguration.RoomView))).OrderBy(Function(x) x.DeviceOrder)

        Await TiczViewModel.Notify.Update(False, "adding devices to view...")
        Await Task.Delay(200)
        For i As Integer = 0 To devicesToAdd.Count - 1
            Select Case i
                Case 0
                    devicesToAdd(i).MoveUpDashboardVisibility = Constants.COLLAPSED
                    devicesToAdd(i).MoveDownDashboardVisibility = Constants.VISIBLE
                Case devicesToAdd.Count - 1
                    devicesToAdd(i).MoveUpDashboardVisibility = Constants.VISIBLE
                    devicesToAdd(i).MoveDownDashboardVisibility = Constants.COLLAPSED
                Case Else
                    devicesToAdd(i).MoveUpDashboardVisibility = Constants.VISIBLE
                    devicesToAdd(i).MoveDownDashboardVisibility = Constants.VISIBLE
            End Select
            Me.GetActiveDeviceList.Add(devicesToAdd(i))
        Next

        Me.RoomConfiguration.DeviceConfigurations.SortRoomDevices()
    End Function

    ''' <summary>
    ''' 'Loads the devices for this room into GroupedRoomDevices, used for all view except Dashboard View
    ''' </summary>
    ''' <returns></returns>
    Public Async Function LoadGroupedDevicesForRoom() As Task
        WriteToDebug("RoomViewModel.LoadGroupedDevicesForRoom()", "executed")
        'If Me.GetActiveDeviceList Is Nothing Then

        '    Me.GetActiveDeviceList = New DeviceGroup(Of DevicesViewModel)
        'Else
        Me.GetActiveGroupedDeviceList.Clear()
        'End If

        Dim roomDevices = Await GetDevicesForRoom(Me.RoomConfiguration.RoomView)

        'Create groups for the Room. Empty groups will be filtered out by the GroupStyle in XAML
        Me.GetActiveGroupedDeviceList.Add(New DevicesViewModel(Constants.GRP_GROUPS_SCENES))
        Me.GetActiveGroupedDeviceList.Add(New DevicesViewModel(Constants.GRP_LIGHTS_SWITCHES))
        Me.GetActiveGroupedDeviceList.Add(New DevicesViewModel(Constants.GRP_WEATHER))
        Me.GetActiveGroupedDeviceList.Add(New DevicesViewModel(Constants.GRP_TEMPERATURE))
        Me.GetActiveGroupedDeviceList.Add(New DevicesViewModel(Constants.GRP_UTILITY))
        Me.GetActiveGroupedDeviceList.Add(New DevicesViewModel(Constants.GRP_OTHER))

        'Go through each device, and map it to its seperate subcollection
        For Each d In roomDevices
            Select Case d.Type
                Case Constants.SCENE, Constants.GROUP
                    Me.GetActiveGroupedDeviceList.Where(Function(x) x.Key = Constants.GRP_GROUPS_SCENES).FirstOrDefault().Add(d)
                Case Constants.LIGHTING_LIMITLESS, Constants.LIGHT_SWITCH, Constants.LIGHTING_2
                    Me.GetActiveGroupedDeviceList.Where(Function(x) x.Key = Constants.GRP_LIGHTS_SWITCHES).FirstOrDefault().Add(d)
                Case Constants.TEMP_HUMI_BARO, Constants.WIND, Constants.UV, Constants.TYPE_RAIN
                    Me.GetActiveGroupedDeviceList.Where(Function(x) x.Key = Constants.GRP_WEATHER).FirstOrDefault().Add(d)
                Case Constants.TEMP, Constants.THERMOSTAT
                    Me.GetActiveGroupedDeviceList.Where(Function(x) x.Key = Constants.GRP_TEMPERATURE).FirstOrDefault().Add(d)
                Case Constants.GENERAL, Constants.USAGE, Constants.P1_SMART_METER
                    Me.GetActiveGroupedDeviceList.Where(Function(x) x.Key = Constants.GRP_UTILITY).FirstOrDefault().Add(d)
                Case Else
                    Me.GetActiveGroupedDeviceList.Where(Function(x) x.Key = Constants.GRP_OTHER).FirstOrDefault().Add(d)
                    WriteToDebug("RoomViewModel.LoadGroupedDevicesForRoom()", String.Format("{0} : {1}", d.Name, d.Type))
            End Select
        Next
    End Function
End Class


Public Class ToastMessageViewModel
    Inherits ViewModelBase
    Implements IDisposable

    Protected Overridable Overloads Sub Dispose(disposing As Boolean)

        If disposing Then
            ' dispose managed resources
            cts.Dispose()
        End If

        ' free native resources

    End Sub 'Dispose


    Public Overloads Sub Dispose() Implements IDisposable.Dispose

        Dispose(True)
        GC.SuppressFinalize(Me)

    End Sub 'Dispose

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
    Public ReadOnly Property IconPathGeometry As String
        Get
            Select Case isError
                Case True
                    Return "m 597.15147 1021.456 c -8.72773 -3.5989 -12.40178 -6.8088 -15.77945 -13.7861 -3.27878 -6.7731 -3.13742 -14.60606 0.41965 -23.25266 3.25771 -7.91892 125.21913 -219.87912 131.68879 -228.86569 2.31457 -3.21501 6.8185 -7.57438 10.00871 -9.6875 5.01793 -3.32374 6.92295 -3.84202 14.12179 -3.84202 7.19884 0 9.10386 0.51828 14.12179 3.84202 3.19021 2.11312 7.69413 6.47249 10.00871 9.6875 6.88745 9.56689 128.59091 221.29918 131.87277 229.42426 6.14047 15.20229 1.45227 28.00599 -12.87827 35.17119 l -7.5 3.75 -134.375 0.2917 -134.375 0.2916 -7.33449 -3.0243 z m 149.58166 -38.62277 c 17.3916 -7.26668 17.64272 -33.08864 0.39794 -40.9198 -17.331 -7.87033 -35.06785 6.59585 -31.48742 25.68115 0.99444 5.3008 7.26201 12.87306 12.56907 15.18549 4.93145 2.14877 13.44641 2.17321 18.52041 0.0531 z m 2.4597 -68.9362 c 0.52628 -4.8125 3.25254 -23.09375 6.05835 -40.625 2.96167 -18.50492 4.85197 -34.11294 4.5066 -37.21038 -0.73522 -6.59403 -5.83966 -14.54946 -11.07113 -17.25476 -6.29995 -3.25783 -17.6813 -2.70895 -23.48638 1.13266 -5.60184 3.70712 -10.08931 12.05312 -10.08931 18.76453 0 2.55351 1.96577 16.01654 4.36839 29.91785 3.41197 19.74141 8.13161 51.26251 8.13161 54.30868 0 0.24338 4.64062 0.27916 10.3125 0.0795 l 10.3125 -0.36305 0.95687 -8.75 z"
                Case False
                    Return "m 653.76938 1027.2491 c -0.4313 -0.6979 -1.43577 -1.034 -2.23214 -0.7468 -0.79638 0.2871 -2.43233 0.016 -3.63545 -0.603 -1.42161 -0.7312 -2.1875 -0.6566 -2.1875 0.2129 0 0.9506 -0.72376 0.9506 -2.5 0 -1.375 -0.7359 -2.5 -1.0171 -2.5 -0.625 0 0.3921 -1.19288 0.074 -2.65085 -0.7057 -2.03559 -1.0895 -2.43576 -1.0707 -1.72415 0.081 0.71161 1.1514 0.31144 1.1702 -1.72415 0.081 -1.45798 -0.7804 -2.65085 -1.0489 -2.65085 -0.5967 0 0.4521 -0.76261 0.1891 -1.6947 -0.5845 -0.93208 -0.7735 -3.39161 -1.604 -5.4656 -1.8455 -2.074 -0.2415 -5.0928 -1.365 -6.70844 -2.4966 -1.61564 -1.1316 -3.94012 -2.0575 -5.16552 -2.0575 -1.2254 0 -5.14301 -1.4063 -8.70581 -3.125 -3.5628 -1.7188 -7.21629 -3.125 -8.11887 -3.125 -0.90258 0 -1.64106 -0.5625 -1.64106 -1.25 0 -0.6875 -1.14265 -1.25 -2.53923 -1.25 -1.39658 0 -2.86301 -0.8438 -3.25874 -1.875 -0.39573 -1.0313 -1.75112 -1.875 -3.012 -1.875 -1.26087 0 -2.62283 -0.5345 -3.02659 -1.1878 -0.87452 -1.415 -19.1254 -16.31222 -19.98443 -16.31222 -0.57659 0 -5.42321 -4.96521 -11.9896 -12.28298 -5.43243 -6.05405 -15.54998 -20.51281 -19.39505 -27.71702 -2.01815 -3.78125 -4.09124 -7.0625 -4.60686 -7.29166 -0.51563 -0.22918 -0.9375 -1.4948 -0.9375 -2.8125 0 -1.31772 -0.5625 -2.39584 -1.25 -2.39584 -0.6875 0 -1.17475 -0.42188 -1.08277 -0.9375 0.33295 -1.8666 -1.76653 -9.13084 -2.82868 -9.78729 -0.5987 -0.37001 -1.08855 -2.01261 -1.08855 -3.65021 0 -1.6376 -0.50035 -3.28669 -1.11189 -3.66464 -0.61154 -0.37796 -1.39013 -2.52052 -1.7302 -4.76127 -0.34007 -2.24075 -1.51158 -8.04118 -2.60334 -12.88984 -2.10906 -9.36661 -2.78605 -30.00613 -1.39187 -42.43425 0.46275 -4.125 0.93981 -8.625 1.06015 -10 0.276 -3.15374 4.43153 -19.17091 5.83743 -22.5 0.58068 -1.375 2.27861 -5.59375 3.77319 -9.375 2.52047 -6.37672 6.81211 -15.50464 11.06316 -23.5303 0.97439 -1.83958 2.44548 -3.9072 3.26908 -4.5947 0.82359 -0.6875 4.45792 -5.1875 8.07628 -10 3.61836 -4.8125 8.75665 -10.77297 11.41842 -13.24549 2.66177 -2.47252 4.83959 -5.14439 4.83959 -5.9375 0 -0.7931 0.84375 -1.44201 1.875 -1.44201 1.03125 0 1.875 -0.5625 1.875 -1.25 0 -0.6875 0.64223 -1.25 1.42717 -1.25 0.78495 0 3.03984 -1.6875 5.01087 -3.75 1.97104 -2.0625 4.42256 -3.75 5.44783 -3.75 1.02527 0 1.86413 -0.5625 1.86413 -1.25 0 -0.6875 0.58602 -1.25 1.30227 -1.25 0.71624 0 2.48893 -1.10555 3.9393 -2.45678 1.45037 -1.35122 3.11435 -2.16177 3.69773 -1.80122 0.58339 0.36055 1.0607 0.0266 1.0607 -0.742 0 -0.76865 0.50356 -1.08633 1.11902 -0.70595 0.61546 0.38037 1.45524 -0.18458 1.86617 -1.25546 0.41093 -1.07087 1.25468 -1.63337 1.875 -1.25 0.62032 0.38338 1.46407 -0.17912 1.875 -1.25 0.41093 -1.07087 1.31363 -1.59695 2.00598 -1.16905 0.69236 0.4279 1.25883 0.1167 1.25883 -0.69156 0 -0.85415 1.04693 -1.19578 2.5 -0.81579 1.375 0.35957 2.5 0.14392 2.5 -0.47922 0 -0.62313 0.65883 -1.13297 1.46407 -1.13297 0.80524 0 2.49274 -0.93093 3.75 -2.06873 1.41689 -1.28227 2.28593 -1.50892 2.28593 -0.59619 0 0.97189 1.07576 0.76769 3.16401 -0.60059 1.74021 -1.14022 3.50166 -1.73548 3.91435 -1.32279 0.41269 0.41269 1.81835 0.17876 3.12369 -0.51984 1.30535 -0.69859 3.90326 -1.52581 5.77315 -1.83825 1.86989 -0.31244 7.05605 -1.37249 11.5248 -2.35566 10.46739 -2.30293 38.08054 -3.36741 39.90945 -1.5385 0.95406 0.95407 1.34055 0.89542 1.34055 -0.20343 0 -1.20389 0.60635 -1.21947 2.75275 -0.0708 2.11664 1.13279 2.86686 1.13089 3.24656 -0.008 0.32076 -0.96229 0.99875 -1.06628 1.93475 -0.29675 0.79252 0.65157 3.40969 1.54062 5.81594 1.97568 6.10435 1.10369 6.67301 1.26311 8.4375 2.36542 0.85937 0.53686 2.125 0.62847 2.8125 0.20357 0.6875 -0.42489 1.25 -0.20914 1.25 0.47946 0 0.6886 1.11317 1.03762 2.47371 0.77561 1.36055 -0.26202 2.76507 -0.005 3.12115 0.57117 0.35609 0.57616 3.46166 1.72269 6.90129 2.54784 3.43961 0.82515 6.25385 2.03987 6.25385 2.69937 0 0.6595 0.5625 0.85145 1.25 0.42655 0.6875 -0.42489 1.25 -0.24245 1.25 0.40544 0 0.97178 2.07348 2.02708 6.25 3.18093 0.34375 0.095 3.25641 1.71751 6.47259 3.60565 3.21617 1.88814 6.15752 3.43298 6.53633 3.43298 2.33632 0 34.49108 27.53117 34.49108 29.53154 0 0.31515 1.17818 1.8151 2.61817 3.33323 1.44 1.51812 4.356 5.29148 6.48 8.38523 2.12402 3.09375 4.1802 5.90668 4.56933 6.25094 0.38913 0.34427 1.74479 2.59427 3.0126 5 3.88135 7.36509 6.41469 11.84385 7.0699 12.49906 0.34375 0.34375 0.80939 1.46875 1.03476 2.5 0.22537 1.03125 1.14951 3.5625 2.05368 5.625 2.79323 6.3718 3.6012 8.66903 3.83891 10.91497 0.12604 1.19074 0.79122 2.87824 1.47821 3.75 0.68699 0.87177 1.60876 4.34078 2.04839 7.70892 0.43962 3.36813 1.46226 6.9227 2.27254 7.89901 0.88558 1.06706 1.52224 6.60187 1.59612 13.8761 0.0676 6.65555 0.5831 12.83808 1.14555 13.73895 0.5737 0.91889 0.33348 3.30174 -0.54723 5.42795 -1.30184 3.14293 -1.26904 3.9837 0.19209 4.92455 1.32759 0.85484 1.39314 1.37375 0.26589 2.10477 -0.82283 0.53363 -1.74894 3.76801 -2.058 7.1875 -0.30906 3.41951 -1.50918 10.15478 -2.66693 14.96728 -1.15777 4.8125 -2.17948 9.73437 -2.27051 10.9375 -0.091 1.20313 -0.56358 2.1875 -1.05016 2.1875 -0.48657 0 -1.26782 1.74449 -1.73612 3.87665 -0.4683 2.13215 -2.13065 6.77277 -3.6941 10.3125 -1.56346 3.53971 -3.19995 7.2796 -3.63664 8.31085 -0.43669 1.03125 -1.90659 3.5625 -3.26645 5.625 -1.35986 2.0625 -2.85283 4.59375 -3.3177 5.625 -1.07212 2.37831 -16.34132 22.4806 -19.03574 25.06105 -1.11186 1.06483 -2.64536 3.03358 -3.4078 4.375 -0.76242 1.34142 -1.97385 2.43895 -2.69203 2.43895 -0.7182 0 -2.73942 1.54688 -4.49159 3.4375 -1.75218 1.89062 -3.56853 3.4375 -4.03634 3.4375 -0.46781 0 -1.76029 1.04174 -2.87217 2.31499 -1.1119 1.27324 -3.92073 3.29314 -6.24185 4.48866 -2.32113 1.19553 -5.53773 3.66933 -7.148 5.49743 -1.80145 2.0452 -2.94297 2.6492 -2.96728 1.5701 -0.0267 -1.1861 -0.79497 -0.8827 -2.37362 0.9375 -1.28377 1.4802 -3.21932 2.6913 -4.30123 2.6913 -1.08191 0 -2.32042 0.5717 -2.75225 1.2704 -0.43182 0.6987 -1.30877 0.9467 -1.94877 0.5512 -0.63999 -0.3955 -1.16363 -0.149 -1.16363 0.548 0 0.6969 -1.92979 1.6291 -4.28841 2.0716 -2.35863 0.4425 -4.61338 1.6513 -5.01054 2.6863 -0.39716 1.035 -1.44407 1.6048 -2.32645 1.2662 -0.88239 -0.3386 -2.64801 0.1475 -3.92361 1.0803 -1.27561 0.9327 -2.7873 1.4067 -3.35931 1.0531 -0.57201 -0.3535 -2.14093 -0.054 -3.48648 0.6666 -1.34556 0.7201 -4.79666 1.6046 -7.66911 1.9655 -2.87245 0.3609 -6.4238 1.3061 -7.89187 2.1004 -1.46952 0.7951 -7.86606 1.4455 -14.23172 1.4471 -6.35938 0 -11.5625 0.3423 -11.5625 0.7571 0 1.4068 -5.54252 1.1809 -6.43447 -0.2623 -0.66649 -1.0784 -1.67319 -1.0759 -4.05638 0.01 -2.11029 0.9615 -3.432 1.0202 -3.95406 0.1754 z m 5.90997 -58.91695 c 0.91296 -0.18973 1.94119 -0.64174 2.28494 -1.00447 0.34375 -0.36273 3.01562 -1.71378 5.9375 -3.00234 2.92187 -1.28857 5.3125 -2.89236 5.3125 -3.564 0 -0.67164 1.99699 -3.53934 4.43774 -6.37266 6.24418 -7.2485 6.81226 -8.02574 6.81226 -9.32069 0 -0.63234 0.84375 -1.84995 1.875 -2.70581 1.03125 -0.85586 1.875 -2.56536 1.875 -3.79889 0 -1.23352 0.52319 -2.43027 1.16265 -2.65945 0.63946 -0.22916 1.34259 -2.10416 1.5625 -4.16666 0.3252 -3.04995 -0.0669 -3.75 -2.10015 -3.75 -1.64091 0 -2.63726 0.96648 -2.89944 2.8125 -0.21969 1.54688 -0.92281 2.8125 -1.5625 2.8125 -0.63969 0 -1.16306 1.03782 -1.16306 2.30626 0 1.26845 -1.18955 3.37783 -2.64345 4.6875 -1.4539 1.30969 -3.12747 3.64686 -3.71906 5.19374 -0.77542 2.02755 -2.09268 2.8125 -4.71979 2.8125 -3.26805 0 -3.69961 -0.47475 -4.18115 -4.59951 -0.55901 -4.78833 0.21006 -10.03078 3.63459 -24.77549 1.11772 -4.8125 2.16675 -10.15625 2.33116 -11.875 1.28681 -13.45199 2.21206 -19.56368 3.1337 -20.69931 0.59111 -0.72837 1.36852 -3.54087 1.72759 -6.25 0.35906 -2.70913 0.97426 -7.17569 1.3671 -9.92569 0.39283 -2.75 0.92911 -7.15166 1.19171 -9.78148 0.26261 -2.62981 1.15391 -5.59653 1.98066 -6.59271 0.82675 -0.99617 1.20164 -2.11277 0.83308 -2.48133 -0.36856 -0.36858 0.10712 -3.58936 1.05703 -7.1573 0.94994 -3.56796 1.73383 -6.61672 1.74199 -6.77504 0.0538 -1.04388 -10.2896 1.00609 -13.52921 2.68136 -2.17578 1.12514 -3.95595 1.49626 -3.95595 0.82471 0 -0.67153 -2.36573 -0.16181 -5.25718 1.13273 -2.89145 1.29453 -5.72859 2.06236 -6.30476 1.70627 -0.57616 -0.35608 -1.72606 -0.0894 -2.55531 0.59268 -0.82926 0.68206 -3.3014 1.51972 -5.49364 1.86148 -2.19224 0.34177 -4.65935 1.03762 -5.48247 1.54633 -0.82313 0.50873 -2.40448 1.02344 -3.51412 1.14383 -4.14893 0.45009 -8.89252 3.17961 -8.89252 5.11684 0 1.57777 0.79945 1.82968 3.75 1.18163 2.30524 -0.50631 3.75 -0.3189 3.75 0.48645 0 0.72055 0.84375 0.98631 1.875 0.59059 1.03125 -0.39574 1.875 -0.13474 1.875 0.57998 0 0.71473 1.16678 1.66983 2.59284 2.12244 2.32051 0.73651 2.51449 1.50024 1.84682 7.27174 -0.4103 3.54684 -1.06309 9.8238 -1.45064 13.9488 -0.38754 4.125 -1.29878 8.21671 -2.02496 9.09271 -0.72617 0.87599 -1.01031 1.9027 -0.63142 2.28159 0.37887 0.37889 0.18532 2.44466 -0.43013 4.59061 -0.61545 2.14595 -1.39947 6.18174 -1.74229 8.96842 -0.34281 2.78667 -1.17781 6.97488 -1.85553 9.30713 -0.67774 2.33226 -0.99305 4.6275 -0.7007 5.10054 0.29236 0.47305 -0.22659 3.73406 -1.1532 7.24671 -0.92663 3.51265 -1.40705 6.66436 -1.06761 7.0038 0.33943 0.33944 0.16558 1.34783 -0.38633 2.24085 -1.10711 1.79133 -1.12281 1.88468 -2.30686 13.70959 -0.6997 6.98789 -0.4444 8.92646 1.62337 12.32665 1.34951 2.21909 3.7717 4.72132 5.38265 5.5605 2.50467 1.30475 13.33715 1.54477 18.76905 0.41587 z m 23.81317 -158.27026 c 4.07378 -3.9483 5.62688 -6.45045 5.70479 -9.19088 0.0589 -2.07108 0.3814 -4.03992 0.71671 -4.37522 0.94557 -0.94558 -2.77451 -10.61813 -5.05138 -13.13405 -1.1197 -1.23726 -3.00476 -2.24956 -4.18903 -2.24956 -1.18427 0 -2.86475 -0.85734 -3.7344 -1.90521 -0.86965 -1.04786 -1.87113 -1.61524 -2.22553 -1.26084 -0.3544 0.3544 -1.39925 0.0179 -2.32188 -0.74785 -0.92783 -0.77003 -1.67751 -0.85854 -1.67751 -0.19806 0 0.65679 -1.26563 1.08805 -2.8125 0.95835 -1.54688 -0.1297 -4.30559 0.80805 -6.13046 2.0839 -1.82488 1.27584 -3.83068 2.31971 -4.45733 2.31971 -0.62665 0 -2.28021 2.10937 -3.67457 4.6875 -2.76423 5.11093 -3.98773 16.5625 -1.76955 16.5625 0.73942 0 1.34441 0.6284 1.34441 1.39645 0 0.76804 2.10937 3.46491 4.6875 5.99305 4.38916 4.30404 5.17456 4.59306 12.34024 4.54105 7.30477 -0.053 7.90726 -0.30223 13.25049 -5.48084 z"
                Case Else
                    Return "m 653.76938 1027.2491 c -0.4313 -0.6979 -1.43577 -1.034 -2.23214 -0.7468 -0.79638 0.2871 -2.43233 0.016 -3.63545 -0.603 -1.42161 -0.7312 -2.1875 -0.6566 -2.1875 0.2129 0 0.9506 -0.72376 0.9506 -2.5 0 -1.375 -0.7359 -2.5 -1.0171 -2.5 -0.625 0 0.3921 -1.19288 0.074 -2.65085 -0.7057 -2.03559 -1.0895 -2.43576 -1.0707 -1.72415 0.081 0.71161 1.1514 0.31144 1.1702 -1.72415 0.081 -1.45798 -0.7804 -2.65085 -1.0489 -2.65085 -0.5967 0 0.4521 -0.76261 0.1891 -1.6947 -0.5845 -0.93208 -0.7735 -3.39161 -1.604 -5.4656 -1.8455 -2.074 -0.2415 -5.0928 -1.365 -6.70844 -2.4966 -1.61564 -1.1316 -3.94012 -2.0575 -5.16552 -2.0575 -1.2254 0 -5.14301 -1.4063 -8.70581 -3.125 -3.5628 -1.7188 -7.21629 -3.125 -8.11887 -3.125 -0.90258 0 -1.64106 -0.5625 -1.64106 -1.25 0 -0.6875 -1.14265 -1.25 -2.53923 -1.25 -1.39658 0 -2.86301 -0.8438 -3.25874 -1.875 -0.39573 -1.0313 -1.75112 -1.875 -3.012 -1.875 -1.26087 0 -2.62283 -0.5345 -3.02659 -1.1878 -0.87452 -1.415 -19.1254 -16.31222 -19.98443 -16.31222 -0.57659 0 -5.42321 -4.96521 -11.9896 -12.28298 -5.43243 -6.05405 -15.54998 -20.51281 -19.39505 -27.71702 -2.01815 -3.78125 -4.09124 -7.0625 -4.60686 -7.29166 -0.51563 -0.22918 -0.9375 -1.4948 -0.9375 -2.8125 0 -1.31772 -0.5625 -2.39584 -1.25 -2.39584 -0.6875 0 -1.17475 -0.42188 -1.08277 -0.9375 0.33295 -1.8666 -1.76653 -9.13084 -2.82868 -9.78729 -0.5987 -0.37001 -1.08855 -2.01261 -1.08855 -3.65021 0 -1.6376 -0.50035 -3.28669 -1.11189 -3.66464 -0.61154 -0.37796 -1.39013 -2.52052 -1.7302 -4.76127 -0.34007 -2.24075 -1.51158 -8.04118 -2.60334 -12.88984 -2.10906 -9.36661 -2.78605 -30.00613 -1.39187 -42.43425 0.46275 -4.125 0.93981 -8.625 1.06015 -10 0.276 -3.15374 4.43153 -19.17091 5.83743 -22.5 0.58068 -1.375 2.27861 -5.59375 3.77319 -9.375 2.52047 -6.37672 6.81211 -15.50464 11.06316 -23.5303 0.97439 -1.83958 2.44548 -3.9072 3.26908 -4.5947 0.82359 -0.6875 4.45792 -5.1875 8.07628 -10 3.61836 -4.8125 8.75665 -10.77297 11.41842 -13.24549 2.66177 -2.47252 4.83959 -5.14439 4.83959 -5.9375 0 -0.7931 0.84375 -1.44201 1.875 -1.44201 1.03125 0 1.875 -0.5625 1.875 -1.25 0 -0.6875 0.64223 -1.25 1.42717 -1.25 0.78495 0 3.03984 -1.6875 5.01087 -3.75 1.97104 -2.0625 4.42256 -3.75 5.44783 -3.75 1.02527 0 1.86413 -0.5625 1.86413 -1.25 0 -0.6875 0.58602 -1.25 1.30227 -1.25 0.71624 0 2.48893 -1.10555 3.9393 -2.45678 1.45037 -1.35122 3.11435 -2.16177 3.69773 -1.80122 0.58339 0.36055 1.0607 0.0266 1.0607 -0.742 0 -0.76865 0.50356 -1.08633 1.11902 -0.70595 0.61546 0.38037 1.45524 -0.18458 1.86617 -1.25546 0.41093 -1.07087 1.25468 -1.63337 1.875 -1.25 0.62032 0.38338 1.46407 -0.17912 1.875 -1.25 0.41093 -1.07087 1.31363 -1.59695 2.00598 -1.16905 0.69236 0.4279 1.25883 0.1167 1.25883 -0.69156 0 -0.85415 1.04693 -1.19578 2.5 -0.81579 1.375 0.35957 2.5 0.14392 2.5 -0.47922 0 -0.62313 0.65883 -1.13297 1.46407 -1.13297 0.80524 0 2.49274 -0.93093 3.75 -2.06873 1.41689 -1.28227 2.28593 -1.50892 2.28593 -0.59619 0 0.97189 1.07576 0.76769 3.16401 -0.60059 1.74021 -1.14022 3.50166 -1.73548 3.91435 -1.32279 0.41269 0.41269 1.81835 0.17876 3.12369 -0.51984 1.30535 -0.69859 3.90326 -1.52581 5.77315 -1.83825 1.86989 -0.31244 7.05605 -1.37249 11.5248 -2.35566 10.46739 -2.30293 38.08054 -3.36741 39.90945 -1.5385 0.95406 0.95407 1.34055 0.89542 1.34055 -0.20343 0 -1.20389 0.60635 -1.21947 2.75275 -0.0708 2.11664 1.13279 2.86686 1.13089 3.24656 -0.008 0.32076 -0.96229 0.99875 -1.06628 1.93475 -0.29675 0.79252 0.65157 3.40969 1.54062 5.81594 1.97568 6.10435 1.10369 6.67301 1.26311 8.4375 2.36542 0.85937 0.53686 2.125 0.62847 2.8125 0.20357 0.6875 -0.42489 1.25 -0.20914 1.25 0.47946 0 0.6886 1.11317 1.03762 2.47371 0.77561 1.36055 -0.26202 2.76507 -0.005 3.12115 0.57117 0.35609 0.57616 3.46166 1.72269 6.90129 2.54784 3.43961 0.82515 6.25385 2.03987 6.25385 2.69937 0 0.6595 0.5625 0.85145 1.25 0.42655 0.6875 -0.42489 1.25 -0.24245 1.25 0.40544 0 0.97178 2.07348 2.02708 6.25 3.18093 0.34375 0.095 3.25641 1.71751 6.47259 3.60565 3.21617 1.88814 6.15752 3.43298 6.53633 3.43298 2.33632 0 34.49108 27.53117 34.49108 29.53154 0 0.31515 1.17818 1.8151 2.61817 3.33323 1.44 1.51812 4.356 5.29148 6.48 8.38523 2.12402 3.09375 4.1802 5.90668 4.56933 6.25094 0.38913 0.34427 1.74479 2.59427 3.0126 5 3.88135 7.36509 6.41469 11.84385 7.0699 12.49906 0.34375 0.34375 0.80939 1.46875 1.03476 2.5 0.22537 1.03125 1.14951 3.5625 2.05368 5.625 2.79323 6.3718 3.6012 8.66903 3.83891 10.91497 0.12604 1.19074 0.79122 2.87824 1.47821 3.75 0.68699 0.87177 1.60876 4.34078 2.04839 7.70892 0.43962 3.36813 1.46226 6.9227 2.27254 7.89901 0.88558 1.06706 1.52224 6.60187 1.59612 13.8761 0.0676 6.65555 0.5831 12.83808 1.14555 13.73895 0.5737 0.91889 0.33348 3.30174 -0.54723 5.42795 -1.30184 3.14293 -1.26904 3.9837 0.19209 4.92455 1.32759 0.85484 1.39314 1.37375 0.26589 2.10477 -0.82283 0.53363 -1.74894 3.76801 -2.058 7.1875 -0.30906 3.41951 -1.50918 10.15478 -2.66693 14.96728 -1.15777 4.8125 -2.17948 9.73437 -2.27051 10.9375 -0.091 1.20313 -0.56358 2.1875 -1.05016 2.1875 -0.48657 0 -1.26782 1.74449 -1.73612 3.87665 -0.4683 2.13215 -2.13065 6.77277 -3.6941 10.3125 -1.56346 3.53971 -3.19995 7.2796 -3.63664 8.31085 -0.43669 1.03125 -1.90659 3.5625 -3.26645 5.625 -1.35986 2.0625 -2.85283 4.59375 -3.3177 5.625 -1.07212 2.37831 -16.34132 22.4806 -19.03574 25.06105 -1.11186 1.06483 -2.64536 3.03358 -3.4078 4.375 -0.76242 1.34142 -1.97385 2.43895 -2.69203 2.43895 -0.7182 0 -2.73942 1.54688 -4.49159 3.4375 -1.75218 1.89062 -3.56853 3.4375 -4.03634 3.4375 -0.46781 0 -1.76029 1.04174 -2.87217 2.31499 -1.1119 1.27324 -3.92073 3.29314 -6.24185 4.48866 -2.32113 1.19553 -5.53773 3.66933 -7.148 5.49743 -1.80145 2.0452 -2.94297 2.6492 -2.96728 1.5701 -0.0267 -1.1861 -0.79497 -0.8827 -2.37362 0.9375 -1.28377 1.4802 -3.21932 2.6913 -4.30123 2.6913 -1.08191 0 -2.32042 0.5717 -2.75225 1.2704 -0.43182 0.6987 -1.30877 0.9467 -1.94877 0.5512 -0.63999 -0.3955 -1.16363 -0.149 -1.16363 0.548 0 0.6969 -1.92979 1.6291 -4.28841 2.0716 -2.35863 0.4425 -4.61338 1.6513 -5.01054 2.6863 -0.39716 1.035 -1.44407 1.6048 -2.32645 1.2662 -0.88239 -0.3386 -2.64801 0.1475 -3.92361 1.0803 -1.27561 0.9327 -2.7873 1.4067 -3.35931 1.0531 -0.57201 -0.3535 -2.14093 -0.054 -3.48648 0.6666 -1.34556 0.7201 -4.79666 1.6046 -7.66911 1.9655 -2.87245 0.3609 -6.4238 1.3061 -7.89187 2.1004 -1.46952 0.7951 -7.86606 1.4455 -14.23172 1.4471 -6.35938 0 -11.5625 0.3423 -11.5625 0.7571 0 1.4068 -5.54252 1.1809 -6.43447 -0.2623 -0.66649 -1.0784 -1.67319 -1.0759 -4.05638 0.01 -2.11029 0.9615 -3.432 1.0202 -3.95406 0.1754 z m 5.90997 -58.91695 c 0.91296 -0.18973 1.94119 -0.64174 2.28494 -1.00447 0.34375 -0.36273 3.01562 -1.71378 5.9375 -3.00234 2.92187 -1.28857 5.3125 -2.89236 5.3125 -3.564 0 -0.67164 1.99699 -3.53934 4.43774 -6.37266 6.24418 -7.2485 6.81226 -8.02574 6.81226 -9.32069 0 -0.63234 0.84375 -1.84995 1.875 -2.70581 1.03125 -0.85586 1.875 -2.56536 1.875 -3.79889 0 -1.23352 0.52319 -2.43027 1.16265 -2.65945 0.63946 -0.22916 1.34259 -2.10416 1.5625 -4.16666 0.3252 -3.04995 -0.0669 -3.75 -2.10015 -3.75 -1.64091 0 -2.63726 0.96648 -2.89944 2.8125 -0.21969 1.54688 -0.92281 2.8125 -1.5625 2.8125 -0.63969 0 -1.16306 1.03782 -1.16306 2.30626 0 1.26845 -1.18955 3.37783 -2.64345 4.6875 -1.4539 1.30969 -3.12747 3.64686 -3.71906 5.19374 -0.77542 2.02755 -2.09268 2.8125 -4.71979 2.8125 -3.26805 0 -3.69961 -0.47475 -4.18115 -4.59951 -0.55901 -4.78833 0.21006 -10.03078 3.63459 -24.77549 1.11772 -4.8125 2.16675 -10.15625 2.33116 -11.875 1.28681 -13.45199 2.21206 -19.56368 3.1337 -20.69931 0.59111 -0.72837 1.36852 -3.54087 1.72759 -6.25 0.35906 -2.70913 0.97426 -7.17569 1.3671 -9.92569 0.39283 -2.75 0.92911 -7.15166 1.19171 -9.78148 0.26261 -2.62981 1.15391 -5.59653 1.98066 -6.59271 0.82675 -0.99617 1.20164 -2.11277 0.83308 -2.48133 -0.36856 -0.36858 0.10712 -3.58936 1.05703 -7.1573 0.94994 -3.56796 1.73383 -6.61672 1.74199 -6.77504 0.0538 -1.04388 -10.2896 1.00609 -13.52921 2.68136 -2.17578 1.12514 -3.95595 1.49626 -3.95595 0.82471 0 -0.67153 -2.36573 -0.16181 -5.25718 1.13273 -2.89145 1.29453 -5.72859 2.06236 -6.30476 1.70627 -0.57616 -0.35608 -1.72606 -0.0894 -2.55531 0.59268 -0.82926 0.68206 -3.3014 1.51972 -5.49364 1.86148 -2.19224 0.34177 -4.65935 1.03762 -5.48247 1.54633 -0.82313 0.50873 -2.40448 1.02344 -3.51412 1.14383 -4.14893 0.45009 -8.89252 3.17961 -8.89252 5.11684 0 1.57777 0.79945 1.82968 3.75 1.18163 2.30524 -0.50631 3.75 -0.3189 3.75 0.48645 0 0.72055 0.84375 0.98631 1.875 0.59059 1.03125 -0.39574 1.875 -0.13474 1.875 0.57998 0 0.71473 1.16678 1.66983 2.59284 2.12244 2.32051 0.73651 2.51449 1.50024 1.84682 7.27174 -0.4103 3.54684 -1.06309 9.8238 -1.45064 13.9488 -0.38754 4.125 -1.29878 8.21671 -2.02496 9.09271 -0.72617 0.87599 -1.01031 1.9027 -0.63142 2.28159 0.37887 0.37889 0.18532 2.44466 -0.43013 4.59061 -0.61545 2.14595 -1.39947 6.18174 -1.74229 8.96842 -0.34281 2.78667 -1.17781 6.97488 -1.85553 9.30713 -0.67774 2.33226 -0.99305 4.6275 -0.7007 5.10054 0.29236 0.47305 -0.22659 3.73406 -1.1532 7.24671 -0.92663 3.51265 -1.40705 6.66436 -1.06761 7.0038 0.33943 0.33944 0.16558 1.34783 -0.38633 2.24085 -1.10711 1.79133 -1.12281 1.88468 -2.30686 13.70959 -0.6997 6.98789 -0.4444 8.92646 1.62337 12.32665 1.34951 2.21909 3.7717 4.72132 5.38265 5.5605 2.50467 1.30475 13.33715 1.54477 18.76905 0.41587 z m 23.81317 -158.27026 c 4.07378 -3.9483 5.62688 -6.45045 5.70479 -9.19088 0.0589 -2.07108 0.3814 -4.03992 0.71671 -4.37522 0.94557 -0.94558 -2.77451 -10.61813 -5.05138 -13.13405 -1.1197 -1.23726 -3.00476 -2.24956 -4.18903 -2.24956 -1.18427 0 -2.86475 -0.85734 -3.7344 -1.90521 -0.86965 -1.04786 -1.87113 -1.61524 -2.22553 -1.26084 -0.3544 0.3544 -1.39925 0.0179 -2.32188 -0.74785 -0.92783 -0.77003 -1.67751 -0.85854 -1.67751 -0.19806 0 0.65679 -1.26563 1.08805 -2.8125 0.95835 -1.54688 -0.1297 -4.30559 0.80805 -6.13046 2.0839 -1.82488 1.27584 -3.83068 2.31971 -4.45733 2.31971 -0.62665 0 -2.28021 2.10937 -3.67457 4.6875 -2.76423 5.11093 -3.98773 16.5625 -1.76955 16.5625 0.73942 0 1.34441 0.6284 1.34441 1.39645 0 0.76804 2.10937 3.46491 4.6875 5.99305 4.38916 4.30404 5.17456 4.59306 12.34024 4.54105 7.30477 -0.053 7.90726 -0.30223 13.25049 -5.48084 z"
            End Select
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
        Get
            Return _isError
        End Get
        Set(value As Boolean)
            _isError = value
            RaisePropertyChanged()
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

Partial Public Class TiczSettings
    Inherits ViewModelBase

    Private app As App = CType(Application.Current, App)
    Private vm As TiczViewModel = app.myViewModel


    Dim settings As Windows.Storage.ApplicationDataContainer

    Const strServerIPKeyName As String = "strServerIP"
    Const strServerPortKeyName As String = "strServerPort"
    Const strUsernameKeyName As String = "strUserName"
    Const strUserPasswordKeyName As String = "strUserPassword"
    Const strMinimumNumberOfColumnsKeyName As String = "strMinimumNumberOfColumns"
    Const strShowMarqueeKeyName As String = "strShowMarquee"
    Const strShowAllDevicesKeyName As String = "strShowAllDevices"
    Const strSecondsForRefreshKeyName As String = "strSecondsForRefresh"
    Const strPreferredRoomIDXKeyName As String = "strPreferredRoomIDX"
    Const strShowLastSeenKeyName As String = "strShowLastSeen"
    Const strUseDarkThemeKeyName As String = "strUseDarkTheme"
    Const strPlaySecPanelSFXKeyName As String = "strPlaySecPanelSFX"

#If DEBUG Then
    'PUT YOUR (TEST) SERVER DETAILS HERE IF YOU WANT TO DEBUG, AND NOT PROVIDE CREDENTIALS AND SERVER DETAILS EACH TIME
    Const strServerIPDefault = ""
    Const strServerPortDefault = ""
    Const strUsernameDefault = ""
    Const strUserPasswordDefault = ""
    Const strTimeOutDefault = 5
    Const strMinimumNumberOfColumnsDefault = 2
    Const strShowMarqueeDefault = "False"
    'Const strShowFavouritesDefault = "True"
    Const strShowAllDevicesDefault = "False"
    Const strSecondsForRefreshDefault = 0
    'Const strUseBitmapIconsDefault = False
    'Const strSwitchIconBackgroundDefault = False
    'Const strcurrentRoomViewDefault = "Grid View"
    'Const strRoomConfigurationsDefault = ""
    Const strPreferredRoomIDXDefault = 0
    Const strShowLastSeenDefault = False
    Const strUseDarkThemeDefault = "True"
    Const strPlaySecPanelSFXDefault = False
#Else
    'PROD SETTINGS
    Const strServerIPDefault = ""
    Const strServerPortDefault = ""
    Const strUsernameDefault = ""
    Const strUserPasswordDefault = ""
    Const strTimeOutDefault = 0
    Const strMinimumNumberOfColumnsDefault = 1
    Const strShowMarqueeDefault = "True"
    Const strShowFavouritesDefault = "True"
    Const strShowAllDevicesDefault = True
    Const strSecondsForRefreshDefault = 10
    Const strUseBitmapIconsDefault = False
    Const strSwitchIconBackgroundDefault = False
    Const strcurrentRoomViewDefault = "Grid View"
    Const strPreferredRoomIDXDefault = 0
    Const strShowLastSeenDefault = False
    Const strUseDarkThemeDefault = "True"
    Const strPlaySecPanelSFXDefault = False
#End If

    Const strConnectionStatusDefault = False



    Public Const strDashboardDevicesFileName As String = "dashboarddevices.xml"

    Public Sub New()
        settings = Windows.Storage.ApplicationData.Current.LocalSettings
    End Sub

    Public ReadOnly Property Notify As ToastMessageViewModel
        Get
            Return TiczViewModel.Notify
        End Get
    End Property

    Public ReadOnly Property TiczRoomConfigs As TiczStorage.RoomConfigurations
        Get
            Return TiczViewModel.TiczRoomConfigs
        End Get
    End Property


    Public Property TestInProgress As Boolean
        Get
            Return _TestInProgress
        End Get
        Set(value As Boolean)
            _TestInProgress = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _TestInProgress As Boolean


    Public ReadOnly Property NavigateBackCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
                                        If rootFrame.CanGoBack Then rootFrame.GoBack()
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property TestConnectionCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        TestInProgress = True
                                        TestConnectionResult = "Testing connection..."
                                        TiczViewModel.TiczRoomConfigs.Clear()
                                        TiczViewModel.DomoRooms.result.Clear()
                                        WriteToDebug("TestConnectionCommand", ServerIP)
                                        If ContainsValidIPDetails() Then
                                            Dim response As retvalue = Await TiczViewModel.DomoRooms.Load()
                                            If response.issuccess Then
                                                TestConnectionResult = "Hurray !"
                                                Dim LoadRoomsSuccess As retvalue = Await TiczViewModel.DomoRooms.Load()
                                                If LoadRoomsSuccess.issuccess Then
                                                    Dim loadRoomConfigsSuccess As Boolean = Await TiczViewModel.TiczRoomConfigs.LoadRoomConfigurations()
                                                    Await TiczViewModel.TiczRoomConfigs.SaveRoomConfigurations()
                                                End If
                                                RaisePropertyChanged("PreferredRoom")
                                                TiczViewModel.currentRoom.SetRoomToLoad()
                                                Await TiczViewModel.currentRoom.LoadDevices()
                                                TiczViewModel.Notify.Clear()
                                                TiczViewModel.TiczMenu.IsMenuOpen = False
                                                TiczViewModel.TiczMenu.ActiveMenuContents = "Rooms"
                                            Else
                                                TestConnectionResult = String.Format("Hmm..doesn't work : {0}", response.err)
                                            End If
                                        Else
                                            TestConnectionResult = "Server IP/Port not valid !"
                                        End If
                                        TestInProgress = False
                                    End Sub)
        End Get
    End Property

    Public Property TestConnectionResult As String
        Get
            Return _TestConnectionResult
        End Get
        Set(value As String)
            _TestConnectionResult = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _TestConnectionResult As String

    'Checks if the Server IP and the Server Port are valid
    Public Function ContainsValidIPDetails() As Boolean
        Dim ValidHostnameRegex As New Regex("^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$")
        Dim ValidIpAddressRegex As New Regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$")

        If ValidHostnameRegex.IsMatch(TiczViewModel.TiczSettings.ServerIP) Or ValidIpAddressRegex.IsMatch(TiczViewModel.TiczSettings.ServerIP) Then
            Return True
        Else
            Return False
        End If
    End Function


    Public Function GetFullURL() As String
        Return "http://" + TiczViewModel.TiczSettings.ServerIP + ":" + ServerPort
    End Function

    Public Function AddOrUpdateValue(Key As String, value As Object)
        Dim valueChanged As Boolean = False

        If value Is Nothing Then Return False
        If settings.Values.ContainsKey(Key) Then
            settings.Values(Key) = value
            valueChanged = True

        Else
            settings.Values.Add(Key, value)
            valueChanged = True
        End If
        Return valueChanged
    End Function

    Public Function GetValueOrDefault(Of T)(Key As String, defaultValue As T) As T

        Dim value As T
        ' If the key exists, retrieve the value.
        If Not settings.Values(Key) Is Nothing Then
            value = DirectCast(settings.Values(Key), T)
        Else
            ' Otherwise, use the default value.
            value = defaultValue
        End If
        Return value
    End Function

    Public Sub Save()
        'settings.Save()
    End Sub

    Private _YesNoList As List(Of String) = New List(Of String)({"True", "False"}).ToList
    Public ReadOnly Property YesNoChoices As List(Of String)
        Get
            Return _YesNoList
        End Get
    End Property

    Public Property UseDarkTheme As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strUseDarkThemeKeyName, strUseDarkThemeDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strUseDarkThemeKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property UseLightTheme As Boolean
        Get
            Return Not UseDarkTheme
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strUseDarkThemeKeyName, Not value) Then
                Save()
            End If
        End Set
    End Property

    Public Property PlaySecPanelSFX As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strPlaySecPanelSFXKeyName, strPlaySecPanelSFXDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strPlaySecPanelSFXKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property ShowLastSeen As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strShowLastSeenKeyName, strShowLastSeenDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strShowLastSeenKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property ShowAllDevices As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strShowAllDevicesKeyName, strShowAllDevicesDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strShowAllDevicesKeyName, value) Then
                Save()
            End If
        End Set
    End Property
    Public Property ShowMarquee As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strShowMarqueeKeyName, strShowMarqueeDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strShowMarqueeKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Private _RoomViews As List(Of String) = New List(Of String)({Constants.ICONVIEW, Constants.GRIDVIEW, Constants.LISTVIEW, Constants.RESIZEVIEW, Constants.DASHVIEW}).ToList
    Public ReadOnly Property RoomViewChoices As List(Of String)
        Get
            Return _RoomViews
        End Get
    End Property

    Public Property PreferredRoom As TiczStorage.RoomConfiguration
        Get
            Return _PreferredRoom
        End Get
        Set(value As TiczStorage.RoomConfiguration)
            If Not value Is Nothing Then
                _PreferredRoom = value
                PreferredRoomIDX = value.RoomIDX
                RaisePropertyChanged("PreferredRoom")

            End If
        End Set
    End Property
    Private Property _PreferredRoom As TiczStorage.RoomConfiguration


    Public Property PreferredRoomIDX As Integer
        Get
            Return GetValueOrDefault(Of Integer)(strPreferredRoomIDXKeyName, strPreferredRoomIDXDefault)
        End Get
        Set(value As Integer)
            If AddOrUpdateValue(strPreferredRoomIDXKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    'Public Property currentRoomView As String
    '    Get
    '        Return GetValueOrDefault(Of String)(strcurrentRoomViewKeyName, strcurrentRoomViewDefault)
    '    End Get
    '    Set(value As String)
    '        If AddOrUpdateValue(strcurrentRoomViewKeyName, value) Then
    '            Save()
    '        End If
    '    End Set
    'End Property
    Private _SecondsForRefresh As List(Of Integer) = New List(Of Integer)({0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60}).ToList
    Public ReadOnly Property SecondsForRefreshChoices As List(Of Integer)
        Get
            Return _SecondsForRefresh
        End Get
    End Property
    Public Property SecondsForRefresh As Integer
        Get
            Return GetValueOrDefault(Of Integer)(strSecondsForRefreshKeyName, strSecondsForRefreshDefault)
        End Get
        Set(value As Integer)
            If AddOrUpdateValue(strSecondsForRefreshKeyName, value) Then
                Save()
            End If
        End Set
    End Property


    Private _NumberOfColumns As List(Of Integer) = New List(Of Integer)({1, 2, 3, 4, 5, 6, 7, 8, 9, 10}).ToList
    Public ReadOnly Property NumberOfColumnsChoices As List(Of Integer)
        Get
            Return _NumberOfColumns
        End Get
    End Property

    Public Property MinimumNumberOfColumns As Integer
        Get
            Return GetValueOrDefault(Of Integer)(strMinimumNumberOfColumnsKeyName, strMinimumNumberOfColumnsDefault)
        End Get
        Set(value As Integer)
            If AddOrUpdateValue(strMinimumNumberOfColumnsKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property ServerPort As String
        Get
            Return GetValueOrDefault(Of String)(strServerPortKeyName, strServerPortDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strServerPortKeyName, value) Then
                Save()
            End If
        End Set
    End Property



    Public Property ServerIP As String
        Get
            Return GetValueOrDefault(Of String)(strServerIPKeyName, strServerIPDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strServerIPKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property Password As String
        Get
            Return GetValueOrDefault(Of String)(strUserPasswordKeyName, strUserPasswordDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strUserPasswordKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property Username As String
        Get
            Return GetValueOrDefault(Of String)(strUsernameKeyName, strUsernameDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strUsernameKeyName, value) Then
                Save()
            End If
        End Set
    End Property

End Class


Public Class TiczMenuSettings
    Inherits ViewModelBase

    Public Sub New()
        ActiveMenuContents = "Rooms"
    End Sub


    Public Property ShowSecurityPanel As Boolean
        Get
            Return _ShowSecurityPanel
        End Get
        Set(value As Boolean)
            _ShowSecurityPanel = value
            TiczViewModel.DomoSecPanel.IsFadingIn = value
            RaisePropertyChanged("ShowSecurityPanel")
        End Set
    End Property
    Private Property _ShowSecurityPanel As Boolean


    Public Property ShowAbout As Boolean
        Get
            Return _ShowAbout
        End Get
        Set(value As Boolean)
            _ShowAbout = value
            RaisePropertyChanged("ShowAbout")
        End Set
    End Property
    Private Property _ShowAbout As Boolean


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

    Public ReadOnly Property ReloadCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("TiczMenuSettings.ReloadCommand()", "executed")
                                        IsMenuOpen = False
                                        Await TiczViewModel.Load()

                                    End Sub)
        End Get
    End Property


    Public ReadOnly Property ShowSecurityPanelCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("TiczMenuSettings.ShowSecurityPanelCommand()", "executed")
                                        IsMenuOpen = False
                                        ShowSecurityPanel = Not ShowSecurityPanel
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ShowAboutCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("TiczMenuSettings.ShowAboutCommand()", "executed")
                                        ShowAbout = Not ShowAbout
                                        If ShowAbout Then IsMenuOpen = False : ShowSecurityPanel = False
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property OpenMenuCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        If Not IsMenuOpen Then ActiveMenuContents = "Rooms"
                                        IsMenuOpen = Not IsMenuOpen
                                        ShowAbout = False
                                        If IsMenuOpen Then SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
                                        WriteToDebug("TiczMenuSettings.OpenMenuCommand()", IsMenuOpen)
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property SettingsMenuGoBack As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        If ActiveMenuContents = "Rooms" Then IsMenuOpen = False : Exit Sub

                                        If ActiveMenuContents = "Rooms Configuration" Then
                                            Await TiczViewModel.TiczRoomConfigs.SaveRoomConfigurations()
                                            ActiveMenuContents = "Settings"
                                            Exit Sub
                                        End If
                                        If ActiveMenuContents = "General" Then
                                            TiczViewModel.TiczSettings.Save()
                                            ActiveMenuContents = "Settings"
                                            Exit Sub
                                        End If
                                        If ActiveMenuContents = "Server settings" Then
                                            TiczViewModel.TiczSettings.Save()
                                            ActiveMenuContents = "Settings"
                                            Exit Sub
                                        End If
                                        If ActiveMenuContents = "Settings" Then
                                            Await TiczViewModel.TiczRoomConfigs.LoadRoomConfigurations()
                                            ActiveMenuContents = "Rooms"
                                            Exit Sub
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ShowSettingsMenu As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        ActiveMenuContents = "Settings"
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ShowRoomSettingsMenu As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        ActiveMenuContents = "Rooms Configuration"
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ShowServerSettingsMenu As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        ActiveMenuContents = "Server settings"
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ShowGeneralSettingsMenu As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        ActiveMenuContents = "General"
                                    End Sub)
        End Get
    End Property
End Class


Public Class TiczViewModel
    Inherits ViewModelBase

    Public Shared Property DomoConfig As New Domoticz.Config
    Public Shared Property DomoSunRiseSet As New Domoticz.SunRiseSet
    Public Shared Property DomoVersion As New Domoticz.Version
    Public Shared Property DomoRooms As New Domoticz.Plans
    Public Shared Property DomoSettings As New Domoticz.Settings
    Public Shared Property DomoSecPanel As New SecurityPanelViewModel
    Public Shared Property EnabledRooms As ObservableCollection(Of TiczStorage.RoomConfiguration)
    Public Shared Property TiczRoomConfigs As New TiczStorage.RoomConfigurations
    Public Shared Property TiczSettings As New TiczSettings
    Public Shared Property TiczMenu As New TiczMenuSettings
    Public Shared Property Notify As New ToastMessageViewModel
    Public Shared Property myDevices As New Devices
    Public Shared Property currentRoom As RoomViewModel
    Public Shared Property LastRefresh As DateTime

    'Properties used for the background refresher
    Public Shared Property TiczRefresher As Task
    Public Shared ct As CancellationToken
    Public Shared tokenSource As New CancellationTokenSource()


    Public Property selectedDevice As Device
        Get
            Return _selectedDevice
        End Get
        Set(value As Device)
            _selectedDevice = value
            RaisePropertyChanged("selectedDevice")
        End Set
    End Property
    Private _selectedDevice As Device


    Public Property ShowDeviceDetails As Boolean
        Get
            Return _ShowDeviceDetails
        End Get
        Set(value As Boolean)
            _ShowDeviceDetails = value
            RaisePropertyChanged("ShowDeviceDetails")
        End Set
    End Property
    Private Property _ShowDeviceDetails As Boolean

    Public Property ShowDevicePassword As Boolean
        Get
            Return _ShowDevicePassword
        End Get
        Set(value As Boolean)
            _ShowDevicePassword = value
            RaisePropertyChanged("ShowDevicePassword")
        End Set
    End Property
    Private Property _ShowDevicePassword As Boolean


    Public ReadOnly Property RoomChangedCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)
                                                   Dim s = TryCast(x, TiczStorage.RoomConfiguration)
                                                   If Not s Is Nothing Then
                                                       If TiczMenu.ShowAbout Then TiczMenu.ShowAbout = False
                                                       If TiczMenu.IsMenuOpen Then TiczMenu.IsMenuOpen = False
                                                       If TiczMenu.ShowSecurityPanel Then TiczMenu.ShowSecurityPanel = False
                                                       SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
                                                       Dim sWatch = Stopwatch.StartNew()
                                                       Me.StopRefresh()
                                                       Await TiczViewModel.Notify.Update(False, "Loading...")
                                                       currentRoom.SetRoomToLoad(s.RoomIDX)
                                                       Await currentRoom.LoadDevices()
                                                       TiczViewModel.Notify.Clear()
                                                       sWatch.Stop()
                                                       WriteToDebug("TiczViewModel.RoomChangedCommand()", String.Format("Room Change took {0} ms", sWatch.ElapsedMilliseconds))
                                                       Me.StartRefresh()
                                                   End If

                                               End Sub)
        End Get
    End Property

    Public ReadOnly Property ViewModelLoadedCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)
                                                   WriteToDebug("TiczViewModel.ViewModelLoadedCommand()", "executed")
                                                   Await Load()
                                               End Sub)
        End Get
    End Property

    Public ReadOnly Property CancelPasswordEntry As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("TiczViewModel.CancelPasswordEntry()", "executed")
                                        ShowDevicePassword = False
                                        If Not selectedDevice Is Nothing Then selectedDevice.PassCode = ""
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ConfirmPasswordEntry As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("TiczViewModel.ConfirmPasswordEntry()", "executed")
                                        ShowDevicePassword = False
                                        If Not selectedDevice Is Nothing Then
                                            If Not selectedDevice.PassCode = "" Then
                                                Await selectedDevice.SwitchDevice(selectedDevice.SwitchingToState)
                                            End If
                                            selectedDevice.PassCode = ""
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property HideDeviceDetails As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        ShowDeviceDetails = False
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property GoToAboutCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        TiczMenu.IsMenuOpen = Not TiczMenu.IsMenuOpen
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
                                        Await Refresh(True)
                                    End Sub)
        End Get
    End Property

    Public Sub New()
        ShowDeviceDetails = False
        ShowDevicePassword = False
        currentRoom = New RoomViewModel With {.ItemHeight = 120}
        EnabledRooms = New ObservableCollection(Of TiczStorage.RoomConfiguration)
    End Sub

    Public Shared Async Sub StartRefresh()
        WriteToDebug("TiczViewModel.StartRefresh()", "")
        If TiczRefresher Is Nothing OrElse TiczRefresher.IsCompleted Then
            If TiczSettings.SecondsForRefresh > 0 Then
                WriteToDebug("TiczViewModel.StartRefresh()", String.Format("every {0} seconds", TiczSettings.SecondsForRefresh))
                tokenSource = New CancellationTokenSource
                ct = tokenSource.Token
                TiczRefresher = Await Task.Factory.StartNew(Function() PerformAutoRefresh(ct), ct)
            Else
                WriteToDebug("TiczViewModel.StartRefresh()", "SecondsForRefresh = 0, not starting background task...")
            End If
        Else
            If ct.IsCancellationRequested Then
                'The Refresh task is still running, but cancellation is requested. Let it finish, before we restart it
                Dim s As New Stopwatch
                s.Start()
                While Not TiczRefresher.IsCompleted
                    Await Task.Delay(10)
                End While
                s.Stop()
                WriteToDebug("TiczViewModel.StartRefresh()", String.Format("refresher had to wait for {0} ms for previous task to complete", s.ElapsedMilliseconds))
                StartRefresh()
            End If
        End If
    End Sub

    Public Sub StopRefresh()
        If ct.CanBeCanceled Then
            tokenSource.Cancel()
        End If
        WriteToDebug("TiczViewModel.StopRefresh()", "")
    End Sub

    Public Shared Async Function PerformAutoRefresh(ct As CancellationToken) As Task
        Dim refreshperiod As Integer = TiczSettings.SecondsForRefresh
        While Not ct.IsCancellationRequested
            WriteToDebug("TiczViewModel.PerformAutoRefresh", "executed")
            Dim i As Integer = 0
            WriteToDebug("TiczViewModel.PerformAutoRefresh", "sleeping")
            While i < refreshperiod * 1000
                Await Task.Delay(100)
                i += 100
                If ct.IsCancellationRequested Then WriteToDebug("TiczViewModel.PerformAutoRefresh", "cancelling") : Exit While
            End While
            If ct.IsCancellationRequested Then Exit While
            WriteToDebug("TiczViewModel.PerformAutoRefresh", "refreshing")
            Await Refresh(False)
        End While

    End Function

    Public Shared Async Function Refresh(Optional LoadAllUpdates As Boolean = False) As Task
        Await Notify.Update(False, "refreshing...", 0)
        Dim sWatch = Stopwatch.StartNew()

        'Refresh the Sunset/Rise values
        Await TiczViewModel.DomoSunRiseSet.Load()

        'Get all devices for this room that have been updated since the LastRefresh (Domoticz will tell you which ones)
        Dim dev_response = Await Task.Run(Function() Domoticz.DownloadJSON(DomoApi.getAllDevicesForRoom(TiczViewModel.currentRoom.RoomIDX, LoadAllUpdates)))
        If dev_response.IsSuccessStatusCode Then
            Dim refreshedDevices = JsonConvert.DeserializeObject(Of Devices)(Await dev_response.Content.ReadAsStringAsync)
            For Each d In refreshedDevices.result
                WriteToDebug("", d.Name)
            Next
            If Not refreshedDevices Is Nothing AndAlso refreshedDevices.result.Count > 0 Then
                WriteToDebug("TiczViewModel.Refresh()", String.Format("Loaded {0} devices", refreshedDevices.result.Count))
                If currentRoom.RoomConfiguration.RoomView = Constants.DASHVIEW Then
                    For Each d In refreshedDevices.result
                        Dim deviceToUpdate = (From devs In currentRoom.GetActiveDeviceList Where devs.idx = d.idx And devs.Name = d.Name Select devs).FirstOrDefault()
                        If Not deviceToUpdate Is Nothing Then
                            Await deviceToUpdate.Update(d)
                        End If
                    Next
                Else
                    For Each d In refreshedDevices.result
                        Dim deviceToUpdate = currentRoom.GetActiveGroupedDeviceList.GetDevice(d.idx, d.Name)
                        If Not deviceToUpdate Is Nothing Then
                            Await deviceToUpdate.Update(d)
                        End If
                    Next
                End If
            End If
        Else
            Await Notify.Update(True, "couldn't load device status", 2)
        End If

        'Get all scenes
        Dim grp_response = Await Task.Run(Function() Domoticz.DownloadJSON(DomoApi.getAllScenesForRoom(TiczViewModel.currentRoom.RoomIDX)))
        If grp_response.IsSuccessStatusCode Then
            Dim refreshedScenes = JsonConvert.DeserializeObject(Of Devices)(Await grp_response.Content.ReadAsStringAsync)
            If Not refreshedScenes Is Nothing Then
                If currentRoom.RoomConfiguration.RoomView = Constants.RESIZEVIEW Or currentRoom.RoomConfiguration.RoomView = Constants.DASHVIEW Then
                    For Each device In currentRoom.GetActiveDeviceList.Where(Function(x) x.Type = "Group" Or x.Type = "Scene").ToList()
                        Dim updatedDevice = (From d In refreshedScenes.result Where d.idx = device.idx And d.Name = device.Name Select d).FirstOrDefault()
                        If Not updatedDevice Is Nothing Then
                            Await device.Update(updatedDevice)
                        End If
                    Next
                Else
                    For Each dg In currentRoom.GetActiveGroupedDeviceList
                        For Each device In dg.Where(Function(x) x.Type = "Group" Or x.Type = "Scene").ToList()
                            Dim updatedDevice = (From d In refreshedScenes.result Where d.idx = device.idx And d.Name = device.Name Select d).FirstOrDefault()
                            If Not updatedDevice Is Nothing Then
                                Await device.Update(updatedDevice)
                            End If
                        Next
                    Next
                End If
            End If
        Else
            Await Notify.Update(True, "couldn't load scene/group status", 2)
        End If


        'Clear the Notification
        sWatch.Stop()
        If dev_response.IsSuccessStatusCode AndAlso grp_response.IsSuccessStatusCode Then
            'But only if the amount of time passed for the Refresh is around 500ms (approx. time for the animation showing "Refreshing" to be on the screen
            WriteToDebug("TiczViewModel.Refresh()", String.Format("Refresh took {0} ms", sWatch.ElapsedMilliseconds))
            If sWatch.ElapsedMilliseconds < 500 Then
                Await Task.Delay(500 - sWatch.ElapsedMilliseconds)
            End If
            Notify.Clear()
        End If
        TiczViewModel.LastRefresh = Date.Now.ToUniversalTime
    End Function

    ''' <summary>
    ''' Performs initial loading of all Data for Ticz. Ensures all data is cleared before reloading
    ''' </summary>
    ''' <returns></returns>
    Public Shared Async Function Load() As Task
        If Not TiczViewModel.TiczSettings.ContainsValidIPDetails Then
            Await Notify.Update(True, "IP/Port settings not valid", 0)
            TiczMenu.ActiveMenuContents = "Server settings"
            Await Task.Delay(500)
            TiczMenu.IsMenuOpen = True
            Exit Function
        End If
        Await Notify.Update(False, "Loading...", 0)

        'Load Domoticz General Config from Domoticz
        Await Notify.Update(False, "Loading Domoticz configuration...", 0)
        If Not (Await DomoConfig.Load()).issuccess Then Exit Function

        Await Notify.Update(False, "Loading Domoticz settings...", 0)
        If Not (Await DomoSettings.Load()).issuccess Then Exit Function

        'Load Domoticz Sunrise/set Info from Domoticz
        Await Notify.Update(False, "Loading Domoticz Sunrise/Set...", 0)
        If Not (Await DomoSunRiseSet.Load()).issuccess Then Exit Function

        'Load Version Information from Domoticz
        Await Notify.Update(False, "Loading Domoticz version info...", 0)
        If Not (Await DomoVersion.Load()).issuccess Then Exit Function

        'Load the Room/Floorplans from the Domoticz Server
        Await Notify.Update(False, "Loading Domoticz rooms...", 0)
        Dim result As retvalue = Await DomoRooms.Load()
        If Not result.issuccess Then
            Await Notify.Update(True, "Connection Error, couldn't load Rooms..", 0)
            Exit Function
        End If
        If DomoRooms.result.Count = 0 Then
            Await Notify.Update(True, "No roomplans are configured on the Domoticz Server. Create one or more roomplans in Domoticz in order to see something here :)", 0)
            Exit Function
        End If

        'TODO : MOVE SECPANEL STUFF TO RIGHT PLACE
        Await Notify.Update(False, "Loading Domoticz Security Panel Status...", 0)
        Await TiczViewModel.DomoSecPanel.GetSecurityStatus()


        'Load the Room Configurations from Storage
        Dim isSuccess As Boolean
        Await Notify.Update(False, "Loading Ticz Room configuration...", 0)
        isSuccess = Await TiczRoomConfigs.LoadRoomConfigurations()
        'Wait for 2 seconds to let any notification stay
        If Not isSuccess Then Await Task.Delay(2000)

        currentRoom.SetRoomToLoad()

        Await Notify.Update(False, "Loading Devices for preferred room...", 0)
        Await currentRoom.LoadDevices()

        'Save the (potentially refreshhed) roomconfigurations again
        Await Notify.Update(False, "Saving Ticz Room configuration...", 0)
        Await TiczRoomConfigs.SaveRoomConfigurations()
        'If Not TiczRooms.Count = 0 Then currentRoom = TiczRooms(0)
        TiczViewModel.LastRefresh = Date.Now.ToUniversalTime
        StartRefresh()

        If TiczViewModel.DomoRooms.result.Any(Function(x) x.Name = "Ticz") Then
            Await Notify.Update(False, "You have a room in Domoticz called  'Ticz'. This is used for troubleshooting purposed, in case there are issues with the app in combination with certain controls. Due to this, no other rooms are loaded. Rename the 'Ticz' room to see other rooms.", 6)
        Else
            Notify.Clear()
        End If




    End Function
End Class
