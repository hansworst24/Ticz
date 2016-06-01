Imports System.Globalization
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports Newtonsoft.Json
Imports Windows.Web.Http

Public Class VariableViewModel
    Inherits ViewModelBase

    Private variable As Domoticz.Variable
    Public Sub New(v As Domoticz.Variable)
        variable = v
        OldValue = variable.Value
    End Sub

    Public ReadOnly Property idx As String
        Get
            Return variable.idx
        End Get
    End Property
    Public Property LastUpdate As DateTime
        Get
            Return variable.LastUpdate
        End Get
        Set(value As DateTime)
            variable.LastUpdate = value
            RaisePropertyChanged("LastUpdate")
        End Set
    End Property
    Public ReadOnly Property Name As String
        Get
            Return variable.Name
        End Get
    End Property
    'Public ReadOnly Property Template As DataTemplate
    '    Get
    '        Dim app As Application = CType(Application.Current, Application)
    '        Select Case variable.Type
    '            Case "0" : Return CType(app.Resources("VariableIntegerTemplate"), DataTemplate)
    '            Case "1" : Return CType(app.Resources("VariableFloatTemplate"), DataTemplate)
    '            Case "2" : Return CType(app.Resources("VariableStringTemplate"), DataTemplate)
    '            Case "3" : Return CType(app.Resources("VariableDateTemplate"), DataTemplate)
    '            Case "4" : Return CType(app.Resources("VariableTimeTemplate"), DataTemplate)
    '        End Select

    '    End Get
    'End Property
    Public ReadOnly Property [Type] As String
        Get
            Return variable.Type
        End Get
    End Property
    Public Property Value As String
        Get
            Return variable.Value
        End Get
        Set(value As String)
            variable.Value = value
            RaisePropertyChanged("Value")
        End Set
    End Property

    Public Property DateValue As DateTimeOffset
        Get
            Dim myDate As Date
            Dim enUS As New CultureInfo("en-US")
            Date.TryParseExact(variable.Value, "dd-MM-yyyy", enUS, DateTimeStyles.None, myDate)
            Return myDate
        End Get
        Set(value As DateTimeOffset)
            variable.Value = value.Date.ToString("dd-MM-yyyy")
        End Set
    End Property

    Public Property TimeValue As TimeSpan
        Get
            Dim myDate As DateTime
            Dim enUS As New CultureInfo("en-US")
            Date.TryParseExact(variable.Value, "HH:mm", enUS, DateTimeStyles.None, myDate)
            Return myDate.TimeOfDay
        End Get
        Set(value As TimeSpan)
            variable.Value = Date.Now.Date.Add(value).ToString("HH:mm")
        End Set
    End Property

    Public Property OldValue As String

    Private ReadOnly Property ValueString As String
        Get
            Select Case variable.Type
                Case "0" : Return "Integer"
                Case "1" : Return "Float"
                Case "2" : Return "String"
                Case "3" : Return "Date"
                Case "4" : Return "Time"
                Case Else : Return "Unknown"
            End Select
        End Get
    End Property

    Public ReadOnly Property UpdateValue As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("VariableViewModel.UpdateValue", Value)
                                        'Perform formatting checks to see if the value entered matches the type
                                        Dim app As Application = CType(Application.Current, Application)
                                        Select Case variable.Type
                                            Case "0" 'Integer
                                                Dim int As Integer
                                                If Not Integer.TryParse(Value, int) Then
                                                    Value = OldValue
                                                    Await app.myViewModel.Variables.message.Update(True, String.Format("Couldn't parse variable. Variable is of type {0}", ValueString), 1, False, 2)
                                                    Exit Sub
                                                End If
                                            Case "1" 'Float
                                                Dim lng As Single
                                                If Not Single.TryParse(Value, lng) Then
                                                    Value = OldValue
                                                    Await app.myViewModel.Variables.message.Update(True, String.Format("Couldn't parse variable. Variable is of type {0}", ValueString), 1, False, 2)
                                                    Exit Sub
                                                End If
                                        End Select
                                        'Set old Value to the same value
                                        OldValue = Value
                                        'Send update to Domoticz Server
                                        Dim url As String = (New DomoApi).setVariable(Me.idx, Me.Name, Me.Type, Me.Value)
                                        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
                                        Dim ret As New Domoticz.Response
                                        Dim wasFailure As Boolean
                                        If response.IsSuccessStatusCode Then
                                            Dim body As String = Await response.Content.ReadAsStringAsync()
                                            Try
                                                ret = JsonConvert.DeserializeObject(Of Domoticz.Response)(body)
                                                If ret.status = "OK" Then
                                                    wasFailure = False
                                                    ret.message = "Variable updated"
                                                Else
                                                    wasFailure = True
                                                End If
                                            Catch ex As Exception
                                                wasFailure = False
                                                ret.message = "Error parsing Domoticz response."
                                            End Try
                                        Else
                                            wasFailure = False
                                            ret.message = response.ReasonPhrase
                                        End If
                                        Await app.myViewModel.Variables.message.Update(wasFailure, ret.message, 1, False, 2)

                                        'Get new values for this Variable
                                        Dim updatedvars As New VariableListViewModel
                                        Await updatedvars.Load()
                                        Dim myupdate As VariableViewModel = (From v In updatedvars Where v.idx = Me.idx Select v).FirstOrDefault()
                                        If Not myupdate Is Nothing Then
                                            Me.LastUpdate = myupdate.LastUpdate
                                            Me.Value = myupdate.Value
                                        End If



                                    End Sub)
        End Get
    End Property

End Class
