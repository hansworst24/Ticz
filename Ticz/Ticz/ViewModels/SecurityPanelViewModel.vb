Imports System.Threading
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports GalaSoft.MvvmLight.Threading
Imports Newtonsoft.Json
Imports Windows.Security.Cryptography
Imports Windows.Security.Cryptography.Core
Imports Windows.Storage.Streams
Imports Windows.Web.Http


Public Class SecurityPanelViewModel
    Inherits ViewModelBase

    Public Event PlayDigitSoundRequested As EventHandler
    Public Event PlayArmRequested As EventHandler
    Public Event PlayDisArmRequested As EventHandler
    Public Event PlayWrongCodeRequested As EventHandler

    Private CountDownTask As Task
    Private cts As CancellationTokenSource
    Private ct As CancellationToken



    Public Property TimestampLastSet As DateTime


    Public Property IsFadingIn As Boolean
        Get
            Return _IsFadingIn
        End Get
        Set(value As Boolean)
            _IsFadingIn = value
            RaisePropertyChanged("IsFadingIn")
        End Set
    End Property
    Private Property _IsFadingIn As Boolean

    Public Property DisplayText As String
        Get
            Return _DisplayText
        End Get
        Set(value As String)
            _DisplayText = value
            DispatcherHelper.CheckBeginInvokeOnUI(Sub()
                                                      RaisePropertyChanged("DisplayText")
                                                  End Sub)
        End Set
    End Property
    Private Property _DisplayText As String

    Public Property CodeInput As String
        Get
            Return _CodeInput
        End Get
        Set(value As String)
            _CodeInput = value
        End Set
    End Property
    Private Property _CodeInput As String

    Public Property CodeHash As String

    Public Property CurrentArmState As String

    'Public ReadOnly Property DigitKeyPressedSound As Uri
    '    Get
    '        Return New Uri("msappx://Media/key.mp3")
    '    End Get
    'End Property

    'Public ReadOnly Property WrongCodeSound As Uri
    '    Get
    '        Return New Uri("msappx://Media/wrongcode.mp3")
    '    End Get
    'End Property
    'Public ReadOnly Property DisarmSound As Uri
    '    Get
    '        Return New Uri("msappx://Media/disarm.mp3")
    '    End Get
    'End Property
    'Public ReadOnly Property ArmSound As Uri
    '    Get
    '        Return New Uri("msappx://Media/arm.mp3")
    '    End Get
    'End Property

    Public Property AudioFile As String
        Get
            Return _AudioFile
        End Get
        Set(value As String)
            _AudioFile = value
            DispatcherHelper.CheckBeginInvokeOnUI(Sub()
                                                      RaisePropertyChanged("AudioFile")
                                                  End Sub)
        End Set
    End Property
    Private Property _AudioFile As String


    Public Async Function StopCountDown() As Task
        If ct.CanBeCanceled Then
            cts.Cancel()
        End If
    End Function



    Public Async Function StartCountDown() As Task
        Dim app As Application = CType(Application.Current, Application)
        Await app.myViewModel.DomoSettings.Load()
        If app.myViewModel.DomoSettings.SecOnDelay > 0 Then
            If CountDownTask Is Nothing OrElse CountDownTask.IsCompleted Then
                cts = New CancellationTokenSource
                ct = cts.Token
                CountDownTask = Await Task.Factory.StartNew(Function() PerformCountDown(ct), ct)
            End If
        Else
            Await RunOnUIThread(Sub()
                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayArmRequested(Me, EventArgs.Empty)
                                    DisplayText = CurrentArmState
                                End Sub)
        End If
    End Function

    Public Async Function PerformCountDown(ct As CancellationToken) As Task
        Dim app As Application = CType(Application.Current, Application)
        For i As Integer = 0 To app.myViewModel.DomoSettings.SecOnDelay Step 1
            If CodeInput = "" Then
                DisplayText = String.Format("ARM DELAY : {0}", app.myViewModel.DomoSettings.SecOnDelay - i)
                Await RunOnUIThread(Sub()
                                        If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDigitSoundRequested(Me, EventArgs.Empty)
                                    End Sub)

            End If
            'Wait for 1 seconds in blocks of 250ms in order to respond to cancel requests in the meantime
            For j As Integer = 0 To 3
                Task.Delay(250).Wait()
                If ct.IsCancellationRequested Then Exit Function
            Next
            'When phones suspend, this task gets suspended as well. So we need to build in checks to verify if during suspend the
            'Security Panel Delay is finished, or if the timer should be re-tuned to the actual amount of seconds remaining
            If Date.Now > TimestampLastSet.AddSeconds(app.myViewModel.DomoSettings.SecOnDelay) Then Exit For
            ' For after app resume (i.e. phones). Check if during suspend the timer has reduced, if so 
            If Date.Now < TimestampLastSet.AddSeconds(app.myViewModel.DomoSettings.SecOnDelay) And
            TimestampLastSet.AddSeconds(i + 1) < Date.Now Then
                Dim secDifference As Integer
                secDifference = (Date.Now - TimestampLastSet.AddSeconds(i + 1)).Seconds
                'If time has drifted more than a second, retune
                If secDifference > 1 Then i += secDifference
            End If
            If ct.IsCancellationRequested Then Exit Function
        Next
        CodeInput = ""
        DisplayText = CurrentArmState
        Await RunOnUIThread(Sub()
                                If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayArmRequested(Me, EventArgs.Empty)
                            End Sub)
    End Function

    Public ReadOnly Property DisarmPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_DISARM)
                                        CodeInput = ""
                                        Dim app As Application = CType(Application.Current, Application)
                                        If ret.issuccess Then
                                            Await StopCountDown()
                                            CurrentArmState = "DISARMED"
                                            DisplayText = CurrentArmState

                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDisArmRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                            Await Task.Delay(2000)
                                            If CodeInput = "" Then DisplayText = CurrentArmState
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ArmHomePressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_ARMHOME)
                                        Dim app As Application = CType(Application.Current, Application)
                                        CodeInput = ""
                                        If ret.issuccess Then
                                            CurrentArmState = "ARM HOME"
                                            TimestampLastSet = Date.Now()
                                            Await StartCountDown()
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                            Await Task.Delay(2000)
                                            If CodeInput = "" Then DisplayText = CurrentArmState
                                        End If
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property ArmAwayPressedCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_ARMAWAY)
                                        Dim app As Application = CType(Application.Current, Application)
                                        CodeInput = ""
                                        If ret.issuccess Then
                                            CurrentArmState = "ARM AWAY"
                                            TimestampLastSet = Date.Now()
                                            Await StartCountDown()
                                        Else
                                            DisplayText = ret.err
                                            Await RunOnUIThread(Sub()
                                                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
                                                                End Sub)
                                            Await Task.Delay(2000)
                                            If CodeInput = "" Then DisplayText = CurrentArmState
                                        End If
                                    End Sub)
        End Get
    End Property

    Public Sub CreateSecurityHash()
        Dim codeBuffer As IBuffer = CryptographicBuffer.ConvertStringToBinary(CodeInput, BinaryStringEncoding.Utf8)
        Dim alg As HashAlgorithmProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5)
        Dim buffHash As IBuffer = alg.HashData(codeBuffer)
        If Not buffHash.Length = alg.HashLength Then
            Throw New Exception("There was an Error creating the hash")
        Else
            CodeHash = CryptographicBuffer.EncodeToHexString(buffHash)
            WriteToDebug("SecurityPanel.CreateSecurityHash()", String.Format("Created a MD5 hash from {0} : {1}", CodeInput, CodeHash))
        End If

    End Sub

    Public Async Sub DisarmPressed()
        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_DISARM)
        CodeInput = ""
        Dim app As Application = CType(Application.Current, Application)
        If ret.issuccess Then
            Await StopCountDown()
            CurrentArmState = "DISARMED"
            DisplayText = CurrentArmState
            If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDisArmRequested(Me, EventArgs.Empty)
        Else
            DisplayText = ret.err
            If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
            Await Task.Delay(2000)
            If CodeInput = "" Then DisplayText = CurrentArmState
        End If
    End Sub

    Public Async Sub ArmHomePressed()
        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_ARMHOME)
        Dim app As Application = CType(Application.Current, Application)
        CodeInput = ""
        If ret.issuccess Then
            CurrentArmState = "ARM HOME"
            TimestampLastSet = Date.Now()
            Await StartCountDown()
        Else
            DisplayText = ret.err
            If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
            Await Task.Delay(2000)
            If CodeInput = "" Then DisplayText = CurrentArmState
        End If
    End Sub

    Public Async Sub ArmAwayPressed()
        Dim ret As retvalue = Await SetSecurityStatus(Constants.SECPANEL.SEC_ARMAWAY)
        Dim app As Application = CType(Application.Current, Application)
        CodeInput = ""
        If ret.issuccess Then
            CurrentArmState = "ARM AWAY"
            TimestampLastSet = Date.Now()
            Await StartCountDown()
        Else
            DisplayText = ret.err
            If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayWrongCodeRequested(Me, EventArgs.Empty)
            Await Task.Delay(2000)
            If CodeInput = "" Then DisplayText = CurrentArmState
        End If
    End Sub

    Public Async Sub CancelPressed()
        'Clear the contents of the Sec Panel Display and restore the current arm state when digits were pressed.
        'If not digits were pressed, remove the secpanel from view
        Dim app As Application = CType(Application.Current, Application)
        If Not CodeInput = "" Then
            CodeInput = ""
            DisplayText = CurrentArmState
        End If
        app.myViewModel.CurrentContentDialog.Hide()
    End Sub

    Public Async Sub DigitPressed(s As Object, e As RoutedEventArgs)
        WriteToDebug("SecurityPanelViewModel.ButtonPressed()", "executed")
        Dim btn As Button = TryCast(e.OriginalSource, Button)
        If Not btn Is Nothing Then
            Await RunOnUIThread(Sub()
                                    Dim app As Application = CType(Application.Current, Application)
                                    If app.myViewModel.TiczSettings.PlaySecPanelSFX Then RaiseEvent PlayDigitSoundRequested(Me, EventArgs.Empty)
                                End Sub)
            Dim digit As Integer = btn.Content
            CodeInput = If(CodeInput = "", digit, CodeInput & digit)
            DisplayText = ""
            For Each d In CodeInput
                DisplayText += "#"
            Next
        End If
    End Sub

    Public Async Function SetSecurityStatus(status As Integer) As Task(Of retvalue)
        CreateSecurityHash()
        Dim ret As New retvalue
        Dim url As String = (New DomoApi).setSecurityStatus(status, CodeHash)
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim result As Domoticz.Response
            Try
                result = JsonConvert.DeserializeObject(Of Domoticz.Response)(body)
                If result.status = "OK" Then
                    ret.issuccess = 1
                Else
                    ret.issuccess = 0
                    ret.err = result.message
                End If
                result = Nothing
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing security response"
            End Try
        Else
            'Await TiczViewModel.Notify.Update(True, String.Format("Error setting Security Status ({0})", response.ReasonPhrase), 0)
        End If
        Return ret
    End Function

    Public Async Function GetSecurityStatus() As Task(Of retvalue)
        Dim ret As New retvalue
        Dim url As String = (New DomoApi).getSecurityStatus()
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim result As Domoticz.Response
            Try
                result = JsonConvert.DeserializeObject(Of Domoticz.Response)(body)
                If result.status = "OK" Then
                    If Not CountDownTask Is Nothing Then WriteToDebug(CountDownTask.Status.ToString, "")
                    Select Case result.secstatus
                        'Update the current ARMSTATE, but only update the Display when the user isn't busy with entering a PIN OR when a Countdown is running
                        Case Constants.SECPANEL.SEC_ARMAWAY
                            CurrentArmState = Constants.SECPANEL.SEC_ARMAWAY_STATUS.ToUpper
                            If CodeInput = "" And (CountDownTask Is Nothing OrElse CountDownTask.IsCompleted) Then DisplayText = Constants.SECPANEL.SEC_ARMAWAY_STATUS.ToUpper
                        Case Constants.SECPANEL.SEC_ARMHOME
                            CurrentArmState = Constants.SECPANEL.SEC_ARMHOME_STATUS.ToUpper
                            If CodeInput = "" And (CountDownTask Is Nothing OrElse CountDownTask.IsCompleted) Then DisplayText = Constants.SECPANEL.SEC_ARMHOME_STATUS.ToUpper
                        Case Constants.SECPANEL.SEC_DISARM
                            CurrentArmState = Constants.SECPANEL.SEC_DISARM_STATUS.ToUpper
                            If CodeInput = "" And (CountDownTask Is Nothing OrElse CountDownTask.IsCompleted) Then DisplayText = Constants.SECPANEL.SEC_DISARM_STATUS.ToUpper
                    End Select
                    ret.issuccess = 1
                Else
                    ret.issuccess = 0
                    ret.err = result.message

                End If
                result = Nothing
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing security response"
            End Try
        Else
            'Await TiczViewModel.Notify.Update(True, String.Format("Error getting Security Status ({0})", response.ReasonPhrase), 0)
        End If
        Return ret
    End Function


    Public Sub New()
    End Sub
End Class