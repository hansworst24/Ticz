' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucDevice_Dynamic
    Inherits UserControl

    Private Sub cbSelector_DropDownClosed(sender As Object, e As Object)
        Dim device As Device = TryCast(sender, ComboBox).DataContext
        device.SelectorSelectionChanged.Execute(sender)
    End Sub
End Class
