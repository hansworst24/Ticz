' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Ticz.TiczViewModel
Imports Windows.Web.Http
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
            vm.Notify.Update(True, 2, "IP/Port settings not valid")
            Await Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                            Me.Frame.Navigate(GetType(AppSettingsPage))
                                                                                        End Sub)

        Else
            'First Load the (Room) Plans
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
            End If
            'Set the datacontext
            Me.DataContext = vm
        End If

    End Sub

    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)

    End Sub
End Class
