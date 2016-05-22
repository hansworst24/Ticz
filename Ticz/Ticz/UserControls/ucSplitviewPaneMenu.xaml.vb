' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucSplitviewPaneMenu
    Inherits UserControl

    Public ReadOnly Property vm As TiczViewModel
        Get
            Return CType(Application.Current, Application).myViewModel
        End Get
    End Property

End Class
