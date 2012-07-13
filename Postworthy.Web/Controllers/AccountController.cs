using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Postworthy.Models;
using System.Configuration;
using System.Xml.Serialization;
using System.IO;
using LinqToTwitter;
using Postworthy.Models.Account;
using Postworthy.Models.Twitter;
using Postworthy.Web.Models;

namespace Postworthy.Web.Controllers
{
    public class AccountController : Controller
    {
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
            var user = UsersCollection.PrimaryUser();
            if (user != null)
                ViewBag.Brand = user.SiteName;
        }

        //
        // GET: /Account/LogOn

        public ActionResult LogOn()
        {
            //return View();
            if (!Request.IsAuthenticated)
            {
                var credentials = new InMemoryCredentials();
                credentials.ConsumerKey = ConfigurationManager.AppSettings["TwitterCustomerKey"];
                credentials.ConsumerSecret = ConfigurationManager.AppSettings["TwitterCustomerSecret"];
                var auth = new MvcAuthorizer { Credentials = credentials };
                auth.CompleteAuthorization(Request.Url);
                if (!auth.IsAuthorized)
                    return auth.BeginAuthorization(Request.Url);
                else
                {
                    FormsAuthentication.SetAuthCookie(auth.ScreenName, true);
                    PostworthyUser pm = UsersCollection.Single(auth.ScreenName, addIfNotFound: true);
                    if (string.IsNullOrEmpty(pm.AccessToken) && string.IsNullOrEmpty(pm.OAuthToken))
                    {
                        pm.AccessToken = auth.Credentials.AccessToken;
                        pm.OAuthToken = auth.Credentials.OAuthToken;
                        UsersCollection.Save();
                    }
                    return RedirectToAction("Index", new { controller = "Home", action = "Index", id = UrlParameter.Optional });
                }
            }
            else
                return RedirectToAction("Index", new { controller = "Home", action = "Index", id = UrlParameter.Optional });
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", new { controller = "Home", action = "Index", id = UrlParameter.Optional });
        }

        //
        // GET: /Account/Register

        public ActionResult Register()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("LogOn");
        }

        //
        // POST: /Account/Register

        [AuthorizePrimaryUser]
        public ActionResult Personalization()
        {  
            PostworthyUser model = UsersCollection.Single(User.Identity.Name);
            return View(model);
        }

        [AuthorizePrimaryUser]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Personalization(FormCollection form)
        {

            var prevModel = UsersCollection.Single(User.Identity.Name);

            prevModel.SiteName = form["SiteName"];
            prevModel.About = form["About"];
            prevModel.IncludeFriends = bool.Parse(form["IncludeFriends"].Split(',').First());
            prevModel.OnlyTweetsWithLinks = bool.Parse(form["OnlyTweetsWithLinks"].Split(',').First());
            prevModel.AnalyticsScript = form["AnalyticsScript"];
            prevModel.AdScript = form["AdScript"];
            prevModel.MobileAdScript = form["MobileAdScript"];
            prevModel.RetweetThreshold = int.Parse(!string.IsNullOrEmpty(form["RetweetThreshold"]) ? form["RetweetThreshold"] : "5");

            UsersCollection.Save();

            return View(prevModel);
        }

        [AuthorizePrimaryUser]
        public ActionResult Friends()
        {
            return View(TwitterModel.Instance.Friends(User.Identity.Name));
        }
    }
}
