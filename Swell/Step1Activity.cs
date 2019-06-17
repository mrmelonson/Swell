using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

/*
 * TODO  
 * MAKE DISTROS VARIABLE
 */


namespace Swell.Main
{
    [Activity(Label = "Step1Activity")]
    public class Step1Activity : Activity
    {
        public class DistroDrops
        {
            public string slug { get; set; }
            public int radioid { get; set; }
            public string name { get; set; }
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Step1);

            var CreatedDroplet = new DigitalOcean.API.Models.Requests.Droplet();

            string[,] slugarr = { { "centos-7-x64", "CentOS 7 x64" },
                                  { "debian-9-x64", "Debian 9 x64" },
                                  { "fedora-30-x64", "Fedora 30 x64"},
                                  { "ubuntu-19-04-x64", "Ubuntu 19.04 x64"} };

            List<DistroDrops> distroDrops = new List<DistroDrops>();

            EditText editText = FindViewById<EditText>(Resource.Id.Dropname);
            RadioGroup rg = FindViewById<RadioGroup>(Resource.Id.radiogroupdistro);

            for (int i = 0; i < slugarr.GetLength(0); i++)
            {
                RadioButton rdbtn = new RadioButton(this);
                rdbtn.Text = slugarr[i, 1];
                rdbtn.Id = View.GenerateViewId();
                rg.AddView(rdbtn);
                DistroDrops x = new DistroDrops();
                x.radioid = rdbtn.Id;
                x.slug = slugarr[i, 0];
                x.name = slugarr[i, 1];
                distroDrops.Add(x);
            }

            RadioButton checkedrdbtn = FindViewById<RadioButton>(rg.CheckedRadioButtonId);

            rg.CheckedChange += (o, e) =>
            {
                checkedrdbtn = FindViewById<RadioButton>(rg.CheckedRadioButtonId);
                for(int i=0;i < distroDrops.Count;i++)
                {
                    if (checkedrdbtn.Text == distroDrops[i].name)
                    {
                        CreatedDroplet.ImageIdOrSlug = distroDrops[i].slug;
                    }
                }
            };

            Button next = FindViewById<Button>(Resource.Id.NextS1);

            next.Click += (o, e) =>
            {
                if (editText.Text == "")
                {
                    Toast.MakeText(this, "Please enter name of droplet", ToastLength.Short).Show();
                    return;
                }
                if (checkedrdbtn == null)
                {
                    Toast.MakeText(this, "Please select distro", ToastLength.Short).Show();
                    return;
                }

                Toast.MakeText(this, "Helloworld", ToastLength.Short).Show();
                var intent = new Intent(this, typeof(Step2Activity));
                intent.PutExtra("dropletName", editText.Text);
                intent.PutExtra("DropletDistro", CreatedDroplet.ImageIdOrSlug.ToString());
                StartActivity(intent);
            };

        }
    }
}