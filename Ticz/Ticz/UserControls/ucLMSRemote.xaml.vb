' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucLMSRemote
    Inherits UserControl

    Public Property Player As LogitechMediaServerDeviceViewModel
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub(s, e)
                                           If Not DataContext Is Nothing Then
                                               WriteToDebug("--------DataContext Changed for : ", (CType(DataContext, LogitechMediaServerDeviceViewModel).Name))
                                           End If

                                           Player = CType(DataContext, LogitechMediaServerDeviceViewModel)
                                       End Sub
    End Sub

End Class
