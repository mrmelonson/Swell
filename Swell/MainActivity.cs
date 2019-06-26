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
using Android.Content;
using Android.Preferences;
using Swell.Resources.Fragments;
using Android.Text;

namespace Swell.Main
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private CancellationTokenSource cts;
        public int currentDropId;

        private Loading_Fragment _Loading_Fragment;
        private Droplet_mainfragment _droplet_Mainfragment;
        private Def_Fragment _Def_Fragment;
        private Def_First_Fragment _Def_First_Fragment;

        private DigitalOceanClient client;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            //var prefedits = prefs.Edit();
            //prefedits.PutString("api_key", null).Commit();
            var api_key = prefs.GetString("api_key", null);

            if (api_key == null)
            {
                SetContentView(Resource.Layout.login);
                Login();
            }
            else
            {
                StartUpdate(-2);
            }
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
            SetContentView(Resource.Layout.activity_main);
            var trans = SupportFragmentManager.BeginTransaction();

            _Loading_Fragment = new Loading_Fragment();
            _droplet_Mainfragment = new Droplet_mainfragment();
            _Def_Fragment = new Def_Fragment();
            _Def_First_Fragment = new Def_First_Fragment();

            trans.Add(Resource.Id.DropletFragment, _droplet_Mainfragment, "Fragment");
            trans.Add(Resource.Id.DropletFragment, _Loading_Fragment, "Fragment");
            trans.Add(Resource.Id.DropletFragment, _Def_Fragment, "Fragment");
            trans.Add(Resource.Id.DropletFragment, _Def_First_Fragment, "Fragment");

            trans.Show(_Loading_Fragment);
            trans.Hide(_droplet_Mainfragment);
            trans.Hide(_Def_Fragment);
            trans.Hide(_Def_First_Fragment);
            trans.Commit();

            currentDropId = id;
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var api_key = prefs.GetString("api_key", null);


            client = new DigitalOceanClient(api_key);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            toolbar.Title = "Swell";

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();       

            SwipeRefreshLayout swipe = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);
            swipe.Refresh += async (o, e) =>
            {
                trans = SupportFragmentManager.BeginTransaction();
                trans.Show(_Loading_Fragment);
                trans.Hide(_droplet_Mainfragment);
                trans.Commit();

                await UpdateNavMenu();
                if (id > 0)
                {
                    await UpdateInfo(currentDropId);
                }

                trans = SupportFragmentManager.BeginTransaction();
                trans.Hide(_Loading_Fragment);
                trans.Show(_droplet_Mainfragment);
                trans.Commit();
                swipe.Refreshing = false;
            };

            await UpdateNavMenu();

            trans = SupportFragmentManager.BeginTransaction();

            if (id == -1)
            {
                trans.Show(_Def_Fragment);
            }
            else if (id == -2)
            {
                trans.Show(_Def_First_Fragment);
            }

            trans.Hide(_Loading_Fragment);
            trans.Hide(_droplet_Mainfragment);
            trans.Commit();


        }

        /*
         * BELOW ARE GENERAL FUNCTIONS 
         * 
         */

        public async Task CreateScreen(int id, string dropname)
        {
            if (id < 0) { return; }
            FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).Title = dropname;
            var trans = SupportFragmentManager.BeginTransaction();
            trans.Hide(_Def_First_Fragment);
            trans.Hide(_Def_Fragment);
            trans.Hide(_droplet_Mainfragment);
            trans.Show(_Loading_Fragment);
            trans.Commit();

            await UpdateInfo(id);
            var droplets = await GetServerInfo();

            currentDropId = id;

            trans = SupportFragmentManager.BeginTransaction();
            trans.Show(_droplet_Mainfragment);
            trans.Hide(_Loading_Fragment);
            trans.Commit();

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            TextView statustext = FindViewById<TextView>(Resource.Id.status);
            Button reboot = FindViewById<Button>(Resource.Id.Reboot);
            Button PowerCycle = FindViewById<Button>(Resource.Id.PowerCycle);
            Switch switcher = FindViewById<Switch>(Resource.Id.switch1);

            if (droplets[id].Status != "active")
            {
                reboot.Enabled = false;
                PowerCycle.Enabled = false;
            }

            reboot.Click += async (o, e) =>
            {
                await PowerDrop(id, 2, droplets[id]);
            };

            PowerCycle.Click += async (o, e) =>
            {
                await PowerDrop(id, 3, droplets[id]);
            };

            switcher.CheckedChange += async (o, e) =>
            {
                reboot.Enabled = false;
                PowerCycle.Enabled = false;
                switcher.Enabled = false;

                if (switcher.Checked)
                {
                    await PowerDrop(id, 1, droplets[id]);
                }
                else if (!switcher.Checked)
                {
                    await PowerDrop(id, 0, droplets[id]);
                }
            };

            return;
        }

        public async Task UpdateInfo(int id)
        {

            var droplets = await GetServerInfo();
            await UpdateNavMenu();

            //FindViewById<TextView>(Resource.Id.name).Text = droplets[id].Name;
            FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).Title = droplets[id].Name;

            //FindViewById<TextView>(Resource.Id.name).Text = droplets[id].Name;
            FindViewById<TextView>(Resource.Id.ip_v4).Text = "IPv4: " + droplets[id].Networks.v4[0].IpAddress;

            FindViewById<TextView>(Resource.Id.ip_v6).Text = "IPv6: Not Enabled";
            FindViewById<TextView>(Resource.Id.ip_priv).Text = "Priv Networking: Not Enabled";
            string tags = string.Join(", ", droplets[id].Tags);
            if (tags == "")
            {
                tags = "N/A";
            }
            FindViewById<TextView>(Resource.Id.tags).Text = "Tags: " + tags;
            foreach (var feature in droplets[id].Features)
            {
                if (feature == "ipv6")
                {
                    FindViewById<TextView>(Resource.Id.ip_v6).Text = "IPv6: " + droplets[id].Networks.v6[0].IpAddress;
                }
                if (feature == "private_networking")
                {
                    FindViewById<TextView>(Resource.Id.ip_priv).Text = "Priv Networking: Enabled";
                }
            }


            //FindViewById<TextView>(Resource.Id.ip_v4).Text = droplets[id].Networks.v6.ToString();


            FindViewById<TextView>(Resource.Id.cpu).Text = "Cpus: " + droplets[id].Vcpus.ToString();
            FindViewById<TextView>(Resource.Id.memory).Text = "Memory: " + droplets[id].Memory.ToString() + "gb";
            FindViewById<TextView>(Resource.Id.disk).Text = "Disk: " + droplets[id].Disk.ToString() + "gb";
            FindViewById<TextView>(Resource.Id.os).Text = "Os: " + droplets[id].Image.Distribution + " " + droplets[id].Image.Name;
            //FindViewById<TextView>(Resource.Id.kernel).Text = "Kernel Version: " + droplets[id].Kernel.Version.ToString();


            //FindViewById<TextView>(Resource.Id.imgsize).Text = "Image Size: " + droplets[id].Image.SizeGigabytes.ToString() + "gb";
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

            if (!(droplets[id].Status == "active" || droplets[id].Status == "off"))
            {
                switcher.Enabled = false;
            }



            return;

        }

        public async Task<IReadOnlyList<DigitalOcean.API.Models.Responses.Droplet>> GetServerInfo()
        {
            var droplets = await client.Droplets.GetAll();
            return droplets;
        }

        public async Task<int> AuthUser(string key)
        {
            client = new DigitalOceanClient(key);
            try
            {
                await client.Droplets.GetAll();
                return 1;
            }
            catch (DigitalOcean.API.Exceptions.ApiException err)
            {
                return 2;
            }
            catch (Exception err)
            {
                return 3;
            }
        }

        public async Task RenameServer(int id)
        {
            EditText input = new EditText(this);

            var droplets = await GetServerInfo();

            Android.Support.V7.App.AlertDialog.Builder rename = new Android.Support.V7.App.AlertDialog.Builder(this);

            rename.SetTitle("Rename to?");
            rename.SetCancelable(false);
            rename.SetView(input);

            rename.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
                Toast.MakeText(this, "Cancelled!", ToastLength.Short).Show();
            });

            rename.SetPositiveButton("OK", async (senderAlert, args) =>
            {
                try
                {
                    var renameAction = await client.DropletActions.Rename(droplets[id].Id, input.Text);
                    var renameActionGet = await client.Actions.Get(renameAction.Id);
                    while (renameActionGet.Status != "completed")
                    {
                        renameActionGet = await client.Actions.Get(renameActionGet.Id);
                    }
                }
                catch
                {
                    Toast.MakeText(this, "Error: Only characters a-z, 0-9, . and -", ToastLength.Short).Show();
                }
                await CreateScreen(id, droplets[id].Name);
            });

            rename.Show();
        }

        public async Task EnableNetworking(int id, string type)
        {

            DigitalOcean.API.Models.Responses.Action EnableAction;

            var droplets = await GetServerInfo();

            Android.Support.V7.App.AlertDialog.Builder Enable = new Android.Support.V7.App.AlertDialog.Builder(this);

            Enable.SetTitle("Warning");
            Enable.SetCancelable(false);

            if (droplets[id].Status != "off")
            {
                Enable.SetMessage("You must power off your droplet before enabling " + type + " networking.");
                Enable.SetNeutralButton("OK", (senderAlert, args) => { });
                Enable.Show();
                return;
            }

            foreach (var feature in droplets[id].Features)
            {
                if (feature == "ipv6")
                {
                    Enable.SetMessage("ipv6 networking already enabled");
                    Enable.SetNeutralButton("OK", (senderAlert, args) => { });
                    Enable.Show();
                    return;
                }

                if (feature == "private_network")
                {
                    Enable.SetMessage(" networking already enabled");
                    Enable.SetNeutralButton("OK", (senderAlert, args) => { });
                    Enable.Show();
                    return;
                }
            }
            Enable.SetMessage("Are you sure?");
            Enable.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
                Toast.MakeText(this, "Cancelled!", ToastLength.Short).Show();
            });
            Enable.SetPositiveButton("OK", async (senderAlert, args) =>
            {
                if (type == "ipv6")
                {
                    try
                    {
                        EnableAction = await client.DropletActions.EnableIpv6(droplets[id].Id);
                        while (EnableAction.Status != "completed")
                        {
                            EnableAction = await client.Actions.Get(EnableAction.Id);
                        }
                    }
                    catch (Exception err)
                    {
                        Toast.MakeText(this, err.ToString(), ToastLength.Short).Show();
                    }
                }
                else if (type == "private_networking")
                {
                    try
                    {
                        EnableAction = await client.DropletActions.EnablePrivateNetworking(droplets[id].Id);
                        while (EnableAction.Status != "completed")
                        {
                            EnableAction = await client.Actions.Get(EnableAction.Id);
                        }
                    }
                    catch (Exception err)
                    {
                        Toast.MakeText(this, err.ToString(), ToastLength.Short).Show();
                    }
                }
                await CreateScreen(id, droplets[id].Name);
            });
            Enable.Show();

            return;
        }

        public async Task DeleteDrop(int id)
        {
            var droplets = await GetServerInfo();

            Android.Support.V7.App.AlertDialog.Builder deletepopup = new Android.Support.V7.App.AlertDialog.Builder(this);
            deletepopup.SetTitle("Warning");
            deletepopup.SetCancelable(false);

            deletepopup.SetMessage("This cannot be undone\nAre you sure?");
            deletepopup.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
                Toast.MakeText(this, "Cancelled!", ToastLength.Short).Show();
            });

            deletepopup.SetPositiveButton("OK", async (senderAlert, args) =>
            {
                var trans = SupportFragmentManager.BeginTransaction();
                trans.Show(_Loading_Fragment);
                trans.Hide(_droplet_Mainfragment);
                trans.Commit();
                try
                {
                    await client.Droplets.Delete(droplets[id].Id);
                }
                catch (Exception err)
                {
                    Toast.MakeText(this, err.ToString(), ToastLength.Long).Show();
                }
                trans = SupportFragmentManager.BeginTransaction();
                trans.Hide(_Loading_Fragment);
                trans.Show(_droplet_Mainfragment);
                trans.Commit();
                StartUpdate(-1);

            });
            deletepopup.Show();
        }

        public async Task PowerDrop(int id, int option, DigitalOcean.API.Models.Responses.Droplet droplet)
        {
            Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
            Button reboot = FindViewById<Button>(Resource.Id.Reboot);
            Button PowerCycle = FindViewById<Button>(Resource.Id.PowerCycle);
            Switch switcher = FindViewById<Switch>(Resource.Id.switch1);
            TextView statustext = FindViewById<TextView>(Resource.Id.status);

            switcher.Enabled = false;
            reboot.Enabled = false;
            PowerCycle.Enabled = false;

            //DigitalOcean.API.Models.Responses.Action action;
            //DigitalOcean.API.Models.Responses.Action actionget;

            alert.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
                switcher.Checked = true;
                Toast.MakeText(this, "Cancelled!", ToastLength.Short).Show();
                switcher.Enabled = true;
                reboot.Enabled = true;
                PowerCycle.Enabled = true;
            });

            Dialog dialog = alert.Create();

            alert.SetCancelable(false);
            switch (option)
            {
                case 0:
                    alert.SetTitle("Confirm Poweroff");
                    alert.SetMessage("Are you sure you want to force a shutdown?\nThis may cause data loss and corruption.\n Do you want to continue?");
                    alert.SetPositiveButton("OK", async (senderAlert, args) =>
                    {
                        statustext.Text = "Shutting down";
                        statustext.SetTextColor(Color.ParseColor("#FF8C00"));
                        var action = await client.DropletActions.PowerOff(droplet.Id);
                        var actionget = await client.Actions.Get(action.Id);
                        Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                        while (actionget.Status != "completed")
                        {
                            actionget = await client.Actions.Get(action.Id);
                            Toast.MakeText(this, actionget.Status, ToastLength.Short).Show();
                        }
                        Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                        await UpdateInfo(id);
                        DisableSwitch();
                    });
                    dialog = alert.Create();
                    dialog.Show();
                    break;
                case 1:
                    statustext.Text = "Powering On";
                    statustext.SetTextColor(Color.ParseColor("#FF8C00"));
                    var _action = await client.DropletActions.PowerOn(droplet.Id);
                    var _actionget = await client.Actions.Get(_action.Id);
                    while (_actionget.Status != "completed")
                    {
                        _actionget = await client.Actions.Get(_action.Id);
                    }
                    await UpdateInfo(id);
                    DisableSwitch();
                    break;
                case 2:
                    alert.SetTitle("Confirm Reboot");
                    alert.SetMessage("Are you sure you want to Reboot?\nThis may cause data loss and corruption, We reccomend trying to reboot from commandline.\n Do you want to continue?");
                    alert.SetPositiveButton("OK", async (senderAlert, args) =>
                    {
                        statustext.Text = "Rebooting";
                        statustext.SetTextColor(Color.ParseColor("#FF8C00"));
                        var action = await client.DropletActions.Reboot(droplet.Id);
                        var actionget = await client.Actions.Get(action.Id);
                        Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                        while (actionget.Status != "completed")
                        {
                            actionget = await client.Actions.Get(action.Id);
                            Toast.MakeText(this, actionget.Status, ToastLength.Short).Show();
                        }
                        Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                        await UpdateInfo(id);
                        DisableSwitch();
                    });
                    dialog = alert.Create();
                    dialog.Show();
                    break;
                case 3:
                    alert.SetTitle("Confirm PowerCycle");
                    alert.SetMessage("Are you sure you want to Powercycle?\nThis may cause data loss and corruption, We reccomend trying to reboot from commandline fist.\n Do you want to continue?");
                    alert.SetPositiveButton("OK", async (senderAlert, args) =>
                    {
                        statustext.Text = "Cycling";
                        statustext.SetTextColor(Color.ParseColor("#FF8C00"));
                        var action = await client.DropletActions.PowerCycle(droplet.Id);
                        var actionget = await client.Actions.Get(action.Id);
                        Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                        while (actionget.Status != "completed")
                        {
                            actionget = await client.Actions.Get(action.Id);
                            Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                        }
                        Toast.MakeText(this, action.Status, ToastLength.Short).Show();
                        await UpdateInfo(id);
                        DisableSwitch();
                    });
                    dialog = alert.Create();
                    dialog.Show();
                    break;
            }
            return;
        }

        public async Task ResetPass(int id)
        {
            var droplets = await GetServerInfo();
            Android.Support.V7.App.AlertDialog.Builder reset = new Android.Support.V7.App.AlertDialog.Builder(this);
            reset.SetTitle("Warning");
            reset.SetCancelable(false);

            reset.SetMessage("Are your sure you want to reset the droplet password?");
            reset.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
                Toast.MakeText(this, "Cancelled!", ToastLength.Short).Show();
            });

            reset.SetPositiveButton("OK", async (senderAlert, args) =>
            {
                var trans = SupportFragmentManager.BeginTransaction();
                trans.Show(_Loading_Fragment);
                trans.Hide(_droplet_Mainfragment);
                trans.Commit();

                try
                {
                    await client.DropletActions.ResetPassword(droplets[id].Id);
                }
                catch
                {
                    Toast.MakeText(this, "ERROR, Cannot reset password", ToastLength.Short);
                }

                trans = SupportFragmentManager.BeginTransaction();
                trans.Hide(_Loading_Fragment);
                trans.Show(_droplet_Mainfragment);
                trans.Commit();
            });

            reset.Show();
        }

            /*
             * BELOW ARE ALL UI FUNCTIONS 
             * 
             */

        public void DisableSwitch()
        {
            Button reboot = FindViewById<Button>(Resource.Id.Reboot);
            Button PowerCycle = FindViewById<Button>(Resource.Id.PowerCycle);
            Switch switcher = FindViewById<Switch>(Resource.Id.switch1);

            Handler h = new Handler();
            Action EnableSwitcher = () =>
            {
                switcher.Enabled = true;
                reboot.Enabled = false;
                PowerCycle.Enabled = false;
            };
            h.PostDelayed(EnableSwitcher, 10000);
            return;
        }

        //Updates Navigation drawer
        public async Task UpdateNavMenu()
        {
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            IMenu menu = navigationView.Menu;
            menu.Clear();

            List<string> dropnames = new List<string>();

            menu = navigationView.Menu;

            ISubMenu submenu = menu.AddSubMenu("Droplets");
            var droplets = await GetServerInfo();

            var ServerCount = droplets.Count;
            for (int i = 0; i < droplets.Count; i++)
            {
                dropnames.Add(droplets[i].Name);
            }

            dropnames.Sort();

            for (int i = 0; i < dropnames.Count; i++)
            {
                submenu.Add(i, i, i, droplets[i].Name);
            }

            navigationView.InflateMenu(Resource.Menu.activity_main_drawer);

            navigationView.SetNavigationItemSelectedListener(this);

            return;
        }

        //Login Screen
        public void Login()
        {
            Button loginButton = FindViewById<Button>(Resource.Id.LoginButton);
            EditText Keyinput = FindViewById<EditText>(Resource.Id.editText1);
            loginButton.Click += async (o, e) =>
            {
                int authd = await AuthUser(Keyinput.Text);
                if (authd == 1)
                {
                    Toast.MakeText(this, "Logging in...", ToastLength.Short).Show();
                    ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                    var prefedit = prefs.Edit();
                    prefedit.PutString("api_key", Keyinput.Text);
                    prefedit.Commit();
                    StartUpdate(-2);
                }
                else if (authd == 2)
                {
                    Toast.MakeText(this, "Invalid API key", ToastLength.Short).Show();
                }
                else if (authd == 3)
                {
                    Toast.MakeText(this, "Uknown error, check connection", ToastLength.Short).Show();
                }
            };
        }

        //Logout
        public void Logout()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var prefedit = prefs.Edit();
            prefedit.PutString("api_key", null);
            prefedit.Commit();
            SetContentView(Resource.Layout.login);
            Login();
        }

        //For navigation drawer actions
        public bool OnNavigationItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            //Toast.MakeText(this, droplets[id].Name, ToastLength.Short).Show();
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);

            Toast.MakeText(this, id.ToString(), ToastLength.Short);

            switch (id)
            {
                case Resource.Id.create_new:
                    var intent = new Intent(this, typeof(Step1Activity));
                    StartActivity(intent);
                    break;
                default:
                    CreateScreen(id, item.TitleFormatted.ToString());
                    break;
            }
            return true;
        }

        //For pressing back button on phone
        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
            }
        }

        //Creates menu for ellipsis
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        //Actions for the menu dropdowns
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);

            if (id == Resource.Id.Logout)
            {
                Logout();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        //Action for floating settings button
        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            PopupMenu popup = new PopupMenu(this, fab);
            popup.MenuInflater.Inflate(Resource.Menu.drop_options, popup.Menu);
            popup.Show();
            popup.MenuItemClick += async (o, e) =>
            {
                var itemid = e.Item.ItemId;

                switch (itemid)
                {
                    case Resource.Id.Rename:
                        await RenameServer(currentDropId);
                        break;
                    case Resource.Id.Ipv6menu:
                        await EnableNetworking(currentDropId, "ipv6");
                        break;
                    case Resource.Id.PrivNetmenu:
                        await EnableNetworking(currentDropId, "private_networking");
                        break;
                    case Resource.Id.DeleteDropmenu:
                        await DeleteDrop(currentDropId);
                        break;
                    case Resource.Id.passreset:
                        await ResetPass(currentDropId);
                        break;
                    default:
                        return;
                }
            };
        }

    }
}

