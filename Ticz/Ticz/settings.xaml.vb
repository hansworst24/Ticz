Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command

Partial Public Class AppSettings
    Inherits ViewModelBase

    Dim settings As Windows.Storage.ApplicationDataContainer

    Const strServerIPKeyName As String = "strServerIP"
    Const strServerPortKeyName As String = "strServerPort"
    Const strUsernameKeyName As String = "strUserName"
    Const strUserPasswordKeyName As String = "strUserPassword"
    Const strTimeOutKeyName As String = "intTimeOut"


#If DEBUG Then
    'PUT YOUR (TEST) SERVER DETAILS HERE IF YOU WANT TO DEBUG, AND NOT PROVIDE CREDENTIALS AND SERVER DETAILS EACH TIME
    Const strServerIPDefault = ""
    Const strServerPortDefault = ""
    Const strUsernameDefault = ""
    Const strUserPasswordDefault = ""
    Const strTimeOutDefault = 5
#Else
    'PROD SETTINGS
    Const strServerIPDefault = ""
    Const strServerPortDefault = ""
    Const strUsernameDefault = ""
    Const strUserPasswordDefault = ""
    Const strTimeOutDefault = 5
#End If

    Const strConnectionStatusDefault = False

    Public Sub New()
        settings = Windows.Storage.ApplicationData.Current.LocalSettings
    End Sub

    Public Property TestInProgress As Boolean
        Get
            Return _TestInProgress
        End Get
        Set(value As Boolean)
            _TestInProgress = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _TestInProgress As Boolean


    Public ReadOnly Property NavigateBackCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
                                        If rootFrame.CanGoBack Then rootFrame.GoBack()
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property TestConnectionCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        TestInProgress = True
                                        TestConnectionResult = "Testing connection..."
                                        WriteToDebug("TestConnectionCommand", "executed")
                                        If ContainsValidIPDetails() Then
                                            Dim response As retvalue = Await (New Plans).Load()
                                            If response.issuccess Then
                                                TestConnectionResult = "Hurray !"
                                            Else
                                                TestConnectionResult = String.Format("Hmm..doesn't work : {0}", response.err)
                                            End If
                                        Else
                                            TestConnectionResult = "Server IP/Port not valid !"
                                        End If
                                        TestInProgress = False
                                    End Sub)
        End Get
    End Property

    Public Property TestConnectionResult As String
        Get
            Return _TestConnectionResult
        End Get
        Set(value As String)
            _TestConnectionResult = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _TestConnectionResult As String

    'Checks if the Server IP and the Server Port are valid
    Public Function ContainsValidIPDetails() As Boolean
        Dim tmpIPAddress As Net.IPAddress
        If Net.IPAddress.TryParse(ServerIP, tmpIPAddress) Then

            If ServerPort.Length > 0 AndAlso ServerPort.All(Function(x) Char.IsDigit(x)) AndAlso CType(ServerPort, Integer) <= 65535 Then
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function


    Public Function GetFullURL() As String
        Return "http://" + ServerIP + ":" + ServerPort
    End Function

    Public Function AddOrUpdateValue(Key As String, value As Object)
        Dim valueChanged As Boolean = False

        If value Is Nothing Then Return False
        If settings.Values.ContainsKey(Key) Then
            settings.Values(Key) = value
            valueChanged = True

        Else
            settings.Values.Add(Key, value)
            valueChanged = True
        End If
        Return valueChanged
    End Function

    Public Function GetValueOrDefault(Of T)(Key As String, defaultValue As T) As T

        Dim value As T
        ' If the key exists, retrieve the value.
        If Not settings.Values(Key) Is Nothing Then
            value = DirectCast(settings.Values(Key), T)
        Else
            ' Otherwise, use the default value.
            value = defaultValue
        End If
        Return value
    End Function

    Public Sub Save()
        'settings.Save()
    End Sub

    Public Property TimeOut As Integer
        Get
            Return GetValueOrDefault(Of Integer)(strTimeOutKeyName, strTimeOutDefault)
        End Get
        Set(value As Integer)
            If AddOrUpdateValue(strTimeOutKeyName, value) Then
                Save()
                RaisePropertyChanged("TimeOutText")
            End If
        End Set
    End Property


    Public Property TimeOutText As String
        Get
            Return String.Format("{0} seconds", TimeOut.ToString)
        End Get
        Set(value As String)
            _TimeOutText = value
            RaisePropertyChanged("TimeOutText")
        End Set
    End Property
    Private Property _TimeOutText As String


    Public Property ServerPort As String
        Get
            Return GetValueOrDefault(Of String)(strServerPortKeyName, strServerPortDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strServerPortKeyName, value) Then
                Save()
            End If
        End Set
    End Property



    Public Property ServerIP As String
        Get
            Return GetValueOrDefault(Of String)(strServerIPKeyName, strServerIPDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strServerIPKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property Password As String
        Get
            Return GetValueOrDefault(Of String)(strUserPasswordKeyName, strUserPasswordDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strUserPasswordKeyName, value) Then
                Save()
            End If
        End Set
    End Property

    Public Property Username As String
        Get
            Return GetValueOrDefault(Of String)(strUsernameKeyName, strUsernameDefault)
        End Get
        Set(value As String)
            If AddOrUpdateValue(strUsernameKeyName, value) Then
                Save()
            End If
        End Set
    End Property

End Class



Public NotInheritable Class AppSettingsPage
    Inherits Page

    Dim app As App = CType(Application.Current, App)

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        Me.DataContext = app.myViewModel.TiczSettings
    End Sub


    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)

    End Sub


    Public Sub New()
        InitializeComponent()
    End Sub
End Class
