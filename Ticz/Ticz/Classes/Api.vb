Public Class Api

    Dim app As App = CType(Application.Current, App)
    Dim vm As TiczViewModel = app.myViewModel

    Public serverIP As String = vm.TiczSettings.ServerIP
    Public serverPort As String = vm.TiczSettings.ServerPort

    'Switch Command On/Off with passcode
    'http://192.168.168.4:8888/json.htm?type=command&param=switchlight&idx=95&switchcmd=Off&level=0&passcode=234 
    'returns when wrong code
    '    {
    '   "message" : "WRONG CODE",
    '   "status" : "ERROR",
    '   "title" : "SwitchLight"
    '}


    Public Function getDevices() As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&filter=all&used=true&order=Name", serverIP, serverPort)
    End Function

    Public Function getLightSwitches() As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=getlightswitches", serverIP, serverPort)
    End Function

    Public Function getDeviceStatus(idx As String) As String
        Return String.Format("http://{0}:{1}/json.htm?type=devices&rid={2}", serverIP, serverPort, idx)
    End Function

    Public Function SwitchLight(idx As String, switchstate As String) As String
        Return String.Format("http://{0}:{1}/json.htm?type=command&param=switchlight&idx={2}&switchcmd={3}", serverIP, serverPort, idx, switchstate)
    End Function

    Public Function getPlans() As String
        Return String.Format("http://{0}:{1}/json.htm?type=plans", serverIP, serverPort)
    End Function
End Class
