Imports System.Threading

Public Class IdleTimerViewModel
    Private SecondsIdle As Integer
    Private currentIdleTime As Integer = 0
    Private IdleCounter As Task
    Private cts As New CancellationTokenSource
    Private ct As New CancellationToken

    Public Sub New(idleTimeBeforeAction As Integer)
        'Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
        'If vm.TiczSettings.ScreenSaverActive Then StartCounter()
    End Sub

    Public Async Function Count(ct As CancellationToken) As Task
        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
        While Not ct.IsCancellationRequested
            Await Task.Delay(1000)
            If vm.TiczSettings.ScreenSaverActive Then
                If currentIdleTime < vm.TiczSettings.IdleTimeBeforeScreenSaver Then
                    currentIdleTime += 1
                    'WriteToDebug("IdleTimerViewModel.StartCounter()", String.Format("currentIdleTime = {0}", currentIdleTime))
                    If currentIdleTime = vm.TiczSettings.IdleTimeBeforeScreenSaver Then
                        RunOnUIThread(Sub()
                                          vm.ShowScreenSaver()
                                      End Sub)
                    End If

                End If
            Else
                If currentIdleTime > 0 Then currentIdleTime = 0
            End If
        End While
    End Function

    Public Sub ResetCounter()
        currentIdleTime = 0
    End Sub

    Public Async Sub StartCounter()
        StopCounter()
        Await Task.Delay(300)
        cts = New CancellationTokenSource
        ct = cts.Token
        currentIdleTime = 0
        IdleCounter = Task.Factory.StartNew(Function() Count(ct), ct)
    End Sub

    Public Sub StopCounter()
        If ct.CanBeCanceled Then
            cts.Cancel()
        End If
    End Sub
End Class
