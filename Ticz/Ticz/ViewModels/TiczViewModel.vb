Imports System.Threading
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports Newtonsoft.Json
Imports Windows.Web.Http

Public Class TiczViewModel
    Inherits ViewModelBase

    Public Property CurrentContentDialog As ContentDialog
    Public Property Cameras As New CameraListViewModel
    Public Property DomoConfig As New Domoticz.Config
    Public Property DomoSunRiseSet As New Domoticz.SunRiseSet
    Public Property DomoVersion As New Domoticz.Version
    Public Property DomoRooms As New Domoticz.Plans
    Public Property DomoSettings As New Domoticz.Settings
    Public Property DomoSecPanel As New SecurityPanelViewModel
    Public Property Variables As New VariableListViewModel
    Public Property EnabledRooms As ObservableCollection(Of TiczStorage.RoomConfiguration)
        Get
            Return _EnabledRooms
        End Get
        Set(value As ObservableCollection(Of TiczStorage.RoomConfiguration))
            _EnabledRooms = value
            RaisePropertyChanged("EnabledRooms")
        End Set
    End Property
    Private Property _EnabledRooms As ObservableCollection(Of TiczStorage.RoomConfiguration)
    Public Property TiczRoomConfigs As New TiczStorage.RoomConfigurations
    Public Property TiczSettings As New TiczSettings
    Public Property TiczMenu As New TiczMenuSettings
    Public Property Notify As New ToastMessageViewModel
    Public Property IsRefreshing As Boolean
        Get
            Return _IsRefreshing
        End Get
        Set(value As Boolean)
            _IsRefreshing = value
            RaisePropertyChanged("IsRefreshing")
        End Set
    End Property
    Private Property _IsRefreshing As Boolean
    Public Property IsLoading As Boolean
    Public Property currentRoom As RoomViewModel
        Get
            Return _currentRoom
        End Get
        Set(value As RoomViewModel)
            _currentRoom = value
            RaisePropertyChanged("RoomContentTemplate")
            RaisePropertyChanged("currentRoom")
        End Set
    End Property
    Private Property _currentRoom As RoomViewModel

    Public ReadOnly Property RoomContentTemplate As DataTemplate
        Get
            If Not currentRoom Is Nothing Then
                Select Case currentRoom.RoomConfiguration.RoomView
                    Case Constants.ROOMVIEW.ICONVIEW : Return CType(CType(Application.Current, Application).Resources("IconViewDataTemplate"), DataTemplate)
                    Case Constants.ROOMVIEW.GRIDVIEW : Return CType(CType(Application.Current, Application).Resources("GridViewDataTemplate"), DataTemplate)
                    Case Constants.ROOMVIEW.LISTVIEW : Return CType(CType(Application.Current, Application).Resources("ListViewDataTemplate"), DataTemplate)
                    Case Constants.ROOMVIEW.RESIZEVIEW : Return CType(CType(Application.Current, Application).Resources("ResizeViewDataTemplate"), DataTemplate)
                    Case Constants.ROOMVIEW.DASHVIEW : Return CType(CType(Application.Current, Application).Resources("DashboardViewDataTemplate"), DataTemplate)
                End Select
            End If
        End Get

    End Property

    Public Property LastRefresh As DateTime

    'Properties used for the background refresher
    Public Property TiczRefresher As Task
    Public ct As CancellationToken
    Public tokenSource As New CancellationTokenSource()


    Public ReadOnly Property ViewModelLoadedCommand As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)
                                                   WriteToDebug("TiczViewModel.ViewModelLoadedCommand()", "executed")
                                                   Await Load()
                                               End Sub)
        End Get
    End Property

    Public Sub New()
    End Sub

    Public Async Sub RoomSelected(sender As Object, e As SelectionChangedEventArgs)
        Dim selectedRoom As TiczStorage.RoomConfiguration = TryCast(sender, ListView).SelectedItem
        If Not selectedRoom Is Nothing Then
            Await Notify.Update(False, "Loading room...", 1, False, 0)
            If TiczMenu.IsMenuOpen Then TiczMenu.IsMenuOpen = False
            Await LoadRoom(selectedRoom.RoomIDX)
            Notify.Clear()
        End If
    End Sub

    Public Async Sub ShowSecurityPanel()
        WriteToDebug("TiczMenuSettings.ShowSecurityPanel()", "executed")
        Me.TiczMenu.IsMenuOpen = False
        CurrentContentDialog = New ContentDialog
        'Because we use a customized ContentDialog Style, the ESC key handler didn't work anymore. Therefore we add our own. 
        Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
                                                       If e.Key = Windows.System.VirtualKey.Escape Then
                                                           CurrentContentDialog.Hide()
                                                       End If
                                                   End Sub)
        CurrentContentDialog.AddHandler(UIElement.KeyDownEvent, escapekeyhandler, True)
        CurrentContentDialog.Title = "Security Panel"
        CurrentContentDialog.Style = CType(Application.Current.Resources("FullScreenContentDialog"), Style)
        CurrentContentDialog.HorizontalAlignment = HorizontalAlignment.Stretch
        CurrentContentDialog.VerticalAlignment = VerticalAlignment.Stretch
        CurrentContentDialog.HorizontalContentAlignment = HorizontalAlignment.Stretch
        CurrentContentDialog.VerticalContentAlignment = VerticalAlignment.Stretch
        Dim details As New ucSecurityPanel()
        CurrentContentDialog.Content = details
        Await CurrentContentDialog.ShowAsync()
    End Sub


    Public Async Sub ShowVariables()
        WriteToDebug("TiczMenuSettings.ShowCameras()", "executed")
        Me.TiczMenu.IsMenuOpen = False
        Await Notify.Update(False, "Loading Domoticz variables...", 0, False, 0)
        If Not (Await Variables.Load()).issuccess Then
            Await Notify.Update(True, "Error loading Domoticz variables...", 1, False, 0)
        Else
            CurrentContentDialog = New ContentDialog
            'Because we use a customized ContentDialog Style, the ESC key handler didn't work anymore. Therefore we add our own. 
            Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
                                                           If e.Key = Windows.System.VirtualKey.Escape Then
                                                               CurrentContentDialog.Hide()
                                                           End If
                                                       End Sub)
            CurrentContentDialog.AddHandler(UIElement.KeyDownEvent, escapekeyhandler, True)
            CurrentContentDialog.Title = "Domoticz Variables"
            CurrentContentDialog.Style = CType(Application.Current.Resources("HalfScreenContentDialog"), Style)
            CurrentContentDialog.MaxHeight = Window.Current.Bounds.Height
            CurrentContentDialog.VerticalAlignment = VerticalAlignment.Stretch
            CurrentContentDialog.VerticalContentAlignment = VerticalAlignment.Stretch
            Dim vlist As VariableListViewModel = CType(Application.Current, Application).myViewModel.Variables
            Dim uclist As New ucVariableList
            uclist.DataContext = vlist
            CurrentContentDialog.Content = uclist
            Notify.Clear()
            Await CurrentContentDialog.ShowAsync()
            CurrentContentDialog = Nothing
        End If

    End Sub

    Public Async Sub ShowCameras()
        WriteToDebug("TiczMenuSettings.ShowCameras()", "executed")
        Me.TiczMenu.IsMenuOpen = False
        CurrentContentDialog = New ContentDialog
        'Because we use a customized ContentDialog Style, the ESC key handler didn't work anymore. Therefore we add our own. 
        Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
                                                       If e.Key = Windows.System.VirtualKey.Escape Then
                                                           CurrentContentDialog.Hide()
                                                       End If
                                                   End Sub)
        CurrentContentDialog.AddHandler(UIElement.KeyDownEvent, escapekeyhandler, True)
        CurrentContentDialog.Title = "Cameras"
        CurrentContentDialog.Style = CType(Application.Current.Resources("FullScreenContentDialog"), Style)
        CurrentContentDialog.HorizontalAlignment = HorizontalAlignment.Stretch
        CurrentContentDialog.VerticalAlignment = VerticalAlignment.Stretch
        CurrentContentDialog.HorizontalContentAlignment = HorizontalAlignment.Stretch
        CurrentContentDialog.VerticalContentAlignment = VerticalAlignment.Stretch
        Dim clist As New ucCameraList()
        'Before Showing the cams, try to capture the latest frame for each
        For Each c In Cameras
            Await c.GetFrameFromJPG()
        Next
        clist.DataContext = Cameras
        CurrentContentDialog.Content = clist
        Await CurrentContentDialog.ShowAsync()
        'Stop refreshing any camera that exists
        For Each c In Cameras
            c.StopRefresh()
        Next
    End Sub


    Public Async Sub ShowAbout()
        WriteToDebug("TiczMenuSettings.ShowAbout()", "executed")
        Me.TiczMenu.IsMenuOpen = False
        CurrentContentDialog = New ContentDialog
        'Because we use a customized ContentDialog Style, the ESC key handler didn't work anymore. Therefore we add our own. 
        Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
                                                       If e.Key = Windows.System.VirtualKey.Escape Then
                                                           CurrentContentDialog.Hide()
                                                       End If
                                                   End Sub)
        CurrentContentDialog.AddHandler(UIElement.KeyDownEvent, escapekeyhandler, True)
        CurrentContentDialog.Title = "About Ticz..."
        CurrentContentDialog.Style = CType(Application.Current.Resources("FullScreenContentDialog"), Style)
        CurrentContentDialog.HorizontalAlignment = HorizontalAlignment.Stretch
        CurrentContentDialog.VerticalAlignment = VerticalAlignment.Stretch
        CurrentContentDialog.HorizontalContentAlignment = HorizontalAlignment.Stretch
        CurrentContentDialog.VerticalContentAlignment = VerticalAlignment.Stretch
        Dim about As New ucAbout()
        CurrentContentDialog.Content = about
        Await CurrentContentDialog.ShowAsync()
    End Sub

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
        Try
            While Not ct.IsCancellationRequested
                'WriteToDebug("TiczViewModel.PerformAutoRefresh", "executed")
                Dim i As Integer = 0
                'WriteToDebug("TiczViewModel.PerformAutoRefresh", "sleeping")
                If TiczSettings.SecondsForRefresh = 0 Then
                    While i < 5 * 1000
                        Await Task.Delay(100)
                        i += 100
                        If ct.IsCancellationRequested Then WriteToDebug("TiczViewModel.PerformAutoRefresh", "cancelling") : Exit While
                    End While
                Else
                    While i < TiczSettings.SecondsForRefresh * 1000
                        Await Task.Delay(100)
                        i += 100
                        If ct.IsCancellationRequested Then WriteToDebug("TiczViewModel.PerformAutoRefresh", "cancelling") : Exit While
                    End While
                End If
                If ct.IsCancellationRequested Then Exit While
                'WriteToDebug("TiczViewModel.PerformAutoRefresh", "refreshing")
                If TiczSettings.SecondsForRefresh > 0 Then Await Refresh(False)
            End While
        Catch ex As Exception
            Notify.Update(True, "AutoRefresh task crashed :(", 2, False, 4)
        End Try

    End Function

    ''' <summary>
    ''' Triggers a full manual refresh of the current Room's devices
    ''' </summary>
    ''' <returns></returns>
    Public Async Function ManualRefresh() As Task
        Await Refresh(True)
    End Function

    Public Async Function Reload() As Task
        WriteToDebug("TiczViewModel.Reload()", "executed")
        TiczMenu.IsMenuOpen = False
        Await Load()
    End Function


    Public Async Function Refresh(Optional LoadAllUpdates As Boolean = False) As Task
        If Not IsLoading Then
            Await RunOnUIThread(Sub()
                                    IsRefreshing = True
                                End Sub)


            'Await Notify.Update(False, "Refreshing...", 0, False, 0)
            Dim sWatch = Stopwatch.StartNew()
            'Refresh the Sunset/Rise values, Exit refresh if getting this fails
            If Not (Await DomoSunRiseSet.Load()).issuccess Then Exit Function
            'Refresh the Security Panel Status, exit refresh if this fails
            If Not (Await DomoSecPanel.GetSecurityStatus).issuccess Then Exit Function

            'Get all devices for this room that have been updated since the LastRefresh (Domoticz will tell you which ones)
            Dim dev_response, grp_response As New HttpResponseMessage
            'Hack in case we're looking at the "All Devices" room, we need to download status for all devices regardless of the room
            If currentRoom.RoomIDX = 12321 Then
                dev_response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllDevices()))
                grp_response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllScenes()))
            Else
                dev_response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllDevicesForRoom(currentRoom.RoomIDX, LoadAllUpdates)))
                grp_response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllScenesForRoom(currentRoom.RoomIDX)))
            End If


            'Collect all updated groups/scenes and devices into a single list
            Dim devicesToRefresh As New List(Of DeviceModel)
            If dev_response.IsSuccessStatusCode Then
                devicesToRefresh.AddRange((JsonConvert.DeserializeObject(Of DevicesModel)(Await dev_response.Content.ReadAsStringAsync)).result)
            End If
            If grp_response.IsSuccessStatusCode Then
                devicesToRefresh.AddRange((JsonConvert.DeserializeObject(Of DevicesModel)(Await grp_response.Content.ReadAsStringAsync)).result)
            End If

            'Iterate through the list of updated devices, find the matching device in the room and update it
            If devicesToRefresh.Count > 0 Then
                'WriteToDebug("TiczViewModel.Refresh()", String.Format("Loaded {0} devices", devicesToRefresh.Count))
                For Each d In devicesToRefresh
                    Dim deviceToUpdate As DeviceViewModel
                    'If currentRoom.RoomConfiguration.RoomView = Constants.ROOMVIEW.DASHVIEW Then
                    deviceToUpdate = (From devs In currentRoom.Devices Where devs.idx = d.idx And devs.Name = d.Name Select devs).FirstOrDefault()
                    'Else
                    '    deviceToUpdate = currentRoom.GetActiveGroupedDeviceList.GetDevice(d.idx, d.Name)
                    'End If
                    If Not deviceToUpdate Is Nothing Then
                        Await RunOnUIThread(Async Sub()
                                                Await deviceToUpdate.Update(d)
                                            End Sub)
                    End If
                Next
            Else
                If Not grp_response.IsSuccessStatusCode Or Not dev_response.IsSuccessStatusCode Then
                    Await Notify.Update(True, "Error loading refreshed devices/groups...", 2, False, 2)
                End If
            End If

            'Clear the Notification
            sWatch.Stop()
            If dev_response.IsSuccessStatusCode AndAlso grp_response.IsSuccessStatusCode Then
                'But only if the amount of time passed for the Refresh Is around 500ms (approx. time for the animation showing "Refreshing" to be on the screen
                WriteToDebug("TiczViewModel.Refresh()", String.Format("Refresh took {0} ms", sWatch.ElapsedMilliseconds))
                If sWatch.ElapsedMilliseconds < 1000 Then
                    Await Task.Delay(1000 - sWatch.ElapsedMilliseconds)
                End If
                Notify.Clear()
            End If
            dev_response = Nothing : grp_response = Nothing
            LastRefresh = Date.Now.ToUniversalTime
            Await RunOnUIThread(Sub()
                                    IsRefreshing = False
                                End Sub)
        End If
    End Function



    Public Async Function LoadRoom(Optional idx As Integer = 0) As Task
        ' Notify.Update(False, "Loading room...", 1, False, 0)
        Dim RoomToLoad As Domoticz.Plan
        If idx = 0 Then
            ' Check for the existence of a Ticz Room. If it exists, load the contents of that room
            Dim TiczRoom As Domoticz.Plan = (From r In DomoRooms.result Where r.Name = "Ticz" Select r).FirstOrDefault()
            If Not TiczRoom Is Nothing Then
                RoomToLoad = TiczRoom
            Else
                Dim PreferredRoom As Domoticz.Plan = (From r In DomoRooms.result Where r.idx = TiczSettings.PreferredRoomIDX Select r).FirstOrDefault()
                If Not PreferredRoom Is Nothing Then
                    RoomToLoad = PreferredRoom
                    TiczSettings.PreferredRoom = TiczRoomConfigs.GetRoomConfig(RoomToLoad.idx, RoomToLoad.Name)
                Else
                    'TODO : CHECK IF THERE ACTUALLY ARE ROOMS DEFINED
                    If Not DomoRooms.result.Count = 0 Then
                        RoomToLoad = DomoRooms.result(0)
                        TiczSettings.PreferredRoom = TiczRoomConfigs.GetRoomConfig(DomoRooms.result(0).idx, DomoRooms.result(0).Name)
                    End If
                End If
            End If
        Else
            RoomToLoad = (From r In DomoRooms.result Where r.idx = idx Select r).FirstOrDefault()
        End If

        If Not RoomToLoad Is Nothing Then
            Dim Room As New RoomViewModel(RoomToLoad, TiczRoomConfigs.GetRoomConfig(RoomToLoad.idx, RoomToLoad.Name))
            Await Room.GetDevicesForRoom(Room.RoomConfiguration.RoomView)
            currentRoom = Room
        End If
    End Function

    ''' <summary>
    ''' Performs initial loading of all Data for Ticz. Ensures all data is cleared before reloading
    ''' </summary>
    ''' <returns></returns>
    Public Async Function Load() As Task
        If Not TiczSettings.ContainsValidIPDetails Then
            Await Notify.Update(True, "IP/Port settings not valid", 2, False, 0)
            TiczMenu.ActiveMenuContents = "Server settings"
            Await Task.Delay(500)
            TiczMenu.IsMenuOpen = True
            Exit Function
        End If
        Await Notify.Update(False, "Loading...", 0, True, 0)
        IsLoading = True

        'Load Domoticz General Config from Domoticz
        Await Notify.Update(False, "Loading Domoticz configuration...", 0, False, 0)
        If Not (Await DomoConfig.Load()).issuccess Then Exit Function

        Await Notify.Update(False, "Loading Domoticz settings...", 0, False, 0)
        If Not (Await DomoSettings.Load()).issuccess Then Exit Function

        'Load Domoticz Sunrise/set Info from Domoticz
        Await Notify.Update(False, "Loading Domoticz Sunrise/Set...", 0, False, 0)
        If Not (Await DomoSunRiseSet.Load()).issuccess Then Exit Function

        'Load Version Information from Domoticz
        Await Notify.Update(False, "Loading Domoticz version info...", 0, False, 0)
        If Not (Await DomoVersion.Load()).issuccess Then Exit Function

        'Load Cameras from Domoticz
        Await Notify.Update(False, "Loading cameras...", 0, False, 0)
        If Not (Await Cameras.Load()).issuccess Then
            Await Notify.Update(True, "Error loading cameras...", 1, False, 0)
            Await Task.Delay(1000)
        End If

        'Load the Room/Floorplans from the Domoticz Server
        Await Notify.Update(False, "Loading Domoticz rooms...", 0)
        If Not (Await DomoRooms.Load()).issuccess Then
            Await Notify.Update(True, "Error loading Domoticz rooms...", 1, False, 0)
        End If

        If DomoRooms.result.Count = 0 Then
            Await Notify.Update(True, "No roomplans are configured on the Domoticz Server. Create one or more roomplans in Domoticz in order to see something here :)", 2, False, 0)
            Exit Function
        End If

        'TODO : MOVE SECPANEL STUFF TO RIGHT PLACE
        Await Notify.Update(False, "Loading Domoticz Security Panel Status...", 0, False, 0)
        Await DomoSecPanel.GetSecurityStatus()

        'Load the Room Configurations from Storage
        Await Notify.Update(False, "Loading Ticz Room configuration...", 0, False, 0)
        If Not Await TiczRoomConfigs.LoadRoomConfigurations() Then
            Await Task.Delay(2000)
        End If

        Await LoadRoom()

        'Save the (potentially refreshhed) roomconfigurations again
        Await Notify.Update(False, "Saving Ticz Room configuration...", 0, False, 0)
        Await TiczRoomConfigs.SaveRoomConfigurations()
        LastRefresh = Date.Now.ToUniversalTime
        StartRefresh()

        If DomoRooms.result.Any(Function(x) x.Name = "Ticz") Then
            Await Notify.Update(False, "You have a room in Domoticz called  'Ticz'. This is used for troubleshooting purposes, in case there are issues with the app in combination with certain controls. Due to this, no other rooms are loaded. Rename the 'Ticz' room to see other rooms.", 1, False, 10)
        Else
            Notify.Clear()
        End If
        IsLoading = False
    End Function
End Class