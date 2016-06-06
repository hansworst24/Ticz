Imports System.Threading
Imports GalaSoft.MvvmLight

Public Class ScreenSaverViewModel
    Inherits ViewModelBase

    Public Property ImageWidth As Double
    Public Property ImageHeight As Double
    Public Property XOffset As Integer
        Get
            Return _XOffset
        End Get
        Set(value As Integer)
            _XOffset = value
            RaisePropertyChanged("XOffset")
        End Set
    End Property
    Private Property _XOffset As Integer
    Public Property YOffset As Integer
        Get
            Return _YOffset
        End Get
        Set(value As Integer)
            _YOffset = value
            RaisePropertyChanged("YOffset")
        End Set
    End Property
    Private Property _YOffset As Integer
    Private Property screenBounds As Rect

    Public Property Refresh As Task
    Public Property cts As CancellationTokenSource
    Public Property ct As CancellationToken

    Public Sub New(bounds As Rect)
        ImageWidth = 100
        ImageHeight = 100
        screenBounds = bounds
    End Sub

    Public Async Sub StartRefresh()
        If Refresh Is Nothing OrElse Refresh.IsCompleted Then
            cts = New CancellationTokenSource
            ct = cts.Token
            Refresh = Await Task.Factory.StartNew(Function() RelocateImage(ct), ct)
        End If

    End Sub

    Public Async Sub StopRefresh()
        If Not cts Is Nothing Then
            cts.Cancel()
        End If
    End Sub


    Public Async Function RelocateImage(ct As CancellationToken) As Task
        While Not ct.IsCancellationRequested
            Dim rnd As New Random()
            Dim sWidth As Integer = screenBounds.Width
            Dim sHeight As Integer = screenBounds.Height
            RunOnUIThread(Sub()
                              XOffset = rnd.Next(0, sWidth - ImageWidth)
                              YOffset = rnd.Next(0, sHeight - ImageHeight)

                          End Sub)
            Await Task.Delay(2000)
        End While

    End Function



End Class
