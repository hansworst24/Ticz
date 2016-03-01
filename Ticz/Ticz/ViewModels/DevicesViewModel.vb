Public Class DevicesViewModel
    Inherits ObservableCollection(Of DeviceViewModel)

    Public Sub New()

    End Sub

    Public Sub New(name As String, items As IEnumerable(Of DeviceViewModel))
        Me.Key = name
        For Each item As DeviceViewModel In items
            Me.Add(item)
        Next
    End Sub

    Public Sub New(name As String)
        Me.Key = name
    End Sub

    Public Property Key As String
        Get
            Return m_Key
        End Get
        Set(value As String)
            m_Key = value
        End Set
    End Property
    Private m_Key As String



End Class