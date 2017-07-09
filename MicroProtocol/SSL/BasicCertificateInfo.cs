using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Ace.Networking.MicroProtocol.SSL
{
    public class BasicCertificateInfo
    {
        public BasicCertificateInfo(X509Certificate certificate)
        {
            Subject = certificate.Subject;
            Issuer = certificate.Issuer;
        }

        public string Subject { get; protected set; }
        public string Issuer { get; protected set; }

        public static string GetAttribute(string str, string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(key))
            {
                return fallback;
            }
            var i = str.IndexOf($"{key}=", StringComparison.Ordinal);
            if (i == -1)
            {
                return fallback;
            }
            i += key.Length + 1;
            var sb = new StringBuilder(str.Length - i);
            while (i < str.Length)
            {
                var c = str[i++];
                if (c == ',')
                {
                    break;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetAttribute(string str, CertificateAttribute key, string fallback = null)
        {
            return GetAttribute(str, GetKey(key), fallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool VerifyAttribute(string str, string key, string target, StringComparison comp)
        {
            return string.Equals(GetAttribute(str, key), target, comp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool VerifyAttribute(string str, string key, string target)
        {
            return VerifyAttribute(str, key, target, StringComparison.OrdinalIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool VerifyAttribute(string str, CertificateAttribute key, string target, StringComparison comp)
        {
            return VerifyAttribute(str, GetKey(key), target, comp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool VerifyAttribute(string str, CertificateAttribute key, string target)
        {
            return VerifyAttribute(str, GetKey(key), target);
        }

        public static string GetKey(CertificateAttribute attribute)
        {
            switch (attribute)
            {
                case CertificateAttribute.CommonName: return "CN";
                case CertificateAttribute.CountryName: return "C";
                case CertificateAttribute.GivenName: return "G";
                case CertificateAttribute.Locality: return "L";
                case CertificateAttribute.Organization: return "O";
                case CertificateAttribute.OrganizationalUnit: return "OU";
                case CertificateAttribute.StateOrProvinceName: return "ST";
                case CertificateAttribute.Surname: return "SN";
                default: return "<UNKNOWN>";
            }
        }
    }
}