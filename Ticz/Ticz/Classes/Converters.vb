Namespace converters

    Public Class IntToObjectConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
            Return value
            'Throw New NotImplementedException()
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
            Return value
        End Function
    End Class

    Public Class RoomConfigToStringConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
            Return value
            'Throw New NotImplementedException()
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
            Return value
        End Function
    End Class

    Public Class RoomViewDataTemplateSelector
        Inherits DataTemplateSelector

        Public Overloads Function SelectTemplate(ByVal item As Object,
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

End Namespace