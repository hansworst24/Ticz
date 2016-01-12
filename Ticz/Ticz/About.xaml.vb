' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class AboutPage
    Inherits Page


    Dim app As App = CType(Application.Current, App)

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        Me.DataContext = app.myViewModel
    End Sub

    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)

    End Sub


End Class
