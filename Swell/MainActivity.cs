using System;
using Android.App;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;
using System.Threading;
using System.Threading.Tasks;
using DigitalOcean.API;
using Android.Widget;
using System.Collections.Generic;
using Android.Graphics;


namespace Swell
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private CancellationTokenSource cts;
        //private int ServerCount;
        //private IReadOnlyList<DigitalOcean.API.Models.Responses.Droplet> droplets;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            StartUpdate(-1); // Start updating UI
        }

        public void StartUpdate(int id)
        {
            if (cts != null) cts.Cancel();
            cts = new CancellationTokenSource();
            var ignore = UpdaterAsync(cts.Token, id);
        }

        public void StopUpdate() // To stop Updating
        {
            if (cts != null) cts.Cancel();
            cts = null;
        }


        public async Task UpdaterAsync(CancellationToken ct, int id)
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
            menu.Clear();
            ISubMenu submenu = menu.AddSubMenu("Servers");

            var droplets = await GetServerInfo();
            
            var ServerCount = droplets.Count;
            for (int i = 0; i < droplets.Count; i++)
            {
                submenu.Add(i, i, i, droplets[i].Name);
            }
            await CreateScreen(id, droplets);
            
        }


        public async Task CreateScreen(int id, IReadOnlyList<DigitalOcean.API.Models.Responses.Droplet> droplets)
        {
            //here is my new function
            if (id < 0) { return; }
            var client = new DigitalOceanClient(API_KEY.Key.ToString());
            Switch switcher = FindViewById<Switch>(Resource.Id.switch1);
            TextView statustext = FindViewById<TextView>(Resource.Id.status);
            await UpdateInfo(id);

            switcher.Click += async (o, e) =>
             {
                 switcher.Enabled = false;
                 if (switcher.Checked)
                 {
                     var action = await client.DropletActions.PowerOn(droplets[id].Id);
                     var actionget = await client.Actions.Get(action.Id);
                     Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                     while (actionget.Status != "completed")
                     {
                         actionget = await client.Actions.Get(action.Id);
                         statustext.Text = "Powering On";
                         statustext.SetTextColor(Color.ParseColor("#FF8C00"));
                     }
                     Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                     await UpdateInfo(id);
                     Toast.MakeText(this, "Info updated", ToastLength.Short).Show();
                 }
                 else if (switcher.Checked == false)
                 {
                     var action = await client.DropletActions.PowerOff(droplets[id].Id);
                     var actionget = await client.Actions.Get(action.Id);
                     Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                     while (actionget.Status != "completed")
                     {
                         actionget = await client.Actions.Get(action.Id);
                         statustext.Text = "Shutting down";
                         statustext.SetTextColor(Color.ParseColor("#FF8C00"));
                     }
                     Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                     await UpdateInfo(id);
                     Toast.MakeText(this, "Info updated", ToastLength.Short).Show();
                 }
                 //switcher.Enabled = true;
                 Handler h = new Handler();
                 Action EnableSwitcher = () =>
                 {
                     switcher.Enabled = true;
                 };
                 h.PostDelayed(EnableSwitcher, 10000);
             };
        }

        public async Task UpdateInfo(int id)
        {
            var droplets = await GetServerInfo();


            FindViewById<TextView>(Resource.Id.name).Text = droplets[id].Name;
            FindViewById<TextView>(Resource.Id.ip_v4).Text = "IPv4: " + droplets[id].Networks.v4[0].IpAddress;

            FindViewById<TextView>(Resource.Id.ip_v6).Text = "IPv6: Not enabled";
            foreach (var feature in droplets[id].Features)
            {
                if(feature == "ipv6")
                {
                    FindViewById<TextView>(Resource.Id.ip_v6).Text = "IPv6: " + droplets[id].Networks.v6[0].IpAddress;
                }
            }


            //FindViewById<TextView>(Resource.Id.ip_v4).Text = droplets[id].Networks.v6.ToString();


            FindViewById<TextView>(Resource.Id.cpu).Text = "Cpus: "+droplets[id].Vcpus.ToString();
            FindViewById<TextView>(Resource.Id.memory).Text = "Memory: " + droplets[id].Memory.ToString() + "gb";
            FindViewById<TextView>(Resource.Id.disk).Text = "Disk: " + droplets[id].Disk.ToString() + "gb";
            FindViewById<TextView>(Resource.Id.os).Text = "Os: " + droplets[id].Image.Distribution + " " + droplets[id].Image.Name;
            //FindViewById<TextView>(Resource.Id.kernel).Text = "Kernel Version: " + droplets[id].Kernel.Version.ToString();
            

            FindViewById<TextView>(Resource.Id.imgsize).Text = "Image Size: " + droplets[id].Image.SizeGigabytes.ToString() + "gb";
            FindViewById<TextView>(Resource.Id.region).Text = "Region: " + droplets[id].Region.Name;
            TextView statustext = FindViewById<TextView>(Resource.Id.status);
            statustext.Text = "Status: " + droplets[id].Status;

            if (droplets[id].Status == "active")
            {
                statustext.SetTextColor(Color.ParseColor("#32CD32"));
            }
            else if (droplets[id].Status == "off")
            {
                statustext.SetTextColor(Color.ParseColor("#C43F3F"));
            }
            else
            {
                statustext.SetTextColor(Color.ParseColor("#FF8C00"));
            }

            Switch switcher = FindViewById<Switch>(Resource.Id.switch1);
            if (droplets[id].Status == "active")
            {
                switcher.Checked = true;
            }
            else
            {
                switcher.Checked = false;
            }

            return;

        }

        public async Task<IReadOnlyList<DigitalOcean.API.Models.Responses.Droplet>> GetServerInfo()
        {
            var client = new DigitalOceanClient(API_KEY.Key.ToString());
            var droplets = await client.Droplets.GetAll();
            var dropdata = new Array[droplets.Count, 100];
            return droplets;
        }

        public bool OnNavigationItemSelected(IMenuItem item) // Actions for the main menu items
        {
            int id = item.ItemId;
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            //Toast.MakeText(this, droplets[id].Name, ToastLength.Short).Show();
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            StartUpdate(id);

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

        public override bool OnOptionsItemSelected(IMenuItem item) // Actions for the settings menu (top right corner of screen)
        {
            int id = item.ItemId;
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);

            Toast.MakeText(this, item.TitleFormatted, ToastLength.Short).Show();


            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }
    }
}

