' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Ticz.AppSettings
Imports Ticz.TiczViewModel
Imports Windows.UI.Core
Imports Windows.Web.Http
Imports WinRTXamlToolkit.Controls
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class MainPage
    Inherits Page

    Dim app As App = CType(Application.Current, App)
    Dim vm As TiczViewModel = App.myViewModel
    'Dim settings As AppSettings = vm.TiczSettings

    Protected Overrides Async Sub OnNavigatedTo(e As NavigationEventArgs)
        'vm.TiczSettings = New AppSettings
        'vm.DomoticzApi = New Api


        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        If rootFrame.CanGoBack Then
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
        Else
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        End If
        'Redirect to Settings Page if IP/Port are not valid
        If Not TiczViewModel.TiczSettings.ContainsValidIPDetails Then
            Await Notify.Update(True, "IP/Port settings not valid", 0)
            Await Task.Delay(1000)
            Await Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                            Me.Frame.Navigate(GetType(AppSettingsPage))
                                                                                        End Sub)

        Else
            'Set datacontext to viewmodel
            Me.DataContext = vm
            'First Load the (Room) Plans
            Await Notify.Update(False, "connecting...", 0)
            MyRooms.Clear()
            Await Notify.Update(False, "loading rooms...", 0)
            Dim retplan As retvalue = Await MyPlans.Load()

            'Load the Room Configuration for each room from Storage
            TiczViewModel.TiczSettings.RoomConfigurations = Await TiczViewModel.TiczSettings.LoadRoomConfigurationsFromFile()
            'Check if the Room Configuration contains the Ticz Rooms as described below. If not, add them
            If TiczSettings.RoomConfigurations.Any(Function(x) x.RoomName = "Dashboard") = False Then
                TiczSettings.RoomConfigurations.Add(New RoomConfiguration With {.RoomName = "Dashboard", .RoomView = 3})
            End If
            If TiczSettings.RoomConfigurations.Any(Function(x) x.RoomName = "Favourites") = False Then
                TiczSettings.RoomConfigurations.Add(New RoomConfiguration With {.RoomName = "Favourites"})
            End If
            If TiczSettings.RoomConfigurations.Any(Function(x) x.RoomName = "All Devices") = False Then
                TiczSettings.RoomConfigurations.Add(New RoomConfiguration With {.RoomName = "All Devices"})
            End If
            'Add any rooms to the Room Configuration that are not present
            For Each r In MyPlans.result
                If TiczSettings.RoomConfigurations.Any(Function(x) x.RoomName = r.Name) = False Then
                    TiczSettings.RoomConfigurations.Add(New RoomConfiguration With {.RoomName = r.Name})
                End If
            Next
            'Save the Room Configuration back to disk
            Await TiczSettings.SaveRoomConfigurationsToFile(TiczViewModel.TiczSettings.RoomConfigurations)


            If retplan.issuccess Then
                'Load all devices
                Await TiczViewModel.Notify.Update(False, "loading devices...", 0)
                Await myDevices.Load()
                'Load the Device Configuration for each Device from Storage
                Await TiczViewModel.TiczSettings.myDeviceConfigurations.Load()

                'Add any devices that are not present in the DeviceConfigurations
                For Each d In myDevices.result
                    If TiczSettings.myDeviceConfigurations.Any(Function(x) x.DeviceIDX = d.idx And x.DeviceName = d.Name) = False Then
                        TiczSettings.myDeviceConfigurations.Add(New DeviceConfiguration With {.DeviceIDX = d.idx, .DeviceName = d.Name, .ColumnSpan = 1, .RowSpan = 1, .OnDashboard = False})
                    End If
                Next

                'Save the Device Configuration back to disk
                Await TiczViewModel.TiczSettings.myDeviceConfigurations.Save()

                Await TiczViewModel.Notify.Update(False, "creating rooms...", 0)

                'Create the rooms
                For Each plan In MyPlans.result.OrderBy(Function(x) x.Order)
                    Dim roomconfig As RoomConfiguration = (From c In TiczSettings.RoomConfigurations Where c.RoomName = plan.Name Select c).FirstOrDefault
                    If Not roomconfig Is Nothing AndAlso roomconfig.ShowRoom = True Then
                        Dim newRoom As New Room With {.RoomIDX = plan.idx, .RoomName = plan.Name, .RoomViewIndex = roomconfig.RoomView}
                        newRoom.Initialize()
                        Await newRoom.Decorate()
                        MyRooms.Add(newRoom)
                    End If
                Next

                'Only show All Devices when there isn't a test-room "Ticz" created
                If Not MyPlans.result.Any(Function(x) x.Name = "Ticz") Then
                    Dim FavRoomConfig As RoomConfiguration = (From r In TiczSettings.RoomConfigurations Where r.RoomName = "Favourites" Select r).FirstOrDefault
                    If Not FavRoomConfig Is Nothing AndAlso FavRoomConfig.ShowRoom Then
                        Dim favs = From d In myDevices.result Where d.Favorite = 1 Select d
                        Dim favRoom As New Room With {.RoomName = FavRoomConfig.RoomName, .RoomViewIndex = FavRoomConfig.RoomView}
                        favRoom.Initialize()
                        Await favRoom.Decorate()
                        MyRooms.Insert(0, favRoom)
                    End If
                    Dim DashRoomConfig As RoomConfiguration = (From r In TiczSettings.RoomConfigurations Where r.RoomName = "Dashboard" Select r).FirstOrDefault
                    If Not DashRoomConfig Is Nothing AndAlso DashRoomConfig.ShowRoom Then
                        Dim dashRoom As New Room With {.RoomName = DashRoomConfig.RoomName, .RoomViewIndex = 4}
                        dashRoom.Initialize()
                        Await dashRoom.Decorate()
                        MyRooms.Insert(0, dashRoom)
                    End If

                    Dim AllDevRoomConfig As RoomConfiguration = (From r In TiczSettings.RoomConfigurations Where r.RoomName = "All Devices" Select r).FirstOrDefault
                    If Not AllDevRoomConfig Is Nothing AndAlso AllDevRoomConfig.ShowRoom Then
                        Dim roomConfig = TiczSettings.RoomConfigurations.Where(Function(x) x.RoomName = "All Devices").FirstOrDefault()
                        If Not roomConfig Is Nothing Then
                            Dim allDevRoom = New Room With {.RoomName = AllDevRoomConfig.RoomName, .RoomViewIndex = roomConfig.RoomView, .DeviceGroups = ConstructDeviceGroups(From d In myDevices.result Select d)}
                            allDevRoom.Initialize()
                            Await allDevRoom.Decorate()
                            MyRooms.Add(allDevRoom)

                        End If

                    End If
                End If
                TiczViewModel.Notify.Clear()
            Else
                Await TiczViewModel.Notify.Update(True, "connection error", 0)
            End If
        End If

        vm.StartRefresh()

    End Sub

    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        vm.StopRefresh()
    End Sub

    Private Sub AppBar_SizeChanged(sender As Object, e As SizeChangedEventArgs)

    End Sub

    Private Sub AppBar_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim a As AppBar = CType(sender, AppBar)
        'If a.IsOpen Then vm.NotifiCationMargin = 36 Else vm.NotificationMargin = 0
        WriteToDebug("AppBar.AppBar_Tapped()", a.ActualHeight)

    End Sub

    Private Sub GridView_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        WriteToDebug("MainPage.GridView_SizeChanged()", "executed")
        Dim gv As GridView = CType(sender, GridView)
        Dim Panel = CType(gv.ItemsPanelRoot, ItemsWrapGrid)
        Dim amountOfColumns = Math.Ceiling(gv.ActualWidth / 400)
        If amountOfColumns < TiczViewModel.TiczSettings.MinimumNumberOfColumns Then amountOfColumns = TiczViewModel.TiczSettings.MinimumNumberOfColumns
        Panel.ItemWidth = e.NewSize.Width / amountOfColumns
        WriteToDebug("Panel Width = ", Panel.ItemWidth)

    End Sub
End Class
