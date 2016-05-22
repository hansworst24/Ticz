' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucDevice_Dynamic
    Inherits UserControl

    Public Property Device As DeviceViewModel
        Get
            If Not Me.DataContext Is Nothing Then
                If Me.DataContext.GetType Is GetType(DeviceViewModel) Then
                    Return CType(Me.DataContext, DeviceViewModel)
                End If
                Return CType(Me.DataContext, LogitechMediaServerDeviceViewModel)

            Else
                Return CType(Me.DataContext, LogitechMediaServerDeviceViewModel)
            End If
        End Get
        Set(value As DeviceViewModel)

        End Set
    End Property
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        AddHandler DataContextChanged, Sub(s, e)
                                           If Not DataContext Is Nothing Then
                                               WriteToDebug("DataContext Changed for : ", (CType(DataContext, DeviceViewModel).Name))
                                           End If

                                           'Device = CType(DataContext, DeviceViewModel)
                                           Me.Bindings.Update()
                                       End Sub
    End Sub
End Class
