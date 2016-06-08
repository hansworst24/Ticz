Imports GalaSoft.MvvmLight.Threading
Imports Windows.UI
''' <summary>
''' Provides application-specific behavior to supplement the default Application class.
''' </summary>
NotInheritable Class Application
    Inherits Xaml.Application

    Public myViewModel As New TiczViewModel

    ''' <summary>
    ''' Initializes a new instance of the App class.
    ''' </summary>
    Public Sub New()
        Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
            Microsoft.ApplicationInsights.WindowsCollectors.Metadata Or
            Microsoft.ApplicationInsights.WindowsCollectors.Session)
        InitializeComponent()
    End Sub

    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>
    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
#If DEBUG Then
        ' Show graphics profiling information while debugging.
        If System.Diagnostics.Debugger.IsAttached Then
            ' Display the current frame rate counters
            Me.DebugSettings.EnableFrameRateCounter = True
        End If
#End If
        'Add Handlers for when the soft keyboard in shown/hidden. Used to adjust the height of an existing ConTentDialog accordingly
        AddHandler InputPane.GetForCurrentView().Hiding, AddressOf KeyboardHiding
        AddHandler InputPane.GetForCurrentView().Showing, AddressOf KeyboardShowing
        'Add handler for when the mainwindow is resized, so that any open contentdialog is stretched to full screen
        AddHandler ApplicationView.GetForCurrentView.VisibleBoundsChanged, AddressOf VisibleBoundsChanged


        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)



        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()
            AddHandler rootFrame.PointerMoved, AddressOf ResetIdleCounter
            'AddHandler rootFrame.PointerPressed, AddressOf ResetIdleCounter
            If myViewModel.TiczSettings.UseDarkTheme Then
                rootFrame.RequestedTheme = ElementTheme.Dark
            Else
                rootFrame.RequestedTheme = ElementTheme.Light
            End If
            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

            If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
                ' TODO: Load state from previously suspended application
            End If
            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If
        If rootFrame.Content Is Nothing Then
            ' When the navigation stack isn't restored navigate to the first page,
            ' configuring the new page by passing required information as a navigation
            ' parameter
            rootFrame.Navigate(GetType(SplitView), e.Arguments)
        End If
        Dim a As Color = CType(CType(Application.Current, Application).Resources("ApplicationPageBackgroundThemeBrush"), SolidColorBrush).Color
        ApplicationView.GetForCurrentView.TitleBar.BackgroundColor = a
        ApplicationView.GetForCurrentView.TitleBar.ButtonBackgroundColor = a
        ApplicationView.GetForCurrentView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible)
        ApplicationView.PreferredLaunchViewSize = New Size(800, 480)
        ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize
        If (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) Then
            Dim sBar As StatusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView()
            If Not sBar Is Nothing Then
                sBar.HideAsync()
            End If
        End If

        DispatcherHelper.Initialize()
        Window.Current.Activate()
    End Sub

    Public Sub VisibleBoundsChanged(sender As ApplicationView, args As Object)
        If Not myViewModel.ActiveContentDialog Is Nothing Then
            ' WriteToDebug("ApplicationView.GetForCurrentView.VisibleBounds : ", String.Format("{0} / {1}", ApplicationView.GetForCurrentView.VisibleBounds.Height, ApplicationView.GetForCurrentView.VisibleBounds.Width))
            myViewModel.ActiveContentDialog.MaxHeight = ApplicationView.GetForCurrentView.VisibleBounds.Height
            myViewModel.ActiveContentDialog.MinHeight = ApplicationView.GetForCurrentView.VisibleBounds.Height
            myViewModel.ActiveContentDialog.MinWidth = ApplicationView.GetForCurrentView.VisibleBounds.Width
            myViewModel.ActiveContentDialog.MaxWidth = ApplicationView.GetForCurrentView.VisibleBounds.Width
        End If
    End Sub
    Public Sub ResetIdleCounter(sender As Object, args As PointerRoutedEventArgs)
        WriteToDebug("App.ResetIdleCounter", "executed")
        If Not myViewModel.IdleTimer Is Nothing Then
            myViewModel.IdleTimer.ResetCounter()
        End If
    End Sub

    Public Sub KeyboardShowing(sender As InputPane, args As InputPaneVisibilityEventArgs)
        WriteToDebug("App.KeyboardShowing", "executed")
        If Not myViewModel.ActiveContentDialog Is Nothing Then
            myViewModel.ActiveContentDialog.MaxHeight = Window.Current.Bounds.Height - sender.OccludedRect.Height
        End If
    End Sub

    Public Async Sub KeyboardHiding(sender As InputPane, args As InputPaneVisibilityEventArgs)
        WriteToDebug("App.KeyboardHiding", "executed")
        'We wait for a few milliseconds because resizing the ContentDialog immediately may cause a click event within the ContentDialog not to trigger properly
        Await Task.Delay(100)
        If Not myViewModel.ActiveContentDialog Is Nothing Then
            myViewModel.ActiveContentDialog.MaxHeight = Window.Current.Bounds.Height
        End If
    End Sub


    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub

End Class
