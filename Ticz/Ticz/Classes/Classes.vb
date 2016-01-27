
Imports Microsoft.Xaml.Interactivity

Public Class VariableGrid
    Inherits GridView

    Protected Overrides Sub PrepareContainerForItemOverride(element As DependencyObject, item As Object)
        MyBase.PrepareContainerForItemOverride(element, item)
        Dim tile = TryCast(item, Device)
        If Not tile Is Nothing Then
            Dim griditem = TryCast(element, GridViewItem)
            If Not griditem Is Nothing Then
                VariableSizedWrapGrid.SetColumnSpan(griditem, tile.ColumnSpan)
                VariableSizedWrapGrid.SetRowSpan(griditem, tile.RowSpan)
            End If
        End If
    End Sub
    'PrepareContainerForItemOverride(element, item)
End Class

Public Class OpenMenuFlyoutAction
    Inherits DependencyObject
    Implements IAction
    Private Function IAction_Execute(sender As Object, parameter As Object) As Object Implements IAction.Execute
        Dim senderElement As FrameworkElement = TryCast(sender, FrameworkElement)
        Dim flyoutBase__1 As FlyoutBase = FlyoutBase.GetAttachedFlyout(senderElement)

        flyoutBase__1.ShowAt(senderElement)

        Return Nothing
    End Function
End Class

