' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Imports System.Threading
Imports Syncfusion.UI.Xaml.Controls.Media
Imports Windows.UI

Public NotInheritable Class ucRGBColorPicker
    Inherits UserControl

    Private tsk As Task
    Private lastrun As DateTime
    Private cts As New CancellationTokenSource
    Private ct As CancellationToken = cts.Token

    Public Property MySelectedColor As Color

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        MySelectedColor = Colors.DarkOrange
        ' Add any initialization after the InitializeComponent() call.

    End Sub


    Private Async Sub SfColorPicker_SelectedColorChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        WriteToDebug("ucRGBColorPicker.SelectedColorChanged", "executed")
        Dim zut As ColorPickerViewModel = Me.DataContext
        Dim device As DeviceViewModel = zut.device
        Dim selectedColor As Color = CType(sender, SfColorPicker).SelectedColor
        Dim selectedColorString = selectedColor.ToString.Substring(3)

        If tsk Is Nothing Then
            tsk = Await Task.Factory.StartNew(Function() WaitAndSetRGB(device, selectedColorString, ct))
        Else
            If Not tsk.IsCompleted Or tsk.IsCanceled Then
                cts.Cancel()
                cts.Dispose()
                cts = New CancellationTokenSource
                ct = cts.Token
            End If
            tsk = Await Task.Factory.StartNew(Function() WaitAndSetRGB(device, selectedColorString, ct))
        End If

    End Sub


    Public Async Sub SetRGBOnDevice()
        Dim zut As ColorPickerViewModel = Me.DataContext
        Dim device As DeviceViewModel = zut.device
        Await RunOnUIThread(Async Sub()
                                Await device.SetRGBValues(MySelectedColor.ToString.Substring(3))
                            End Sub)
    End Sub


    Public Async Function WaitAndSetRGB(device As DeviceViewModel, hex As String, ct As CancellationToken) As Task
        WriteToDebug("ucRGBColorPicker.WaitAndSetRGB", "executed")
        Dim TimeToWait As Integer = 500
        Dim TimePassed As Integer = 0
            While TimePassed < TimeToWait
                Await Task.Delay(100)
                TimePassed += 100
                If ct.IsCancellationRequested Then Exit While
            End While
        If ct.IsCancellationRequested Then
            WriteToDebug("ucRGBColorPicker.WaitAndSetRGB()", "cancellation requested")
        Else
            Await RunOnUIThread(Async Sub()
                                    Await device.SetRGBValues(hex)
                                End Sub)
        End If


    End Function
End Class
