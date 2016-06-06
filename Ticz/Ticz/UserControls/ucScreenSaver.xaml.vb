' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucScreenSaver
    Inherits UserControl

    Public Property ScreenSaver As ScreenSaverViewModel

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub()
                                           ScreenSaver = TryCast(DataContext, ScreenSaverViewModel)
                                       End Sub
        ' Add any initialization after the InitializeComponent() call.

    End Sub


End Class
