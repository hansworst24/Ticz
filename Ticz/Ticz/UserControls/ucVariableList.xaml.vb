' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucVariableList
    Inherits UserControl

    'Public Property Variables As VariableListViewModel

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        'AddHandler DataContextChanged, Sub(s, e)
        '                                   Variables = CType(DataContext, VariableListViewModel)
        '                                   'Me.Bindings.Update()
        '                               End Sub

        ' Add any initialization after the InitializeComponent() call.

    End Sub

End Class
