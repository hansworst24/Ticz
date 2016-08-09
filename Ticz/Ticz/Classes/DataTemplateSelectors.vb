Public Class VariableViewModelItemDataTemplateSelector
    Inherits DataTemplateSelector
    Protected Overrides Function SelectTemplateCore(ByVal item As Object,
                    ByVal container As DependencyObject) As DataTemplate

        Dim element As FrameworkElement = TryCast(container, FrameworkElement)

        If element IsNot Nothing AndAlso item IsNot Nothing AndAlso TypeOf item Is VariableViewModel Then
            Dim VariableItem As VariableViewModel = TryCast(item, VariableViewModel)
            Select Case VariableItem.Type
                Case "0" : Return TryCast(CType(Application.Current, Application).Resources("VariableIntegerTemplate"), DataTemplate)
                Case "1" : Return TryCast(CType(Application.Current, Application).Resources("VariableFloatTemplate"), DataTemplate)
                Case "2" : Return TryCast(CType(Application.Current, Application).Resources("VariableStringTemplate"), DataTemplate)
                Case "3" : Return TryCast(CType(Application.Current, Application).Resources("VariableDateTemplate"), DataTemplate)
                Case "4" : Return TryCast(CType(Application.Current, Application).Resources("VariableTimeTemplate"), DataTemplate)
            End Select
        End If
        Return Nothing
    End Function
End Class

