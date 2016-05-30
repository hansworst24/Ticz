Imports System.Threading
Imports GalaSoft.MvvmLight
Imports Windows.Storage.Streams
Imports Windows.Web.Http

Public Class CameraViewModel
    Inherits ViewModelBase

    Private cameraidx As Integer
    Public Property name As String
    Public Property frame1 As BitmapImage

    Public Property AutoRefreshEnabled As Boolean
        Get
            Return _AutoRefreshEnabled
        End Get
        Set(value As Boolean)
            If value = True Then
                'turn on auto refresh
                StartRefresh()
            Else
                'turn off auto refresh
                StopRefresh()
            End If
        End Set
    End Property
    Private Property _AutoRefreshEnabled As Boolean
    Public Property RefreshDelay As Double
        Get
            Return _RefreshDelay
        End Get
        Set(value As Double)
            _RefreshDelay = value
            RaisePropertyChanged("RefreshDelayText")
        End Set
    End Property
    Private Property _RefreshDelay As Double
    Public ReadOnly Property RefreshDelayText As String
        Get
            Return String.Format("{0}ms", RefreshDelay)
        End Get
    End Property

    Public Property FrameRefresher As Task
    Public cts As New CancellationTokenSource
    Public ct As CancellationToken

    Public ReadOnly Property imgurl As String
        Get
            Return (New DomoApi).getCamFrame(cameraidx)
        End Get
    End Property

    Public Sub New(idx As Integer, name As String)
        cameraidx = idx
        Me.name = name
        RefreshDelay = 2000
        AutoRefreshEnabled = False
    End Sub

    Public Async Sub StartRefresh()
        If FrameRefresher Is Nothing OrElse FrameRefresher.IsCompleted Then
            cts = New CancellationTokenSource
            ct = cts.Token
            FrameRefresher = Await Task.Factory.StartNew(Function() PerformFrameRefresh(ct), ct)
        End If
    End Sub


    Public Async Sub StopRefresh()
        If ct.CanBeCanceled Then
            cts.Cancel()
        End If
        WriteToDebug("CameraViewModel.StopRefresh()", "")
    End Sub

    Public Async Function PerformFrameRefresh(ct As CancellationToken) As Task
        While Not ct.IsCancellationRequested
            Await GetFrameFromJPG()
            Await Task.Delay(RefreshDelay)
        End While
    End Function


    Public Async Function GetFrameFromJPG() As Task
        Dim url As String = (New DomoApi).getCamFrame(cameraidx)
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url, 1000)
        If response.IsSuccessStatusCode Then
            Dim imageStream As IBuffer = Await response.Content.ReadAsBufferAsync()
            If Not imageStream.Length = 0 Then
                Await RunOnUIThread(Async Sub()
                                        Dim newFrame As New BitmapImage
                                        Using RandomAccessStream As InMemoryRandomAccessStream = New InMemoryRandomAccessStream
                                            Await RandomAccessStream.WriteAsync(imageStream)
                                            RandomAccessStream.Seek(0)
                                            If Not RandomAccessStream.Size = 0 Then
                                                Await newFrame.SetSourceAsync(RandomAccessStream)
                                            End If
                                        End Using
                                        WriteToDebug("CameraViewModel.GetFrameFromJPG()", String.Format("Frame rendered for camera : {0}", name))
                                        frame1 = newFrame
                                        RaisePropertyChanged("frame1")
                                    End Sub)
            End If
        End If

    End Function
End Class
