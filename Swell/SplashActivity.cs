using Android.App;
using Android.OS;
using Android.Content;

namespace Swell.Main
{
    [Activity(Theme = "@style/Swell.Splash", Label = "@string/app_name", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : Activity
    {
        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnCreate(savedInstanceState, persistentState);
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }

        // Launches the startup task
        protected override void OnResume()
        {
            base.OnResume();
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }
    }
}