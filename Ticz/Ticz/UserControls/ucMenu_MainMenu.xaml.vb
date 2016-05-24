' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucMenu_MainMenu
    Inherits UserControl

    Public ReadOnly Property vm As TiczViewModel
        Get
            Return CType(Application.Current, Application).myViewModel
        End Get
    End Property
    Public ReadOnly Property TiczSettings As TiczSettings
        Get
            Return CType(Application.Current, Application).myViewModel.TiczSettings
        End Get
    End Property

    Public Property Menu As TiczMenuSettings

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub(s, e)
                                           If Not TryCast(Me.DataContext, TiczMenuSettings) Is Nothing Then
                                               Menu = CType(Me.DataContext, TiczMenuSettings)
                                           End If

                                       End Sub
        ' Add any initialization after the InitializeComponent() call.

    End Sub

End Class
