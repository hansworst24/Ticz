Imports System.IO.IsolatedStorage
Imports System.Xml.Serialization
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports Windows.Storage.Streams
Imports Windows.UI.Core

Partial Public Class AppSettings
    Inherits ViewModelBase

    Private app As App = CType(Application.Current, App)
    Private vm As TiczViewModel = app.myViewModel

    Public Class DeviceConfiguration
        Public Property DeviceIDX As Integer
        Public Property DeviceName As String
        Public Property OnDashboard As Boolean
        Public Property DashboardOrder As Integer
        Public Property ColumnSpan As Integer
        Public Property RowSpan As Integer

        Public Sub New()
            ColumnSpan = 1
            RowSpan = 1
            OnDashboard = False
        End Sub
    End Class


    Public Class DeviceConfigurations
        Inherits List(Of DeviceConfiguration)

        Public Sub SortDashboardDevices()
            Dim intIndex As Integer = 0
            For Each Device In Me.Where(Function(x) x.OnDashboard).OrderBy(Function(x) x.DashboardOrder)
                Device.DashboardOrder = intIndex
                intIndex += 1
                WriteToDebug(Device.DeviceName, Device.DashboardOrder)
            Next
        End Sub

        Public Async Sub AddToDashboard(devidx As Integer, devname As String)
            Dim devToAdd = (From d In Me Where d.DeviceIDX = devidx And d.DeviceName = devname Select d).FirstOrDefault()
            Dim lastDev = (From d In Me Where d.OnDashboard Select d).OrderBy(Function(x) x.DashboardOrder).LastOrDefault()
            If Not devToAdd Is Nothing Then
                devToAdd.OnDashboard = True
                If Not lastDev Is Nothing Then
                    devToAdd.DashboardOrder = lastDev.DashboardOrder + 1
                Else
                    devToAdd.DashboardOrder = 0
                End If
            End If
            SortDashboardDevices()
            Await Save()
        End Sub

        Public Async Sub RemoveFromDashboard(devidx As Integer, devname As String)
            Dim devToAdd = (From d In Me Where d.DeviceIDX = devidx And d.DeviceName = devname Select d).FirstOrDefault()
            If Not devToAdd Is Nothing Then
                devToAdd.OnDashboard = False
                devToAdd.DashboardOrder = 0
            End If
            SortDashboardDevices()
            Await Save()
        End Sub

        Public Async Sub MoveUp(idx As Integer, name As String)
            Dim devToMove = (From d In Me Where d.DeviceIDX = idx And d.DeviceName = name Select d).FirstOrDefault()
            If Not devToMove Is Nothing Then
                Dim oldDevice = (From d In Me Where d.OnDashboard And d.DashboardOrder = devToMove.DashboardOrder - 1 Select d).FirstOrDefault()
                If Not oldDevice Is Nothing Then
                    Dim oldDeviceIndex = oldDevice.DashboardOrder
                    oldDevice.DashboardOrder = devToMove.DashboardOrder
                    devToMove.DashboardOrder = oldDeviceIndex
                End If
                SortDashboardDevices()
                Await Save()
            End If
        End Sub

        Public Async Sub MoveDown(idx As Integer, name As String)
            Dim devToMove = (From d In Me Where d.DeviceIDX = idx And d.DeviceName = name Select d).FirstOrDefault()
            If Not devToMove Is Nothing Then
                Dim oldDevice = (From d In Me Where d.OnDashboard And d.DashboardOrder = devToMove.DashboardOrder + 1 Select d).FirstOrDefault()
                If Not oldDevice Is Nothing Then
                    Dim oldDeviceIndex = oldDevice.DashboardOrder
                    oldDevice.DashboardOrder = devToMove.DashboardOrder
                    devToMove.DashboardOrder = oldDeviceIndex
                End If
                SortDashboardDevices()
                Await Save()
            End If
        End Sub

        Public Async Function Save() As Task
            Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim storageFile As Windows.Storage.StorageFile = Await storageFolder.CreateFileAsync("deviceconfigurations.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)
            Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
            Dim sessionOutputStream As IOutputStream = stream.GetOutputStreamAt(0)
            Dim stuffToSave = New List(Of DeviceConfiguration)
            stuffToSave.AddRange(Me)
            Dim serializer = New XmlSerializer(stuffToSave.GetType())
            serializer.Serialize(sessionOutputStream.AsStreamForWrite(), stuffToSave)
            Await sessionOutputStream.FlushAsync()
            sessionOutputStream.Dispose()
            stream.Dispose()
        End Function

        Public Async Function Load() As Task
            Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim storageFile As Windows.Storage.StorageFile
            Dim stuffToLoad As New List(Of DeviceConfiguration)
            Try
                storageFile = Await storageFolder.GetFileAsync("deviceconfigurations.xml")
            Catch ex As Exception
                TiczViewModel.Notify.Update(True, "error loading configuration file", 2)
                Exit Function
            End Try
            Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read)
            Dim sessionInputStream As IInputStream = stream.GetInputStreamAt(0)
            Dim serializer = New XmlSerializer((New DeviceConfigurations).GetType())
            Try
                stuffToLoad = serializer.Deserialize(sessionInputStream.AsStreamForRead())
            Catch ex As Exception
                TiczViewModel.Notify.Update(True, "error loading configuration file", 2)
                Exit Function
            End Try
            stream.Dispose()
            Me.Clear()
            Me.AddRange(stuffToLoad)
        End Function

        Public Function IsOnDashboard(idx As Integer, name As String)
            Return Me.Any(Function(x) x.DeviceIDX = idx And x.DeviceName = name And x.OnDashboard)
        End Function

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


        Public Function IsFirstOnDashboard(idx As Integer, name As String)
            Dim firstDashboardItem = (From d In Me Where d.OnDashboard).OrderBy(Function(x) x.DashboardOrder).FirstOrDefault()
            If Not firstDashboardItem Is Nothing Then
                If firstDashboardItem.DeviceIDX = idx And firstDashboardItem.DeviceName = name Then Return True Else Return False
            End If
            Return False
        End Function

        Public Function IsLastOnDashboard(idx As Integer, name As String)
            Dim lastDashboardItem = (From d In Me Where d.OnDashboard).OrderBy(Function(x) x.DashboardOrder).LastOrDefault()
            If Not lastDashboardItem Is Nothing Then
                If lastDashboardItem.DeviceIDX = idx And lastDashboardItem.DeviceName = name Then Return True Else Return False
            End If
            Return False
        End Function

    End Class


    Public Class RoomConfiguration
        Inherits ViewModelBase
        Public Property RoomIDX As Integer
        Public Property RoomName As String
        Public Property ShowRoom As Boolean
            Get
                Return _ShowRoom
            End Get
            Set(value As Boolean)
                _ShowRoom = value
                RaisePropertyChanged()
            End Set
        End Property
        Private Property _ShowRoom As Boolean
        Public Property RoomView As Integer
            Get
                Return _RoomView
            End Get
            Set(value As Integer)
                _RoomView = value
                RaisePropertyChanged()
            End Set
        End Property
        Private Property _RoomView As Integer

        Public Sub New()
            RoomView = 0
            ShowRoom = True
        End Sub


    End Class


    Dim settings As Windows.Storage.ApplicationDataContainer

    Const strServerIPKeyName As String = "strServerIP"
    Const strServerPortKeyName As String = "strServerPort"
    Const strUsernameKeyName As String = "strUserName"
    Const strUserPasswordKeyName As String = "strUserPassword"
    Const strMinimumNumberOfColumnsKeyName As String = "strMinimumNumberOfColumns"
    Const strShowMarqueeKeyName As String = "strShowMarquee"
    Const strShowFavouritesKeyName As String = "strShowFavourites"
    Const strShowAllDevicesKeyName As String = "strShowAllDevices"
    Const strSecondsForRefreshKeyName As String = "strSecondsForRefresh"
    Const strUseBitmapIconsKeyName As String = "blUseBitmapIcons"
    Const strSwitchIconBackgroundKeyName As String = "strSwitchIconBackground"
    Const strSelectedRoomViewKeyName As String = "strSelectedRoomView"
    Const strRoomConfigurationsKeyName As String = "strRoomConfigurations"

#If DEBUG Then
    'PUT YOUR (TEST) SERVER DETAILS HERE IF YOU WANT TO DEBUG, AND NOT PROVIDE CREDENTIALS AND SERVER DETAILS EACH TIME
    Const strServerIPDefault = ""
    Const strServerPortDefault = ""
    Const strUsernameDefault = ""
    Const strUserPasswordDefault = ""
    Const strTimeOutDefault = 5
    Const strMinimumNumberOfColumnsDefault = 2
    Const strShowMarqueeDefault = "False"
    Const strShowFavouritesDefault = "True"
    Const strShowAllDevicesDefault = "False"
    Const strSecondsForRefreshDefault = 0
    Const strUseBitmapIconsDefault = False
    Const strSwitchIconBackgroundDefault = False
    Const strSelectedRoomViewDefault = "Grid View"
    Const strRoomConfigurationsDefault = ""
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
    Const strShowAllDevicesDefault = "True"
    Const strSecondsForRefreshDefault = 10
    Const strUseBitmapIconsDefault = False
    Const strSwitchIconBackgroundDefault = False
    Const strSelectedRoomViewDefault = "Grid View"
#End If

    Const strConnectionStatusDefault = False
    Public Const strDeviceConfigurationFileName As String = "deviceconfigurations.xml"
    Public Const strRoomConfigurationFileName As String = "roomconfigurations.xml"
    Public Const strDashboardDevicesFileName As String = "dashboarddevices.xml"

    Public Sub New()
        settings = Windows.Storage.ApplicationData.Current.LocalSettings
        RoomConfigurations = New List(Of RoomConfiguration)
        myDeviceConfigurations = New DeviceConfigurations
    End Sub



    Public Async Function LoadRoomConfigurationsFromFile() As Task(Of List(Of RoomConfiguration))
        Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Dim storageFile As Windows.Storage.StorageFile
        Dim fileExists As Boolean = True
        Try
            storageFile = Await storageFolder.GetFileAsync(strRoomConfigurationFileName)
        Catch ex As Exception
            fileExists = False
            Return New List(Of RoomConfiguration)
        End Try
        Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read)
        Dim sessionInputStream As IInputStream = stream.GetInputStreamAt(0)
        Dim serializer = New XmlSerializer((New List(Of RoomConfiguration)).GetType())
        Dim stuffToLoad As List(Of RoomConfiguration) = serializer.Deserialize(sessionInputStream.AsStreamForRead())
        stream.Dispose()
        Return stuffToLoad

    End Function




    Public Async Function SaveRoomConfigurationsToFile(roomList As List(Of RoomConfiguration)) As Task
        Dim storageFolder As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Dim storageFile As Windows.Storage.StorageFile = Await storageFolder.CreateFileAsync("roomconfigurations.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)
        Dim stream = Await storageFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
        Dim sessionOutputStream As IOutputStream = stream.GetOutputStreamAt(0)
        Dim stuffToSave = RoomConfigurations
        Dim serializer = New XmlSerializer(stuffToSave.GetType())
        serializer.Serialize(sessionOutputStream.AsStreamForWrite(), stuffToSave)
        Await sessionOutputStream.FlushAsync()
        sessionOutputStream.Dispose()
        stream.Dispose()
    End Function


    Public Property RoomConfigurations As List(Of RoomConfiguration)
    Public Property myDeviceConfigurations As DeviceConfigurations


    Public Function GetDeviceSize(dIDX As Integer) As DeviceConfiguration
        Dim d As DeviceConfiguration = (From device In myDeviceConfigurations Where device.DeviceIDX = dIDX Select device).FirstOrDefault()
        If Not d Is Nothing Then
            Return d
        End If
        Return Nothing
    End Function

    Public Sub SetDeviceSize(dIDX As Integer, cSpan As Integer, rSpan As Integer)

        Dim d As DeviceConfiguration = (From device In myDeviceConfigurations Where device.DeviceIDX = dIDX Select device).FirstOrDefault()
        If Not d Is Nothing Then
            d.RowSpan = rSpan
            d.ColumnSpan = cSpan
        End If
    End Sub


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
                                        WriteToDebug("TestConnectionCommand", "executed")
                                        If ContainsValidIPDetails() Then
                                            Dim response As retvalue = Await (New Plans).Load()
                                            If response.issuccess Then
                                                TestConnectionResult = "Hurray !"
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
        Dim tmpIPAddress As Net.IPAddress
        If Net.IPAddress.TryParse(ServerIP, tmpIPAddress) Then

            If ServerPort.Length > 0 AndAlso ServerPort.All(Function(x) Char.IsDigit(x)) AndAlso CType(ServerPort, Integer) <= 65535 Then
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function


    Public Function GetFullURL() As String
        Return "http://" + ServerIP + ":" + ServerPort
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

    Public Property SwitchIconBackground As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strSwitchIconBackgroundKeyName, strSwitchIconBackgroundDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strSwitchIconBackgroundKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property UseBitmapIcons As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strUseBitmapIconsKeyName, strUseBitmapIconsDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strUseBitmapIconsKeyName, value) Then
                Save()
            End If
        End Set
    End Property


    Public Property ShowFavourites As String
        Get
            Return GetValueOrDefault(Of String)(strShowFavouritesKeyName, strShowFavouritesDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strShowFavouritesKeyName, value) Then
                Save()
            End If
        End Set
    End Property
    Public Property ShowAllDevices As String
        Get
            Return GetValueOrDefault(Of String)(strShowAllDevicesKeyName, strShowAllDevicesDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strShowAllDevicesKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property ShowMarquee As String
        Get
            Return GetValueOrDefault(Of String)(strShowMarqueeKeyName, strShowMarqueeDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strShowMarqueeKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Private _RoomViews As List(Of String) = New List(Of String)({"Icon View", "Grid View", "List View", "Resize View", "Dashboard View"}).ToList
    Public ReadOnly Property RoomViewChoices As List(Of String)
        Get
            Return _RoomViews
        End Get
    End Property

    Public Property SelectedRoomView As String
        Get
            Return GetValueOrDefault(Of String)(strSelectedRoomViewKeyName, strSelectedRoomViewDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strSelectedRoomViewKeyName, value) Then
                Save()
            End If
        End Set
    End Property

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



Public NotInheritable Class AppSettingsPage
    Inherits Page

    Dim app As App = CType(Application.Current, App)

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        Me.DataContext = TiczViewModel.TiczSettings
        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        If rootFrame.CanGoBack Then
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
        Else
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        End If

        'Dim list = app.myViewModel.TiczSettings.RoomConfigurations
        'Dim serializer As New XmlSerializer(list.GetType)
        'Dim file = IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication
        'Dim stream = New IsolatedStorageFileStream("roomconfiguration.xml", FileMode.OpenOrCreate, file)
        'serializer.Serialize(stream, list)
        'If Not stream Is Nothing Then
        '    WriteToDebug(stream.ToString, "")
        'End If
    End Sub


    Protected Overrides Async Sub OnNavigatedFrom(e As NavigationEventArgs)
        Await TiczViewModel.TiczSettings.SaveRoomConfigurationsToFile(TiczViewModel.TiczSettings.RoomConfigurations)
    End Sub


    Public Sub New()
        InitializeComponent()
    End Sub
End Class
