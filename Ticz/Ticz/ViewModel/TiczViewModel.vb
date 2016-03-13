Imports System.Runtime.Serialization.Json
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


Public Class SecurityPanelViewModel
    Inherits ViewModelBase

    Public Event PlayDigitSoundRequested As EventHandler
    Public Event PlayArmRequested As EventHandler
    Public Event PlayDisArmRequested As EventHandler
    Public Event PlayWrongCodeRequested As EventHandler

    Private CountDownTask As Task
    Private cts As CancellationTokenSource
    Private ct As CancellationToken



    Public Property TimestampLastSet As DateTime


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
            Return (New DomoApi).getButtonPressedSound()
        End Get
    End Property

    Public ReadOnly Property WrongCodeSound As String
        Get
            Return (New DomoApi).getWrongCodeSound()
        End Get
    End Property
    Public ReadOnly Property DisarmSound As String
        Get
            Return (New DomoApi).getDisarmedSound()
        End Get
    End Property
    Public ReadOnly Property ArmSound As String
        Get
            Return (New DomoApi).getArmSound()
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
        Dim app As Application = CType(Xaml.Application.Current, Application)
        Await app.myViewModel.DomoSettings.Load()
        If app.myViewModel.DomoSettings.SecOnDelay > 0 Then
            If CountDownTask Is Nothing OrElse CountDownTask.IsCompleted Then
                cts = New CancellationTokenSource
                ct = cts.Token
                CountDownTask = Await Task.Factory.StartNew(Function() PerformCountDown(ct), ct)
            End If
        Else
            Await RunOnUIThread(Sub()
                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayArmRequested(Me, EventArgs.Empty)
                                    DisplayText = CurrentArmState
                                End Sub)
        End If
    End Function

    Public Async Function PerformCountDown(ct As CancellationToken) As Task
        Dim app As Application = CType(Xaml.Application.Current, Application)
        For i As Integer = 0 To app.myViewModel.DomoSettings.SecOnDelay Step 1
            If CodeInput = "" Then
                DisplayText = String.Format("ARM DELAY : {0}", app.myViewModel.DomoSettings.SecOnDelay - i)
                Await RunOnUIThread(Sub()
                                        If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDigitSoundRequested(Me, EventArgs.Empty)
                                    End Sub)

            End If
            'Wait for 1 seconds in blocks of 250ms in order to respond to cancel requests in the meantime
            For j As Integer = 0 To 3
                Task.Delay(250).Wait()
                If ct.IsCancellationRequested Then Exit Function
            Next
            'When phones suspend, this task gets suspended as well. So we need to build in checks to verify if during suspend the
            'Security Panel Delay is finished, or if the timer should be re-tuned to the actual amount of seconds remaining
            If Date.Now > TimestampLastSet.AddSeconds(app.myViewModel.DomoSettings.SecOnDelay) Then Exit For
            ' For after app resume (i.e. phones). Check if during suspend the timer has reduced, if so 
            If Date.Now < TimestampLastSet.AddSeconds(app.myViewModel.DomoSettings.SecOnDelay) And
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
                                If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayArmRequested(Me, EventArgs.Empty)
                            End Sub)
    End Function

    Public ReadOnly Property DigitPressedCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)
                                                   Dim btn As Button = TryCast(x, Button)
                                                   If Not btn Is Nothing Then
                                                       Await RunOnUIThread(Sub()
                                                                               Dim app As Application = CType(Xaml.Application.Current, Application)
                                                                               If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDigitSoundRequested(Me, EventArgs.Empty)
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
                                        Dim app As Application = CType(Xaml.Application.Current, Application)

                                        If CodeInput = "" Then
                                            IsFadingIn = False
                                            app.myViewModel.TiczMenu.ShowSecurityPanel = False
                                            app.myViewModel.ShowBackButton = False
                                        Else
                                            CodeInput = ""
                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDigitSoundRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                            DisplayText = CurrentArmState
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property DisarmPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_DISARM)
                                        CodeInput = ""
                                        Dim app As Application = CType(Xaml.Application.Current, Application)
                                        If ret.issuccess Then
                                            Await StopCountDown()
                                            CurrentArmState = "DISARMED"
                                            DisplayText = CurrentArmState

                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDisArmRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
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
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_ARMHOME)
                                        Dim app As Application = CType(Xaml.Application.Current, Application)
                                        CodeInput = ""
                                        If ret.issuccess Then
                                            CurrentArmState = "ARM HOME"
                                            TimestampLastSet = Date.Now()
                                            Await StartCountDown()
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
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
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_ARMAWAY)
                                        Dim app As Application = CType(Xaml.Application.Current, Application)
                                        CodeInput = ""
                                        If ret.issuccess Then
                                            CurrentArmState = "ARM AWAY"
                                            TimestampLastSet = Date.Now()
                                            Await StartCountDown()
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
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
            Throw New Exception("There was an Error creating the hash")
        Else
            CodeHash = CryptographicBuffer.EncodeToHexString(buffHash)
            WriteToDebug("SecurityPanel.CreateSecurityHash()", String.Format("Created a MD5 hash from {0} : {1}", CodeInput, CodeHash))
        End If

    End Sub

    Public Async Function SetSecurityStatus(status As Integer) As Task(Of retvalue)
        CreateSecurityHash()
        Dim ret As New retvalue
        Dim url As String = (New DomoApi).setSecurityStatus(status, CodeHash)
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
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
                result = Nothing
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing security response"
            End Try
        Else
            'Await TiczViewModel.Notify.Update(True, String.Format("Error setting Security Status ({0})", response.ReasonPhrase), 0)
        End If
        Return ret
    End Function

    Public Async Function GetSecurityStatus() As Task(Of retvalue)
        Dim ret As New retvalue
        Dim url As String = (New DomoApi).getSecurityStatus()
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim result As Domoticz.Response
            Try
                result = JsonConvert.DeserializeObject(Of Domoticz.Response)(body)
                If result.status = "OK" Then
                    Select Case result.secstatus
                        Case Constants.SECPANEL.SEC_ARMAWAY : CurrentArmState = Constants.SECPANEL.SEC_ARMAWAY_STATUS : DisplayText = Constants.SECPANEL.SEC_ARMAWAY_STATUS
                        Case Constants.SECPANEL.SEC_ARMHOME : CurrentArmState = Constants.SECPANEL.SEC_ARMHOME_STATUS : DisplayText = Constants.SECPANEL.SEC_ARMHOME_STATUS
                        Case Constants.SECPANEL.SEC_DISARM : CurrentArmState = Constants.SECPANEL.SEC_DISARM_STATUS : DisplayText = Constants.SECPANEL.SEC_DISARM_STATUS
                    End Select
                    ret.issuccess = 1
                Else
                    ret.issuccess = 0
                    ret.err = result.message

                End If
                result = Nothing
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing security response"
            End Try
        Else
            'Await TiczViewModel.Notify.Update(True, String.Format("Error getting Security Status ({0})", response.ReasonPhrase), 0)
        End If
        Return ret
    End Function


    Public Sub New()
    End Sub
End Class

Public Class DeviceGroup(Of T)
    Inherits ObservableCollection(Of DevicesViewModel)

    Public Sub New()
    End Sub

    Public Function GetDevice(idx As Integer, name As String) As DeviceViewModel
        For Each group In Me
            Dim dev As DeviceViewModel = (From d In group Where d.idx = idx And d.Name = name Select d).FirstOrDefault()
            If Not dev Is Nothing Then Return dev : Exit For
        Next
        Return Nothing
    End Function

End Class


Public Class RoomViewModel
    Inherits ViewModelBase

    Private app As Application = CType(Xaml.Application.Current, Application)

    Public Const constAllDevices As String = "All Devices"
    Public Const constDashboard As String = "Dashboard"
    Public Const constFavourites As String = "Favourites"

    Public Property RoomConfiguration As New TiczStorage.RoomConfiguration


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

    Public Property IconViewDevices As DeviceGroup(Of DevicesViewModel)
        Get
            Return _IconViewDevices
        End Get
        Set(value As DeviceGroup(Of DevicesViewModel))
            _IconViewDevices = value
            RaisePropertyChanged("IconViewDevices")
        End Set
    End Property
    Private Property _IconViewDevices As DeviceGroup(Of DevicesViewModel)

    Public Property ListViewDevices As DeviceGroup(Of DevicesViewModel)
        Get
            Return _ListViewDevices
        End Get
        Set(value As DeviceGroup(Of DevicesViewModel))
            _ListViewDevices = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _ListViewDevices As DeviceGroup(Of DevicesViewModel)
    Public Property GridViewDevices As DeviceGroup(Of DevicesViewModel)
        Get
            Return _GridViewDevices
        End Get
        Set(value As DeviceGroup(Of DevicesViewModel))
            _GridViewDevices = value
            RaisePropertyChanged("GridViewDevices")
        End Set
    End Property
    Private _GridViewDevices As DeviceGroup(Of DevicesViewModel)
    Public Property ResizeViewDevices As DeviceGroup(Of DevicesViewModel)
        Get
            Return _ResizeViewDevices
        End Get
        Set(value As DeviceGroup(Of DevicesViewModel))
            _ResizeViewDevices = value
            RaisePropertyChanged("ResizeViewDevices")
        End Set
    End Property
    Private Property _ResizeViewDevices As DeviceGroup(Of DevicesViewModel)
    Public Property DashboardViewDevices As DevicesViewModel
        Get
            Return _DashboardViewDevices
        End Get
        Set(value As DevicesViewModel)
            _DashboardViewDevices = value
            RaisePropertyChanged("DashboardViewDevices")
        End Set
    End Property
    Private Property _DashboardViewDevices As DevicesViewModel


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
                                                       If amountOfColumns < app.myViewModel.TiczSettings.MinimumNumberOfColumns Then amountOfColumns = app.myViewModel.TiczSettings.MinimumNumberOfColumns
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
        If Me.RoomConfiguration.RoomView = Constants.ROOMVIEW.DASHVIEW Or Me.RoomConfiguration.RoomView = Constants.ROOMVIEW.RESIZEVIEW Then iWidth = 120 : iMargin = 0
        If Me.RoomConfiguration.RoomView = Constants.ROOMVIEW.GRIDVIEW Then iWidth = 180 : iMargin = 4
        Dim completeItems = Math.Floor(ApplicationView.GetForCurrentView.VisibleBounds.Width / iWidth)
        Dim remainder = ApplicationView.GetForCurrentView.VisibleBounds.Width - (completeItems * iWidth) - (completeItems * iMargin)
        ItemWidth = (iWidth + Math.Floor(remainder / completeItems))
        WriteToDebug("RoomViewModel.Initialize()", String.Format("Visible Bounds:{0} / Complete Items:{1} / Remainder:{2} ItemWidth:{3}", ApplicationView.GetForCurrentView.VisibleBounds.Width, completeItems, remainder, ItemWidth))
    End Sub

    Public Sub SetRoomToLoad(Optional idx As Integer = 0)
        Dim RoomToLoad As Domoticz.Plan
        If idx = 0 Then
            ' Check for the existence of a Ticz Room. If it exists, load the contents of that room
            Dim TiczRoom As Domoticz.Plan = (From r In app.myViewModel.DomoRooms.result Where r.Name = "Ticz" Select r).FirstOrDefault()
            If Not TiczRoom Is Nothing Then
                RoomToLoad = TiczRoom
            Else
                Dim PreferredRoom As Domoticz.Plan = (From r In app.myViewModel.DomoRooms.result Where r.idx = app.myViewModel.TiczSettings.PreferredRoomIDX Select r).FirstOrDefault()
                If Not PreferredRoom Is Nothing Then
                    RoomToLoad = PreferredRoom
                    app.myViewModel.TiczSettings.PreferredRoom = app.myViewModel.TiczRoomConfigs.GetRoomConfig(RoomToLoad.idx, RoomToLoad.Name)
                Else
                    RoomToLoad = app.myViewModel.DomoRooms.result(0)
                    app.myViewModel.TiczSettings.PreferredRoom = app.myViewModel.TiczRoomConfigs.GetRoomConfig(app.myViewModel.DomoRooms.result(0).idx, app.myViewModel.DomoRooms.result(0).Name)
                End If
            End If
        Else
            RoomToLoad = (From r In app.myViewModel.DomoRooms.result Where r.idx = idx Select r).FirstOrDefault()
        End If
        RoomIDX = RoomToLoad.idx
        RoomName = RoomToLoad.Name
        RoomConfiguration.RoomView = ""
        Dim tmpRoomConfiguration = app.myViewModel.TiczRoomConfigs.GetRoomConfig(RoomToLoad.idx, RoomToLoad.Name)
        RoomConfiguration.RoomIDX = tmpRoomConfiguration.RoomIDX
        RoomConfiguration.RoomName = tmpRoomConfiguration.RoomName
        RoomConfiguration.ShowRoom = tmpRoomConfiguration.ShowRoom
        RoomConfiguration.RoomView = tmpRoomConfiguration.RoomView
        RoomConfiguration.DeviceConfigurations = tmpRoomConfiguration.DeviceConfigurations
        Initialize()
    End Sub

    Public Function GetActiveGroupedDeviceList() As DeviceGroup(Of DevicesViewModel)
        Select Case RoomConfiguration.RoomView
            Case Constants.ROOMVIEW.DASHVIEW : Return Nothing
            Case Constants.ROOMVIEW.ICONVIEW : Return IconViewDevices
            Case Constants.ROOMVIEW.LISTVIEW : Return ListViewDevices
            Case Constants.ROOMVIEW.GRIDVIEW : Return GridViewDevices
            Case Constants.ROOMVIEW.RESIZEVIEW : Return ResizeViewDevices
            Case Else : Return Nothing
        End Select
    End Function

    Public Function GetActiveDeviceList() As DevicesViewModel
        Select Case RoomConfiguration.RoomView
            Case Constants.ROOMVIEW.DASHVIEW : Return DashboardViewDevices
            Case Else : Return Nothing
        End Select
    End Function


    Public Async Function GetDevicesForRoom(RoomView As String) As Task(Of DevicesViewModel)
        'TODO : Remove DeviceModel DeviceViewModel tests
        Dim ret As New DevicesViewModel
        Dim url As String = (New DomoApi).getAllDevicesForRoom(RoomIDX, True)
        'Hack to change the URL used when the Room is a "All Devices" room, with a static IDX of 12321
        If Me.RoomIDX = 12321 Then url = (New DomoApi).getAllDevices()
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
        Dim devicelist As New List(Of DeviceModel)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim deserialized = JsonConvert.DeserializeObject(Of DevicesModel)(body)
            devicelist = deserialized.result.ToList()
            For Each d In devicelist
                Dim DevToAdd As New DeviceViewModel(d, RoomView)
                If app.myViewModel.TiczSettings.OnlyShowFavourites Then
                    If d.Favorite = 1 Then ret.Add(DevToAdd)
                Else
                    ret.Add(DevToAdd)
                End If


            Next
            deserialized = Nothing
        End If
        Return ret
    End Function


    ''' <summary>
    ''' Loads the devices for this room. Depending on the Room's configuration for the View, it will load the devices in a grouped list, or in a single list (Dashboard only)
    ''' </summary>
    ''' <returns></returns>
    Public Async Function LoadDevices() As Task
        Await app.myViewModel.Notify.Update(False, "loading devices...")
        If Not DashboardViewDevices Is Nothing Then DashboardViewDevices.Clear()
        If Not IconViewDevices Is Nothing Then IconViewDevices.Clear()
        If Not GridViewDevices Is Nothing Then GridViewDevices.Clear()
        If Not ListViewDevices Is Nothing Then ListViewDevices.Clear()
        If Not ResizeViewDevices Is Nothing Then ResizeViewDevices.Clear()
        If Me.RoomConfiguration.RoomView <> Constants.ROOMVIEW.DASHVIEW Then
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
        Dim devicesToAdd = (Await GetDevicesForRoom(Me.RoomConfiguration.RoomView)).OrderBy(Function(x) x.DeviceOrder)
        Me.DashboardViewDevices = New DevicesViewModel(RoomName, devicesToAdd)
        Me.RoomConfiguration.DeviceConfigurations.SortRoomDevices()
    End Function

    ''' <summary>
    ''' 'Loads the devices for this room into GroupedRoomDevices, used for all view except Dashboard View
    ''' </summary>
    ''' <returns></returns>
    Public Async Function LoadGroupedDevicesForRoom() As Task
        WriteToDebug("RoomViewModel.LoadGroupedDevicesForRoom()", "executed")
        Dim NewDevices As New DeviceGroup(Of DevicesViewModel)
        Dim roomDevices = Await GetDevicesForRoom(Me.RoomConfiguration.RoomView)

        'Create groups for the Room. Empty groups will be filtered out by the GroupStyle in XAML
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_GROUPS_SCENES))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_LIGHTS_SWITCHES))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_WEATHER))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_TEMPERATURE))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_UTILITY))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_OTHER))

        'Go through each device, and map it to its seperate subcollection
        For Each d In roomDevices
            Select Case d.Type
                Case Constants.DEVICE.TYPE.SCENE, Constants.DEVICE.TYPE.GROUP
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_GROUPS_SCENES).FirstOrDefault().Add(d)
                Case Constants.DEVICE.TYPE.LIGHTING_LIMITLESS, Constants.DEVICE.TYPE.LIGHT_SWITCH, Constants.DEVICE.TYPE.LIGHTING_2
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_LIGHTS_SWITCHES).FirstOrDefault().Add(d)
                Case Constants.DEVICE.TYPE.TEMP_HUMI_BARO, Constants.DEVICE.TYPE.WIND, Constants.DEVICE.TYPE.UV, Constants.DEVICE.TYPE.RAIN
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_WEATHER).FirstOrDefault().Add(d)
                Case Constants.DEVICE.TYPE.TEMP, Constants.DEVICE.TYPE.THERMOSTAT
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_TEMPERATURE).FirstOrDefault().Add(d)
                Case Constants.DEVICE.TYPE.GENERAL, Constants.DEVICE.TYPE.USAGE, Constants.DEVICE.TYPE.P1_SMART_METER
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_UTILITY).FirstOrDefault().Add(d)
                Case Else
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_OTHER).FirstOrDefault().Add(d)
                    WriteToDebug("RoomViewModel.LoadGroupedDevicesForRoom()", String.Format("{0} : {1}", d.Name, d.Type))
            End Select
        Next

        Select Case RoomConfiguration.RoomView
            Case Constants.ROOMVIEW.ICONVIEW : IconViewDevices = NewDevices
            Case Constants.ROOMVIEW.LISTVIEW : ListViewDevices = NewDevices
            Case Constants.ROOMVIEW.GRIDVIEW : GridViewDevices = NewDevices
            Case Constants.ROOMVIEW.RESIZEVIEW : ResizeViewDevices = NewDevices
        End Select
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

    Private app As Application = CType(Xaml.Application.Current, Application)
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
    Const strOnlyShowFavouritesKeyName As String = "strOnlyShowFavourites"

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
    Const strOnlyShowFavouritesDefault As Boolean = False
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
    Const strOnlyShowFavouritesDefault As Boolean = False
#End If

    Const strConnectionStatusDefault = False



    Public Const strDashboardDevicesFileName As String = "dashboarddevices.xml"

    Public Sub New()
        settings = Windows.Storage.ApplicationData.Current.LocalSettings
    End Sub

    Public ReadOnly Property TiczRoomConfigs As TiczStorage.RoomConfigurations
        Get
            Return app.myViewModel.TiczRoomConfigs
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
                                        app.myViewModel.TiczRoomConfigs.Clear()
                                        app.myViewModel.DomoRooms.result.Clear()
                                        WriteToDebug("TestConnectionCommand", ServerIP)
                                        If ContainsValidIPDetails() Then
                                            Dim response As retvalue = Await app.myViewModel.DomoRooms.Load()
                                            If response.issuccess Then
                                                TestConnectionResult = "Hurray !"
                                                Dim LoadRoomsSuccess As retvalue = Await app.myViewModel.DomoRooms.Load()
                                                If LoadRoomsSuccess.issuccess Then
                                                    Dim loadRoomConfigsSuccess As Boolean = Await app.myViewModel.TiczRoomConfigs.LoadRoomConfigurations()
                                                    Await app.myViewModel.TiczRoomConfigs.SaveRoomConfigurations()
                                                End If
                                                RaisePropertyChanged("PreferredRoom")
                                                app.myViewModel.currentRoom.SetRoomToLoad()
                                                Await app.myViewModel.currentRoom.LoadDevices()
                                                app.myViewModel.Notify.Clear()
                                                app.myViewModel.TiczMenu.IsMenuOpen = False
                                                app.myViewModel.TiczMenu.ActiveMenuContents = "Rooms"
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

        If ValidHostnameRegex.IsMatch(app.myViewModel.TiczSettings.ServerIP) Or ValidIpAddressRegex.IsMatch(app.myViewModel.TiczSettings.ServerIP) Then
            Return True
        Else
            Return False
        End If
    End Function


    Public Function GetFullURL() As String
        Return "http://" + app.myViewModel.TiczSettings.ServerIP + ":" + ServerPort
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


    Public Property OnlyShowFavourites As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strOnlyShowFavouritesKeyName, strOnlyShowFavouritesDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strOnlyShowFavouritesKeyName, value) Then
                Save()
            End If
        End Set
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

    Private _RoomViews As List(Of String) = New List(Of String)({Constants.ROOMVIEW.ICONVIEW, Constants.ROOMVIEW.GRIDVIEW, Constants.ROOMVIEW.LISTVIEW,
                                                                Constants.ROOMVIEW.RESIZEVIEW, Constants.ROOMVIEW.DASHVIEW}).ToList
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

    Private app As Application = CType(Xaml.Application.Current, Application)

    Public Sub New()
        ActiveMenuContents = "Rooms"
    End Sub


    Public Property ShowSecurityPanel As Boolean
        Get
            Return _ShowSecurityPanel
        End Get
        Set(value As Boolean)
            _ShowSecurityPanel = value
            app.myViewModel.DomoSecPanel.IsFadingIn = value
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

    Public Property ShowBackButton As Boolean
        Get
            Return _ShowBackButton
        End Get
        Set(value As Boolean)
            _ShowBackButton = value
            RaisePropertyChanged("ShowBackButton")
        End Set
    End Property
    Private Property _ShowBackButton As Boolean


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
                                        ShowSecurityPanel = False
                                        Dim app As Application = CType(Xaml.Application.Current, Application)
                                        app.myViewModel.ShowDeviceGraph = False
                                        app.myViewModel.ShowDeviceDetails = False
                                        app.myViewModel.ShowDevicePassword = False
                                        Await app.myViewModel.Load()

                                    End Sub)
        End Get
    End Property


    Public ReadOnly Property ShowSecurityPanelCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("TiczMenuSettings.ShowSecurityPanelCommand()", "executed")
                                        IsMenuOpen = False
                                        app.myViewModel.ShowDeviceGraph = False
                                        app.myViewModel.ShowDeviceDetails = False
                                        app.myViewModel.ShowDevicePassword = False
                                        ShowSecurityPanel = Not ShowSecurityPanel
                                        If ShowSecurityPanel Then ShowBackButton = True
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ShowAboutCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        WriteToDebug("TiczMenuSettings.ShowAboutCommand()", "executed")
                                        ShowAbout = Not ShowAbout
                                        If ShowAbout Then IsMenuOpen = False : ShowSecurityPanel = False : ShowBackButton = True
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property OpenMenuCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        If Not IsMenuOpen Then ActiveMenuContents = "Rooms"
                                        IsMenuOpen = Not IsMenuOpen
                                        ShowAbout = False
                                        If IsMenuOpen Then
                                            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
                                            app.myViewModel.ShowBackButton = True
                                        Else
                                            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
                                            app.myViewModel.ShowBackButton = False
                                        End If
                                        WriteToDebug("TiczMenuSettings.OpenMenuCommand()", IsMenuOpen)
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property SettingsMenuGoBack As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        If ActiveMenuContents = "Rooms" Then IsMenuOpen = False : ShowBackButton = False : Exit Sub

                                        If ActiveMenuContents = "Rooms Configuration" Then
                                            Await app.myViewModel.TiczRoomConfigs.SaveRoomConfigurations()
                                            ActiveMenuContents = "Settings"
                                            Exit Sub
                                        End If
                                        If ActiveMenuContents = "General" Then
                                            app.myViewModel.TiczSettings.Save()
                                            ActiveMenuContents = "Settings"
                                            Exit Sub
                                        End If
                                        If ActiveMenuContents = "Server settings" Then
                                            app.myViewModel.TiczSettings.Save()
                                            ActiveMenuContents = "Settings"
                                            Exit Sub
                                        End If
                                        If ActiveMenuContents = "Settings" Then
                                            Await app.myViewModel.TiczRoomConfigs.LoadRoomConfigurations()
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


Public Class TiczViewModel
    Inherits ViewModelBase

    Public Property GraphList As GraphListViewModel
        Get
            Return _GraphList
        End Get
        Set(value As GraphListViewModel)
            _GraphList = value
            RaisePropertyChanged("GraphList")
        End Set
    End Property
    Private Property _GraphList As GraphListViewModel

    Public Property DomoConfig As New Domoticz.Config
    Public Property DomoSunRiseSet As New Domoticz.SunRiseSet
    Public Property DomoVersion As New Domoticz.Version
    Public Property DomoRooms As New Domoticz.Plans
    Public Property DomoSettings As New Domoticz.Settings
    Public Property DomoSecPanel As New SecurityPanelViewModel
    Public Property EnabledRooms As ObservableCollection(Of TiczStorage.RoomConfiguration)
    Public Property TiczRoomConfigs As New TiczStorage.RoomConfigurations
    Public Property TiczSettings As New TiczSettings
    Public Property TiczMenu As New TiczMenuSettings
    Public Property Notify As New ToastMessageViewModel
    Public Property currentRoom As RoomViewModel
        Get
            Return _currentRoom
        End Get
        Set(value As RoomViewModel)
            _currentRoom = value
            RaisePropertyChanged("currentRoom")
        End Set
    End Property
    Private Property _currentRoom As RoomViewModel
    Public Property LastRefresh As DateTime

    'Properties used for the background refresher
    Public Property TiczRefresher As Task
    Public ct As CancellationToken
    Public tokenSource As New CancellationTokenSource()


    Public Property selectedDevice As DeviceViewModel
        Get
            Return _selectedDevice
        End Get
        Set(value As DeviceViewModel)
            _selectedDevice = value
            RaisePropertyChanged("selectedDevice")
        End Set
    End Property
    Private _selectedDevice As DeviceViewModel


    Public Property ShowDeviceGraph As Boolean
        Get
            Return _ShowDeviceGraph
        End Get
        Set(value As Boolean)
            _ShowDeviceGraph = value
            RaisePropertyChanged("ShowDeviceGraph")
        End Set
    End Property
    Private Property _ShowDeviceGraph As Boolean

    Public ReadOnly Property CanGoBack As Boolean
        Get
            If Not ShowDeviceDetails And Not ShowDeviceGraph And Not ShowDevicePassword And Not TiczMenu.ShowSecurityPanel And Not TiczMenu.ShowAbout And Not TiczMenu.IsMenuOpen Then
                Return False
            Else
                Return True
            End If
        End Get
    End Property

    Public Property ShowBackButton As Boolean
        Get
            If Not Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons") Then
                If CanGoBack Then
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
                    Return True
                Else
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
                    Return False
                End If
            Else
                Return False
            End If
        End Get
        Set(value As Boolean)
            _ShowBackButton = value
            RaisePropertyChanged("ShowBackButton")
            RaisePropertyChanged("BackButtonVisibility")
        End Set
    End Property
    Private Property _ShowBackButton As Boolean

    Public ReadOnly Property BackButtonVisibility As String
        Get
            If ShowBackButton Then Return Constants.VISIBLE Else Return Constants.COLLAPSED
        End Get
    End Property



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


    Public ReadOnly Property GoBackCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Sub(x)
                                                   WriteToDebug("App.GoBackCommand", "executed")

                                                   If ShowDeviceGraph Then
                                                       ShowDeviceGraph = False
                                                   ElseIf ShowDeviceDetails Then
                                                       ShowDeviceDetails = False
                                                   ElseIf TiczMenu.ShowAbout Then
                                                       TiczMenu.ShowAbout = False
                                                   ElseIf TiczMenu.ShowSecurityPanel Then
                                                       TiczMenu.ShowSecurityPanel = False
                                                   ElseIf TiczMenu.IsMenuOpen And TiczMenu.ActiveMenuContents = "Rooms" Then
                                                       TiczMenu.IsMenuOpen = False
                                                   ElseIf TiczMenu.IsMenuOpen And TiczMenu.ActiveMenuContents = "Rooms Configuration" Then
                                                       TiczMenu.ActiveMenuContents = "Settings"
                                                   ElseIf TiczMenu.IsMenuOpen And TiczMenu.ActiveMenuContents = "General" Then
                                                       TiczMenu.ActiveMenuContents = "Settings"
                                                   ElseIf TiczMenu.IsMenuOpen And TiczMenu.ActiveMenuContents = "Server settings" Then
                                                       TiczMenu.ActiveMenuContents = "Settings"
                                                   ElseIf TiczMenu.IsMenuOpen And TiczMenu.ActiveMenuContents = "Settings" Then
                                                       TiczMenu.ActiveMenuContents = "Rooms"
                                                   End If


                                                   ShowBackButton = False



                                               End Sub)

        End Get
    End Property

    Public ReadOnly Property RoomChangedCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)
                                                   Dim s = TryCast(x, TiczStorage.RoomConfiguration)
                                                   If Not s Is Nothing Then
                                                       If TiczMenu.ShowAbout Then TiczMenu.ShowAbout = False
                                                       If ShowDeviceGraph Then ShowDeviceGraph = False
                                                       If ShowDeviceDetails Then ShowDeviceDetails = False
                                                       If ShowDevicePassword Then ShowDevicePassword = False
                                                       If TiczMenu.IsMenuOpen Then TiczMenu.IsMenuOpen = False
                                                       If TiczMenu.ShowSecurityPanel Then TiczMenu.ShowSecurityPanel = False
                                                       ShowBackButton = False
                                                       SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
                                                       Dim sWatch = Stopwatch.StartNew()
                                                       Me.StopRefresh()
                                                       Await Notify.Update(False, "Loading...")
                                                       currentRoom.SetRoomToLoad(s.RoomIDX)
                                                       Await currentRoom.LoadDevices()
                                                       Notify.Clear()
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
                                                Await selectedDevice.SwitchDevice()
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

    'Public ReadOnly Property GoToAboutCommand As RelayCommand
    '    Get
    '        Return New RelayCommand(Sub()
    '                                    TiczMenu.IsMenuOpen = Not TiczMenu.IsMenuOpen
    '                                    Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
    '                                    If Not rootFrame.Navigate(GetType(AboutPage)) Then
    '                                        Throw New Exception("Couldn't nagivate to settings page")
    '                                    End If
    '                                End Sub)
    '    End Get
    'End Property


    Public ReadOnly Property RefreshCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await Refresh(True)
                                    End Sub)
        End Get
    End Property

    Public Sub New()
        GraphList = New GraphListViewModel
        ShowDeviceDetails = False
        ShowDevicePassword = False
        currentRoom = New RoomViewModel With {.ItemHeight = 120}
        EnabledRooms = New ObservableCollection(Of TiczStorage.RoomConfiguration)
    End Sub

    Public Async Function LoadGraphData(ByVal d As DeviceViewModel) As Task
        GraphList = New GraphListViewModel
        ShowDeviceGraph = True
        Await Notify.Update(False, "Loading graphs, please wait...", 0)

        'GraphList.deviceIDX = d.idx
        GraphList.deviceName = d.Name
        'GraphList.deviceType = d.Type
        'GraphList.deviceSubType = d.SubType
        Dim GraphsToAdd As New List(Of Domoticz.DeviceGraphContainer)

        Select Case d.Type
            Case Constants.DEVICE.TYPE.WIND
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphWindDay"), DataTemplate), (New DomoApi).getGraph(d.idx, "day", "wind")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphWindMonth"), DataTemplate), (New DomoApi).getGraph(d.idx, "month", "wind")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphWindYear"), DataTemplate), (New DomoApi).getGraph(d.idx, "year", "wind")))
            Case Constants.DEVICE.TYPE.RAIN
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphRainDay"), DataTemplate), (New DomoApi).getGraph(d.idx, "day", "rain")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphRainWeek"), DataTemplate), (New DomoApi).getGraph(d.idx, "week", "rain")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphRainMonth"), DataTemplate), (New DomoApi).getGraph(d.idx, "month", "rain")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphRainYear"), DataTemplate), (New DomoApi).getGraph(d.idx, "year", "rain")))

            Case Constants.DEVICE.TYPE.THERMOSTAT
                Select Case d.SubType
                    Case Constants.DEVICE.SUBTYPE.SETPOINT
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureDay"), DataTemplate), (New DomoApi).getGraph(d.idx, "day", "temp")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureMonth"), DataTemplate), (New DomoApi).getGraph(d.idx, "month", "temp")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureYear"), DataTemplate), (New DomoApi).getGraph(d.idx, "year", "temp")))
                End Select
            Case Constants.DEVICE.TYPE.LIGHT_SWITCH
                Select Case d.SubType
                    Case Constants.DEVICE.SUBTYPE.SELECTOR_SWITCH
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "", TryCast(Xaml.Application.Current.Resources("FastGraph"), DataTemplate), (New DomoApi).getLightLog(d.idx)))
                End Select
            Case Constants.DEVICE.TYPE.LIGHTING_2
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "", TryCast(Xaml.Application.Current.Resources("FastGraph"), DataTemplate), (New DomoApi).getLightLog(d.idx)))
            Case Constants.DEVICE.TYPE.TEMP, Constants.DEVICE.TYPE.TEMP_HUMI, Constants.DEVICE.TYPE.TEMP_HUMI_BARO
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureDay"), DataTemplate), (New DomoApi).getGraph(d.idx, "day", "temp")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureMonth"), DataTemplate), (New DomoApi).getGraph(d.idx, "month", "temp")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureYear"), DataTemplate), (New DomoApi).getGraph(d.idx, "year", "temp")))
            Case Constants.DEVICE.TYPE.USAGE
                Select Case d.SubType
                    Case Constants.DEVICE.SUBTYPE.ELECTRIC
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphUsageElectricDay"), DataTemplate), (New DomoApi).getGraph(d.idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphUsageElectricMonth"), DataTemplate), (New DomoApi).getGraph(d.idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphUsageElectricYear"), DataTemplate), (New DomoApi).getGraph(d.idx, "year", "counter")))
                End Select
            Case Else
                Select Case d.SubType
                    Case Constants.DEVICE.SUBTYPE.PERCENTAGE
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphPercentageDay"), DataTemplate), (New DomoApi).getGraph(d.idx, "day", "Percentage")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphPercentageMonth"), DataTemplate), (New DomoApi).getGraph(d.idx, "month", "Percentage")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphPercentageYear"), DataTemplate), (New DomoApi).getGraph(d.idx, "year", "Percentage")))
                    Case Constants.DEVICE.SUBTYPE.P1_ELECTRIC
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyDay"), DataTemplate), (New DomoApi).getGraph(d.idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyWeek"), DataTemplate), (New DomoApi).getGraph(d.idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyMonth"), DataTemplate), (New DomoApi).getGraph(d.idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyYear"), DataTemplate), (New DomoApi).getGraph(d.idx, "year", "counter")))
                    Case Constants.DEVICE.SUBTYPE.P1_GAS
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphGasDay"), DataTemplate), (New DomoApi).getGraph(d.idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphGasWeek"), DataTemplate), (New DomoApi).getGraph(d.idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphGasMonth"), DataTemplate), (New DomoApi).getGraph(d.idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphGasYear"), DataTemplate), (New DomoApi).getGraph(d.idx, "year", "counter")))
                    Case Else
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(d.idx, d.Type, d.SubType, d.Name, "year", TryCast(Xaml.Application.Current.Resources("NoGraphAvailable"), DataTemplate), ""))
                End Select
        End Select


        For Each g In GraphsToAdd
            Await Task.Run(Function() g.Load(d))
            GraphList.graphDataList.Add(g)
        Next

        Notify.Clear()
    End Function



    Public Async Sub StartRefresh()
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

    Public Async Function PerformAutoRefresh(ct As CancellationToken) As Task
        Dim refreshperiod As Integer = TiczSettings.SecondsForRefresh
        Try
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
        Catch ex As Exception
            Notify.Update(True, "AutoRefresh crashed :(", 4)
        End Try

    End Function

    Public Async Function Refresh(Optional LoadAllUpdates As Boolean = False) As Task
        Await Notify.Update(False, "refreshing...", 0)
        Dim sWatch = Stopwatch.StartNew()

        'Refresh the Sunset/Rise values
        Await DomoSunRiseSet.Load()

        'Get all devices for this room that have been updated since the LastRefresh (Domoticz will tell you which ones)
        Dim dev_response As New HttpResponseMessage
        'Hack in case we're looking at the "All Devices" room, we need to download status for all devices
        If currentRoom.RoomIDX = 12321 Then
            dev_response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllDevices()))
        Else
            dev_response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllDevicesForRoom(currentRoom.RoomIDX, LoadAllUpdates)))
        End If

        If dev_response.IsSuccessStatusCode Then
            Dim refreshedDevices = JsonConvert.DeserializeObject(Of DevicesModel)(Await dev_response.Content.ReadAsStringAsync)
            If Not refreshedDevices Is Nothing AndAlso refreshedDevices.result.Count > 0 Then
                WriteToDebug("TiczViewModel.Refresh()", String.Format("Loaded {0} devices", refreshedDevices.result.Count))
                If currentRoom.RoomConfiguration.RoomView = Constants.ROOMVIEW.DASHVIEW Then
                    For Each d In refreshedDevices.result
                        Dim deviceToUpdate = (From devs In currentRoom.GetActiveDeviceList Where devs.idx = d.idx And devs.Name = d.Name Select devs).FirstOrDefault()
                        If Not deviceToUpdate Is Nothing Then
                            Await RunOnUIThread(Async Sub()
                                                    Await deviceToUpdate.Update(d)
                                                End Sub)
                        End If
                    Next
                Else
                    For Each d In refreshedDevices.result
                        Dim deviceToUpdate = currentRoom.GetActiveGroupedDeviceList.GetDevice(d.idx, d.Name)
                        If Not deviceToUpdate Is Nothing Then
                            Await RunOnUIThread(Async Sub()
                                                    Await deviceToUpdate.Update(d)
                                                End Sub)
                        End If
                    Next
                End If
                refreshedDevices = Nothing
            End If
        Else
            Await Notify.Update(True, "couldn't load device status", 2)
        End If

        'Get all scenes
        Dim grp_response As New HttpResponseMessage
        If currentRoom.RoomIDX = 12321 Then
            grp_response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllScenes()))
        Else
            grp_response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllScenesForRoom(currentRoom.RoomIDX)))
        End If
        If grp_response.IsSuccessStatusCode Then
            Dim refreshedScenes = JsonConvert.DeserializeObject(Of DevicesModel)(Await grp_response.Content.ReadAsStringAsync)
            If Not refreshedScenes Is Nothing Then
                If currentRoom.RoomConfiguration.RoomView = Constants.ROOMVIEW.DASHVIEW Then
                    For Each device In currentRoom.GetActiveDeviceList.Where(Function(x) x.Type = "Group" Or x.Type = "Scene").ToList()
                        Dim updatedDevice = (From d In refreshedScenes.result Where d.idx = device.idx And d.Name = device.Name Select d).FirstOrDefault()
                        If Not updatedDevice Is Nothing Then
                            Await RunOnUIThread(Async Sub()
                                                    Await device.Update(updatedDevice)
                                                End Sub)

                        End If
                    Next
                Else
                    For Each dg In currentRoom.GetActiveGroupedDeviceList
                        For Each device In dg.Where(Function(x) x.Type = "Group" Or x.Type = "Scene").ToList()
                            Dim updatedDevice = (From d In refreshedScenes.result Where d.idx = device.idx And d.Name = device.Name Select d).FirstOrDefault()
                            If Not updatedDevice Is Nothing Then
                                Await RunOnUIThread(Async Sub()
                                                        Await device.Update(updatedDevice)
                                                    End Sub)
                            End If
                        Next
                    Next
                End If
                refreshedScenes = Nothing
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
        dev_response = Nothing : grp_response = Nothing
        LastRefresh = Date.Now.ToUniversalTime
    End Function

    ''' <summary>
    ''' Performs initial loading of all Data for Ticz. Ensures all data is cleared before reloading
    ''' </summary>
    ''' <returns></returns>
    Public Async Function Load() As Task
        If Not TiczSettings.ContainsValidIPDetails Then
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
        Await DomoSecPanel.GetSecurityStatus()


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
        LastRefresh = Date.Now.ToUniversalTime
        StartRefresh()

        If DomoRooms.result.Any(Function(x) x.Name = "Ticz") Then
            Await Notify.Update(False, "You have a room in Domoticz called  'Ticz'. This is used for troubleshooting purposed, in case there are issues with the app in combination with certain controls. Due to this, no other rooms are loaded. Rename the 'Ticz' room to see other rooms.", 6)
        Else
            Notify.Clear()
        End If
    End Function
End Class

