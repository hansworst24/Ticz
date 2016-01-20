' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
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
    Dim vm As TiczViewModel = app.myViewModel

    Protected Overrides Async Sub OnNavigatedTo(e As NavigationEventArgs)
        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        If rootFrame.CanGoBack Then
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
        Else
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        End If
        'Redirect to Settings Page if IP/Port are not valid
        If Not vm.TiczSettings.ContainsValidIPDetails Then
            Await vm.Notify.Update(True, "IP/Port settings not valid", 0)
            Await Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                            Me.Frame.Navigate(GetType(AppSettingsPage))
                                                                                        End Sub)

        Else
            'Set datacontext to viewmodel
            Me.DataContext = vm
            'First Load the (Room) Plans
            Await vm.Notify.Update(False, "connecting...", 0)
            vm.MyRooms.Clear()
            Await vm.Notify.Update(False, "loading rooms...", 0)
            Dim retplan As retvalue = Await vm.MyPlans.Load()
            If retplan.issuccess Then
                'Load all devices
                Await vm.Notify.Update(False, "loading devices...", 0)
                Await vm.myDevices.Load()
                Await vm.Notify.Update(False, "creating rooms...", 0)

                'Create the rooms
                For Each plan In vm.MyPlans.result.OrderBy(Function(x) x.Order)
                    Dim newRoom As New Room With {.RoomIDX = plan.idx, .RoomName = plan.Name}
                    'In order to get the right order for devices in each Room, we need to ask Domoticz which devices it has in each room.
                    'For each received device, we take the IDX and take the equivalent Device out of the main myDevices List
                    Dim newRoomDevices = Await newRoom.LoadDevicesForRoom()
                    Dim newRoomDevicesToAdd As New List(Of Device)

                    For Each d In newRoomDevices
                        Dim dev As Device = (From n In vm.myDevices.result Where n.idx = d.idx And n.Name = d.Name Select n).FirstOrDefault()
                        If Not dev Is Nothing Then
                            dev.Initialize()
                            newRoomDevicesToAdd.Add(dev)
                        End If
                    Next
                    'Construct the DeviceGroups 
                    newRoom.DeviceGroups = ConstructDeviceGroups(newRoomDevicesToAdd)
                    If newRoom.DeviceGroups.Count > 0 Then
                        'and add the Room
                        vm.MyRooms.Add(newRoom)
                    End If
                Next


                'Only show Favourites when there isn't a test-room "Ticz" created
                If Not vm.MyPlans.result.Any(Function(x) x.Name = "Ticz") Then
                    If vm.TiczSettings.ShowFavourites Then
                        'Construct a first Room in which we'll show all favourite devices
                        Dim favs = From d In vm.myDevices.result Where d.Favorite = 1 Select d
                        If Not favs Is Nothing Then
                            vm.MyRooms.Insert(0, (New Room With {.RoomName = "Favourites", .DeviceGroups = ConstructDeviceGroups(favs)}))
                        End If
                    End If
                End If

                'Only show All Devices when there isn't a test-room "Ticz" created
                If Not vm.MyPlans.result.Any(Function(x) x.Name = "Ticz") Then
                    If vm.TiczSettings.ShowAllDevices Then
                        'Construct a last Room in which we'll show all devices
                        Dim alldevs = From d In vm.myDevices.result Select d
                        If Not alldevs Is Nothing Then
                            vm.MyRooms.Add(New Room With {.RoomName = "All Devices", .DeviceGroups = ConstructDeviceGroups(alldevs)})
                        End If
                    End If
                End If
                vm.Notify.Clear()
            Else
                Await vm.Notify.Update(True, "connection error", 0)
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
        If amountOfColumns < vm.TiczSettings.MinimumNumberOfColumns Then amountOfColumns = vm.TiczSettings.MinimumNumberOfColumns
        Panel.ItemWidth = e.NewSize.Width / amountOfColumns
        WriteToDebug("Panel Width = ", Panel.ItemWidth)

    End Sub
End Class
