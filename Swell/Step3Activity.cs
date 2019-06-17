using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DigitalOcean.API;

namespace Swell.Main
{
    [Activity(Label = "Step3Activity")]
    public class Step3Activity : Activity
    {
        private CancellationTokenSource cts;
        DigitalOcean.API.Models.Requests.Droplet createdrop = new DigitalOcean.API.Models.Requests.Droplet();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Step2);

            var createdrop = new DigitalOcean.API.Models.Responses.Droplet();
            createdrop.Name = Intent.Extras.GetString("dropletName");
            createdrop.Image.Slug = Intent.Extras.GetString("DropletDistro");
            StartUpdate();

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
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var api_key = prefs.GetString("api_key", null);

            DigitalOceanClient client = new DigitalOceanClient(api_key);

            RadioGroup lls = FindViewById<RadioGroup>(Resource.Id.sshkeygroup);

            createdrop.Backups = false;
            createdrop.Ipv6 = false;
            createdrop.Monitoring = false;
            createdrop.PrivateNetworking = false;

            FindViewById<CheckBox>(Resource.Id.checkBoxbackup).CheckedChange += (o, e) =>
            {
                if (FindViewById<CheckBox>(Resource.Id.checkBoxbackup).Checked)
                {
                    createdrop.Backups = true;
                }
                else
                {
                    createdrop.Backups = false;
                }
            };

            FindViewById<CheckBox>(Resource.Id.checkBoxipv6net).CheckedChange += (o, e) => 
            {
                if (FindViewById<CheckBox>(Resource.Id.checkBoxipv6net).Checked)
                {
                    createdrop.Ipv6 = true;
                }
                else
                {
                    createdrop.Ipv6 = false;
                }
            };

            FindViewById<CheckBox>(Resource.Id.checkBoxmonitor).CheckedChange += (o, e) => 
            {
                if (FindViewById<CheckBox>(Resource.Id.checkBoxmonitor).Checked)
                {
                    createdrop.Monitoring = true;
                }
                else
                {
                    createdrop.Monitoring = false;
                }
            };

            FindViewById<CheckBox>(Resource.Id.checkBoxprivnet).CheckedChange += (o, e) => 
            {
                if (FindViewById<CheckBox>(Resource.Id.checkBoxprivnet).Checked)
                {
                    createdrop.PrivateNetworking = true;
                }
                else
                {
                    createdrop.PrivateNetworking = false;
                }
            };

            Button next = FindViewById<Button>(Resource.Id.NextS3);

            next.Click += (o, e) =>
            {
                Toast.MakeText(this, createdrop.Backups.ToString() + 
                    createdrop.Ipv6.ToString() + 
                    createdrop.Monitoring.ToString() + 
                    createdrop.PrivateNetworking.ToString(), ToastLength.Short).Show();

            };




        }

    }
}