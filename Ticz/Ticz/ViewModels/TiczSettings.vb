Imports System.Text.RegularExpressions
Imports System.Threading
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command

Partial Public Class TiczSettings
    Inherits ViewModelBase

    Private app As Application = CType(Application.Current, Application)
    Private vm As TiczViewModel = app.myViewModel


    Dim settings As Windows.Storage.ApplicationDataContainer

    Const strServerIPKeyName As String = "strServerIP"
    Const strServerPortKeyName As String = "strServerPort"
    Const strUsernameKeyName As String = "strUserName"
    Const strUserPasswordKeyName As String = "strUserPassword"
    Const strMinimumNumberOfColumnsKeyName As String = "strMinimumNumberOfColumns"
    Const strShowMarqueeKeyName As String = "strShowMarquee"
    Const strShowAllDevicesKeyName As String = "strShowAllDevices"
    Const strShowFavoritesDevicesKeyName As String = "strShowFavouritesDevices"
    Const strSecondsForRefreshKeyName As String = "strSecondsForRefresh"
    Const strPreferredRoomIDXKeyName As String = "strPreferredRoomIDX"
    Const strShowLastSeenKeyName As String = "strShowLastSeen"
    Const strUseDarkThemeKeyName As String = "strUseDarkTheme"
    Const strPlaySecPanelSFXKeyName As String = "strPlaySecPanelSFX"
    Const strOnlyShowFavouritesKeyName As String = "strOnlyShowFavourites"
    Const strUseBitmapIconsKeyName As String = "strUseBitmapIcons"
    Const strUseHTTPSKeyName As String = "strUseHTTPS"
    Const strIgnoreSSLErrorsKeyName As String = "strIgnoreSSLErrors"
    Const strScreenSaverActiveKeyName As String = "strScreenSaverActive"
    Const strIdleTimeBeforeScreenSaverKeyName As String = "strIdleTimeBeforeScreenSaver"
    Const strZoomFactorKeyName As String = "strZoomFactor"
    Const strHTTPTimeOutKeyName As String = "strHTTPTimeOut"

#If DEBUG Then
    'PUT YOUR (TEST) SERVER DETAILS HERE IF YOU WANT TO DEBUG, AND NOT PROVIDE CREDENTIALS AND SERVER DETAILS EACH TIME
    Const strServerIPDefault = ""
    Const strServerPortDefault = ""
    Const strUsernameDefault = ""
    Const strUserPasswordDefault = ""
    Const strTimeOutDefault = 5
    Const strMinimumNumberOfColumnsDefault = 2
    Const strShowMarqueeDefault = "False"
    Const strShowAllDevicesDefault = "False"
    Const strShowFavoritesDevicesDefault = "False"
    Const strSecondsForRefreshDefault = 0
    Const strUseBitmapIconsDefault = False
    'Const strSwitchIconBackgroundDefault = False
    'Const strcurrentRoomViewDefault = "Grid View"
    'Const strRoomConfigurationsDefault = ""
    Const strPreferredRoomIDXDefault = 0
    Const strShowLastSeenDefault = False
    Const strUseDarkThemeDefault = "True"
    Const strPlaySecPanelSFXDefault = False
    Const strOnlyShowFavouritesDefault As Boolean = False
    Const strUseHTTPSDefault As Boolean = False
    Const strIgnoreSSLErrorsDefault As Boolean = False
    Const strScreenSaverActiveDefault = False
    Const strIdleTimeBeforeScreenSaverDefault As Integer = 120
    Const strZoomFactorDefault As Double = 1.0
    Const strHTTPTimeOutDefault As Double = 5
#Else
    'PROD SETTINGS
    Const strServerIPDefault = ""
    Const strServerPortDefault = ""
    Const strUsernameDefault = ""
    Const strUserPasswordDefault = ""
    Const strTimeOutDefault = 0
    Const strMinimumNumberOfColumnsDefault = 1
    Const strShowMarqueeDefault = "True"
    Const strShowAllDevicesDefault = True
    Const strShowFavoritesDevicesDefault = "False"
    Const strSecondsForRefreshDefault = 10
    Const strUseBitmapIconsDefault = False
    Const strSwitchIconBackgroundDefault = False
    Const strcurrentRoomViewDefault = "Grid View"
    Const strPreferredRoomIDXDefault = 0
    Const strShowLastSeenDefault = False
    Const strUseDarkThemeDefault = "True"
    Const strPlaySecPanelSFXDefault = False
    Const strOnlyShowFavouritesDefault As Boolean = False
    Const strUseHTTPSDefault As Boolean = False
    Const strIgnoreSSLErrorsDefault As Boolean = False
    Const strScreenSaverActiveDefault = False
    Const strIdleTimeBeforeScreenSaverDefault As Integer = 120
    Const strZoomFactorDefault As Double = 1.0
    Const strHTTPTimeOutDefault As Double = 5
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
                                        app.myViewModel.TiczRoomConfigs.DomoticzRooms.result.Clear()
                                        app.myViewModel.Notify.Clear(True)
                                        WriteToDebug("TestConnectionCommand", ServerIP)
                                        If ContainsValidIPDetails() Then
                                            Dim response As retvalue = Await app.myViewModel.DomoConfig.Load()
                                            If response.issuccess Then
                                                TestConnectionResult = "Hurray !"
                                                Await app.myViewModel.Load()
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
        If UseHTTPS Then
            Return "https://" + app.myViewModel.TiczSettings.ServerIP + ":" + ServerPort
        Else
            Return "http://" + app.myViewModel.TiczSettings.ServerIP + ":" + ServerPort
        End If

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

    Public Property HTTPTimeOut As Double
        Get
            Return GetValueOrDefault(Of Double)(strHTTPTimeOutKeyName, strHTTPTimeOutDefault)
        End Get
        Set(value As Double)
            If AddOrUpdateValue(strHTTPTimeOutKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property ScreenSaverActive As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strScreenSaverActiveKeyName, strScreenSaverActiveDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strScreenSaverActiveKeyName, value) Then
                Save()
            End If
        End Set
    End Property


    Private _ZoomFactorChoices As List(Of Double) = New List(Of Double)({1.0, 1.2, 1.5, 1.7, 2.0, 2.5, 3.0})
    Public ReadOnly Property ZoomFactorChoices As List(Of Double)
        Get
            Return _ZoomFactorChoices
        End Get
    End Property

    Public Property ZoomFactor As Double
        Get
            Return GetValueOrDefault(Of Double)(strZoomFactorKeyName, strZoomFactorDefault)
        End Get
        Set(value As Double)
            If AddOrUpdateValue(strZoomFactorKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Private _IdleTimeBeforeScreenSaverChoices As List(Of Integer) = New List(Of Integer)({60, 120, 180, 240, 360})

    Public ReadOnly Property IdleTimeBeforeScreenSaverChoices As List(Of Integer)
        Get
            Return _IdleTimeBeforeScreenSaverChoices
        End Get
    End Property

    Public Property IdleTimeBeforeScreenSaver As Integer
        Get
            Return GetValueOrDefault(Of Integer)(strIdleTimeBeforeScreenSaverKeyName, strIdleTimeBeforeScreenSaverDefault)
        End Get
        Set(value As Integer)
            If AddOrUpdateValue(strIdleTimeBeforeScreenSaverKeyName, value) Then
                Save()
            End If
        End Set
    End Property



    Public Property IgnoreSSLErrors As Boolean?
        Get
            Return GetValueOrDefault(Of Boolean)(strIgnoreSSLErrorsKeyName, strIgnoreSSLErrorsDefault)
        End Get
        Set(value As Boolean?)
            If AddOrUpdateValue(strIgnoreSSLErrorsKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property UseHTTPS As Boolean?
        Get
            Return GetValueOrDefault(Of Boolean)(strUseHTTPSKeyName, strUseHTTPSDefault)
        End Get
        Set(value As Boolean?)
            If AddOrUpdateValue(strUseHTTPSKeyName, value) Then
                Save()
            End If
        End Set
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


    Public Property UseDomoticzIcons As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strUseBitmapIconsKeyName, strUseBitmapIconsDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strUseBitmapIconsKeyName, value) Then
                Save()
            End If
        End Set
    End Property



    Public Property UseDarkTheme As Boolean?
        Get
            Return GetValueOrDefault(Of Boolean)(strUseDarkThemeKeyName, strUseDarkThemeDefault)
        End Get
        Set(value As Boolean?)
            If AddOrUpdateValue(strUseDarkThemeKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property UseLightTheme As Boolean?
        Get
            Return Not UseDarkTheme
        End Get
        Set(value As Boolean?)
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
            app.myViewModel.TiczRoomConfigs.LoadRoomConfigurations()
        End Set
    End Property

    Public Property ShowFavorites As Boolean
        Get
            Return GetValueOrDefault(Of Boolean)(strShowFavoritesDevicesKeyName, strShowFavoritesDevicesDefault)
        End Get
        Set(value As Boolean)
            If AddOrUpdateValue(strShowFavoritesDevicesKeyName, value) Then
                Save()
            End If
            app.myViewModel.TiczRoomConfigs.LoadRoomConfigurations()
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
                If _PreferredRoom Is Nothing Then
                    _PreferredRoom = value
                    PreferredRoomIDX = value.RoomIDX
                    RaisePropertyChanged("PreferredRoom")
                Else
                    _PreferredRoom = value
                    PreferredRoomIDX = value.RoomIDX
                End If
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
