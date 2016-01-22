
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


'Public Class VariableSizedGridView :  GridView
'{
'    Protected override void PrepareContainerForItemOverride(Windows.UI.Xaml.DependencyObject element, Object item)
'    {
'        Try
'        {
'            dynamic gridItem = item;

'            var typeItem = item As CommonType;
'            If (typeItem!= null)
'            {
'                var heightPecentage = (300.0 / typeItem.WbmImage.PixelHeight);
'                var itemWidth = typeItem.WbmImage.PixelWidth * heightPecentage;
'                var columnSpan = Convert.ToInt32(itemWidth / 10.0);


'                If (gridItem!= null)
'                {
'                    element.SetValue(VariableSizedWrapGrid.ItemWidthProperty, itemWidth);
'                    element.SetValue(VariableSizedWrapGrid.ColumnSpanProperty, columnSpan);
'                    element.SetValue(VariableSizedWrapGrid.RowSpanProperty, 1);
'                }
'            }
'        }
'        Catch
'        {
'            element.SetValue(VariableSizedWrapGrid.ItemWidthProperty, 100);
'            element.SetValue(VariableSizedWrapGrid.ColumnSpanProperty, 1);
'            element.SetValue(VariableSizedWrapGrid.RowSpanProperty, 1);
'        }
'        Finally
'        {
'            base.PrepareContainerForItemOverride(element, item);
'        }
'    }