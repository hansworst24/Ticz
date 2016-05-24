' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucDevice_Dynamic
    Inherits UserControl

    Public Property Device As DeviceViewModel
    Public ReadOnly Property Room As RoomViewModel
        Get
            Return CType(Application.Current, Application).myViewModel.currentRoom
        End Get
    End Property
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub(s, e)
                                           Device = CType(DataContext, DeviceViewModel)
                                           Me.Bindings.Update()
                                       End Sub
    End Sub
End Class
