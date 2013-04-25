﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Xml.Linq;
using Composite.C1Console.Security;
using Composite.Core.IO;
using Composite.Core.Logging;
using Composite.Core.Xml;


namespace Composite.Core.WebClient.HttpModules
{
    /// <summary>
    ///  Http Module that ensures that only authenticated users can access /Composite/* files 
    ///  not explicitly allowed to everyone.
    /// </summary>
    internal class AdministrativeAuthorizationHttpModule : IHttpModule
    {
        private static List<string> _allAllowedPaths = new List<string>();
        private static string _adminRootPath;
        private static string _loginPagePath;
        private static object _lock = new object();
        private static bool _protect = true;

        private const string webauthorizationRelativeConfigPath = "~/Composite/webauthorization.config";
        private const string loginPagePathAttributeName = "loginPagePath";
        private const string allowElementName = "allow";
        private const string allow_pathAttributeName = "path";

        static AdministrativeAuthorizationHttpModule()
        {
            if (C1Directory.Exists(HostingEnvironment.MapPath(UrlUtils.AdminRootPath)))
            {
                LoadConfiguration();
            }
            else
            {
                _protect = false;
            }
        }

        private void context_AuthenticateRequest(object sender, EventArgs e)
        {
            if (!_protect)
            {
                return;
            }

            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;

            string currentPath = context.Request.Path.ToLowerInvariant();

            if (currentPath.StartsWith(_adminRootPath) == true && currentPath.Length > _adminRootPath.Length)
            {
                if (UserValidationFacade.IsLoggedIn() == false)
                {
                    if (_allAllowedPaths.Any(p => currentPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)) == false)
                    {
                        LoggingService.LogWarning("Authorization", string.Format("DENIED {0} access to {1}", context.Request.UserHostAddress, currentPath));
                        string redirectUrl = string.Format("{0}?ReturnUrl={1}", _loginPagePath, HttpUtility.UrlEncodeUnicode(context.Request.Url.PathAndQuery));
                        context.Response.Redirect(redirectUrl, true);
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += new EventHandler(context_AuthenticateRequest);
        }

        private static void LoadConfiguration()
        {
            lock (_lock)
            {
                _adminRootPath = UrlUtils.AdminRootPath.ToLowerInvariant();

                if (_adminRootPath.EndsWith("/") == false)
                    _adminRootPath = string.Format("{0}/", _adminRootPath);

                _allAllowedPaths.Clear();

                string webauthorizationConfigPath = HostingEnvironment.MapPath(webauthorizationRelativeConfigPath);

                Verify.That(C1File.Exists(webauthorizationConfigPath), "Missing file '{0}'.", webauthorizationRelativeConfigPath);


                XDocument webauthorizationConfigDocument = XDocumentUtils.Load(webauthorizationConfigPath);

                XAttribute loginPagePathAttribute = Verify.ResultNotNull<XAttribute>(webauthorizationConfigDocument.Root.Attribute("loginPagePath"), "Missing '{0}' attribute on '{1}' root element", loginPagePathAttributeName, webauthorizationRelativeConfigPath);
                string relativeLoginPagePath = Verify.StringNotIsNullOrWhiteSpace(loginPagePathAttribute.Value, "Unexpected empty '{0}' attribute on '{1}' root element", loginPagePathAttributeName, webauthorizationRelativeConfigPath);

                _loginPagePath = UrlUtils.ResolveAdminUrl(relativeLoginPagePath);

                foreach (XElement allowElement in webauthorizationConfigDocument.Root.Elements(allowElementName))
                {
                    XAttribute relativePathAttribute = Verify.ResultNotNull<XAttribute>(allowElement.Attribute(allow_pathAttributeName), "Missing '{0}' attribute on '{1}' element in '{2}'.", allow_pathAttributeName, allowElement, webauthorizationRelativeConfigPath);
                    string relativePath = Verify.StringNotIsNullOrWhiteSpace(relativePathAttribute.Value, "Empty '{0}' attribute on '{1}' element in '{2}'.", allow_pathAttributeName, allowElement, webauthorizationRelativeConfigPath);

                    string fullPath = UrlUtils.ResolveAdminUrl(relativePath).ToLowerInvariant();
                    _allAllowedPaths.Add(fullPath);
                }
            }
        }
    }
}