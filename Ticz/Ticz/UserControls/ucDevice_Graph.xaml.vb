' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Imports WinRTXamlToolkit.Controls.DataVisualization.Charting
Public NotInheritable Class ucDevice_Graph
    Inherits UserControl

    Private Sub ContentControl_DataContextChanged(sender As FrameworkElement, args As DataContextChangedEventArgs)
        WriteToDebug("ContentControl_DataContextChanged", "---------------------")
    End Sub
End Class
