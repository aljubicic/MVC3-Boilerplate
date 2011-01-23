﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Web.Models;
using System.ComponentModel.DataAnnotations;
using System.Web.Routing;
using Web.Infrastructure.Session;

namespace Web.Controllers
{
    public class SessionController : Controller
    {

        public IFormsAuthenticationService FormsService { get; set; }
        public IMembershipService MembershipService { get; set; }
        public IUserSession UserSession { get; set; }

        SiteDB _db;
        UserActivity _log;

        public SessionController(IFormsAuthenticationService FormsService, IMembershipService MembershipService, IUserSession UserSession)
        {
            _db = new SiteDB();
            _log = new UserActivity(_db);

            this.FormsService = FormsService;
            this.MembershipService = MembershipService;
            this.UserSession = UserSession;
        }
        //protected override void Initialize(RequestContext requestContext)
        //{
        //    _db = new SiteDB();
        //    _log = new UserActivity(_db);

        //    if (FormsService == null) { FormsService = new FormsAuthenticationService(); }
        //    if (MembershipService == null) { MembershipService = new AccountMembershipService(_db); }

        //    base.Initialize(requestContext);
        //}

        public ActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Default Create action is to do a Login.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(ComboSignupLoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (MembershipService.ValidateUser(model.UserLogin.LoginName, model.UserLogin.Password))
                {
                    //get user.
                    User user = UserRepository.GetUser(_db, model.UserLogin.LoginName);
                    if (user != null)
                    {
                        //log that the user logged in.
                        _log.LogIt(user.UserId, "User Logged In");

                        FormsService.SignIn(user.UserId.ToString(), model.UserLogin.RememberMe);
                        return SaveFriendlyInfoAndRedirect(user, returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("", "User info could not be found.");
                        this.FlashValidationSummaryErrors();
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                    this.FlashValidationSummaryErrors();
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignUp(ComboSignupLoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                MembershipCreateStatus createStatus = MembershipService.CreateUser(model.UserNew.UserName, model.UserNew.Password, model.UserNew.Email);

                if (createStatus == MembershipCreateStatus.Success)
                {
                    //get userid.
                    User user = UserRepository.GetUser(_db, model.UserNew.UserName);
                    if (user != null)
                    {
                        //log that user registered.
                        _log.LogIt(user.UserId, "User registered");

                        this.FlashInfo("Thank you for signing up!");

                        FormsService.SignIn(user.UserId.ToString(), false /* createPersistentCookie */);
                        return SaveFriendlyInfoAndRedirect(user, returnUrl);   
                    }
                    else
                    {
                        ModelState.AddModelError("", "User info could not be found.");
                        this.FlashValidationSummaryErrors();
                    }
                }
                else
                {
                    ModelState.AddModelError("", AccountValidation.ErrorCodeToString(createStatus));
                    this.FlashValidationSummaryErrors();
                }
            }

            // If we got this far, something failed, redisplay form
            return View("login", model);
        }

        //Logout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Response.Cookies["friendly"].Value = null;
            _log.LogIt(UserSession.GetCurrentUserId(), "Logged out");
            FormsService.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                if (MembershipService.ChangePassword(UserSession.GetCurrentUserName(), model.OldPassword, model.NewPassword))
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                    this.FlashValidationSummaryErrors();
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }

        protected ActionResult SaveFriendlyInfoAndRedirect(User user, string returnUrl)
        {
            //save a friendly name for view use.
            Response.Cookies["friendly"].Value = user.Username;
            Response.Cookies["friendly"].Expires = DateTime.Now.AddDays(30);
            Response.Cookies["friendly"].HttpOnly = true;

            //redirect to specified url or default.
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

    }

}
