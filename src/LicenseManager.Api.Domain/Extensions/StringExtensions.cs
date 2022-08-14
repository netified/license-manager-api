// Copyright (c) 2022 Netified <contact@netified.io>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LicenseManager.Api.Domain.Extensions
{
    /// <summary>
    /// String Extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Obfuscates the email.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <returns>The obfuscated email</returns>
        public static string ObfuscateEmail(this string email)
        {
            var displayCase = email;

            var partToBeObfuscated = Regex.Match(displayCase, "[^@]*").Value;
            if (partToBeObfuscated.Length - 3 > 0)
            {
                var obfuscation = "";
                for (var i = 0; i < partToBeObfuscated.Length - 3; i++) obfuscation += "*";
                displayCase = string.Format("{0}{1}{2}{3}", displayCase[0], displayCase[1], obfuscation, displayCase[(partToBeObfuscated.Length - 1)..]);
            }
            else if (partToBeObfuscated.Length - 3 == 0)
            {
                displayCase = string.Format("{0}*{1}", displayCase[0], displayCase[2..]);
            }

            return displayCase;
        }

        /// <summary>
        /// Computes the message to MD5 hash.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The computed message</returns>
        public static string ComputeMd5Hash(this string message)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] input = Encoding.ASCII.GetBytes(message);
                byte[] hash = md5.ComputeHash(input);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}