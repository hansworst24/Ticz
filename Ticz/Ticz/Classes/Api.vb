Public Class Api

    'Dim vm As App = CType(Application.Current, App)
    'Dim vm As TiczViewModel = vm.myViewModel

    Public serverIP As String = TiczViewModel.TiczSettings.ServerIP
    Public serverPort As String = TiczViewModel.TiczSettings.ServerPort

    'Switch Command On/Off with passcode
    'http://{0}:{1}/json.htm?type=command&param=switchlight&idx=95&switchcmd=Off&level=0&passcode=234 
    'returns when wrong code
    '    {
    '   "message" : "WRONG CODE",
    '   "status" : "ERROR",
    '   "title" : "SwitchLight"
    '}

    Public Function getAllDevicesForRoom(roomIDX As String)
        'Using order=Name, ensures that the devices are returned in the order in which they are set in the WebUI
        Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true&order=Name&plan={2}", serverIP, serverPort, roomIDX)
    End Function

    Public Function getAllDevices() As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true", serverIP, serverPort)
    End Function

    Public Function getAllScenes() As String
        Return String.Format("http://{0}:{1}/json.htm?type=scenes", serverIP, serverPort)
    End Function


    Public Function getDeviceStatus(idx As String) As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&rid={2}", serverIP, serverPort, idx)
    End Function

    Public Function setDimmer(idx As String, switchstate As String, Optional passcode As String = "") As String
        Dim switchstring As String
        If Not switchstate = "On" Then switchstring = "Set%20Level&level=" Else switchstring = ""
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}{4}", serverIP, serverPort, idx, switchstring, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}{4}&passcode={5}", serverIP, serverPort, idx, switchstring, switchstate, passcode)
        End If

    End Function


    Public Function SwitchScene(idx As String, switchstate As String, Optional passcode As String = "") As String
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchscene&idx={2}&switchcmd={3}", serverIP, serverPort, idx, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchscene&idx={2}&switchcmd={3}&passcode={4}", serverIP, serverPort, idx, switchstate, passcode)
        End If

    End Function

    Public Function SwitchLight(idx As String, switchstate As String, Optional passcode As String = "") As String
        If passcode = "" Then
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}", serverIP, serverPort, idx, switchstate)
        Else
            Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}&passcode={4}", serverIP, serverPort, idx, switchstate, passcode)
        End If

    End Function

    Public Function getPlans() As String
        Return String.Format("http://{0}:{1}/json.htm?type=plans", serverIP, serverPort)
    End Function
End Class
