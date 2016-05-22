' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucDevice_Password
    Inherits UserControl

    Public Property Device As DeviceViewModel

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub(s, e)
                                           'Me.Bindings.Update()
                                           Device = CType(Me.DataContext, DeviceViewModel)
                                       End Sub
        ' Add any initialization after the InitializeComponent() call.

    End Sub
End Class
