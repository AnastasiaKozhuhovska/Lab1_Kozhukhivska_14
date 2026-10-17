using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace StudentApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ExcludeFromRecents = false,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "studentapp",
    DataHost = "student")]
public class MainActivity : MauiAppCompatActivity
{

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent?.Data is not null)
        {
            Microsoft.Maui.ApplicationModel.Platform.OnNewIntent(intent);
        }
    }
}
