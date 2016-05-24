' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class Menu_ServerSettings
    Inherits UserControl
    Public Property Menu As TiczMenuSettings
    Public ReadOnly Property TiczSettings As TiczSettings
        Get
            Return CType(Application.Current, Application).myViewModel.TiczSettings
        End Get
    End Property
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub(s, e)
                                           Menu = CType(Me.DataContext, TiczMenuSettings)
                                       End Sub

        ' Add any initialization after the InitializeComponent() call.

    End Sub
End Class
