using System;
using Android;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using System.Threading;
using System.Threading.Tasks;
using DigitalOcean.API;
using Android.Widget;

namespace Swell
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private CancellationTokenSource cts;
        private int ServerCount;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            //RIGHT HERE IS THE MENU SHIT WORKING PROGMATTICALLY

            StartUpdate(); // Start updating UI
        }

        public void StartUpdate()
        {
            if (cts != null) cts.Cancel();
            cts = new CancellationTokenSource();
            var ignore = UpdaterAsync(cts.Token);
        }

        public void StopUpdate() // To stop Updating
        {
            if (cts != null) cts.Cancel();
            cts = null;
        }

        public async Task UpdaterAsync(CancellationToken ct)
        {
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            IMenu menu = navigationView.Menu;
            ISubMenu submenu = menu.AddSubMenu("Servers");
            var client = new DigitalOcean.API.DigitalOceanClient(API_KEY.Key.ToString());
            var droplets = await client.Droplets.GetAll();
            dropdata = new Array[droplets.Count, 100];
            ServerCount = droplets.Count;

            for (int i = 0; i < droplets.Count; i++)
            {
                submenu.Add(i, Menu.First, Menu.First + i, droplets[i].Name);
            }
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);

            Toast.MakeText(this, item.TitleFormatted, ToastLength.Short).Show();

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }

        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if(drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);

            Toast.MakeText(this, item.SubMenu.ToString(), ToastLength.Short).Show();


            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }
    }
}

