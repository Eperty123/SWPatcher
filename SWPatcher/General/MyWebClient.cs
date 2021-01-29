﻿/*
 * This file is part of Soulworker Patcher.
 * Copyright (C) 2016-2017 Miyu, Dramiel Leayal
 * 
 * Soulworker Patcher is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Soulworker Patcher is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Soulworker Patcher. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace SWPatcher.General
{
    /// <summary>
    /// A <c>System.Net.WebClient</c> that has a <c>System.Net.CookieCollection</c> and a <c>System.Security.Cryptography.X509Certificates.X509Certificate</c>
    /// </summary>
    internal class MyWebClient : WebClient
    {
        private readonly CookieContainer _container = new CookieContainer();
        internal CookieCollection ResponseCookies { get; private set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.ClientCertificates.Add(new X509Certificate());
            request.CookieContainer = _container;
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            HttpWebResponse response = (HttpWebResponse)base.GetWebResponse(request);
            if (ResponseCookies != null)
                ResponseCookies.Add(response.Cookies);
            else
                ResponseCookies = response.Cookies;

            return response;
        }
    }
}
