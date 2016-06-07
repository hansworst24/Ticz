' The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

Imports GalaSoft.MvvmLight.Command
Imports Windows.Foundation.Metadata

Public Class CustomContentDialog
    Inherits ContentDialog

    Public Shared Property CloseButtonVisibilityProperty As DependencyProperty _
                           = DependencyProperty.Register("CloseButtonVisibility",
                           GetType(String), GetType(CustomContentDialog), New PropertyMetadata(""))
    Public Shared Property HeaderVisibilityProperty As DependencyProperty _
                           = DependencyProperty.Register("HeaderVisibility",
                           GetType(Visibility), GetType(CustomContentDialog), New PropertyMetadata(""))
    Public Shared Property BackgroundOpacityProperty As DependencyProperty _
                           = DependencyProperty.Register("BackgroundOpacity",
                           GetType(Double), GetType(CustomContentDialog), New PropertyMetadata(""))

    Public Property CloseButtonVisibility As String
        Get
            CloseButtonVisibility = CType(GetValue(CloseButtonVisibilityProperty), String)
        End Get
        Set(value As String)
            SetValue(CloseButtonVisibilityProperty, value)
        End Set
    End Property

    Public Property HeaderVisibility As Visibility
        Get
            HeaderVisibility = CType(GetValue(HeaderVisibilityProperty), Visibility)
        End Get
        Set(value As Visibility)
            SetValue(HeaderVisibilityProperty, value)
        End Set
    End Property

    Public Property BackgroundOpacity As Double
        Get
            BackgroundOpacity = CType(GetValue(BackgroundOpacityProperty), Double)
        End Get
        Set(value As Double)
            SetValue(BackgroundOpacityProperty, value)
        End Set
    End Property


    Public ReadOnly Property CloseCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        Me.Hide()
                                    End Sub)
        End Get
    End Property

    Public Sub New()
        CloseButtonVisibility = If(ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"), "Collapsed", "Visible")
        HeaderVisibility = Visibility.Visible
        BackgroundOpacity = 0.8
        Me.Template = CType(Application.Current.Resources("CustomContentDialogTemplate"), ControlTemplate)
        'ThemeResource SystemControlBackgroundChromeMediumBrush
        Me.Background = CType(Application.Current.Resources("SystemControlBackgroundChromeMediumBrush"), SolidColorBrush)
        Me.MaxHeight = ApplicationView.GetForCurrentView.VisibleBounds.Height
        Me.MinHeight = ApplicationView.GetForCurrentView.VisibleBounds.Height
        Me.MaxWidth = ApplicationView.GetForCurrentView.VisibleBounds.Width
        Me.MinWidth = ApplicationView.GetForCurrentView.VisibleBounds.Width
        Me.HorizontalContentAlignment = HorizontalAlignment.Stretch
        Me.VerticalContentAlignment = VerticalAlignment.Stretch
        Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
                                                       If e.Key = Windows.System.VirtualKey.Escape Then
                                                           Me.Hide()
                                                       End If
                                                   End Sub)
        Me.AddHandler(KeyDownEvent, escapekeyhandler, True)
    End Sub
End Class
