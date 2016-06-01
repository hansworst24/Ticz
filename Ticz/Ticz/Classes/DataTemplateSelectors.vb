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


Public Class RoomViewDataTemplateSelector
        Inherits DataTemplateSelector

    Protected Overrides Function SelectTemplateCore(ByVal item As Object,
                    ByVal container As DependencyObject) As DataTemplate

        Return CType(CType(Application.Current, Application).Resources("IconViewDataTemplate"), DataTemplate)
        'Dim numberStr As String = item '

        'If Not (numberStr Is Nothing) Then
        '    Dim num As Integer
        '    Dim win As Window = Application.Current.MainWindow

        '    Try
        '        num = Convert.ToInt32(numberStr)
        '    Catch
        '        Return Nothing
        '    End Try

        '    ' Select one of the DataTemplate objects, based on the 
        '    ' value of the selected item in the ComboBox.
        '    If num < 5 Then
        '        Return win.FindResource("numberTemplate") '

        '    Else
        '        Return win.FindResource("largeNumberTemplate") '
        '    End If
        'End If

        'Return Nothing

    End Function 'SelectTemplate
End Class 'NumderDataTemplateSelector
