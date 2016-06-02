' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucKODIRemote
    Inherits UserControl

    Public Property Player As KODIDeviceViewModel
        Get
            Return CType(Me.DataContext, KODIDeviceViewModel)
        End Get
        Set(value As KODIDeviceViewModel)

        End Set
    End Property

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub(s, e)
                                           Me.Bindings.Update()
                                       End Sub
    End Sub

End Class
