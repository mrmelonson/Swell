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
using Swell.Resources.Fragments;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;

namespace Swell.Main
{

    [Activity(Label = "Create Droplet - Step 2")]
    public class Step2Activity : AppCompatActivity
    {
        private Loading_Fragment _Loading_Fragment;
        private Step2_Fragment _Step2_Fragment;
        DigitalOcean.API.Models.Requests.Droplet createdrop = new DigitalOcean.API.Models.Requests.Droplet();

        private CancellationTokenSource cts;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Step2);

            createdrop.Name = Intent.Extras.GetString("dropletName");
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
            string[,] sizesSlugsArr = { { "s-1vcpu-1gb", "5$/month\n1 vCPU : 1GB\n25gb Disk : 1TB Transfer\n" },
                                     { "s-1vcpu-2gb", "10$/month\n1 vCPU : 2GB\n50gb Disk : 2TB Transfer\n"},
                                     { "s-1vcpu-3gb", "15$/month\n1 vCPU : 3GB\n60gb Disk : 3TB Transfer\n"},
                                     { "s-2vcpu-2gb", "15$/month\n2 vCPU : 2GB\n60gb Disk : 3TB Transfer\n"},
                                     { "s-3vcpu-1gb", "15$/month\n3 vCPU : 1GB\n60gb Disk : 3TB Transfer\n"},
                                     { "s-2vcpu-4gb", "20$/month\n2 vCPU : 4GB\n80gb Disk : 4TB Transfer\n"},
                                   };

            _Loading_Fragment = new Loading_Fragment();
            _Step2_Fragment = new Step2_Fragment();

            var currentdrops = new List<DisplayInfo>();
            var regionsslug = new List<DisplayInfo>();
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var api_key = prefs.GetString("api_key", null);

            DigitalOceanClient client = new DigitalOceanClient(api_key);

            var trans = SupportFragmentManager.BeginTransaction();
            trans.Add(Resource.Id.Step2_Frame, _Step2_Fragment, "Fragment");
            trans.Add(Resource.Id.Step2_Frame, _Loading_Fragment, "Fragment");
            trans.Hide(_Step2_Fragment);
            trans.Show(_Loading_Fragment);
            trans.Commit();

            var dropletsizes = await client.Sizes.GetAll();
            var regions = await client.Regions.GetAll();

            var trans2 = SupportFragmentManager.BeginTransaction();
            trans2.Hide(_Loading_Fragment);
            trans2.Show(_Step2_Fragment);
            trans2.Commit();

            RadioGroup rgp = FindViewById<RadioGroup>(Resource.Id.radiogroupplan);
            RadioGroup rgr = FindViewById<RadioGroup>(Resource.Id.radiogroupregion);

            for (int i = 0; i < dropletsizes.Count; i++)
            {
                for (int x = 0; x < sizesSlugsArr.GetLength(0); x++)
                {
                    if (dropletsizes[i].Slug == sizesSlugsArr[x, 0] /*&& dropletsizes[i].Available*/)
                    {
                        RadioButton rdbtn = new RadioButton(this);
                        rdbtn.Text = sizesSlugsArr[x, 1];
                        rdbtn.Id = View.GenerateViewId();
                        var toadd = new DisplayInfo();
                        toadd.dropindex = i;
                        toadd.radioid = rdbtn.Id;
                        toadd.slug = sizesSlugsArr[x, 0];
                        toadd.text = sizesSlugsArr[x, 1];
                        currentdrops.Add(toadd);
                        rgp.AddView(rdbtn);
                    }
                }
                Console.WriteLine(dropletsizes[i].Slug);
            }

            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i].Available == true)
                {
                    var x = new DisplayInfo();
                    x.name = regions[i].Name;
                    x.slug = regions[i].Slug;
                    regionsslug.Add(x);
                }
            }

            regionsslug.Sort(new Comparison<DisplayInfo>((x, y) => string.Compare(x.name, y.name)));

            for (int i = 0; i < regionsslug.Count; i++)
            {
                RadioButton rdbtn = new RadioButton(this);
                rdbtn.Text = regionsslug[i].name;
                rdbtn.Id = View.GenerateViewId();
                regionsslug[i].radioid = rdbtn.Id;
                rgr.AddView(rdbtn);
            }

            RadioButton checkedrgp = FindViewById<RadioButton>(rgp.CheckedRadioButtonId);
            RadioButton checkedrgr = FindViewById<RadioButton>(rgr.CheckedRadioButtonId);

            rgp.CheckedChange += (o, e) =>
            {
                checkedrgp = FindViewById<RadioButton>(rgp.CheckedRadioButtonId);
                for (int i = 0; i < currentdrops.Count; i++)
                {
                    if(currentdrops[i].radioid == checkedrgp.Id)
                    {
                        createdrop.SizeSlug = currentdrops[i].slug;
                        break;
                    }
                }
            };

            rgr.CheckedChange += (o, e) =>
            {
                checkedrgr = FindViewById<RadioButton>(rgr.CheckedRadioButtonId);
                for (int i = 0; i < regionsslug.Count; i++)
                {
                    if (regionsslug[i].radioid == checkedrgr.Id)
                    {
                        createdrop.RegionSlug = regionsslug[i].slug;
                        break;
                    }
                }
            };

            Button next = FindViewById<Button>(Resource.Id.NextS2);
            Button back = FindViewById<Button>(Resource.Id.BackS2);

            back.Click += (o, e) => 
            {
                var intent = new Intent(this, typeof(Step1Activity));
                StartActivity(intent);
            };

            next.Click += (o, e) =>
            {
                if (checkedrgp == null)
                {
                    Toast.MakeText(this, "Please select plan", ToastLength.Short).Show();
                    return;
                }
                if (checkedrgr == null)
                {
                    Toast.MakeText(this, "Please select region", ToastLength.Short).Show();
                    return;
                }

                var intent = new Intent(this, typeof(Step3Activity));
                intent.PutExtra("DropletName", createdrop.Name);
                intent.PutExtra("DropletDistro", Intent.Extras.GetString("DropletDistro"));
                intent.PutExtra("DropletRegion", createdrop.RegionSlug);
                intent.PutExtra("DropletSize", createdrop.SizeSlug);
                StartActivity(intent);

            };

        }

    }
}