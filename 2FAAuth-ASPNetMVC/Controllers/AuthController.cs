using Google.Authenticator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;

namespace _2FAAuth_ASPNetMVC.Controllers
{
    public class AuthController : Controller
    {
        // GET: Auth
        [HttpGet]
        public ActionResult Login()
        {
            Session["UserName"] = null;
            Session["IsValidTwoFactorAuthentication"] = null;
            return View();
        }
        [HttpPost]
        public ActionResult Login(_2FAAuth_ASPNetMVC.Models.Login login)
        {
            bool status = false;

            if (Session["Username"] == null || Session["IsValidTwoFactorAuthentication"] == null || !(bool)Session["IsValidTwoFactorAuthentication"])
            {
                string googleAuthKey = WebConfigurationManager.AppSettings["GoogleAuthKey"];
                string UserUniqueKey = (login.UserName + googleAuthKey);

                //Takeing UserName And Password As Static here
                if (login.UserName == "admin" && login.Password == "admin123")
                {
                    Session["UserName"] = login.UserName;

                    //Two Factor Authentication Setup
                    TwoFactorAuthenticator TwoFacAuth = new TwoFactorAuthenticator();
                    var setupInfo = TwoFacAuth.GenerateSetupCode("jayanttripathy.com", login.UserName, ConvertSecretToBytes(UserUniqueKey, false), 300);
                    Session["UserUniqueKey"] = UserUniqueKey;
                    ViewBag.BarcodeImageUrl = setupInfo.QrCodeSetupImageUrl;
                    ViewBag.SetupCode = setupInfo.ManualEntryKey;
                    status = true;
                }
            }
            else
            {
                return RedirectToRoute("Index");
            }
            ViewBag.Status = status;
            return View();
        }
        [HttpPost]
        public ActionResult TwoFactorAuthenticate()
        {
            var token = Request["JayantTripathy"];
            TwoFactorAuthenticator TwoFacAuth = new TwoFactorAuthenticator();
            string UserUniqueKey = Session["UserUniqueKey"].ToString();
            bool isValid = TwoFacAuth.ValidateTwoFactorPIN(UserUniqueKey, token, false);
            if (isValid)
            {
                HttpCookie TwoFCookie = new HttpCookie("TwoFCookie");
                string UserCode = Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(UserUniqueKey)));

                Session["IsValidTwoFactorAuthentication"] = true;
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = "Google Two Factor PIN is expired or wrong";
            return RedirectToAction("Login");
        }
        private static byte[] ConvertSecretToBytes(string secret, bool secretIsBase32) =>
           secretIsBase32 ? Base32Encoding.ToBytes(secret) : Encoding.UTF8.GetBytes(secret);
    }
}