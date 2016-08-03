Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports Newtonsoft.Json
Imports Windows.Web.Http

Public Class RoomViewModel
    Inherits ViewModelBase

    Private app As Application = CType(Application.Current, Application)

    'Public Const constAllDevices As String = "All Devices"
    'Public Const constDashboard As String = "Dashboard"
    'Public Const constFavourites As String = "Favourites"


    Public ReadOnly Property RoomContentTemplate As DataTemplate
        Get
            Select Case RoomConfiguration.RoomView
                Case Constants.ROOMVIEW.ICONVIEW : Return CType(CType(Application.Current, Application).Resources("IconViewDataTemplate"), DataTemplate)
                Case Constants.ROOMVIEW.GRIDVIEW : Return CType(CType(Application.Current, Application).Resources("GridViewDataTemplate"), DataTemplate)
                Case Constants.ROOMVIEW.LISTVIEW : Return CType(CType(Application.Current, Application).Resources("ListViewDataTemplate"), DataTemplate)
                Case Constants.ROOMVIEW.RESIZEVIEW : Return CType(CType(Application.Current, Application).Resources("ResizeViewDataTemplate"), DataTemplate)
                Case Constants.ROOMVIEW.DASHVIEW : Return CType(CType(Application.Current, Application).Resources("DashboardViewDataTemplate"), DataTemplate)
            End Select
        End Get

    End Property

    Public Property RoomConfiguration As TiczStorage.RoomConfiguration

    Public ReadOnly Property ResizeContextMenuVisibility As String
        Get
            Select Case RoomConfiguration.RoomView
                Case Constants.ROOMVIEW.DASHVIEW, Constants.ROOMVIEW.RESIZEVIEW : Return Constants.VISIBLE
                Case Else : Return Constants.COLLAPSED
            End Select
        End Get
    End Property

    Public ReadOnly Property MoveUpDashboardVisibility As String
        Get
            Select Case RoomConfiguration.RoomView
                Case Constants.ROOMVIEW.DASHVIEW : Return Constants.VISIBLE
                Case Else : Return Constants.COLLAPSED
            End Select
        End Get
    End Property
    Public ReadOnly Property MoveDownDashboardVisibility As String
        Get
            Select Case RoomConfiguration.RoomView
                Case Constants.ROOMVIEW.DASHVIEW : Return Constants.VISIBLE
                Case Else : Return Constants.COLLAPSED
            End Select
        End Get
    End Property

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
            ' RaisePropertyChanged("RoomContentViewTemplate")
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

    Public Sub New(roomplan As Domoticz.Plan, roomConfig As TiczStorage.RoomConfiguration)
        RoomIDX = roomplan.idx
        RoomName = roomplan.Name
        RoomConfiguration = roomConfig
        ItemHeight = 120
        'TiczRoomConfigs.GetRoomConfig(RoomToLoad.idx, RoomToLoad.Name)
        SetItemWidthHeight()

    End Sub

    Public Sub SetItemWidthHeight()
        Const DefaultItemWidth = 120 'The Default Width for an Device in Icon View
        Const DefaultItemHeight = 120 'The Default Height for an Device in Icon View
        Dim iWidth As Integer  'Minimum Item Width
        Dim iMargin As Integer 'Any additional Margin that the item carries
        Select Case RoomConfiguration.RoomView
            Case Constants.ROOMVIEW.DASHVIEW : iWidth = DefaultItemWidth : iMargin = 0
            Case Constants.ROOMVIEW.RESIZEVIEW : iWidth = DefaultItemWidth : iMargin = 0
            Case Constants.ROOMVIEW.GRIDVIEW : iWidth = 200 : iMargin = 0
            Case Constants.ROOMVIEW.ICONVIEW : iWidth = DefaultItemWidth : iMargin = 4
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

    'Public Function GetActiveGroupedDeviceList() As DeviceGroup(Of DevicesViewModel)
    '    Return GroupedDevices
    'End Function

    'Public Function GetActiveDeviceList() As DevicesViewModel
    '    Return Devices
    'End Function


    Public Async Function GetDevicesForRoom(RoomView As String) As Task
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
                Dim DevToAdd
                Dim devConfig As TiczStorage.DeviceConfiguration = (From dev In RoomConfiguration.DeviceConfigurations
                                                                    Where dev.DeviceIDX = d.idx And dev.DeviceName = d.Name Select dev).FirstOrDefault
                If devConfig Is Nothing Then
                    Dim newDevConfig As TiczStorage.DeviceConfiguration = (New TiczStorage.DeviceConfiguration With {.DeviceIDX = d.idx, .DeviceName = d.Name,
                                                                           .DeviceRepresentation = Constants.DEVICEVIEWS.ICON, .DeviceOrder = 9999})
                    RoomConfiguration.DeviceConfigurations.Add(newDevConfig)
                    devConfig = newDevConfig
                End If
                If d.HardwareType = Constants.DEVICE.HARDWARETYPE.LOGITECHMEDIASERVER Then
                    DevToAdd = New LogitechMediaServerDeviceViewModel(d, RoomView, devConfig)
                ElseIf d.HardwareType = Constants.DEVICE.HARDWARETYPE.KODIMEDIASERVER Then
                    DevToAdd = New KODIDeviceViewModel(d, RoomView, devConfig)
                Else
                    DevToAdd = New DeviceViewModel(d, RoomView, devConfig)
                End If
                'Dim DevToAdd As New LogitechMediaServerDeviceViewModel(d, RoomView)
                If app.myViewModel.TiczSettings.OnlyShowFavourites Then
                    If d.Favorite = 1 Then ret.Add(DevToAdd)
                Else
                    ret.Add(DevToAdd)
                End If
            Next
            'TEST : REMOVE INJECTION OF TEST DEVICE
            'Dim testDevice As New DeviceModel With {.Type = "RFXMeter", .TypeImg = "water", .SwitchTypeVal = 2, .Data = "0.030 m3", .Name = "RFXMetert"}
            'ret.Add(New DeviceViewModel(testDevice, RoomView))
            deserialized = Nothing
        Else
            Await app.myViewModel.Notify.Update(True, String.Format("Connection error {0}", response.ReasonPhrase), 2, False, 4)
        End If
        WriteToDebug("RoomViewModel.GetDevicesForRoom()", String.Format("Retrieved {0} devices", ret.Count))
        Devices = New DevicesViewModel(Me.RoomName, ret.OrderBy(Function(x) x.DeviceOrder))
    End Function


    '''' <summary>
    '''' Loads the devices for this room. Depending on the Room's configuration for the View, it will load the devices in a grouped list, or in a single list (Dashboard only)
    '''' </summary>
    '''' <returns></returns>
    'Public Async Function LoadDevices() As Task
    '    Await app.myViewModel.Notify.Update(False, "loading devices...", 0, False, 0)
    '    'If Not GroupedDevices Is Nothing Then GroupedDevices.Clear()
    '    'If Not IconViewDevices Is Nothing Then IconViewDevices.Clear()
    '    'If Not GridViewDevices Is Nothing Then GridViewDevices.Clear()
    '    'If Not ListViewDevices Is Nothing Then ListViewDevices.Clear()
    '    'If Not ResizeViewDevices Is Nothing Then ResizeViewDevices.Clear()
    '    'RaisePropertyChanged("ItemWidth")
    '    If Me.RoomConfiguration.RoomView <> Constants.ROOMVIEW.DASHVIEW Then
    '        'Await LoadGroupedDevicesForRoom()
    '        Await LoadDevicesForRoom()
    '    Else
    '        Await LoadDevicesForRoom()
    '    End If

    'End Function


    ''' <summary>
    ''' 'Loads the devices for this room into RoomDevices, used for Dashboard View only (not grouped)
    ''' </summary>
    ''' <returns></returns>
    Public Async Function LoadDevicesForRoom() As Task
        Await GetDevicesForRoom(Me.RoomConfiguration.RoomView)
        'Me.Devices = New DevicesViewModel(RoomName, devicesToAdd)
        Me.RoomConfiguration.DeviceConfigurations.SortRoomDevices()
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
                Case Constants.DEVICE.TYPE.LIGHTING_LIMITLESS, Constants.DEVICE.TYPE.LIGHT_SWITCH, Constants.DEVICE.TYPE.LIGHTING_1, Constants.DEVICE.TYPE.LIGHTING_2, Constants.DEVICE.TYPE.SECURITY
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



    '''' <summary>
    '''' 'Loads the devices for this room into GroupedRoomDevices, used for all view except Dashboard View
    '''' </summary>
    '''' <returns></returns>
    'Public Async Function LoadGroupedDevicesForRoom() As Task
    '    WriteToDebug("RoomViewModel.LoadGroupedDevicesForRoom()", "executed")
    '    Dim NewDevices As New DeviceGroup(Of DevicesViewModel)
    '    Dim roomDevices = Await GetDevicesForRoom(Me.RoomConfiguration.RoomView)

    '    'Create groups for the Room. Empty groups will be filtered out by the GroupStyle in XAML
    '    NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_GROUPS_SCENES))
    '    NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_LIGHTS_SWITCHES))
    '    NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_WEATHER))
    '    NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_TEMPERATURE))
    '    NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_UTILITY))
    '    NewDevices.Add(New DevicesViewModel(Constants.DEVICEGROUPS.GRP_OTHER))

    '    'Go through each device, and map it to its seperate subcollection
    '    For Each d In roomDevices
    '        Select Case d.Type
    '            Case Constants.DEVICE.TYPE.SCENE, Constants.DEVICE.TYPE.GROUP
    '                NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_GROUPS_SCENES).FirstOrDefault().Add(d)
    '            Case Constants.DEVICE.TYPE.LIGHTING_LIMITLESS, Constants.DEVICE.TYPE.LIGHT_SWITCH, Constants.DEVICE.TYPE.LIGHTING_1, Constants.DEVICE.TYPE.LIGHTING_2, Constants.DEVICE.TYPE.SECURITY
    '                NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_LIGHTS_SWITCHES).FirstOrDefault().Add(d)
    '            Case Constants.DEVICE.TYPE.TEMP_HUMI_BARO, Constants.DEVICE.TYPE.WIND, Constants.DEVICE.TYPE.UV, Constants.DEVICE.TYPE.RAIN
    '                NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_WEATHER).FirstOrDefault().Add(d)
    '            Case Constants.DEVICE.TYPE.TEMP, Constants.DEVICE.TYPE.THERMOSTAT
    '                NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_TEMPERATURE).FirstOrDefault().Add(d)
    '            Case Constants.DEVICE.TYPE.GENERAL, Constants.DEVICE.TYPE.USAGE, Constants.DEVICE.TYPE.P1_SMART_METER,
    '                 Constants.DEVICE.TYPE.LUX, Constants.DEVICE.TYPE.AIR_QUALITY, Constants.DEVICE.TYPE.RFXMETER,
    '                 Constants.DEVICE.TYPE.HUMIDITY, Constants.DEVICE.TYPE.CURRENT, Constants.DEVICE.TYPE.WEIGHT
    '                NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_UTILITY).FirstOrDefault().Add(d)
    '            Case Else
    '                NewDevices.Where(Function(x) x.Key = Constants.DEVICEGROUPS.GRP_OTHER).FirstOrDefault().Add(d)
    '                WriteToDebug("RoomViewModel.LoadGroupedDevicesForRoom()", String.Format("{0} : {1}", d.Name, d.Type))
    '        End Select
    '    Next

    '    Select Case RoomConfiguration.RoomView
    '        'Case Constants.ROOMVIEW.ICONVIEW : GroupedDevices = NewDevices
    '        'Case Constants.ROOMVIEW.LISTVIEW : GroupedDevices = NewDevices
    '        'Case Constants.ROOMVIEW.GRIDVIEW : GroupedDevices = NewDevices
    '        'Case Constants.ROOMVIEW.RESIZEVIEW : GroupedDevices = NewDevices
    '    End Select
    'End Function
End Class