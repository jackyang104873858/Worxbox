﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CapTech.Modules.Worxbox.Foundation.Filter;
using CapTech.Modules.Worxbox.Foundation.Models;
using CapTech.Modules.Worxbox.Foundation.Repositories;
using Sitecore;

using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Globalization;

using Sitecore.Resources;
using Sitecore.Shell.Data;
using Sitecore.Shell.Feeds;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.CommandBuilders;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;
using Sitecore.Web.UI.XmlControls;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;
using windows = Sitecore.Shell.Framework.Windows;

namespace CapTech.Modules.Worxbox.Feature.Client.Workbox
{
    public class WorxboxForm : BaseForm
    {
        /// <summary>Gets or sets the offset(what page we are on).</summary>
        /// <value>The size of the offset.</value>
        private OffsetCollection Offset = new OffsetCollection();


        /// <summary>The pager.</summary>
        protected Border Pager;

        /// <summary>The ribbon panel.</summary>
        protected Border RibbonPanel;

        /// <summary>The states.</summary>
        protected Border States;

        /// <summary>The view menu.</summary>
        protected Toolmenubutton ViewMenu;

        /// <summary>The _state names.</summary>
        private NameValueCollection stateNames;

        private IItemFilterer _filterer;

        /// <summary>Gets or sets the size of the page.</summary>
        /// <value>The size of the page.</value>
        public int PageSize
        {
            get { return Registry.GetInt("/Current_User/Workbox/Page Size", 10); }
            set { Registry.SetInt("/Current_User/Workbox/Page Size", value); }
        }

        /// <summary>
        /// Gets a value indicating whether page is reloads by reload button on the ribbon.
        /// </summary>
        /// <value><c>true</c> if this instance is reload; otherwise, <c>false</c>.</value>
        protected virtual bool IsReload
        {
            get { return new UrlString(WebUtil.GetRawUrl())["reload"] == "1"; }
        }

        public WorxboxForm()
        {
            _filterer = new WorxBoxItemFilterer();
        }

        public void CompositeComment(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            if (!args.IsPostBack)
            {
                Context.ClientPage.ClientResponse.Input("Enter a comment:", string.Empty);
                args.WaitForPostBack();
            }
            else if (args.Result.Length > 2000)
            {
                Context.ClientPage.ClientResponse.ShowError(new Exception(string.Format("The comment is too long.\n\nYou have entered {0} characters.\nA comment cannot contain more than 2000 characters.", (object)args.Result.Length)));
                Context.ClientPage.ClientResponse.Input("Enter a comment:", string.Empty);
                args.WaitForPostBack();
            }
            else
            {
                if (args.Result == null || !(args.Result != "null") || !(args.Result != "undefined"))
                    return;
                IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
                if (workflowProvider == null)
                    return;
                IWorkflow workflow = workflowProvider.GetWorkflow(Context.ClientPage.ServerProperties["workflowid"] as string);
                if (workflow == null)
                    return;
                Item obj = Context.ContentDatabase.Items[(Context.ClientPage.ServerProperties["id"] ?? (object)string.Empty).ToString(), Language.Parse(Context.ClientPage.ServerProperties["language"] as string), Sitecore.Data.Version.Parse(Context.ClientPage.ServerProperties["version"] as string)];
                if (obj == null)
                    return;
                string stateId = obj.State.GetWorkflowState().StateID;
                try
                {
                    var workflowFromPage = obj.Database.WorkflowProvider.GetWorkflow(obj);
                    var repository = new WorxboxItemsRepository(workflowFromPage);
                    var compositeStates = repository.GetWorxboxWorkflowStates(workflowFromPage);
                    var workflowState = obj.Database.WorkflowProvider.GetWorkflow(obj).GetState(obj);

                    var compositeItems =
                        repository.GetWorxboxItems(
                            compositeStates.First(x => x.WorkflowState.StateID.Equals(workflowState.StateID)), obj);
                    foreach (var compositeItem in compositeItems)
                    {
                        workflow.Execute(Context.ClientPage.ServerProperties["command"] as string, compositeItem, args.Result, true);
                        this.Refresh(((IEnumerable<WorkflowState>)workflowFromPage.GetStates()).ToDictionary<WorkflowState, string, string>((Func<WorkflowState, string>)(state => state.StateID), (Func<WorkflowState, string>)(state => this.Offset[state.StateID].ToString())));
                       
                    }

                    workflow.Execute(Context.ClientPage.ServerProperties["command"] as string, obj, args.Result, true);
                }
                catch (WorkflowStateMissingException ex)
                {
                    SheerResponse.Alert("One or more items could not be processed because their workflow state does not specify the next step.");
                }
                UrlString urlString = new UrlString(WebUtil.GetRawUrl());
                urlString["reload"] = "1";
                int itemCount = workflow.GetItemCount(stateId);
                if (this.PageSize > 0 && itemCount % this.PageSize == 0)
                    this.Offset[stateId] = itemCount / this.PageSize <= 1 ? 0 : this.Offset[stateId] - 1;
                foreach (WorkflowState state in workflow.GetStates())
                    urlString.Add(state.StateID, this.Offset[state.StateID].ToString());
                Context.ClientPage.ClientResponse.SetLocation(urlString.ToString());
            }
        }

        public void Comment(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            if (!args.IsPostBack)
            {
                Context.ClientPage.ClientResponse.Input("Enter a comment:", string.Empty);
                args.WaitForPostBack();
            }
            else if (args.Result.Length > 2000)
            {
                Context.ClientPage.ClientResponse.ShowError(new Exception(string.Format("The comment is too long.\n\nYou have entered {0} characters.\nA comment cannot contain more than 2000 characters.", (object)args.Result.Length)));
                Context.ClientPage.ClientResponse.Input("Enter a comment:", string.Empty);
                args.WaitForPostBack();
            }
            else
            {
                if (args.Result == null || !(args.Result != "null") || !(args.Result != "undefined"))
                    return;
                IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
                if (workflowProvider == null)
                    return;
                IWorkflow workflow = workflowProvider.GetWorkflow(Context.ClientPage.ServerProperties["workflowid"] as string);
                if (workflow == null)
                    return;
                Item obj = Context.ContentDatabase.Items[(Context.ClientPage.ServerProperties["id"] ?? (object)string.Empty).ToString(), Language.Parse(Context.ClientPage.ServerProperties["language"] as string), Sitecore.Data.Version.Parse(Context.ClientPage.ServerProperties["version"] as string)];
                if (obj == null)
                    return;
                string stateId = obj.State.GetWorkflowState().StateID;
                try
                {
                    workflow.Execute(Context.ClientPage.ServerProperties["command"] as string, obj, args.Result, true);
                }
                catch (WorkflowStateMissingException ex)
                {
                    SheerResponse.Alert("One or more items could not be processed because their workflow state does not specify the next step.");
                }
                UrlString urlString = new UrlString(WebUtil.GetRawUrl());
                urlString["reload"] = "1";
                int itemCount = workflow.GetItemCount(stateId);
                if (this.PageSize > 0 && itemCount % this.PageSize == 0)
                    this.Offset[stateId] = itemCount / this.PageSize <= 1 ? 0 : this.Offset[stateId] - 1;
                foreach (WorkflowState state in workflow.GetStates())
                    urlString.Add(state.StateID, this.Offset[state.StateID].ToString());
                Context.ClientPage.ClientResponse.SetLocation(urlString.ToString());
            }
        }


      

        private IWorkflow GetWorkflowFromPage()
        {
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return (IWorkflow)null;
            return workflowProvider.GetWorkflow(Context.ClientPage.ServerProperties["workflowid"] as string);
        }


        /// <summary>Handles the message.</summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            switch (message.Name)
            {
                case "worxbox:send":
                    this.SendComposite(message);
                    break;
                case "worxbox:sendselected":
                    this.SendCompositeSelected(message);
                    break;
                case "worxbox:sendall":
                    this.SendCompositeAll(message);
                    break;
                case "workflow:send":
                    this.Send(message);
                    return;
                case "workflow:sendselected":
                    this.SendSelected(message);
                    return;
                case "workflow:sendall":
                    this.SendAll(message);
                    return;
                case "window:close":
                    windows.Close();
                    return;
                case "workflow:showhistory":
                    ShowHistory(message, Context.ClientPage.ClientRequest.Control);
                    return;
                case "workbox:hide":
                    Context.ClientPage.SendMessage((object)this, "pane:hide(id=" + message["id"] + ")");
                    Context.ClientPage.ClientResponse.SetAttribute("Check_Check_" + message["id"], "checked", "false");
                    break;
                case "pane:hidden":
                    Context.ClientPage.ClientResponse.SetAttribute("Check_Check_" + message["paneid"], "checked",
                        "false");
                    break;
                case "workbox:show":
                    Context.ClientPage.SendMessage((object)this, "pane:show(id=" + message["id"] + ")");
                    Context.ClientPage.ClientResponse.SetAttribute("Check_Check_" + message["id"], "checked", "true");
                    break;
                case "pane:showed":
                    Context.ClientPage.ClientResponse.SetAttribute("Check_Check_" + message["paneid"], "checked", "true");
                    break;
            }

            base.HandleMessage(message);
            string index = message["id"];
            if (string.IsNullOrEmpty(index))
                return;
            string string1 = StringUtil.GetString(new string[1]
            {
                message["language"]
            });
            string string2 = StringUtil.GetString(new string[1]
            {
                message["version"]
            });
            Item obj =
                Context.ContentDatabase.Items[
                    index, Language.Parse(string1), global::Sitecore.Data.Version.Parse(string2)];
            if (obj == null)
                return;
            Dispatcher.Dispatch(message, obj);
        }

        /// <summary>Diffs the specified id.</summary>
        /// <param name="id">The id.</param>
        /// <param name="language">The language.</param>
        /// <param name="version">The version.</param>
        protected void Diff(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, "id");
            Assert.ArgumentNotNull((object)language, "language");
            Assert.ArgumentNotNull((object)version, "version");
            UrlString urlString = new UrlString(UIUtil.GetUri("control:Diff"));
            urlString.Append("id", id);
            urlString.Append("la", language);
            urlString.Append("vs", version);
            urlString.Append("wb", "1");
            Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString());
        }
        

        /// <summary>Displays the state.</summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="state">The state.</param>
        /// <param name="items">The items.</param>
        /// <param name="control">The control.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="pageSize">Size of the page.</param>
        protected virtual void DisplayCompositeState(IWorkflow workflow, WorxboxWorkflowState state, DataUri[] items,
            System.Web.UI.Control control, int offset, int pageSize)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)state, "state");
            Assert.ArgumentNotNull((object)items, "items");
            Assert.ArgumentNotNull((object)control, "control");
            if (items.Length <= 0)
                return;
            int num = offset + pageSize;
            if (num > items.Length)
                num = items.Length;
            for (int index = offset; index < num; ++index)
            {
                Item obj = Context.ContentDatabase.Items[items[index]];
                if (obj != null)
                {
                    CreateWorxBoxItem(workflow, obj, control, state);
                }
            }

            Border border1 = new Border();
            border1.Background = "#fff";
            Border border2 = border1;
            control.Controls.Add((System.Web.UI.Control)border2);
            border2.Margin = "0 5px 10px 15px";
            border2.Padding = "5px 10px";
            border2.Class = "scWorkboxToolbarButtons";

            var visibleCommands =
                WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(state.WorkflowState.StateID));
            //// Filter out those that can't be fired by the user
            var commands = from command in visibleCommands
                           join cmd in state.WorkflowCommands
                               on command.CommandID equals cmd.CommandID
                           select command;

            foreach (var filterVisibleCommand in commands)
            {
                XmlControl xmlControl1 = Resource.GetWebControl("WorkboxCommand") as XmlControl;
                Assert.IsNotNull((object)xmlControl1, "workboxCommand is null");
                xmlControl1["Header"] = (object)(filterVisibleCommand.DisplayName + " " + Translate.Text("(selected)"));
                xmlControl1["Icon"] = (object)filterVisibleCommand.Icon;
                xmlControl1["Command"] =
                    (object)
                        ("worxbox:sendselected(command=" + filterVisibleCommand.CommandID + ",ws=" +
                         state.WorkflowState.StateID + ",wf=" + workflow.WorkflowID + ")");
                border2.Controls.Add((System.Web.UI.Control)xmlControl1);
                XmlControl xmlControl2 = Resource.GetWebControl("WorkboxCommand") as XmlControl;
                Assert.IsNotNull((object)xmlControl2, "workboxCommand is null");
                xmlControl2["Header"] = (object)(filterVisibleCommand.DisplayName + " " + Translate.Text("(all)"));
                xmlControl2["Icon"] = (object)filterVisibleCommand.Icon;
                xmlControl2["Command"] =
                    (object)
                        ("worxbox:sendall(command=" + filterVisibleCommand.CommandID + ",ws=" +
                         state.WorkflowState.StateID + ",wf=" + workflow.WorkflowID + ")");
                border2.Controls.Add((System.Web.UI.Control)xmlControl2);
            }
        }

        /// <summary>Displays the state.</summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="state">The state.</param>
        /// <param name="items">The items.</param>
        /// <param name="control">The control.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="pageSize">Size of the page.</param>
        protected virtual void DisplayState(IWorkflow workflow, WorkflowState state, DataUri[] items,
            System.Web.UI.Control control, int offset, int pageSize)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)state, "state");
            Assert.ArgumentNotNull((object)items, "items");
            Assert.ArgumentNotNull((object)control, "control");
            if (items.Length <= 0)
                return;
            int num = offset + pageSize;
            if (num > items.Length)
                num = items.Length;
            for (int index = offset; index < num; ++index)
            {
                Item obj = Context.ContentDatabase.Items[items[index]];
                if (obj != null)
                    this.CreateItem(workflow, obj, control);
            }
            Border border1 = new Border();
            border1.Background = "#fff";
            Border border2 = border1;
            control.Controls.Add((System.Web.UI.Control)border2);
            border2.Margin = "0 5px 10px 15px";
            border2.Padding = "5px 10px";
            border2.Class = "scWorkboxToolbarButtons";

            foreach (
                WorkflowCommand filterVisibleCommand in
                    WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(state.StateID)))
            {
                XmlControl xmlControl1 = Resource.GetWebControl("WorkboxCommand") as XmlControl;
                Assert.IsNotNull((object)xmlControl1, "workboxCommand is null");
                xmlControl1["Header"] = (object)(filterVisibleCommand.DisplayName + " " + Translate.Text("(selected)"));
                xmlControl1["Icon"] = (object)filterVisibleCommand.Icon;
                xmlControl1["Command"] =
                    (object)
                        ("workflow:sendselected(command=" + filterVisibleCommand.CommandID + ",ws=" + state.StateID +
                         ",wf=" + workflow.WorkflowID + ")");
                border2.Controls.Add((System.Web.UI.Control)xmlControl1);
                XmlControl xmlControl2 = Resource.GetWebControl("WorkboxCommand") as XmlControl;
                Assert.IsNotNull((object)xmlControl2, "workboxCommand is null");
                xmlControl2["Header"] = (object)(filterVisibleCommand.DisplayName + " " + Translate.Text("(all)"));
                xmlControl2["Icon"] = (object)filterVisibleCommand.Icon;
                xmlControl2["Command"] =
                    (object)
                        ("workflow:sendall(command=" + filterVisibleCommand.CommandID + ",ws=" + state.StateID + ",wf=" +
                         workflow.WorkflowID + ")");
                border2.Controls.Add((System.Web.UI.Control)xmlControl2);
            }
        }

        /// <summary>Displays the states.</summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="placeholder">The placeholder.</param>
        protected virtual void DisplayStates(IWorkflow workflow, XmlControl placeholder)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)placeholder, "placeholder");
            this.stateNames = (NameValueCollection)null;

            var workflowStates = workflow.GetStates();
            var repository = new WorxboxItemsRepository(workflow);
            // TODO:  Refactor to repository method
            var compositeStates = repository.GetWorxboxWorkflowStates(workflow);

           
            foreach (var state in compositeStates)
            {
                DataUri[] items = this.GetWorxboxItems(state.WorkflowState, workflow);

                Assert.IsNotNull((object)items, "items is null");
                string str1 = ShortID.Encode(workflow.WorkflowID) + "_" + ShortID.Encode(state.WorkflowState.StateID);
                var section1 = new global::Sitecore.Web.UI.HtmlControls.Section();
                section1.ID = str1 + "_compositesection";
                var section2 = section1;
                placeholder.AddControl((System.Web.UI.Control)section2);
                int length = items.Length;
                string str2 = string.Format("<span style=\"font-weight:normal\"> - ({0})</span>",
                    length > 0
                        ? (length != 1
                            ? (object)string.Format("{0} {1}", (object)length, (object)Translate.Text("items"))
                            : (object)string.Format("1 {0}", (object)Translate.Text("item")))
                        : (object)Translate.Text("None"));
                var worxBoxIcon = "/~/icon/worxbox/48x48/worxbox.png";
                section2.Header = $"<span><img src='{worxBoxIcon}'  style='vertical-align:middle; padding-right: 10px;'/></span></span>Worxbox {state.WorkflowState.DisplayName} {str2}</span>";
                if (Settings.ClientFeeds.Enabled)
                {
                    FeedUrlOptions feedUrlOptions = new FeedUrlOptions("/sitecore/shell/~/feed/workflowstate.aspx")
                    {
                        UseUrlAuthentication = true
                    };
                    feedUrlOptions.Parameters["wf"] = workflow.WorkflowID;
                    feedUrlOptions.Parameters["st"] = state.WorkflowState.StateID;
                    section2.FeedLink = feedUrlOptions.ToString();
                }

                section2.Collapsed = length <= 0;
                Border border = new Border();
                section2.Controls.Add((System.Web.UI.Control)border);
                border.ID = str1 + "_worxboxcontent";
                this.DisplayCompositeState(workflow, state, items, (System.Web.UI.Control)border,
                    this.Offset[state.WorkflowState.StateID], this.PageSize);
                this.CreateNavigator(section2, str1 + "_worxboxnavigator", length,
                    this.Offset[state.WorkflowState.StateID]);
            }

            foreach (var state in workflowStates)
            {
                if (WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(state.StateID)).Length > 0)
                {
                    DataUri[] items = this.GetItems(state, workflow);
                    Assert.IsNotNull((object)items, "items is null");
                    string str1 = ShortID.Encode(workflow.WorkflowID) + "_" + ShortID.Encode(state.StateID);
                    var section1 = new global::Sitecore.Web.UI.HtmlControls.Section();
                    section1.ID = str1 + "_section";
                    var section2 = section1;
                    placeholder.AddControl((System.Web.UI.Control)section2);
                    int length = items.Length;
                    string str2 = string.Format("<span style=\"font-weight:normal\"> - ({0})</span>",
                        length > 0
                            ? (length != 1
                                ? (object)string.Format("{0} {1}", (object)length, (object)Translate.Text("items"))
                                : (object)string.Format("1 {0}", (object)Translate.Text("item")))
                            : (object)Translate.Text("None"));
                    section2.Header = state.DisplayName + str2;
                    section2.Icon = state.Icon;
                    if (Settings.ClientFeeds.Enabled)
                    {
                        FeedUrlOptions feedUrlOptions = new FeedUrlOptions("/sitecore/shell/~/feed/workflowstate.aspx")
                        {
                            UseUrlAuthentication = true
                        };
                        feedUrlOptions.Parameters["wf"] = workflow.WorkflowID;
                        feedUrlOptions.Parameters["st"] = state.StateID;
                        section2.FeedLink = feedUrlOptions.ToString();
                    }

                    section2.Collapsed = length <= 0;
                    Border border = new Border();
                    section2.Controls.Add((System.Web.UI.Control)border);
                    border.ID = str1 + "_content";
                    this.DisplayState(workflow, state, items, (System.Web.UI.Control)border, this.Offset[state.StateID],
                        this.PageSize);
                    this.CreateNavigator(section2, str1 + "_navigator", length, this.Offset[state.StateID]);
                }
            }
        }

        /// <summary>Displays the workflow.</summary>
        /// <param name="workflow">The workflow.</param>
        protected virtual void DisplayWorkflow(IWorkflow workflow)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Context.ClientPage.ServerProperties["WorkflowID"] = (object)workflow.WorkflowID;
            XmlControl xmlControl = Resource.GetWebControl("Pane") as XmlControl;
            Error.AssertXmlControl(xmlControl, "Pane");
            this.States.Controls.Add((System.Web.UI.Control)xmlControl);
            Assert.IsNotNull((object)xmlControl, "pane");
            xmlControl["PaneID"] = (object)this.GetPaneID(workflow);
            xmlControl["Header"] = (object)workflow.Appearance.DisplayName;
            xmlControl["Icon"] = (object)workflow.Appearance.Icon;
            FeedUrlOptions feedUrlOptions = new FeedUrlOptions("/sitecore/shell/~/feed/workflow.aspx")
            {
                UseUrlAuthentication = true
            };
            feedUrlOptions.Parameters["wf"] = workflow.WorkflowID;
            xmlControl["FeedLink"] = (object)feedUrlOptions.ToString();
            this.DisplayStates(workflow, xmlControl);
            if (!Context.ClientPage.IsEvent)
                return;
            SheerResponse.Insert(this.States.ClientID, "append",
                HtmlUtil.RenderControl((System.Web.UI.Control)xmlControl));
        }

        /// <summary>Raises the load event.</summary>
        /// <param name="e">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        /// request for the page it is associated with, such as setting up a database query. At this
        /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
        /// property to determine whether the page is being loaded in response to a client postback,
        /// or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull((object)e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
                if (workflowProvider != null)
                {
                    IWorkflow[] workflows = workflowProvider.GetWorkflows();
                    foreach (IWorkflow workflow in workflows)
                    {
                        string str = "P" + Regex.Replace(workflow.WorkflowID, "\\W", string.Empty);
                        if (!this.IsReload && workflows.Length == 1 &&
                            string.IsNullOrEmpty(Registry.GetString("/Current_User/Panes/" + str)))
                            Registry.SetString("/Current_User/Panes/" + str, "visible");
                        if ((Registry.GetString("/Current_User/Panes/" + str) ?? string.Empty) == "visible")
                            this.DisplayWorkflow(workflow);
                    }
                }
                this.UpdateRibbon();
            }
            this.WireUpNavigators((System.Web.UI.Control)Context.ClientPage);
        }

        /// <summary>Called when the view menu is clicked.</summary>
        protected void OnViewMenuClick()
        {
            Menu menu = new Menu();
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider != null)
            {
                foreach (IWorkflow workflow in workflowProvider.GetWorkflows())
                {
                    string paneId = this.GetPaneID(workflow);
                    string @string = Registry.GetString("/Current_User/Panes/" + paneId);
                    string str = @string != "hidden" ? "workbox:hide" : "workbox:show";
                    menu.Add(global::Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("ctl"),
                        workflow.Appearance.DisplayName,
                        workflow.Appearance.Icon,
                        string.Empty,
                        str + "(id=" + paneId + ")",
                        @string != "hidden",
                        string.Empty,
                        MenuItemType.Check);
                }

                if (menu.Controls.Count > 0)
                    menu.AddDivider();
                menu.Add("Refresh", "Office/16x16/refresh.png", "Refresh");
            }

            Context.ClientPage.ClientResponse.ShowPopup("ViewMenu", "below", (System.Web.UI.Control)menu);
        }

        /// <summary>Opens the specified item.</summary>
        /// <param name="id">The id.</param>
        /// <param name="language">The language.</param>
        /// <param name="version">The version.</param>
        protected void Open(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, "id");
            Assert.ArgumentNotNull((object)language, "language");
            Assert.ArgumentNotNull((object)version, "version");
            string sectionId = RootSections.GetSectionID(id);
            UrlString urlString = new UrlString();
            urlString.Append("ro", sectionId);
            urlString.Append("fo", id);
            urlString.Append("id", id);
            urlString.Append("la", language);
            urlString.Append("vs", version);
            windows.RunApplication("Content editor", urlString.ToString());
        }

        /// <summary>Called with the pages size changes.</summary>
        protected void PageSize_Change()
        {
            this.PageSize = MainUtil.GetInt(Context.ClientPage.ClientRequest.Form["PageSize"], 10);
            this.Refresh();
        }

        /// <summary>Toggles the pane.</summary>
        /// <param name="id">The id.</param>
        protected void Pane_Toggle(string id)
        {
            Assert.ArgumentNotNull((object)id, "id");
            string id1 = "P" + Regex.Replace(id, "\\W", string.Empty);
            string @string = Registry.GetString("/Current_User/Panes/" + id1);
            if (Context.ClientPage.FindControl(id1) == null)
            {
                IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
                if (workflowProvider == null)
                    return;
                this.DisplayWorkflow(workflowProvider.GetWorkflow(id));
            }
            if (string.IsNullOrEmpty(@string) || @string == "hidden")
            {
                Registry.SetString("/Current_User/Panes/" + id1, "visible");
                Context.ClientPage.ClientResponse.SetStyle(id1, "display", string.Empty);
            }
            else
            {
                Registry.SetString("/Current_User/Panes/" + id1, "hidden");
                Context.ClientPage.ClientResponse.SetStyle(id1, "display", "none");
            }
            SheerResponse.SetReturnValue(true);
        }

        /// <summary>Previews the specified item.</summary>
        /// <param name="id">The id.</param>
        /// <param name="language">The language.</param>
        /// <param name="version">The version.</param>
        protected void Preview(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, "id");
            Assert.ArgumentNotNull((object)language, "language");
            Assert.ArgumentNotNull((object)version, "version");
            Context.ClientPage.SendMessage((object)this,
                "item:preview(id=" + id + ",language=" + language + ",version=" + version + ")");
        }

        /// <summary>Refreshes the page.</summary>
        protected void Refresh()
        {
            this.Refresh((Dictionary<string, string>)null);
        }

        /// <summary>Refreshes the page.</summary>
        /// <param name="urlArguments">The URL arguments.</param>
        protected void Refresh(Dictionary<string, string> urlArguments)
        {
            UrlString urlString = new UrlString(WebUtil.GetRawUrl());
            urlString["reload"] = "1";
            if (urlArguments != null)
            {
                foreach (KeyValuePair<string, string> urlArgument in urlArguments)
                    urlString[urlArgument.Key] = urlArgument.Value;
            }
            Context.ClientPage.ClientResponse.SetLocation(WebUtil.GetFullUrl(urlString.ToString()));
        }

        /// <summary>Shows the history.</summary>
        /// <param name="message">The message.</param>
        /// <param name="control">The control.</param>
        private static void ShowHistory(Message message, string control)
        {
            Assert.ArgumentNotNull((object)message, "message");
            Assert.ArgumentNotNull((object)control, "control");
            XmlControl xmlControl = Resource.GetWebControl("WorkboxHistory") as XmlControl;
            Assert.IsNotNull((object)xmlControl, "history is null");
            xmlControl["ItemID"] = (object)message["id"];
            xmlControl["Language"] = (object)message["la"];
            xmlControl["Version"] = (object)message["vs"];
            xmlControl["WorkflowID"] = (object)message["wf"];
            Context.ClientPage.ClientResponse.ShowPopup(control, "below", (System.Web.UI.Control)xmlControl);
        }

        /// <summary>Creates the command.</summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="command">The command.</param>
        /// <param name="item">The item.</param>
        /// <param name="workboxItem">The workbox item.</param>
        private void CreateCommand(IWorkflow workflow, WorkflowCommand command, Item item, XmlControl workboxItem, string message="workflow:send")
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)command, "command");
            Assert.ArgumentNotNull((object)item, "item");
            Assert.ArgumentNotNull((object)workboxItem, "workboxItem");
            XmlControl xmlControl = Resource.GetWebControl("WorkboxCommand") as XmlControl;
            Assert.IsNotNull((object)xmlControl, "workboxCommand is null");
            xmlControl["Header"] = (object)command.DisplayName;
            xmlControl["Icon"] = (object)command.Icon;
            CommandBuilder commandBuilder = new CommandBuilder(message);
            commandBuilder.Add("id", item.ID.ToString());
            commandBuilder.Add("la", item.Language.Name);
            commandBuilder.Add("vs", item.Version.ToString());
            commandBuilder.Add("command", command.CommandID);
            commandBuilder.Add("wf", workflow.WorkflowID);
            commandBuilder.Add("ui", command.HasUI);
            commandBuilder.Add("suppresscomment", command.SuppressComment);
            xmlControl["Command"] = (object)commandBuilder.ToString();
            workboxItem.AddControl((System.Web.UI.Control)xmlControl);
        }

        private void CreateWorxBoxItem(IWorkflow workflow, Item item, System.Web.UI.Control control, WorxboxWorkflowState state)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)item, "item");
            Assert.ArgumentNotNull((object)control, "control");
            XmlControl webControl = Resource.GetWebControl("WorkboxItem") as XmlControl;
            Assert.IsNotNull((object)webControl, "workboxItem is null");
            control.Controls.Add((System.Web.UI.Control)webControl);
            StringBuilder stringBuilder = new StringBuilder(" - (");
            Language language = item.Language;
            stringBuilder.Append(language.CultureInfo.DisplayName);
            stringBuilder.Append(", ");
            stringBuilder.Append(Translate.Text("version"));
            stringBuilder.Append(' ');
            stringBuilder.Append(item.Version.ToString());
            stringBuilder.Append(")");
            Assert.IsNotNull((object)webControl, "workboxItem");
            webControl["Header"] = (object)item.DisplayName;
            webControl["Details"] = (object)stringBuilder.ToString();

            var childItems = new Literal();
            var builder = new StringBuilder();
            builder.AppendLine("<div style='margin-left:80px;padding-top:25px;padding-bottom:25px;'>");
            webControl.Controls.Add(childItems);

            var repository = new WorxboxItemsRepository(workflow);
            var otherItems = repository.GetWorxboxItems(state, item);
            foreach (var oItem in otherItems)
            {
                var iconImage = ThemeManager.GetIconImage(oItem, 24, 24, "", "", "0 10px", oItem.ID.ToString(), false);
                builder.AppendLine(
                    $"<div><span>{iconImage}</span><span>{oItem.DisplayName} Version: {oItem.Version.Number} Language: {oItem.Language.Name}</span></div>");
            }

            builder.AppendLine("</div>");

            childItems.Text = builder.ToString();

            webControl["Icon"] = (object)item.Appearance.Icon;
            webControl["ShortDescription"] = (object)item.Help.ToolTip;
            webControl["History"] = (object)this.GetHistory(workflow, item);
            webControl["HistoryMoreID"] = (object)Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("ctl");
            webControl["HistoryClick"] = (object)("workflow:showhistory(id=" + (object)item.ID + ",la=" + item.Language.Name + ",vs=" + (object)item.Version + ",wf=" + workflow.WorkflowID + ")");
            webControl["PreviewClick"] = (object)("Preview(\"" + (object)item.ID + "\", \"" + (object)item.Language + "\", \"" + (object)item.Version + "\")");
            webControl["Click"] = (object)("Open(\"" + (object)item.ID + "\", \"" + (object)item.Language + "\", \"" + (object)item.Version + "\")");
            webControl["DiffClick"] = (object)("Diff(\"" + (object)item.ID + "\", \"" + (object)item.Language + "\", \"" + (object)item.Version + "\")");
            webControl["Display"] = (object)"none";
            string uniqueId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(string.Empty);
            webControl["CheckID"] = (object)("check_" + uniqueId);
            webControl["HiddenID"] = (object)("hidden_" + uniqueId);
            webControl["CheckValue"] = (object)(item.ID.ToString() + "," + (object)item.Language + "," + (object)item.Version);
            // Tweak #1:  For an item that is a "Composite", only show the comamnds that are compositable.
            var filteredCommands = WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(item));
            var commands = from command in filteredCommands
                           join cmd in state.WorkflowCommands
                               on command.CommandID equals cmd.CommandID
                           select command;

            foreach (WorkflowCommand filterVisibleCommand in commands)
            {
                this.CreateCommand(workflow, filterVisibleCommand, item, webControl, message: "worxbox:send");
            }
        }


        private void CreateItem(IWorkflow workflow, Item item, System.Web.UI.Control control)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)item, "item");
            Assert.ArgumentNotNull((object)control, "control");
            XmlControl webControl = Resource.GetWebControl("WorkboxItem") as XmlControl;
            Assert.IsNotNull((object)webControl, "workboxItem is null");
            control.Controls.Add((System.Web.UI.Control)webControl);
            StringBuilder stringBuilder = new StringBuilder(" - (");
            Language language = item.Language;
            stringBuilder.Append(language.CultureInfo.DisplayName);
            stringBuilder.Append(", ");
            stringBuilder.Append(Translate.Text("version"));
            stringBuilder.Append(' ');
            stringBuilder.Append(item.Version.ToString());
            stringBuilder.Append(")");
            Assert.IsNotNull((object)webControl, "workboxItem");
            webControl["Header"] = (object)item.DisplayName;
            webControl["Details"] = (object)stringBuilder.ToString();
            webControl["Icon"] = (object)item.Appearance.Icon;
            webControl["ShortDescription"] = (object)item.Help.ToolTip;
            webControl["History"] = (object)this.GetHistory(workflow, item);
            webControl["HistoryMoreID"] = (object)Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("ctl");
            webControl["HistoryClick"] = (object)("workflow:showhistory(id=" + (object)item.ID + ",la=" + item.Language.Name + ",vs=" + (object)item.Version + ",wf=" + workflow.WorkflowID + ")");
            webControl["PreviewClick"] = (object)("Preview(\"" + (object)item.ID + "\", \"" + (object)item.Language + "\", \"" + (object)item.Version + "\")");
            webControl["Click"] = (object)("Open(\"" + (object)item.ID + "\", \"" + (object)item.Language + "\", \"" + (object)item.Version + "\")");
            webControl["DiffClick"] = (object)("Diff(\"" + (object)item.ID + "\", \"" + (object)item.Language + "\", \"" + (object)item.Version + "\")");
            webControl["Display"] = (object)"none";
            string uniqueId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(string.Empty);
            webControl["CheckID"] = (object)("check_" + uniqueId);
            webControl["HiddenID"] = (object)("hidden_" + uniqueId);
            webControl["CheckValue"] = (object)(item.ID.ToString() + "," + (object)item.Language + "," + (object)item.Version);
            foreach (WorkflowCommand filterVisibleCommand in WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(item)))
                this.CreateCommand(workflow, filterVisibleCommand, item, webControl);
        }
    

        /// <summary>Creates the navigator.</summary>
        /// <param name="section">The section.</param>
        /// <param name="id">The id.</param>
        /// <param name="count">The count.</param>
        /// <param name="offset">The offset.</param>
        private void CreateNavigator(global::Sitecore.Web.UI.HtmlControls.Section section, string id, int count,
            int offset)
        {
            Assert.ArgumentNotNull((object)section, "section");
            Assert.ArgumentNotNull((object)id, "id");
            Navigator navigator = new Navigator();
            section.Controls.Add((System.Web.UI.Control)navigator);
            navigator.ID = id;
            navigator.Offset = offset;
            navigator.Count = count;
            navigator.PageSize = this.PageSize;
        }

        private string GetHistory(IWorkflow workflow, Item item)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)item, "item");
            WorkflowEvent[] history = workflow.GetHistory(item);
            string str;
            if (history.Length > 0)
            {
                WorkflowEvent workflowEvent = history[history.Length - 1];
                string text = workflowEvent.User;
                string name = Context.Domain.Name;
                if (text.StartsWith(name + "\\", StringComparison.OrdinalIgnoreCase))
                    text = StringUtil.Mid(text, name.Length + 1);
                str = string.Format(Translate.Text("{0} changed from <b>{1}</b> to <b>{2}</b> on {3}."), (object)StringUtil.GetString(new string[2]
                {
          text,
          Translate.Text("Unknown")
                }), (object)this.GetStateName(workflow, workflowEvent.OldState), (object)this.GetStateName(workflow, workflowEvent.NewState), (object)DateUtil.FormatDateTime(workflowEvent.Date, "D", Context.User.Profile.Culture));
            }
            else
                str = Translate.Text("No changes have been made.");
            return str;
        }


        /// <summary>Gets the items.</summary>
        /// <param name="state">The state.</param>
        /// <param name="workflow">The workflow.</param>
        /// <returns>Array of item DataUri.</returns>
        private DataUri[] GetItems(WorkflowState state, IWorkflow workflow)
        {
            Assert.ArgumentNotNull((object)state, "state");
            Assert.ArgumentNotNull((object)workflow, "workflow");
            ArrayList arrayList = new ArrayList();

            DataUri[] items = workflow.GetItems(state.StateID);

            if (items != null)
            {
                foreach (DataUri index in items)
                {
                    Item obj = Context.ContentDatabase.Items[index];
                    if (obj != null && obj.Access.CanRead() &&
                        (obj.Access.CanReadLanguage() && obj.Access.CanWriteLanguage()) &&
                        (Context.IsAdministrator || obj.Locking.CanLock() || obj.Locking.HasLock()))
                        arrayList.Add((object)index);
                }
            }
            var result = _filterer.FilterItems(arrayList.ToArray(typeof(DataUri)) as DataUri[]);
            return result.ToArray();
        }

        private DataUri[] GetWorxboxItems(WorkflowState state, IWorkflow workflow)
        {
            var  repository = new WorxboxItemsRepository(workflow);
            var items = GetItems(state, workflow);
            var result = items.Where(x => repository.IsWorxboxItem(state, x));
            return _filterer.FilterItems(result).ToArray();
        }

        /// <summary>Gets the pane ID.</summary>
        /// <param name="workflow">The workflow.</param>
        /// <returns>The get pane id.</returns>
        private string GetPaneID(IWorkflow workflow)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            return "P" + Regex.Replace(workflow.WorkflowID, "\\W", string.Empty);
        }

        /// <summary>Gets the name of the state.</summary>
        /// <param name="workflow">The workflow.</param>
        /// <param name="stateID">The state ID.</param>
        /// <returns>The get state name.</returns>
        private string GetStateName(IWorkflow workflow, string stateID)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)stateID, "stateID");
            if (this.stateNames == null)
            {
                this.stateNames = new NameValueCollection();
                foreach (WorkflowState state in workflow.GetStates())
                    this.stateNames.Add(state.StateID, state.DisplayName);
            }
            return StringUtil.GetString(new string[2] { this.stateNames[stateID], "?" });
        }

        /// <summary>Jumps the specified sender.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="message">The message.</param>
        /// <param name="offset">The offset.</param>
        private void Jump(object sender, Message message, int offset)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull((object)message, "message");
            string control = Context.ClientPage.ClientRequest.Control;
            string workflowID = ShortID.Decode(control.Substring(0, 32));
            string stateID = ShortID.Decode(control.Substring(33, 32));
            string str = control.Substring(0, 65);
            this.Offset[stateID] = offset;
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            Assert.IsNotNull((object)workflowProvider,
                "Workflow provider for database \"" + Context.ContentDatabase.Name + "\" not found.");
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            Error.Assert(workflow != null, "Workflow \"" + workflowID + "\" not found.");
            Assert.IsNotNull((object)workflow, "workflow");
            WorkflowState state = workflow.GetState(stateID);
            Assert.IsNotNull((object)state, "Workflow state \"" + stateID + "\" not found.");
            Border border1 = new Border();
            border1.ID = str + "_content";
            Border border2 = border1;
            DataUri[] items = this.GetItems(state, workflow);
            this.DisplayState(workflow, state, items ?? new DataUri[0], (System.Web.UI.Control)border2, offset,
                this.PageSize);
            Context.ClientPage.ClientResponse.SetOuterHtml(str + "_content", (System.Web.UI.Control)border2);
        }

        private void SendComposite(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;
            string workflowID = message["wf"];
            if (workflowProvider.GetWorkflow(workflowID) == null ||
                Context.ContentDatabase.Items[
                    message["id"], Language.Parse(message["la"]), global::Sitecore.Data.Version.Parse(message["vs"])] ==
                null)
                return;
            Context.ClientPage.ServerProperties["id"] = (object)message["id"];
            Context.ClientPage.ServerProperties["language"] = (object)message["la"];
            Context.ClientPage.ServerProperties["version"] = (object)message["vs"];
            Context.ClientPage.ServerProperties["command"] = (object)message["command"];
            Context.ClientPage.ServerProperties["workflowid"] = (object)workflowID;
            Context.ClientPage.Start((object)this, "CompositeComment", new NameValueCollection()
            {
                {
                    "ui",
                    message["ui"]
                },
                {
                    "suppresscomment",
                    message["suppresscomment"]
                }
            });
        }

        /// <summary>Sends the specified message.</summary>
        /// <param name="message">The message.</param>
        private void Send(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;
            string workflowID = message["wf"];
            if (workflowProvider.GetWorkflow(workflowID) == null ||
                Context.ContentDatabase.Items[
                    message["id"], Language.Parse(message["la"]), global::Sitecore.Data.Version.Parse(message["vs"])] ==
                null)
                return;
            Context.ClientPage.ServerProperties["id"] = (object)message["id"];
            Context.ClientPage.ServerProperties["language"] = (object)message["la"];
            Context.ClientPage.ServerProperties["version"] = (object)message["vs"];
            Context.ClientPage.ServerProperties["command"] = (object)message["command"];
            Context.ClientPage.ServerProperties["workflowid"] = (object)workflowID;
            Context.ClientPage.Start((object)this, "Comment", new NameValueCollection()
            {
                {
                    "ui",
                    message["ui"]
                },
                {
                    "suppresscomment",
                    message["suppresscomment"]
                }
            });
        }

        private void SendCompositeAll(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;
            string workflowID = message["wf"];
            string stateID = message["ws"];
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            if (workflow == null)
                return;
            WorkflowState state = workflow.GetState(stateID);
            DataUri[] items = this.GetItems(state, workflow);
            Assert.IsNotNull((object)items, "uris is null");

            var repository = new WorxboxItemsRepository(workflow);
            var compositeStates = repository.GetWorxboxWorkflowStates(workflow);

            var compositeState = compositeStates.First(x => x.WorkflowState.StateID.Equals(state.StateID));
            var itemList = from index in items
                           where repository.IsWorxboxItem(state, index)
                           select index;


            string comments = state != null ? state.DisplayName : string.Empty;
            bool flag = false;
            foreach (DataUri index in items)
            {
                Item obj = Context.ContentDatabase.Items[index];
                if (obj != null)
                {
                    try
                    {
                        var additionalItems =
                            repository.GetWorxboxItems(
                                compositeStates.First(x => x.WorkflowState.StateID.Equals(state.StateID)), obj);
                        foreach (var compositeItem in additionalItems)
                        {
                            workflow.Execute(message["command"] as string, compositeItem, "Composite Submission", true);

                        }

                        workflow.Execute(message["command"] as string, obj, "Composite Submission", true);
                    }
                    catch (WorkflowStateMissingException)
                    {
                        flag = true;
                    }
                }
            }
            if (flag)
                SheerResponse.Alert("One or more items could not be processed because their workflow state does not specify the next step.");
            UrlString urlString = new UrlString(WebUtil.GetRawUrl());
            urlString["reload"] = "1";
            Context.ClientPage.ClientResponse.SetLocation(urlString.ToString());
        }

        /// <summary>Sends all.</summary>
        /// <param name="message">The message.</param>
        private void SendAll(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;
            string workflowID = message["wf"];
            string stateID = message["ws"];
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            if (workflow == null)
                return;
            WorkflowState state = workflow.GetState(stateID);
            DataUri[] items = this.GetItems(state, workflow);
            Assert.IsNotNull((object)items, "uris is null");
            string comments = state != null ? state.DisplayName : string.Empty;
            bool flag = false;
            foreach (DataUri index in items)
            {
                Item obj = Context.ContentDatabase.Items[index];
                if (obj != null)
                {
                    try
                    {
                        workflow.Execute(message["command"], obj, comments, true);
                    }
                    catch (WorkflowStateMissingException ex)
                    {
                        flag = true;
                    }
                }
            }
            if (flag)
                SheerResponse.Alert("One or more items could not be processed because their workflow state does not specify the next step.");
            UrlString urlString = new UrlString(WebUtil.GetRawUrl());
            urlString["reload"] = "1";
            Context.ClientPage.ClientResponse.SetLocation(urlString.ToString());
        }

      private void SendCompositeSelected(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;
            string workflowID = message["wf"];
            string str = message["ws"];
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            if (workflow == null)
                return;
            int num = 0;
            bool flag = false;

            var repository = new WorxboxItemsRepository(workflow);
            var compositeStates = repository.GetWorxboxWorkflowStates(workflow);

            foreach (string key in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (key != null && key.StartsWith("check_", StringComparison.InvariantCulture))
                {
                    string[] strArray = Context.ClientPage.ClientRequest.Form["hidden_" + key.Substring(6)].Split(',');
                    Item obj =
                        Context.ContentDatabase.Items[
                            strArray[0], Language.Parse(strArray[1]), global::Sitecore.Data.Version.Parse(strArray[2])];
                    if (obj != null)
                    {
                        WorkflowState state = workflow.GetState(obj);
                        if (state.StateID == str)
                        {
                            try
                            {
                                var compositeItems =
                                       repository.GetWorxboxItems(
                                           compositeStates.First(x => x.WorkflowState.StateID.Equals(state.StateID)),
                                               obj);
                                foreach (var compositeItem in compositeItems)
                                {
                                    workflow.Execute(message["command"], compositeItem, state.DisplayName, true);
                                }
                                workflow.Execute(message["command"], obj, state.DisplayName, true);
                            }
                            catch (WorkflowStateMissingException)
                            {
                                flag = true;
                            }
                            ++num;
                        }
                    }
                }
            }
            if (flag)
                SheerResponse.Alert(
                    "One or more items could not be processed because their workflow state does not specify the next step.");
            if (num == 0)
                Context.ClientPage.ClientResponse.Alert("There are no selected items.");
            else
                this.Refresh();
        }


        /// <summary>Sends the selected.</summary>
        /// <param name="message">The message.</param>
        private void SendSelected(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;
            string workflowID = message["wf"];
            string str = message["ws"];
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            if (workflow == null)
                return;
            int num = 0;
            bool flag = false;

            var repository = new WorxboxItemsRepository(workflow);
            var compositeStates = repository.GetWorxboxWorkflowStates(workflow);

            foreach (string key in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (key != null && key.StartsWith("check_", StringComparison.InvariantCulture))
                {
                    string[] strArray = Context.ClientPage.ClientRequest.Form["hidden_" + key.Substring(6)].Split(',');
                    Item obj =
                        Context.ContentDatabase.Items[
                            strArray[0], Language.Parse(strArray[1]), global::Sitecore.Data.Version.Parse(strArray[2])];
                    if (obj != null)
                    {
                        WorkflowState state = workflow.GetState(obj);
                        if (state.StateID == str)
                        {
                            try
                            {
                                workflow.Execute(message["command"], obj, state.DisplayName, true);
                            }
                            catch (WorkflowStateMissingException)
                            {
                                flag = true;
                            }
                            ++num;
                        }
                    }
                }
            }
            if (flag)
                SheerResponse.Alert(
                    "One or more items could not be processed because their workflow state does not specify the next step.");
            if (num == 0)
                Context.ClientPage.ClientResponse.Alert("There are no selected items.");
            else
                this.Refresh();
        }

        /// <summary>Updates the ribbon.</summary>
        private void UpdateRibbon()
        {
            Ribbon ribbon1 = new Ribbon();
            ribbon1.ID = "WorkboxRibbon";
            ribbon1.CommandContext = new CommandContext();
            Ribbon ribbon2 = ribbon1;
            Item obj = Context.Database.GetItem("/sitecore/content/Applications/Workbox/Ribbon");
            Error.AssertItemFound(obj, "/sitecore/content/Applications/Workbox/Ribbon");
            ribbon2.CommandContext.RibbonSourceUri = obj.Uri;
            ribbon2.CommandContext.CustomData = (object)this.IsReload;
            this.RibbonPanel.Controls.Add((System.Web.UI.Control)ribbon2);
        }

        /// <summary>Wires the up navigators.</summary>
        /// <param name="control">The control.</param>
        private void WireUpNavigators(System.Web.UI.Control control)
        {
            foreach (System.Web.UI.Control control1 in control.Controls)
            {
                Navigator navigator = control1 as Navigator;
                if (navigator != null)
                {
                    navigator.Jump += new Navigator.NavigatorDelegate(this.Jump);
                    navigator.Previous += new Navigator.NavigatorDelegate(this.Jump);
                    navigator.Next += new Navigator.NavigatorDelegate(this.Jump);
                }
                this.WireUpNavigators(control1);
            }
        }

        protected void SetFilter()
        {
            _filterer.SetFilter();
            Refresh(null);
        }

        protected void ClearFilter()
        {
            _filterer.ClearFilter();
            Refresh(null);
        }

        private class OffsetCollection
        {
            public int this[string key]
            {
                get
                {
                    if (Context.ClientPage.ServerProperties[key] != null)
                        return (int)Context.ClientPage.ServerProperties[key];
                    UrlString urlString = new UrlString(WebUtil.GetRawUrl());
                    int result;
                    if (urlString[key] != null && int.TryParse(urlString[key], out result))
                        return result;
                    return 0;
                }
                set { Context.ClientPage.ServerProperties[key] = (object)value; }
            }
        }
    }
}
