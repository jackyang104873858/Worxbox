﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapTech.Modules.Worxbox.Foundation.Models;
using CapTech.Modules.Worxbox.Foundation.Repositories;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.Rules.RulesEditor;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace CapTech.Modules.Worxbox.Feature.Client.Workbox
{
    public class WorxboxFilterForm : RulesEditorPage
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OK_Click()
        {
            base.OK_Click();
        }

        protected override void Cancel_Click()
        {
            base.Cancel_Click();
        }

        //protected Literal FilterSection;

        //protected override void OnLoad(EventArgs e)
        //{
        //    Assert.ArgumentNotNull((object)e, "e");
        //    base.OnLoad(e);
        //    var repository = new WorxboxSettingsRepository();
        //    var fields = repository.GetFilterFields();
        //    FilterSection.Text = JsonConvert.SerializeObject(fields);
        //}

        //protected override void OnCancel(object sender, EventArgs args)
        //{
        //    Assert.ArgumentNotNull(sender, "sender");
        //    Assert.ArgumentNotNull((object)args, "args");
        //    SheerResponse.CloseWindow();
        //}

        //protected override void OnOK(object sender, EventArgs args)
        //{
        //    Assert.ArgumentNotNull(sender, "sender");
        //    Assert.ArgumentNotNull((object)args, "args");

        //    var filter = new List<WorxboxFieldFilter>
        //    {
        //        new WorxboxFieldFilter
        //        {
        //            Field = new WorxboxFilterField
        //            {
        //                FriendlyName = "Updated By",
        //                FieldName = "__Updated by"
        //            },
        //            Operator = Operator.Equals,
        //            Value = "sitecore\tim"
        //        }
        //    };
        //    Registry.SetString("/Current_User/Workbox/FieldFilter", JsonConvert.SerializeObject(filter));
        //    SheerResponse.CloseWindow();
        //}
    }
}
