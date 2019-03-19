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
using Android.Content;
 

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
            //RIGHT HERE IS THE MENU SHIT WORKING PROGMATTICALLY

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
            TextView subtext = FindViewById<TextView>(Resource.Id.InfoText);
            subtext.Text = ct.ToString();
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
            TextView text = FindViewById<TextView>(Resource.Id.Titletext);
            TextView subtext = FindViewById<TextView>(Resource.Id.InfoText);
            text.Text = droplets[id].Name;
            subtext.Text = droplets[id].Image.Slug;
            Android.Widget.Switch switcher = FindViewById<Android.Widget.Switch>(Resource.Id.switch1);
            if (droplets[id].Status == "active")
            {
                switcher.Checked = true;
            }
            switcher.Click += async (o, e) =>
            {
                if (switcher.Checked)
                {
                    Toast.MakeText(this, "On", ToastLength.Short).Show();
                    var action = await client.DropletActions.PowerOn(droplets[id].Id);
                }
                else
                {
                    bool originalState = switcher.Checked;
                    Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(this);
                    Android.App.AlertDialog alert = dialog.Create();
                    alert.SetTitle("Warning");
                    alert.SetMessage("Turning your droplet off may cause a hard shutdown which can cause file corruption.\nWe recommend you shutdown any processes through a command line first.");
                    alert.SetButton3("Cancel", (c, v) => {
                        switcher.Checked = true;
                        return;
                    });
                    alert.SetOnDismissListener(Onclick);
                    alert.SetButton("Power Off", async (c, v) =>
                    {
                        switcher.Checked = false;
                        var action = await client.DropletActions.PowerOff(droplets[id].Id);
                        return;
                    });
                    alert.Show();
                    switcher.Checked = originalState;
                }
            };
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

