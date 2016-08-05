Imports System.Xml.Serialization
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports Newtonsoft.Json
Imports Windows.Storage.Streams
Imports Windows.Web.Http


Public Class RoomsViewModel
    Inherits ViewModelBase

    Public Property RoomList As ObservableCollection(Of RoomViewModel)
    Public ReadOnly Property EnabledRooms As List(Of RoomViewModel)
        Get
            Return (From r In RoomList Where r.ShowRoom Select r).ToList
        End Get
    End Property

    Public Property ActiveRoom As RoomViewModel
        Get
            Return _ActiveRoom
        End Get
        Set(value As RoomViewModel)
            _ActiveRoom = value
            RaisePropertyChanged("ActiveRoom")
        End Set
    End Property
    Private Property _ActiveRoom As RoomViewModel

    Public Sub New()
        RoomList = New ObservableCollection(Of RoomViewModel)
    End Sub

    Public Async Function Load() As Task
        Me.RoomList.Clear()
        Dim domoPlans As New Domoticz.Plans
        Await domoPlans.Load()
        Dim ticzRoomConfigs As List(Of RoomConfigurationModel) = Await LoadRoomConfigurations()
        For Each plan In domoPlans.result
            Dim Room As New RoomViewModel(plan, (From c In ticzRoomConfigs Where c.RoomIDX = plan.idx Select c).FirstOrDefault)
            Me.RoomList.Add(Room)
        Next
        Await SaveRoomConfigurations()
        Await SetActiveRoom()
    End Function

    Public Async Function LoadRoomConfigurations() As Task(Of List(Of RoomConfigurationModel))
        Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Dim storageFile As Windows.Storage.StorageFile
        Dim fileExists As Boolean = True
        Dim stuffToLoad As New List(Of RoomConfigurationModel)
        Try
            storageFile = Await storageFolder.GetFileAsync("ticzconfig.xml")
        Catch ex As Exception
            fileExists = False
            ' app.myViewModel.Notify.Update(False, String.Format("No configuration file present. We will create a new one"), 0, False, 2)
        End Try
        If fileExists Then
            Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read)
            Dim sessionInputStream As IInputStream = stream.GetInputStreamAt(0)
            Dim serializer = New XmlSerializer((New List(Of RoomConfigurationModel)).GetType())
            Try
                stuffToLoad = serializer.Deserialize(sessionInputStream.AsStreamForRead())
            Catch ex As Exception
                'Casting the contents of the file to a RoomConfigurations object failed. Potentially the file is empty or malformed. Return a new object
                'app.myViewModel.Notify.Update(True, String.Format("Config file seems corrupt. We created a new one : {0}", ex.Message), 2, False, 2)
            End Try
            stream.Dispose()
        End If

        'If Not app.myViewModel.EnabledRooms Is Nothing Then
        '    app.myViewModel.EnabledRooms.Clear()
        'Else
        '    app.myViewModel.EnabledRooms = New ObservableCollection(Of RoomModel)
        'End If

        'For Each f In stuffToLoad
        '    Me.Add(f)
        'Next
        'Dim tst = stuffToLoad.Where(Function(x) x.RoomIDX = 12321).FirstOrDefault
        'If Not tst Is Nothing Then tst.ShowRoom = False

        'For Each r In DomoticzPlans.result.OrderBy(Function(x) x.Order)
        '    Dim retreivedRoomConfig = (From configs In stuffToLoad Where configs.RoomIDX = r.idx And configs.RoomName = r.Name Select configs).FirstOrDefault()
        '    If retreivedRoomConfig Is Nothing Then
        '        retreivedRoomConfig = New RoomModel With {.RoomIDX = r.idx, .RoomName = r.Name, .RoomView = Constants.ROOMVIEW.ICONVIEW, .ShowRoom = True}
        '    End If
        '    Me.Add(retreivedRoomConfig)
        '    If retreivedRoomConfig.ShowRoom Then app.myViewModel.EnabledRooms.Add(retreivedRoomConfig)
        'Next
        WriteToDebug("RoomsConfigurations.LoadRoomConfigurations()", "end")
        Return stuffToLoad
    End Function


    Public Async Function SaveRoomConfigurations() As Task
        Dim roomconfigs As New List(Of RoomConfigurationModel)
        For Each room In Me.RoomList
            roomconfigs.Add(room._RoomConfiguration)
        Next
        WriteToDebug("Rooms.SaveRoomConfigurations()", "start")

        If Me.RoomList.Any(Function(x) x.RoomName = "Ticz") Then
            'We are running in 'Debug mode', therefore we won't save the roomconfigurations
            Exit Function
        End If
        Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Dim storageFile As Windows.Storage.StorageFile = Await storageFolder.CreateFileAsync("ticzconfig.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)
        Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
        Dim sessionOutputStream As IOutputStream = stream.GetOutputStreamAt(0)

        Dim stuffToSave = roomconfigs
        Dim serializer As XmlSerializer = New XmlSerializer(stuffToSave.GetType())
        serializer.Serialize(sessionOutputStream.AsStreamForWrite(), stuffToSave)
        Await sessionOutputStream.FlushAsync()
        sessionOutputStream.Dispose()
        stream.Dispose()
        RaisePropertyChanged("EnabledRooms")
        WriteToDebug("RoomsConfigurations.SaveRoomConfigurations()", "end")
    End Function

    ''' <summary>
    ''' Sets the Current Room in Ticz, which will display the Devices within this Room. If the IDX parameter is ommited, Ticz will Load the Preferred Room based on the Room that is Selected
    ''' In the Settings Menu. If that isn't Set, it will load the first Room that it has
    ''' </summary>
    ''' <param name="idx"></param>
    ''' <returns></returns>
    Public Async Function SetActiveRoom(Optional idx As Integer = -1) As Task
        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
        ' Notify.Update(False, "Loading room...", 1, False, 0)
        If idx = -1 Then
            ' Check for the existence of a Ticz Room. If it exists, load the contents of that room
            Dim TiczRoom As RoomViewModel = (From r In Me.RoomList Where r.RoomName = "Ticz" Select r).FirstOrDefault()
            If Not TiczRoom Is Nothing Then
                ActiveRoom = TiczRoom
            Else
                Dim PreferredRoom As RoomViewModel = (From r In Me.RoomList Where r.RoomIDX = vm.TiczSettings.PreferredRoomIDX Select r).FirstOrDefault()
                If Not PreferredRoom Is Nothing Then
                    ActiveRoom = PreferredRoom
                Else
                    'TODO : CHECK IF THERE ACTUALLY ARE ROOMS DEFINED
                    If Not Me.RoomList.Count = 0 Then
                        ActiveRoom = Me.RoomList(0)
                    End If
                End If
            End If
        Else
            ActiveRoom = (From r In Me.RoomList Where r.RoomIDX = idx Select r).FirstOrDefault()
        End If

        If Not ActiveRoom Is Nothing Then
            Await ActiveRoom.GetDevicesForRoom(ActiveRoom.RoomView)
            ActiveRoom.SetItemWidthHeight()
        End If
    End Function

End Class


Public Class RoomViewModel
    Inherits ViewModelBase
    Public Property _RoomConfiguration As RoomConfigurationModel
    Private Property _RoomModel As Domoticz.Plan
    Private Property _RoomDevices As DevicesViewModel

    Private app As Application = CType(Application.Current, Application)

    Public ReadOnly Property RoomContentTemplate As DataTemplate
        Get
            Select Case _RoomConfiguration.RoomView
                Case Constants.ROOMVIEW.ICONVIEW : Return CType(CType(Application.Current, Application).Resources("GridViewDataTemplate"), DataTemplate)
                Case Constants.ROOMVIEW.GRIDVIEW : Return CType(CType(Application.Current, Application).Resources("GridViewDataTemplate"), DataTemplate)
                Case Constants.ROOMVIEW.LISTVIEW : Return CType(CType(Application.Current, Application).Resources("ListViewDataTemplate"), DataTemplate)
                Case Constants.ROOMVIEW.RESIZEVIEW : Return CType(CType(Application.Current, Application).Resources("ResizeViewDataTemplate"), DataTemplate)
                Case Constants.ROOMVIEW.DASHVIEW : Return CType(CType(Application.Current, Application).Resources("DashboardViewDataTemplate"), DataTemplate)
            End Select
        End Get

    End Property

    Public Property RoomView As String
        Get
            Return _RoomConfiguration.RoomView
        End Get
        Set(value As String)
            _RoomConfiguration.RoomView = value
        End Set
    End Property

    Public ReadOnly Property RoomViewChoices As List(Of String)
        Get
            Return _RoomConfiguration.RoomViewChoices
        End Get
    End Property

    Public Property ShowRoom As Boolean
        Get
            Return _RoomConfiguration.ShowRoom
        End Get
        Set(value As Boolean)
            _RoomConfiguration.ShowRoom = value
        End Set
    End Property




    Public ReadOnly Property ResizeContextMenuVisibility As String
        Get
            Select Case _RoomConfiguration.RoomView
                Case Constants.ROOMVIEW.DASHVIEW, Constants.ROOMVIEW.RESIZEVIEW : Return Constants.VISIBLE
                Case Else : Return Constants.COLLAPSED
            End Select
        End Get
    End Property

    Public ReadOnly Property MoveUpDashboardVisibility As String
        Get
            Select Case _RoomConfiguration.RoomView
                Case Constants.ROOMVIEW.DASHVIEW : Return Constants.VISIBLE
                Case Else : Return Constants.COLLAPSED
            End Select
        End Get
    End Property
    Public ReadOnly Property MoveDownDashboardVisibility As String
        Get
            Select Case _RoomConfiguration.RoomView
                Case Constants.ROOMVIEW.DASHVIEW : Return Constants.VISIBLE
                Case Else : Return Constants.COLLAPSED
            End Select
        End Get
    End Property

    Public ReadOnly Property RoomName As String
        Get
            Return _RoomModel.Name
        End Get
    End Property
    Public ReadOnly Property RoomIDX As String
        Get
            Return _RoomModel.idx
        End Get
    End Property


    Public ReadOnly Property GroupedDevices As DeviceGroup(Of DevicesViewModel)
        Get
            If Not Devices Is Nothing Then
                Return CreateGroupedDevices()
            Else Return Nothing
            End If

        End Get
    End Property

    Public Property Devices As DevicesViewModel
        Get
            Return _Devices
        End Get
        Set(value As DevicesViewModel)
            _Devices = value
            RaisePropertyChanged("Devices")
            RaisePropertyChanged("GroupedDevices")
        End Set
    End Property
    Private Property _Devices As DevicesViewModel

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
                                                   'WriteToDebug("GridViewSizeChanged", "Executed")
                                                   'SetItemWidthHeight()
                                               End Sub)
        End Get
    End Property

    'Public Sub New()
    '    RoomConfiguration = New TiczStorage.RoomConfiguration
    '    ItemHeight = 120
    'End Sub

    Public Sub New(roomplan As Domoticz.Plan, Optional RoomConfig As RoomConfigurationModel = Nothing)
        _RoomModel = roomplan
        _RoomConfiguration = If(Not RoomConfig Is Nothing, RoomConfig, New RoomConfigurationModel With {.RoomView = Constants.ROOMVIEW.ICONVIEW, .RoomIDX = roomplan.idx, .ShowRoom = True})
        'ItemHeight = 112
        SetItemWidthHeight()

    End Sub

    Public Sub SetItemWidthHeight()
        Const DefaultItemWidth = 120 'The Default Width for an Device in Icon View
        Const DefaultItemHeight = 120 'The Default Height for an Device in Icon View
        Dim iWidth As Integer  'Minimum Item Width
        Dim iMargin As Integer 'Any additional Margin that the item carries
        Select Case _RoomConfiguration.RoomView
            Case Constants.ROOMVIEW.DASHVIEW : iWidth = DefaultItemWidth : iMargin = 0
            Case Constants.ROOMVIEW.RESIZEVIEW : iWidth = DefaultItemWidth : iMargin = 0
            Case Constants.ROOMVIEW.GRIDVIEW : iWidth = 200 : iMargin = 0
            Case Constants.ROOMVIEW.ICONVIEW : iWidth = DefaultItemWidth : iMargin = 0
            Case Constants.ROOMVIEW.LISTVIEW : iWidth = DefaultItemWidth : iMargin = 0
            Case Else
                Throw New Exception("RoomView unkown, cant calculate itemwidth")
        End Select
        If Not iWidth = 0 Then
            iWidth = iWidth * app.myViewModel.TiczSettings.ZoomFactor
            ItemHeight = DefaultItemHeight * app.myViewModel.TiczSettings.ZoomFactor
            Dim completeItems = Math.Floor(ApplicationView.GetForCurrentView.VisibleBounds.Width / iWidth)
            If completeItems < app.myViewModel.TiczSettings.MinimumNumberOfColumns Then completeItems = app.myViewModel.TiczSettings.MinimumNumberOfColumns
            Dim remainder = ApplicationView.GetForCurrentView.VisibleBounds.Width - (completeItems * iWidth) - (completeItems * iMargin)
            ItemWidth = (iWidth + Math.Floor(remainder / completeItems))
            WriteToDebug("RoomViewModel.SetItemWidth()", String.Format("Visible Bounds:{0} / Complete Items:{1} / Remainder:{2} ItemWidth:{3}", ApplicationView.GetForCurrentView.VisibleBounds.Width, completeItems, remainder, ItemWidth))
        Else
            app.myViewModel.Notify.Update(True, "Room Layout unknown - Item Width can't be calculated", 2, False, 0)
        End If

    End Sub


    Public Async Function GetDevicesForRoom(RoomView As String) As Task
        'TODO : Remove DeviceModel DeviceViewModel tests
        Dim ret As New DevicesViewModel
        Dim url As String = (New DomoApi).getAllDevicesForRoom(RoomIDX, True)
        'Hack to change the URL used when the Room is a "All Devices" room, with a static IDX of 12321
        If Me.RoomIDX = 12321 Then url = (New DomoApi).getAllDevices()
        'Hack to change the URL used when the Room is the "Favourites" room, which has a room IDX of 0
        If Me.RoomIDX = 0 Then url = (New DomoApi).getFavouriteDevices()
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
        Dim devicelist As New List(Of DeviceModel)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim deserialized = JsonConvert.DeserializeObject(Of DevicesModel)(body)
            devicelist = deserialized.result.ToList()
            For Each d In devicelist
                Dim DevToAdd
                'Retreive the Devices' Configuration From the RoomConfiguration
                Dim devConfig As TiczStorage.DeviceConfiguration = (From dev In _RoomConfiguration.DeviceConfigurations
                                                                    Where dev.DeviceIDX = d.idx And dev.DeviceName = d.Name Select dev).FirstOrDefault
                'If the device configuration doesn't exist (new device), create a default deviceconfig
                If devConfig Is Nothing Then
                    devConfig = (New TiczStorage.DeviceConfiguration With {.DeviceIDX = d.idx, .DeviceName = d.Name,
                                                                           .DeviceRepresentation = Constants.DEVICEVIEWS.ICON, .DeviceOrder = 9999})
                    _RoomConfiguration.DeviceConfigurations.Add(devConfig)
                End If
                'Add devices with a specific ViewModel depending on their type
                If d.HardwareType = Constants.DEVICE.HARDWARETYPE.LOGITECHMEDIASERVER Then
                    DevToAdd = New LogitechMediaServerDeviceViewModel(d, RoomView, devConfig)
                ElseIf d.HardwareType = Constants.DEVICE.HARDWARETYPE.KODIMEDIASERVER Then
                    DevToAdd = New KODIDeviceViewModel(d, RoomView, devConfig)
                Else
                    DevToAdd = New DeviceViewModel(d, RoomView, devConfig)
                End If

                'If we only want to show favourites, filter the ones out that aren't
                If app.myViewModel.TiczSettings.OnlyShowFavourites Then
                    If d.Favorite = 1 Then ret.Add(DevToAdd)
                Else
                    ret.Add(DevToAdd)
                End If
            Next
            deserialized = Nothing
        Else
            Await app.myViewModel.Notify.Update(True, String.Format("Connection error {0}", response.ReasonPhrase), 2, False, 4)
        End If
        WriteToDebug("RoomViewModel.GetDevicesForRoom()", String.Format("Retrieved {0} devices", ret.Count))
        Devices = New DevicesViewModel(Me.RoomName, ret.OrderBy(Function(x) x.DeviceOrder))
    End Function

    ''' <summary>
    ''' 'Loads the devices for this room into RoomDevices, used for Dashboard View only (not grouped)
    ''' </summary>
    ''' <returns></returns>
    Public Async Function LoadDevicesForRoom() As Task
        Await GetDevicesForRoom(Me._RoomConfiguration.RoomView)
        'Me.Devices = New DevicesViewModel(RoomName, devicesToAdd)
        Me._RoomConfiguration.DeviceConfigurations.SortRoomDevices()
    End Function

    ''' <summary>
    ''' Triggers raisepropertychanged on Grouped Devices
    ''' </summary>
    Public Sub Refresh()
        RaisePropertyChanged("GroupedDevices")
    End Sub

    Public Function CreateGroupedDevices() As DeviceGroup(Of DevicesViewModel)
        Dim NewDevices As New DeviceGroup(Of DevicesViewModel)
        'Create groups for the Room. Empty groups will be filtered out by the GroupStyle in XAML
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_GROUPS_SCENES))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_LIGHTS_SWITCHES))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_WEATHER))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_TEMPERATURE))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_UTILITY))
        NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_OTHER))

        For Each d In Devices
            Select Case d.Type
                Case Constants.DEVICE.TYPE.SCENE, Constants.DEVICE.TYPE.GROUP
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_GROUPS_SCENES).FirstOrDefault().Add(d)
                Case Constants.DEVICE.TYPE.LIGHTING_LIMITLESS, Constants.DEVICE.TYPE.LIGHT_SWITCH, Constants.DEVICE.TYPE.LIGHTING_1,
                     Constants.DEVICE.TYPE.LIGHTING_2, Constants.DEVICE.TYPE.SECURITY
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_LIGHTS_SWITCHES).FirstOrDefault().Add(d)
                Case Constants.DEVICE.TYPE.TEMP_HUMI_BARO, Constants.DEVICE.TYPE.WIND, Constants.DEVICE.TYPE.UV, Constants.DEVICE.TYPE.RAIN
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_WEATHER).FirstOrDefault().Add(d)
                Case Constants.DEVICE.TYPE.TEMP, Constants.DEVICE.TYPE.THERMOSTAT
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_TEMPERATURE).FirstOrDefault().Add(d)
                Case Constants.DEVICE.TYPE.GENERAL, Constants.DEVICE.TYPE.USAGE, Constants.DEVICE.TYPE.P1_SMART_METER,
                     Constants.DEVICE.TYPE.LUX, Constants.DEVICE.TYPE.AIR_QUALITY, Constants.DEVICE.TYPE.RFXMETER,
                     Constants.DEVICE.TYPE.HUMIDITY, Constants.DEVICE.TYPE.CURRENT, Constants.DEVICE.TYPE.WEIGHT
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_UTILITY).FirstOrDefault().Add(d)
                Case Else
                    NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_OTHER).FirstOrDefault().Add(d)
                    WriteToDebug("RoomViewModel.LoadGroupedDevicesForRoom()", String.Format("{0} : {1}", d.Name, d.Type))
            End Select
        Next
        Return NewDevices
    End Function
End Class