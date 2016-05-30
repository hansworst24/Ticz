' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucCameraList
    Inherits UserControl
    Public Property Cameras As CameraListViewModel

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, (Sub(s, e)
                                            Cameras = CType(DataContext, CameraListViewModel)
                                        End Sub)
        ' Add any initialization after the InitializeComponent() call.

    End Sub
End Class
