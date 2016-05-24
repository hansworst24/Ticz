' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucDeviceList_Dashboard
    Inherits UserControl
    Public Property RoomViewModel As RoomViewModel

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub(s, e)
                                           WriteToDebug("ucDeviceList_Dashboard - DataContextChanged", "")
                                           'RoomViewModel = CType(DataContext, TiczViewModel).currentRoom
                                           'If Not TryCast(DataContext, RoomViewModel) Is Nothing Then
                                           RoomViewModel = CType(DataContext, RoomViewModel)
                                           If Not RoomViewModel Is Nothing AndAlso Not RoomViewModel.Devices Is Nothing Then
                                               WriteToDebug(String.Format("{0} devices, Itemwidth {1}", RoomViewModel.Devices.Count, RoomViewModel.ItemWidth), "")
                                           End If

                                           'End If

                                       End Sub

        ' Add any initialization after the InitializeComponent() call.
    End Sub
End Class
