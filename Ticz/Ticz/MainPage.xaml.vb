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
            Await vm.Notify.Update(True, "IP/Port settings not valid", 0)
            Await Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                            Me.Frame.Navigate(GetType(AppSettingsPage))
                                                                                        End Sub)

        Else
            'Set datacontext
            Me.DataContext = vm
            'First Load the (Room) Plans
            Await vm.Notify.Update(False, "connecting...", 0)
            vm.MyRooms.Clear()
            Dim retplan As retvalue = Await vm.MyPlans.Load()
            If retplan.issuccess Then
                'Load all devices
                Await vm.myDevices.Load()

                    For Each plan In vm.MyPlans.result.OrderBy(Function(x) x.Order)
                        Dim devicesForThisRoom = From d In vm.myDevices.result Where d.PlanIDs.Contains(plan.idx) Select d
                        If Not devicesForThisRoom Is Nothing Then
                            Dim scenesForThisRoom = (From d In devicesForThisRoom Where d.Type = "Scene" Or d.Type = "Group" Select d).ToList
                            Dim switchesForThisRoom = (From d In devicesForThisRoom Where d.Type = "Lighting 2" Select d).ToList
                            Dim tempsForThisRoom = (From d In devicesForThisRoom Where d.Type = "Temp" Select d).ToList
                            Dim utilsForThisRoom = (From d In devicesForThisRoom Where d.Type = "General" Select d).ToList
                            Dim dglist As New List(Of Devices)
                            If Not scenesForThisRoom.Count = 0 Then dglist.Add(New Devices With {.title = "Scenes / Groups", .result = scenesForThisRoom.ToObservableCollection()})
                            If Not switchesForThisRoom.Count = 0 Then dglist.Add(New Devices With {.title = "Lights / Switches", .result = switchesForThisRoom.ToObservableCollection()})
                            If Not tempsForThisRoom.Count = 0 Then dglist.Add(New Devices With {.title = "Temp. Sensors", .result = tempsForThisRoom.ToObservableCollection()})
                            If Not utilsForThisRoom.Count = 0 Then dglist.Add(New Devices With {.title = "Utility Sensors", .result = utilsForThisRoom.ToObservableCollection()})
                            vm.MyRooms.Add(New Room With {.RoomName = plan.Name, .DeviceGroups = dglist})
                        End If

                    Next
                    Await vm.Notify.Clear()
                Else
                    Await vm.Notify.Update(True, "connection error", 0)
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
        WriteToDebug("MainPage.GridView_SizeChanged()", "executed")
        Dim gv As GridView = CType(sender, GridView)
        Dim Panel = CType(gv.ItemsPanelRoot, WrapPanel)
        Dim amountOfColumns = Math.Ceiling(gv.ActualWidth / 400)
        If amountOfColumns < vm.TiczSettings.MinimumNumberOfColumns Then amountOfColumns = vm.TiczSettings.MinimumNumberOfColumns
        Panel.ItemWidth = e.NewSize.Width / amountOfColumns

    End Sub
End Class
