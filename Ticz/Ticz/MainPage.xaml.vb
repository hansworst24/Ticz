' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Ticz.TiczViewModel
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

        'Redirect to Settings Page if IP/Port are not valid
        If Not vm.TiczSettings.ContainsValidIPDetails Then
            vm.Notify.Update(True, "IP/Port settings not valid", 0)
            Await Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                            Me.Frame.Navigate(GetType(AppSettingsPage))
                                                                                        End Sub)

        Else
            'First Load the (Room) Plans
            vm.Notify.Update(False, "connecting...", 0)
            Dim retplan As retvalue = Await vm.MyPlans.Load()
            If retplan.issuccess Then
                'Load the devices
                Dim retDevice As retvalue = Await vm.myDevices.Load()
                If retDevice.issuccess Then
                    'Load the favourites
                    vm.myFavourites.result.Clear()
                    For Each device In vm.myDevices.result.Where(Function(x) x.Favorite = 1)
                        vm.myFavourites.result.Add(device)
                    Next
                End If
                'Create the room/floor plans
                vm.MyDeviceGroups.Clear()
                vm.MyDeviceGroups.Add(New DeviceGroup With {.DeviceGroupName = "Favourites", .Devices = vm.myFavourites.result})
                vm.MyDeviceGroups.Add(New DeviceGroup With {.DeviceGroupName = "All Devices", .Devices = vm.myDevices.result})
                For Each plan In vm.MyPlans.result.OrderBy(Function(x) x.Order)
                    vm.MyDeviceGroups.Add(New DeviceGroup With {.DeviceGroupName = plan.Name, .Devices = (From d In vm.myDevices.result Where d.PlanIDs.Contains(plan.idx) Select d).ToObservableCollection()})
                Next
                vm.Notify.Clear()
            Else
                vm.Notify.Update(True, "connection error", 0)
            End If
            'Set the datacontext
            Me.DataContext = vm
        End If

    End Sub

    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)

    End Sub

    Private Sub AppBar_SizeChanged(sender As Object, e As SizeChangedEventArgs)

    End Sub

    Private Sub AppBar_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim a As AppBar = CType(sender, AppBar)
        'If a.IsOpen Then vm.NotifiCationMargin = 36 Else vm.NotificationMargin = 0
        WriteToDebug("AppBar.AppBar_Tapped()", a.ActualHeight)

    End Sub

    Private Sub GridView_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        Dim gv As GridView = CType(sender, GridView)
        Dim Panel = CType(gv.ItemsPanelRoot, WrapPanel)
        Dim amountOfColumns = Math.Ceiling(gv.ActualWidth / 400)
        If amountOfColumns < vm.TiczSettings.MinimumNumberOfColumns Then amountOfColumns = vm.TiczSettings.MinimumNumberOfColumns
        Panel.ItemWidth = e.NewSize.Width / amountOfColumns

    End Sub
End Class
