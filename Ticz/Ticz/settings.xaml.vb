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
    Const strcurrentRoomViewKeyName As String = "strcurrentRoomView"
    Const strRoomConfigurationsKeyName As String = "strRoomConfigurations"
    Const strPreferredRoomIDXKeyName As String = "strPreferredRoomIDX"

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
    Const strcurrentRoomViewDefault = "Grid View"
    Const strRoomConfigurationsDefault = ""
    Const strPreferredRoomIDXDefault = 0
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
    Const strcurrentRoomViewDefault = "Grid View"
    Const strPreferredRoomIDXDefault = 0
#End If

    Const strConnectionStatusDefault = False


    Public Const strDashboardDevicesFileName As String = "dashboarddevices.xml"

    Public Sub New()
        settings = Windows.Storage.ApplicationData.Current.LocalSettings
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
                                        WriteToDebug("TestConnectionCommand", ServerIP)
                                        If ContainsValidIPDetails() Then
                                            Dim response As retvalue = Await TiczViewModel.DomoRooms.Load()
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
        If Net.IPAddress.TryParse(TiczViewModel.TiczSettings.ServerIP, tmpIPAddress) Then

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

    Private _RoomViews As List(Of String) = New List(Of String)({Constants.ICONVIEW, Constants.GRIDVIEW, Constants.LISTVIEW, Constants.RESIZEVIEW, Constants.DASHVIEW}).ToList
    Public ReadOnly Property RoomViewChoices As List(Of String)
        Get
            Return _RoomViews
        End Get
    End Property

    Public Property PreferredRoom As TiczStorage.RoomConfiguration
        Get
            If Not TiczViewModel.TiczRoomConfigs Is Nothing Then
                Dim room = (From t In TiczViewModel.TiczRoomConfigs Where t.RoomIDX = PreferredRoomIDX Select t).FirstOrDefault
                If Not room Is Nothing Then
                    Return room
                Else
                    If TiczViewModel.TiczRoomConfigs.Count > 0 Then
                        Return TiczViewModel.TiczRoomConfigs(0)
                    Else
                        Return Nothing
                    End If
                End If
            Else
                Return Nothing
            End If

        End Get
        Set(value As TiczStorage.RoomConfiguration)
            If Not value Is Nothing Then
                PreferredRoomIDX = value.RoomIDX
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




    Public Property currentRoomView As String
        Get
            Return GetValueOrDefault(Of String)(strcurrentRoomViewKeyName, strcurrentRoomViewDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strcurrentRoomViewKeyName, value) Then
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
        Me.DataContext = app.myViewModel
        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        If rootFrame.CanGoBack Then
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
        Else
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        End If

        If (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) Then
            Dim sBar As StatusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView()
            If Not sBar Is Nothing Then
                sBar.HideAsync()
            End If
        End If

    End Sub


    Protected Overrides Async Sub OnNavigatedFrom(e As NavigationEventArgs)
        Await Task.Run(Function() TiczViewModel.TiczRoomConfigs.SaveRoomConfigurations())
    End Sub


    Public Sub New()
        InitializeComponent()
    End Sub
End Class
