Imports Windows.UI



Public Class CanvasWidthToClipWidthConvertor
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
        Dim width As Integer = CType(value, Integer)
        Return New Rect With {.X = 0, .Y = 0, .Width = 200, .Height = 50}
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException
    End Function
End Class


Public Class DeviceStatusToColorConvertor
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
        Dim isOn As Boolean = CType(value, Boolean)
        If isOn Then
            Return Colors.Red
        Else
            Return Colors.Blue
        End If
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
        Dim ts As TimeSpan = CType(value, TimeSpan)
        Dim dt As New Date
        Return dt.Add(ts)
    End Function
End Class

'Public Class TypeImageToDataTemplateConvertor
'    Implements IValueConverter
'    Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
'        Dim imageType As String = CType(value, String)
'        Select Case imageType
'            Case "lightbulb"
'                Return CType(Application.Current.Resources("lightbulb"), DataTemplate)
'            Case "contact"
'                Return CType(Application.Current.Resources("contact"), DataTemplate)
'            Case "temperature"
'                Return CType(Application.Current.Resources("temperature"), DataTemplate)
'            Case "LogitechMediaServer"
'                Return CType(Application.Current.Resources("music"), DataTemplate)
'            Case "hardware"
'                Return CType(Application.Current.Resources("percentage"), DataTemplate)
'            Case "doorbell"
'                Return CType(Application.Current.Resources("doorbell"), DataTemplate)
'            Case "counter"
'                Return CType(Application.Current.Resources("counter"), DataTemplate)
'            Case "Media"
'                Return CType(Application.Current.Resources("media"), DataTemplate)
'            Case "current"
'                Return CType(Application.Current.Resources("current"), DataTemplate)
'            Case "override_mini"
'                Return CType(Application.Current.Resources("setpoint"), DataTemplate)
'            Case "error"
'                Return CType(Application.Current.Resources("error"), DataTemplate)
'            Case "info"
'                Return CType(Application.Current.Resources("info"), DataTemplate)
'            Case "scene"
'                Return CType(Application.Current.Resources("scene"), DataTemplate)
'            Case "group"
'                Return CType(Application.Current.Resources("group"), DataTemplate)
'            Case "visibility"
'                Return CType(Application.Current.Resources("visibility"), DataTemplate)

'            Case Else
'                Return CType(Application.Current.Resources("unknown"), DataTemplate)

'        End Select
'    End Function

'    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
'        Dim ts As TimeSpan = CType(value, TimeSpan)
'        Dim dt As New Date
'        Return dt.Add(ts)
'    End Function
'End Class