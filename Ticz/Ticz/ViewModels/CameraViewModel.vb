Imports System.Threading
Imports GalaSoft.MvvmLight
Imports Windows.Security.Cryptography.Certificates
Imports Windows.Storage.Streams
Imports Windows.Web.Http
Imports Windows.Web.Http.Filters

Public Class CameraViewModel
    Inherits ViewModelBase

    Private cameraidx As Integer
    Public Property name As String
    Public Property MaxItemHeight As Integer
        Get
            Return _MaxItemHeight
        End Get
        Set(value As Integer)
            _MaxItemHeight = value
            RaisePropertyChanged("MaxItemHeight")
        End Set
    End Property
    Private Property _MaxItemHeight As Integer

    Public Property frame1 As BitmapImage
    Private Property _FrameBytes As Long
    Private Property _TotalFrameBytes As Long
    Private Property _FrameCount As Integer
    Private Property _LastFrameCapture As DateTime
    Public ReadOnly Property FrameSize As String
        Get
            Return String.Format("{0} {1}/frame", Math.Round(_FrameBytes / 1024, 2), "KB")
        End Get
    End Property

    Public ReadOnly Property TotalFrameSize As String
        Get
            Return String.Format("{0} {1}", Math.Round(_TotalFrameBytes / 1024, 2), "KB")
        End Get
    End Property

    Public Property FramesPerSecond As String
        Get
            Return _FramesPerSecond
        End Get
        Set(value As String)
            _FramesPerSecond = value
            RaisePropertyChanged("FramesPerSecond")
        End Set
    End Property
    Private Property _FramesPerSecond As String


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
    Private FPSCounter As Task
    Private fpscts As New CancellationTokenSource
    Private fpsct As CancellationToken
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
        Me.MaxItemHeight = ApplicationView.GetForCurrentView.VisibleBounds.Height - 40
        RefreshDelay = 2000
        AutoRefreshEnabled = False
    End Sub

    Public Async Sub StartRefresh()
        If FrameRefresher Is Nothing OrElse FrameRefresher.IsCompleted Then
            cts = New CancellationTokenSource
            ct = cts.Token
            FrameRefresher = Await Task.Factory.StartNew(Function() PerformFrameRefresh(ct), ct)

            fpscts = New CancellationTokenSource
            fpsct = fpscts.Token
            FPSCounter = Await Task.Factory.StartNew(Function() CountFramesPerSecond(fpsct), fpsct)

        End If
    End Sub


    Public Async Sub StopRefresh()
        If ct.CanBeCanceled Then
            cts.Cancel()
        End If
        If fpsct.CanBeCanceled Then
            fpscts.Cancel()
        End If
        WriteToDebug("CameraViewModel.StopRefresh()", "")
    End Sub

    Public Async Function CountFramesPerSecond(ct As CancellationToken) As Task
        While Not ct.IsCancellationRequested
            Await Task.Delay(1000)
            WriteToDebug("CameraViewModel.CountFramesPerSecond", String.Format("{0} FPS", _FrameCount))

            Await RunOnUIThread(Sub()
                                    FramesPerSecond = String.Format("{0} fps", _FrameCount)
                                End Sub)
            _FrameCount = 0

        End While
    End Function

    Public Async Function PerformFrameRefresh(ct As CancellationToken) As Task
        While Not ct.IsCancellationRequested
            Dim startTime As DateTime = Date.Now
            Await GetFrameFromJPG()
            Dim elapsedMilliseconds As Integer = Date.Now.Subtract(startTime).Milliseconds
            If elapsedMilliseconds < RefreshDelay Then
                Await Task.Delay(RefreshDelay - elapsedMilliseconds)
            End If
        End While
    End Function


    Public Async Function GetFrameFromJPG() As Task
        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
        Dim httpfilter = (New Domoticz).httpfilter
        Using httpfilter

            'If Not vm.TiczSettings.Password = "" AndAlso Not vm.TiczSettings.Username = "" Then
            '    filter.ServerCredential = New Windows.Security.Credentials.PasswordCredential With {.Password = vm.TiczSettings.Password, .UserName = vm.TiczSettings.Username}
            '    Try
            '        'might fail on older W10 versions
            '        filter.CookieUsageBehavior = HttpCookieUsageBehavior.NoCookies
            '    Catch ex As Exception

            '    End Try
            'End If
            'filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.Default
            'filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache

            ''Ignore SSL Certificate issues
            'If vm.TiczSettings.IgnoreSSLErrors Then
            '    filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted)
            '    filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName)
            'Else
            '    filter.IgnorableServerCertificateErrors.Clear()
            'End If
            'filter.AllowUI = False
            'filter.UseProxy = False
            Dim url As String = (New DomoApi).getCamFrame(cameraidx)
            'TODO ADD HTTP FILTER
            Using httpClient As New HttpClient(httpfilter)
                Dim cts As New CancellationTokenSource(1000)
                Dim ct_GrabFrame As CancellationToken = cts.Token
                Dim imageStream As IBuffer
                Try
                    imageStream = Await httpClient.GetBufferAsync(New Uri(url)).AsTask(ct_GrabFrame)
                Catch ex As Exception
                    WriteToDebug("CameraViewModel.GetFrameFromJPG()", "Frame Skipped")
                    Exit Function
                End Try
                _FrameBytes = imageStream.Length
                _TotalFrameBytes += imageStream.Length
                If Not imageStream.Length = 0 Then
                    _FrameCount += 1
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
                                            'Only update framesize and totalframesize once every second or so
                                            If Date.Now.Subtract(_LastFrameCapture).Seconds >= 1 Then
                                                RaisePropertyChanged("FrameSize")
                                                RaisePropertyChanged("TotalFrameSize")
                                                _LastFrameCapture = Date.Now
                                            End If
                                        End Sub)
                End If
            End Using
        End Using
        ' Dim httpClient As New HttpClient()


        'Dim memoryStream = New MemoryStream(contentBytes)
        'Dim bitmap As New System.Windows.Media.Imaging.BitmapImage
        'Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url, 1000)
        'If response.IsSuccessStatusCode Then
        '    Dim imageStream As IBuffer = Await response.Content.ReadAsBufferAsync()
        '    _FrameBytes = imageStream.Length
        '    _TotalFrameBytes += imageStream.Length
        '    If Not imageStream.Length = 0 Then
        '        Await RunOnUIThread(Async Sub()
        '                                Dim newFrame As New BitmapImage
        '                                Using RandomAccessStream As InMemoryRandomAccessStream = New InMemoryRandomAccessStream
        '                                    Await RandomAccessStream.WriteAsync(imageStream)
        '                                    RandomAccessStream.Seek(0)
        '                                    If Not RandomAccessStream.Size = 0 Then
        '                                        Await newFrame.SetSourceAsync(RandomAccessStream)
        '                                    End If
        '                                End Using
        '                                WriteToDebug("CameraViewModel.GetFrameFromJPG()", String.Format("Frame rendered for camera : {0}", name))
        '                                frame1 = newFrame
        '                                RaisePropertyChanged("frame1")
        '                                RaisePropertyChanged("FrameSize")
        '                                RaisePropertyChanged("TotalFrameSize")
        '                            End Sub)
        '    End If
        'End If

    End Function
End Class
